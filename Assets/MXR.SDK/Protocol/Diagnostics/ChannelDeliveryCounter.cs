namespace MXR.SDK.Protocol.Diagnostics {
    /// <summary>
    /// Per-channel delivery tallies for the current connection.
    /// Diagnostics only: never sent on the wire, never written to analytics,
    /// and not stripped in release builds.
    /// </summary>
    public readonly struct ChannelDeliveryCounter {
        /// <summary>
        /// Envelopes written on this channel.
        /// </summary>
        public int Sent { get; }

        /// <summary>
        /// Envelopes on this channel whose peer ack arrived.
        /// </summary>
        public int Acked { get; }

        /// <summary>
        /// Requests on this channel that received <c>sys/reply</c>.
        /// </summary>
        public int Replied { get; }

        /// <summary>
        /// Requests on this channel rejected with a permanent <c>sys/err</c>.
        /// </summary>
        public int RejectedPermanent { get; }

        /// <summary>
        /// Requests on this channel rejected with a retryable <c>sys/err</c>.
        /// </summary>
        public int RejectedRetryable { get; }

        /// <summary>
        /// Creates a counter snapshot.
        /// </summary>
        public ChannelDeliveryCounter(
            int sent,
            int acked,
            int replied,
            int rejectedPermanent,
            int rejectedRetryable
        ) {
            Sent = sent;
            Acked = acked;
            Replied = replied;
            RejectedPermanent = rejectedPermanent;
            RejectedRetryable = rejectedRetryable;
        }
    }
}
