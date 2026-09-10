namespace MXR.SDK.Protocol.Framing {
    /// <summary>
    /// Outcome of attempting to decode one length-prefixed frame from a buffer.
    /// </summary>
    public sealed class FrameDecodeResult {
        /// <summary>
        /// Whether a complete frame was decoded.
        /// </summary>
        public bool Success => Error == FrameDecodeError.None;

        /// <summary>
        /// Failure reason when <see cref="Success"/> is false.
        /// </summary>
        public FrameDecodeError Error { get; private set; }

        /// <summary>
        /// Frame body when <see cref="Success"/> is true.
        /// </summary>
        public byte[] Body { get; private set; }

        /// <summary>
        /// Bytes consumed from the input buffer (length prefix, or full frame on success).
        /// </summary>
        public int BytesConsumed { get; private set; }

        /// <summary>
        /// Creates a successful decode result.
        /// </summary>
        public static FrameDecodeResult Decoded(byte[] body, int bytesConsumed) {
            return new FrameDecodeResult {
                Error = FrameDecodeError.None,
                Body = body,
                BytesConsumed = bytesConsumed
            };
        }

        /// <summary>
        /// Creates a failed decode result with a named error.
        /// </summary>
        public static FrameDecodeResult Failed(FrameDecodeError error, int bytesConsumed) {
            return new FrameDecodeResult {
                Error = error,
                Body = null,
                BytesConsumed = bytesConsumed
            };
        }

        FrameDecodeResult() { }
    }
}
