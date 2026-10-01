namespace MXR.SDK.Protocol {
    /// <summary>
    /// Body of one length-prefixed frame. Payload is opaque to transport and framing;
    /// domain or codec layers deserialize it.
    /// </summary>
    public sealed class Envelope {
        /// <summary>
        /// Protocol version in effect (from handshake).
        /// </summary>
        public int V { get; set; }

        /// <summary>
        /// Routing key: <c>domain/verb</c>, <c>sys/*</c>, or <c>state/*</c>.
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Sender-assigned id, monotonic per connection per direction. Absent on <see cref="ProtocolTypes.Ack"/>.
        /// </summary>
        public long? Id { get; set; }

        /// <summary>
        /// Correlates ack, reply, and error envelopes to a prior peer id.
        /// </summary>
        public long? ReplyTo { get; set; }

        /// <summary>
        /// State channel name. Present on subscribe and push only.
        /// </summary>
        public string Channel { get; set; }

        /// <summary>
        /// Message body bytes. Not JSON-specific at this layer.
        /// </summary>
        public byte[] Payload { get; set; }
    }
}
