namespace MXR.SDK.Protocol {
    /// <summary>
    /// Whether the socket is up. Independent of data readiness.
    /// </summary>
    public enum TransportState {
        /// <summary>
        /// No live socket.
        /// </summary>
        Disconnected,

        /// <summary>
        /// Bootstrap or connect is in progress.
        /// </summary>
        Connecting,

        /// <summary>
        /// The socket is up.
        /// </summary>
        Connected
    }
}
