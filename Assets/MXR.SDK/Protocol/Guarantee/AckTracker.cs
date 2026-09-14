using System;
using System.Collections.Generic;

using MXR.SDK.Protocol;

namespace MXR.SDK.Protocol.Guarantee {
    /// <summary>
    /// Tracks sent envelopes until the peer's <c>sys/ack</c> arrives.
    /// Sender-side half of the delivered guarantee.
    /// </summary>
    public sealed class AckTracker {
        readonly IProtocolClock _clock;
        readonly TimeSpan _defaultDeadline;
        readonly Dictionary<long, DateTime> _registered = new Dictionary<long, DateTime>();
        readonly HashSet<long> _completed = new HashSet<long>();
        readonly Dictionary<long, LateAck> _late = new Dictionary<long, LateAck>();

        /// <summary>
        /// Creates a tracker with the given clock and optional default ack deadline.
        /// </summary>
        public AckTracker(IProtocolClock clock, TimeSpan? defaultDeadline = null) {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _defaultDeadline = defaultDeadline ?? GuaranteeDefaults.AckDeadline;
        }

        /// <summary>
        /// Registers a sent envelope that expects an ack from the peer.
        /// </summary>
        public AckRegisterResult RegisterSent(Envelope envelope, TimeSpan? ackDeadline = null) {
            if (envelope == null) {
                throw new ArgumentNullException(nameof(envelope));
            }

            if (envelope.Type == ProtocolTypes.Ack) {
                return AckRegisterResult.IsAck;
            }

            if (!envelope.Id.HasValue) {
                return AckRegisterResult.NoId;
            }

            var sentId = envelope.Id.Value;
            if (_registered.ContainsKey(sentId)) {
                return AckRegisterResult.DuplicateId;
            }

            ClearId(sentId);

            var deadline = _clock.UtcNow.Add(ackDeadline ?? _defaultDeadline);
            _registered[sentId] = deadline;
            return AckRegisterResult.Registered;
        }

        /// <summary>
        /// Records an ack received from the peer for a prior sent id.
        /// </summary>
        public AckCompleteResult CompleteAckFromReceiver(ReceiverAck ack) {
            if (_completed.Contains(ack.ReplyTo)) {
                return AckCompleteResult.AlreadyCompleted;
            }

            if (!_registered.TryGetValue(ack.ReplyTo, out var deadline)) {
                return AckCompleteResult.OrphanAck;
            }

            _registered.Remove(ack.ReplyTo);
            _completed.Add(ack.ReplyTo);

            if (_clock.UtcNow >= deadline) {
                _late[ack.ReplyTo] = new LateAck(ack.ReplyTo, deadline, _clock.UtcNow);
                return AckCompleteResult.LateAck;
            }

            return AckCompleteResult.Completed;
        }

        /// <summary>
        /// Returns registered sent ids still awaiting an ack before their deadline.
        /// </summary>
        public IReadOnlyList<PendingAck> GetPendingAcks() {
            var now = _clock.UtcNow;
            var pending = new List<PendingAck>();

            foreach (var entry in _registered) {
                if (now < entry.Value) {
                    pending.Add(new PendingAck(entry.Key, entry.Value));
                }
            }

            return pending;
        }

        /// <summary>
        /// Returns registered sent ids whose ack deadline has passed without an ack.
        /// </summary>
        public IReadOnlyList<ExpiredAck> GetExpiredAcks() {
            var now = _clock.UtcNow;
            var expired = new List<ExpiredAck>();

            foreach (var entry in _registered) {
                if (now >= entry.Value) {
                    expired.Add(new ExpiredAck(entry.Key, entry.Value));
                }
            }

            return expired;
        }

        /// <summary>
        /// Returns sent ids that were acked after their deadline.
        /// </summary>
        public IReadOnlyList<LateAck> GetLateAcks() {
            return new List<LateAck>(_late.Values);
        }

        /// <summary>
        /// Clears all registered, completed, and late ack state.
        /// </summary>
        public void Reset() {
            _registered.Clear();
            _completed.Clear();
            _late.Clear();
        }

        void ClearId(long sentId) {
            _completed.Remove(sentId);
            _late.Remove(sentId);
        }
    }
}
