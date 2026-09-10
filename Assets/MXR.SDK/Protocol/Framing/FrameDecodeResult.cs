namespace MXR.SDK.Protocol.Framing {
    /// <summary>
    /// Outcome of attempting to decode one length-prefixed frame from a buffer.
    /// </summary>
    public sealed class FrameDecodeResult {
        /// <summary>
        /// What the reader must do next: consume the frame, wait for more bytes, or close
        /// the socket.
        /// </summary>
        public FrameDecodeStatus Status { get; private set; }

        /// <summary>
        /// Whether a complete frame was decoded.
        /// </summary>
        public bool Success => Status == FrameDecodeStatus.Decoded;

        /// <summary>
        /// Named reason when <see cref="Success"/> is false.
        /// </summary>
        public FrameDecodeError Error { get; private set; }

        /// <summary>
        /// Frame body when <see cref="Success"/> is true, null otherwise.
        /// </summary>
        public byte[] Body { get; private set; }

        /// <summary>
        /// Bytes consumed from the input buffer. Zero unless a frame was decoded: an
        /// incomplete frame is retried from the same position, and a violation ends the
        /// connection rather than advancing past anything.
        /// </summary>
        public int BytesConsumed { get; private set; }

        /// <summary>
        /// Creates a result for one complete frame.
        /// </summary>
        public static FrameDecodeResult Decoded(byte[] body, int bytesConsumed) {
            return new FrameDecodeResult {
                Status = FrameDecodeStatus.Decoded,
                Error = FrameDecodeError.None,
                Body = body,
                BytesConsumed = bytesConsumed
            };
        }

        /// <summary>
        /// Creates a result for a buffer that does not hold a whole frame yet.
        /// </summary>
        public static FrameDecodeResult Incomplete() {
            return new FrameDecodeResult {
                Status = FrameDecodeStatus.Incomplete,
                Error = FrameDecodeError.TruncatedFrame,
                Body = null,
                BytesConsumed = 0
            };
        }

        /// <summary>
        /// Creates a result for a protocol violation. The caller logs the reason and closes
        /// the socket.
        /// </summary>
        public static FrameDecodeResult Violation(FrameDecodeError error) {
            return new FrameDecodeResult {
                Status = FrameDecodeStatus.Violation,
                Error = error,
                Body = null,
                BytesConsumed = 0
            };
        }

        FrameDecodeResult() { }
    }
}
