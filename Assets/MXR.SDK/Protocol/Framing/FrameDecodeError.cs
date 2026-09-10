namespace MXR.SDK.Protocol.Framing {
    /// <summary>
    /// Named failure reason when a frame cannot be decoded from a byte buffer.
    /// </summary>
    public enum FrameDecodeError {
        /// <summary>
        /// Buffer contained a complete valid frame.
        /// </summary>
        None,

        /// <summary>
        /// Fewer than four length-prefix bytes, or body not yet fully received.
        /// </summary>
        TruncatedFrame,

        /// <summary>
        /// Declared body length exceeds the 64 MiB transport cap.
        /// </summary>
        OversizeFrame,

        /// <summary>
        /// Declared body length is zero.
        /// </summary>
        ZeroLengthBody
    }
}
