namespace MXR.SDK.Protocol.Framing {
    /// <summary>
    /// What a reader must do with a <see cref="FrameDecodeResult"/>.
    /// </summary>
    public enum FrameDecodeStatus {
        /// <summary>
        /// A complete frame was decoded. Advance the read position by
        /// <see cref="FrameDecodeResult.BytesConsumed"/> and handle the body.
        /// </summary>
        Decoded,

        /// <summary>
        /// The buffer does not hold a complete frame yet. Not an error: read more bytes
        /// and try again from the same position. EOF here is an error, but only the
        /// transport can see EOF.
        /// </summary>
        Incomplete,

        /// <summary>
        /// The frame violates the protocol and the stream cannot be recovered. Log
        /// <see cref="FrameDecodeResult.Error"/> and close the socket. Never read on:
        /// the declared body of an oversize frame still follows on the wire, so
        /// continuing loses frame alignment for the life of the connection.
        /// </summary>
        Violation
    }
}
