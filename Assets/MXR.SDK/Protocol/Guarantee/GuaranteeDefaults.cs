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
        /// Default reply deadline (used by <c>ReplyTracker</c>).
        /// </summary>
        public static readonly TimeSpan ReplyDeadline = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Number of recently completed sent ids retained in <c>AckTracker</c>.
        /// </summary>
        public const int CompletedRetentionWindow = 4096;
    }
}
