namespace MXR.SDK.Protocol.Subscriptions {
    /// <summary>
    /// Result of registering a channel for snapshot bootstrap on the current connection.
    /// </summary>
    public enum SubscribeResult {
        /// <summary>
        /// The channel is now awaiting its first snapshot. The caller sends <c>state/subscribe</c>.
        /// Also returned when re-subscribing a channel whose previous subscribe failed.
        /// </summary>
        Subscribed,

        /// <summary>
        /// The channel is already awaiting or has received its snapshot on this connection.
        /// The caller does not send another <c>state/subscribe</c>.
        /// </summary>
        AlreadySubscribed
    }

    /// <summary>
    /// Result of recording a subscribe reply snapshot for a channel.
    /// </summary>
    public enum SnapshotReceivedResult {
        /// <summary>
        /// The channel was awaiting its snapshot, or its subscribe had failed, and now has its snapshot.
        /// </summary>
        Received,

        /// <summary>
        /// The snapshot was already recorded for this channel on this connection.
        /// </summary>
        AlreadyReceived,

        /// <summary>
        /// The channel was never subscribed on this connection.
        /// </summary>
        NotSubscribed
    }

    /// <summary>
    /// Result of recording that a channel subscribe failed.
    /// </summary>
    public enum SubscribeFailedResult {
        /// <summary>
        /// The channel was awaiting its snapshot and is now marked failed.
        /// </summary>
        Failed,

        /// <summary>
        /// The snapshot already arrived for this channel; the failure is ignored.
        /// </summary>
        AlreadyReceived,

        /// <summary>
        /// The channel was already marked failed.
        /// </summary>
        AlreadyFailed,

        /// <summary>
        /// The channel was never subscribed on this connection.
        /// </summary>
        NotSubscribed
    }

    /// <summary>
    /// Snapshot bootstrap progress for a channel on the current connection.
    /// </summary>
    public enum SubscriptionState {
        /// <summary>
        /// The channel is subscribed and still waiting for its first snapshot.
        /// </summary>
        AwaitingSnapshot,

        /// <summary>
        /// The first snapshot has arrived for this subscribed channel.
        /// </summary>
        ReceivedSnapshot,

        /// <summary>
        /// The subscribe was rejected or its reply deadline passed without a snapshot.
        /// </summary>
        SubscribeFailed,

        /// <summary>
        /// The channel was never subscribed on this connection.
        /// </summary>
        NoSubscriptionFound
    }
}
