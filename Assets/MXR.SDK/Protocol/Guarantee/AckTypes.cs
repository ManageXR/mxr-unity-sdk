using System;

namespace MXR.SDK.Protocol.Guarantee {
    /// <summary>
    /// Identifies a received <c>sys/ack</c> from the peer.
    /// </summary>
    public readonly struct ReceiverAck {
        /// <summary>
        /// Prior sender-assigned id this ack correlates to.
        /// </summary>
        public long ReplyTo { get; }

        /// <summary>
        /// Creates an ack correlation from the peer.
        /// </summary>
        public ReceiverAck(long replyTo) {
            ReplyTo = replyTo;
        }
    }

    /// <summary>
    /// Result of registering a sent envelope for ack tracking.
    /// </summary>
    public enum AckRegisterResult {
        /// <summary>
        /// Envelope with an id is now pending an ack.
        /// </summary>
        Registered,

        /// <summary>
        /// The same id is already registered and incomplete.
        /// </summary>
        DuplicateId,

        /// <summary>
        /// Envelope has no <c>id</c> to track.
        /// </summary>
        NoId,

        /// <summary>
        /// <c>sys/ack</c> is never registered for ack tracking.
        /// </summary>
        IsAck
    }

    /// <summary>
    /// Result of completing a pending ack.
    /// </summary>
    public enum AckCompleteResult {
        /// <summary>
        /// A pending sent id was acked before its deadline.
        /// </summary>
        Completed,

        /// <summary>
        /// No tracked entry matched <see cref="ReceiverAck.ReplyTo"/>.
        /// </summary>
        OrphanAck,

        /// <summary>
        /// This id was already acked.
        /// </summary>
        AlreadyCompleted,

        /// <summary>
        /// A pending sent id was acked after its deadline.
        /// </summary>
        LateAck
    }

    /// <summary>
    /// One sent id still awaiting an ack before its deadline.
    /// </summary>
    public readonly struct PendingAck {
        /// <summary>
        /// Sender-assigned id awaiting an ack.
        /// </summary>
        public long SentId { get; }

        /// <summary>
        /// Ack deadline (UTC).
        /// </summary>
        public DateTime DeadlineUtc { get; }

        /// <summary>
        /// Creates a pending ack record.
        /// </summary>
        public PendingAck(long sentId, DateTime deadlineUtc) {
            SentId = sentId;
            DeadlineUtc = deadlineUtc;
        }
    }

    /// <summary>
    /// One sent id whose ack deadline has passed without an ack.
    /// </summary>
    public readonly struct ExpiredAck {
        /// <summary>
        /// Sender-assigned id that was not acked in time.
        /// </summary>
        public long SentId { get; }

        /// <summary>
        /// Deadline that was missed (UTC).
        /// </summary>
        public DateTime DeadlineUtc { get; }

        /// <summary>
        /// Creates an expired ack record.
        /// </summary>
        public ExpiredAck(long sentId, DateTime deadlineUtc) {
            SentId = sentId;
            DeadlineUtc = deadlineUtc;
        }
    }

    /// <summary>
    /// One sent id that was acked after its deadline.
    /// </summary>
    public readonly struct LateAck {
        /// <summary>
        /// Sender-assigned id that was acked late.
        /// </summary>
        public long SentId { get; }

        /// <summary>
        /// Deadline that was missed (UTC).
        /// </summary>
        public DateTime DeadlineUtc { get; }

        /// <summary>
        /// When the ack was recorded (UTC).
        /// </summary>
        public DateTime CompletedUtc { get; }

        /// <summary>
        /// Creates a late ack record.
        /// </summary>
        public LateAck(long sentId, DateTime deadlineUtc, DateTime completedUtc) {
            SentId = sentId;
            DeadlineUtc = deadlineUtc;
            CompletedUtc = completedUtc;
        }
    }
}
