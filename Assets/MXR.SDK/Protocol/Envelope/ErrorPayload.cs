namespace MXR.SDK.Protocol {
    /// <summary>
    /// Payload for <see cref="ProtocolTypes.Err"/>.
    /// </summary>
    public sealed class ErrorPayload {
        /// <summary>
        /// Namespaced error code, e.g. <c>wifi/invalidNetwork</c>.
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable detail.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// When false the sender must drop the message. When true the sender may retry later.
        /// </summary>
        public bool Retryable { get; set; }
    }
}
