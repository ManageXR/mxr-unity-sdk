namespace MXR.SDK.Protocol {
    /// <summary>
    /// Closed set of protocol envelope types. Domain messages use <c>domain/verb</c>
    /// routing keys and are not listed here.
    /// </summary>
    public static class ProtocolTypes {
        /// <summary>
        /// First envelope in each direction (see handshake).
        /// </summary>
        public const string Hello = "sys/hello";

        /// <summary>
        /// Transport receipt for one peer id.
        /// </summary>
        public const string Ack = "sys/ack";

        /// <summary>
        /// Single success answer to a request.
        /// </summary>
        public const string Reply = "sys/reply";

        /// <summary>
        /// Single failure answer to a request.
        /// </summary>
        public const string Err = "sys/err";

        /// <summary>
        /// Join a state channel; the subscribe reply is the snapshot.
        /// </summary>
        public const string Subscribe = "state/subscribe";

        /// <summary>
        /// One state change on a subscribed channel.
        /// </summary>
        public const string Push = "state/push";
    }
}
