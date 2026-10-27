using System;

namespace MXR.SDK.Protocol.Guarantee {
    /// <summary>
    /// A received <c>sys/reply</c> or <c>sys/err</c> from the peer.
    /// </summary>
    public readonly struct ReceiverReply {
        /// <summary>
        /// Request id this answer correlates to.
        /// </summary>
        public long ReplyTo { get; }

        /// <summary>
        /// The reply or error envelope from the peer.
        /// </summary>
        public Envelope Envelope { get; }

        /// <summary>
        /// Creates a receiver reply correlation.
        /// </summary>
        public ReceiverReply(long replyTo, Envelope envelope) {
            ReplyTo = replyTo;
            Envelope = envelope;
        }
    }

    /// <summary>
    /// Result of registering a request that expects an answer.
    /// </summary>
    public enum ReplyRegisterResult {
        /// <summary>
        /// Request id is now pending an answer.
        /// </summary>
        Registered,

        /// <summary>
        /// The same request id is already registered and incomplete.
        /// </summary>
        DuplicateId,

        /// <summary>
        /// Envelope has no <c>id</c> to track.
        /// </summary>
        NoId,

        /// <summary>
        /// <c>sys/reply</c>, <c>sys/err</c>, and <c>sys/ack</c> are never registered as requests.
        /// </summary>
        IsNotRequest
    }

    /// <summary>
    /// Result of matching a receiver answer to a pending request.
    /// </summary>
    public enum ReplyCompleteResult {
        /// <summary>
        /// A pending request received its answer before its deadline.
        /// </summary>
        Completed,

        /// <summary>
        /// No tracked entry matched the request id.
        /// </summary>
        OrphanReply,

        /// <summary>
        /// This request already received an answer.
        /// </summary>
        AlreadyCompleted,

        /// <summary>
        /// Answer arrived after the reply deadline expired.
        /// </summary>
        LateReply,

        /// <summary>
        /// Envelope is not <c>sys/reply</c> or <c>sys/err</c>, or correlation is invalid.
        /// </summary>
        InvalidReply
    }

    /// <summary>
    /// One request id still awaiting an answer before its deadline.
    /// </summary>
    public readonly struct PendingReply {
        /// <summary>
        /// Request id awaiting an answer.
        /// </summary>
        public long RequestId { get; }

        /// <summary>
        /// Reply deadline (UTC).
        /// </summary>
        public DateTime DeadlineUtc { get; }

        /// <summary>
        /// Creates a pending reply record.
        /// </summary>
        public PendingReply(long requestId, DateTime deadlineUtc) {
            RequestId = requestId;
            DeadlineUtc = deadlineUtc;
        }
    }

    /// <summary>
    /// One request id whose reply deadline has passed without an answer.
    /// </summary>
    public readonly struct ExpiredReply {
        /// <summary>
        /// Request id that was not answered in time.
        /// </summary>
        public long RequestId { get; }

        /// <summary>
        /// Deadline that was missed (UTC).
        /// </summary>
        public DateTime DeadlineUtc { get; }

        /// <summary>
        /// Creates an expired reply record.
        /// </summary>
        public ExpiredReply(long requestId, DateTime deadlineUtc) {
            RequestId = requestId;
            DeadlineUtc = deadlineUtc;
        }
    }

    /// <summary>
    /// One request id that received an answer after its deadline.
    /// </summary>
    public readonly struct LateCompletedReply {
        /// <summary>
        /// Request id that was answered late.
        /// </summary>
        public long RequestId { get; }

        /// <summary>
        /// Deadline that was missed (UTC).
        /// </summary>
        public DateTime DeadlineUtc { get; }

        /// <summary>
        /// When the answer was recorded (UTC).
        /// </summary>
        public DateTime CompletedUtc { get; }

        /// <summary>
        /// Creates a late completed reply record.
        /// </summary>
        public LateCompletedReply(long requestId, DateTime deadlineUtc, DateTime completedUtc) {
            RequestId = requestId;
            DeadlineUtc = deadlineUtc;
            CompletedUtc = completedUtc;
        }
    }

    /// <summary>
    /// Outcome of attempting to complete one pending request from a receiver answer.
    /// </summary>
    public readonly struct ReplyMatch {
        /// <summary>
        /// How the answer matched pending state.
        /// </summary>
        public ReplyCompleteResult Result { get; }

        /// <summary>
        /// Request id the answer correlates to, when known.
        /// </summary>
        public long RequestId { get; }

        /// <summary>
        /// The answer envelope from the peer, when present.
        /// </summary>
        public Envelope Reply { get; }

        /// <summary>
        /// Whether <see cref="Reply"/> is a <c>sys/err</c> envelope.
        /// </summary>
        public bool IsError => Reply != null && Reply.Type == ProtocolTypes.Err;

        /// <summary>
        /// Creates a reply match outcome.
        /// </summary>
        public ReplyMatch(ReplyCompleteResult result, long requestId, Envelope reply) {
            Result = result;
            RequestId = requestId;
            Reply = reply;
        }
    }
}
