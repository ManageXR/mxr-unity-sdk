using System;

namespace MXR.SDK.Protocol {
    /// <summary>
    /// Known <see cref="IEnvelopeCodec"/> implementations keyed by negotiated format.
    /// The hello exchange always uses <see cref="HelloCodec"/> (<see cref="JsonEnvelopeCodec"/>),
    /// regardless of the format negotiated for post-handshake envelopes.
    /// </summary>
    public static class EnvelopeCodecRegistry {
        static readonly JsonEnvelopeCodec JsonCodec = new JsonEnvelopeCodec();

        /// <summary>
        /// Codec for the hello exchange. Always JSON.
        /// </summary>
        public static IEnvelopeCodec HelloCodec { get; } = JsonCodec;

        /// <summary>
        /// Returns the shared codec for a negotiated post-handshake format.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="format"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="format"/> is not supported.</exception>
        public static IEnvelopeCodec ForFormat(string format) {
            if (format == null) {
                throw new ArgumentNullException(nameof(format));
            }

            if (format == JsonEnvelopeCodec.FormatName) {
                return JsonCodec;
            }

            throw new ArgumentException($"Unsupported envelope format: '{format}'.", nameof(format));
        }
    }
}
