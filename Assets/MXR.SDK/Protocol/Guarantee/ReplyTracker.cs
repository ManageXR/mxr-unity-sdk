using System;
using System.Collections.Generic;

namespace MXR.SDK.Protocol.Guarantee {
    /// <summary>
    /// Tracks sent requests until the peer's <c>sys/reply</c> or <c>sys/err</c> arrives.
    /// Sender-side half of the answered guarantee.
    /// </summary>
    public sealed class ReplyTracker {
        readonly IProtocolClock _clock;
        readonly TimeSpan _defaultDeadline;
        readonly Dictionary<long, DateTime> _registered = new Dictionary<long, DateTime>();
        readonly HashSet<long> _completed = new HashSet<long>();
        readonly Dictionary<long, LateCompletedReply> _late = new Dictionary<long, LateCompletedReply>();

        /// <summary>
        /// Creates a tracker with the given clock and optional default reply deadline.
        /// </summary>
        public ReplyTracker(IProtocolClock clock, TimeSpan? defaultDeadline = null) {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _defaultDeadline = defaultDeadline ?? GuaranteeDefaults.ReplyDeadline;
        }

        /// <summary>
        /// Registers a sent request that expects a correlated answer from the peer.
        /// </summary>
        public ReplyRegisterResult RegisterRequest(Envelope envelope, TimeSpan? replyDeadline = null) {
            if (envelope == null) {
                throw new ArgumentNullException(nameof(envelope));
            }

            if (!envelope.Id.HasValue) {
                return ReplyRegisterResult.NoId;
            }

            var requestId = envelope.Id.Value;
            if (_registered.ContainsKey(requestId)) {
                return ReplyRegisterResult.DuplicateId;
            }

            ClearId(requestId);

            var deadline = _clock.UtcNow.Add(replyDeadline ?? _defaultDeadline);
            _registered[requestId] = deadline;
            return ReplyRegisterResult.Registered;
        }

        /// <summary>
        /// Records an answer received from the peer for a prior request id.
        /// </summary>
        public ReplyMatch CompleteReplyFromReceiver(ReceiverReply reply) {
            var envelope = reply.Envelope;
            if (envelope == null) {
                throw new ArgumentNullException(nameof(reply.Envelope));
            }

            if (envelope.Type != ProtocolTypes.Reply && envelope.Type != ProtocolTypes.Err) {
                var requestId = envelope.ReplyTo ?? reply.ReplyTo;
                return new ReplyMatch(ReplyCompleteResult.InvalidReply, requestId, envelope);
            }

            if (!envelope.ReplyTo.HasValue || envelope.ReplyTo.Value != reply.ReplyTo) {
                return new ReplyMatch(ReplyCompleteResult.InvalidReply, reply.ReplyTo, envelope);
            }

            var matchedRequestId = reply.ReplyTo;
            if (_completed.Contains(matchedRequestId)) {
                return new ReplyMatch(ReplyCompleteResult.AlreadyCompleted, matchedRequestId, envelope);
            }

            if (!_registered.TryGetValue(matchedRequestId, out var deadline)) {
                return new ReplyMatch(ReplyCompleteResult.OrphanReply, matchedRequestId, envelope);
            }

            _registered.Remove(matchedRequestId);
            _completed.Add(matchedRequestId);

            if (_clock.UtcNow >= deadline) {
                _late[matchedRequestId] = new LateCompletedReply(
                    matchedRequestId,
                    deadline,
                    _clock.UtcNow
                );
                return new ReplyMatch(ReplyCompleteResult.LateReply, matchedRequestId, envelope);
            }

            return new ReplyMatch(ReplyCompleteResult.Completed, matchedRequestId, envelope);
        }

        /// <summary>
        /// Returns registered request ids still awaiting an answer before their deadline.
        /// </summary>
        public IReadOnlyList<PendingReply> GetPendingReplies() {
            var now = _clock.UtcNow;
            var pending = new List<PendingReply>();

            foreach (var entry in _registered) {
                if (now < entry.Value) {
                    pending.Add(new PendingReply(entry.Key, entry.Value));
                }
            }

            return pending;
        }

        /// <summary>
        /// Returns registered request ids whose reply deadline has passed without an answer.
        /// </summary>
        public IReadOnlyList<ExpiredReply> GetExpiredReplies() {
            var now = _clock.UtcNow;
            var expired = new List<ExpiredReply>();

            foreach (var entry in _registered) {
                if (now >= entry.Value) {
                    expired.Add(new ExpiredReply(entry.Key, entry.Value));
                }
            }

            return expired;
        }

        /// <summary>
        /// Returns request ids that received an answer after their deadline.
        /// </summary>
        public IReadOnlyList<LateCompletedReply> GetLateReplies() {
            return new List<LateCompletedReply>(_late.Values);
        }

        /// <summary>
        /// Clears all registered, completed, and late reply state.
        /// </summary>
        public void Reset() {
            _registered.Clear();
            _completed.Clear();
            _late.Clear();
        }

        void ClearId(long requestId) {
            _completed.Remove(requestId);
            _late.Remove(requestId);
        }
    }
}
