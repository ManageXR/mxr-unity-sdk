using System;
using System.Collections.Generic;

using MXR.SDK.Protocol;

namespace MXR.SDK.Protocol.Guarantee {
    /// <summary>
    /// Tracks sent envelopes until the peer's <c>sys/ack</c> arrives.
    /// Sender-side half of the delivered guarantee. Thread-safe for transport reader/writer paths.
    /// </summary>
    public sealed class AckTracker {
        readonly object _sync = new object();
        readonly IProtocolClock _clock;
        readonly TimeSpan _defaultDeadline;
        readonly Dictionary<long, DateTime> _registered = new Dictionary<long, DateTime>();
        readonly HashSet<long> _completed = new HashSet<long>();
        readonly Dictionary<long, LateAck> _late = new Dictionary<long, LateAck>();
        long _highestSentId;

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

            lock (_sync) {
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
                if (sentId > _highestSentId) {
                    _highestSentId = sentId;
                }

                PruneCompleted();
                return AckRegisterResult.Registered;
            }
        }

        /// <summary>
        /// Records an ack received from the peer for a prior sent id.
        /// </summary>
        public AckCompleteResult CompleteAckFromReceiver(ReceiverAck ack) {
            lock (_sync) {
                if (_completed.Contains(ack.ReplyTo)) {
                    return AckCompleteResult.AlreadyCompleted;
                }

                if (!_registered.TryGetValue(ack.ReplyTo, out var deadline)) {
                    return AckCompleteResult.OrphanAck;
                }

                _registered.Remove(ack.ReplyTo);
                _completed.Add(ack.ReplyTo);
                PruneCompleted();

                if (_clock.UtcNow >= deadline) {
                    _late[ack.ReplyTo] = new LateAck(ack.ReplyTo, deadline, _clock.UtcNow);
                    return AckCompleteResult.LateAck;
                }

                return AckCompleteResult.Completed;
            }
        }

        /// <summary>
        /// Returns registered sent ids still awaiting an ack before their deadline.
        /// </summary>
        public IReadOnlyList<PendingAck> GetPendingAcks() {
            lock (_sync) {
                var now = _clock.UtcNow;
                var pending = new List<PendingAck>();

                foreach (var entry in _registered) {
                    if (now < entry.Value) {
                        pending.Add(new PendingAck(entry.Key, entry.Value));
                    }
                }

                return pending;
            }
        }

        /// <summary>
        /// Returns registered sent ids whose ack deadline has passed without an ack.
        /// </summary>
        public IReadOnlyList<ExpiredAck> GetExpiredAcks() {
            lock (_sync) {
                var now = _clock.UtcNow;
                var expired = new List<ExpiredAck>();

                foreach (var entry in _registered) {
                    if (now >= entry.Value) {
                        expired.Add(new ExpiredAck(entry.Key, entry.Value));
                    }
                }

                return expired;
            }
        }

        /// <summary>
        /// Returns sent ids that were acked after their deadline.
        /// </summary>
        public IReadOnlyList<LateAck> GetLateAcks() {
            lock (_sync) {
                return new List<LateAck>(_late.Values);
            }
        }

        /// <summary>
        /// Clears all registered, completed, and late ack state.
        /// </summary>
        public void Reset() {
            lock (_sync) {
                _registered.Clear();
                _completed.Clear();
                _late.Clear();
                _highestSentId = 0;
            }
        }

        void ClearId(long sentId) {
            _completed.Remove(sentId);
            _late.Remove(sentId);
        }

        void PruneCompleted() {
            if (_completed.Count == 0) {
                return;
            }

            var threshold = _highestSentId - GuaranteeDefaults.CompletedRetentionWindow;
            if (threshold <= 0) {
                return;
            }

            var toRemove = new List<long>();
            foreach (var sentId in _completed) {
                if (sentId <= threshold) {
                    toRemove.Add(sentId);
                }
            }

            foreach (var sentId in toRemove) {
                _completed.Remove(sentId);
            }
        }
    }
}
