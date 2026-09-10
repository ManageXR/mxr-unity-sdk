using System;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MXR.SDK.Protocol {
    /// <summary>
    /// UTF-8 JSON envelope codec. Unknown top-level fields are ignored on decode.
    /// </summary>
    public sealed class JsonEnvelopeCodec : IEnvelopeCodec {
        /// <summary>
        /// Negotiated format name for this codec.
        /// </summary>
        public const string FormatName = "json";

        static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            Formatting = Formatting.None
        };

        /// <inheritdoc />
        public string Format => FormatName;

        /// <inheritdoc />
        public byte[] Serialize(Envelope envelope) {
            if (envelope == null) {
                throw new ArgumentNullException(nameof(envelope));
            }

            var wire = ToWire(envelope);
            var json = JsonConvert.SerializeObject(wire, SerializerSettings);
            return Encoding.UTF8.GetBytes(json);
        }

        /// <inheritdoc />
        public bool TryDeserialize(byte[] bytes, out Envelope envelope, out string error) {
            envelope = null;
            error = null;

            if (bytes == null || bytes.Length == 0) {
                error = "Envelope body is empty.";
                return false;
            }

            string json;
            try {
                json = Encoding.UTF8.GetString(bytes);
            } catch (Exception ex) {
                error = ex.Message;
                return false;
            }

            EnvelopeWire wire;
            try {
                wire = JsonConvert.DeserializeObject<EnvelopeWire>(json, SerializerSettings);
            } catch (JsonException ex) {
                error = ex.Message;
                return false;
            }

            if (wire == null) {
                error = "Envelope body is not a JSON object.";
                return false;
            }

            envelope = FromWire(wire);
            return true;
        }

        static EnvelopeWire ToWire(Envelope envelope) {
            var wire = new EnvelopeWire {
                V = envelope.V,
                Type = envelope.Type ?? string.Empty,
                Id = envelope.Id,
                ReplyTo = envelope.ReplyTo,
                Channel = envelope.Channel
            };

            if (envelope.Payload != null && envelope.Payload.Length > 0) {
                var payloadJson = Encoding.UTF8.GetString(envelope.Payload);
                wire.Payload = JToken.Parse(payloadJson);
            }

            return wire;
        }

        static Envelope FromWire(EnvelopeWire wire) {
            byte[] payload = null;
            if (wire.Payload != null && wire.Payload.Type != JTokenType.Null) {
                payload = Encoding.UTF8.GetBytes(wire.Payload.ToString(Formatting.None));
            }

            return new Envelope {
                V = wire.V,
                Type = wire.Type ?? string.Empty,
                Id = wire.Id,
                ReplyTo = wire.ReplyTo,
                Channel = wire.Channel,
                Payload = payload
            };
        }

        sealed class EnvelopeWire {
            [JsonProperty("v")]
            public int V { get; set; }

            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("id")]
            public long? Id { get; set; }

            [JsonProperty("replyTo")]
            public long? ReplyTo { get; set; }

            [JsonProperty("channel")]
            public string Channel { get; set; }

            [JsonProperty("payload")]
            public JToken Payload { get; set; }
        }
    }
}
