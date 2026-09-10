using System;

namespace MXR.SDK.Protocol.Framing {
    /// <summary>
    /// Length-prefixed frame codec: 4-byte big-endian unsigned body length + body bytes.
    /// Format-agnostic — no JSON or protobuf knowledge.
    /// </summary>
    public static class FrameCodec {
        /// <summary>
        /// Size of the length prefix in bytes.
        /// </summary>
        public const int LengthPrefixSize = 4;

        /// <summary>
        /// Maximum allowed body length (64 MiB). N counts body only.
        /// </summary>
        public const int MaxBodyLength = 64 * 1024 * 1024;

        /// <summary>
        /// Encodes a frame from a body byte array.
        /// </summary>
        /// <exception cref="ArgumentNullException">body is null.</exception>
        /// <exception cref="ArgumentException">body is empty or exceeds <see cref="MaxBodyLength"/>.</exception>
        public static byte[] Encode(byte[] body) {
            if (body == null) {
                throw new ArgumentNullException(nameof(body));
            }

            if (body.Length == 0) {
                throw new ArgumentException("Frame body length must be greater than zero.", nameof(body));
            }

            if (body.Length > MaxBodyLength) {
                throw new ArgumentException("Frame body exceeds the 64 MiB cap.", nameof(body));
            }

            var frame = new byte[LengthPrefixSize + body.Length];
            WriteBodyLength(frame, 0, body.Length);
            Buffer.BlockCopy(body, 0, frame, LengthPrefixSize, body.Length);
            return frame;
        }

        /// <summary>
        /// Attempts to decode one frame from a buffer slice. Does not throw on malformed input.
        /// </summary>
        public static FrameDecodeResult TryDecode(byte[] buffer, int offset, int count) {
            if (buffer == null) {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0 || count < 0 || offset + count > buffer.Length) {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (count < LengthPrefixSize) {
                return FrameDecodeResult.Failed(FrameDecodeError.TruncatedFrame, 0);
            }

            var bodyLength = ReadBodyLength(buffer, offset);

            if (bodyLength == 0) {
                return FrameDecodeResult.Failed(FrameDecodeError.ZeroLengthBody, LengthPrefixSize);
            }

            if (bodyLength > (uint)MaxBodyLength) {
                return FrameDecodeResult.Failed(FrameDecodeError.OversizeFrame, LengthPrefixSize);
            }

            var frameLength = LengthPrefixSize + (int)bodyLength;
            if (count < frameLength) {
                return FrameDecodeResult.Failed(FrameDecodeError.TruncatedFrame, 0);
            }

            var body = new byte[(int)bodyLength];
            Buffer.BlockCopy(buffer, offset + LengthPrefixSize, body, 0, (int)bodyLength);
            return FrameDecodeResult.Decoded(body, frameLength);
        }

        static void WriteBodyLength(byte[] buffer, int offset, int bodyLength) {
            buffer[offset] = (byte)(bodyLength >> 24);
            buffer[offset + 1] = (byte)(bodyLength >> 16);
            buffer[offset + 2] = (byte)(bodyLength >> 8);
            buffer[offset + 3] = (byte)bodyLength;
        }

        static uint ReadBodyLength(byte[] buffer, int offset) {
            return ((uint)buffer[offset] << 24)
                | ((uint)buffer[offset + 1] << 16)
                | ((uint)buffer[offset + 2] << 8)
                | buffer[offset + 3];
        }
    }
}
