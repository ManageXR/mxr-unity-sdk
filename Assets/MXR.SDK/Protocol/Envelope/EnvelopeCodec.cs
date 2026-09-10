using System;

using MXR.SDK.Protocol.Framing;

namespace MXR.SDK.Protocol {
    /// <summary>
    /// Framing helpers that compose <see cref="IEnvelopeCodec"/> with <see cref="Framing.FrameCodec"/>.
    /// </summary>
    public static class EnvelopeCodec {
        /// <summary>
        /// Encodes one envelope as a length-prefixed frame.
        /// </summary>
        public static byte[] Encode(IEnvelopeCodec codec, Envelope envelope) {
            if (codec == null) {
                throw new ArgumentNullException(nameof(codec));
            }

            return FrameCodec.Encode(codec.Serialize(envelope));
        }

        /// <summary>
        /// Decodes one length-prefixed frame into an envelope using <paramref name="codec"/>.
        /// </summary>
        public static bool TryDecode(
            IEnvelopeCodec codec,
            byte[] buffer,
            int offset,
            int count,
            out Envelope envelope,
            out string error,
            out FrameDecodeResult frameResult
        ) {
            if (codec == null) {
                throw new ArgumentNullException(nameof(codec));
            }

            frameResult = FrameCodec.TryDecode(buffer, offset, count);
            if (!frameResult.Success) {
                envelope = null;
                error = null;
                return false;
            }

            return codec.TryDeserialize(frameResult.Body, out envelope, out error);
        }
    }
}
