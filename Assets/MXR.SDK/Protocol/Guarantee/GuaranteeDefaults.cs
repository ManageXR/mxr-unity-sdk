using System;

namespace MXR.SDK.Protocol.Guarantee {
    /// <summary>
    /// Default deadlines for guarantee-rule tracking.
    /// </summary>
    public static class GuaranteeDefaults {
        /// <summary>
        /// Default ack delivery deadline. Expiry means the connection is broken.
        /// </summary>
        public static readonly TimeSpan AckDeadline = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Default reply deadline (used by <c>ReplyTracker</c> in Ticket 5b).
        /// </summary>
        public static readonly TimeSpan ReplyDeadline = TimeSpan.FromSeconds(10);
    }
}
