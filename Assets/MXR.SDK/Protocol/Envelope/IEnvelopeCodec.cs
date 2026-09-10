using System;

namespace MXR.SDK.Protocol {
    /// <summary>
    /// Serializes and deserializes <see cref="Envelope"/> bodies. Implementations map
    /// negotiated payload formats (JSON today, protobuf later) to the shared envelope model.
    /// </summary>
    public interface IEnvelopeCodec {
        /// <summary>
        /// Negotiated format name, e.g. <c>json</c>.
        /// </summary>
        string Format { get; }

        /// <summary>
        /// Encodes one envelope to its on-wire body bytes.
        /// </summary>
        byte[] Serialize(Envelope envelope);

        /// <summary>
        /// Decodes one envelope body. Returns false and sets <paramref name="error"/> on failure.
        /// </summary>
        bool TryDeserialize(byte[] bytes, out Envelope envelope, out string error);
    }
}
