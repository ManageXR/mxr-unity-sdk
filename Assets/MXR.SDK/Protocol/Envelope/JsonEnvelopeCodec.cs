using System;
using System.IO;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MXR.SDK.Protocol {
    /// <summary>
    /// UTF-8 JSON envelope codec. Unknown top-level fields are ignored on decode.
    /// Serialize embeds payload JSON inline via <see cref="JRaw"/> without re-encoding.
    /// Deserialize streams UTF-8 through <see cref="JsonTextReader"/> without materializing the full body as a string.
    /// Returns payload bytes from the embedded JSON value.
    /// Parse depth is capped at <see cref="MaxParseDepth"/>.
    /// </summary>
    public sealed class JsonEnvelopeCodec : IEnvelopeCodec {
        /// <summary>
        /// Negotiated format name for this codec.
        /// </summary>
        public const string FormatName = "json";

        /// <summary>
        /// Maximum JSON nesting depth accepted on decode. Matches Newtonsoft.Json 13 default.
        /// </summary>
        public const int MaxParseDepth = 64;

        static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            Formatting = Formatting.None,
            MaxDepth = MaxParseDepth
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
            return StrictUtf8.GetBytes(json);
        }

        /// <inheritdoc />
        public bool TryDeserialize(byte[] bytes, out Envelope envelope, out string error) {
            envelope = null;
            error = null;

            if (bytes == null || bytes.Length == 0) {
                error = "Envelope body is empty.";
                return false;
            }

            EnvelopeWire wire;
            try {
                wire = DeserializeWire(bytes);
            } catch (DecoderFallbackException ex) {
                error = ex.Message;
                return false;
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

        static EnvelopeWire DeserializeWire(byte[] bytes) {
            using (var stream = new MemoryStream(bytes, writable: false))
            using (var textReader = new StreamReader(stream, StrictUtf8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: false))
            using (var jsonReader = new JsonTextReader(textReader) { MaxDepth = MaxParseDepth }) {
                var serializer = JsonSerializer.Create(SerializerSettings);
                return serializer.Deserialize<EnvelopeWire>(jsonReader);
            }
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
                var payloadJson = StrictUtf8.GetString(envelope.Payload);
                EnsureSingleJsonValue(payloadJson);
                wire.Payload = new JRaw(payloadJson);
            }

            return wire;
        }

        static Envelope FromWire(EnvelopeWire wire) {
            byte[] payload = null;
            if (wire.Payload != null) {
                var raw = wire.Payload.ToString();
                if (!string.IsNullOrEmpty(raw)) {
                    payload = StrictUtf8.GetBytes(raw);
                }
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

        static void EnsureSingleJsonValue(string json) {
            using (var reader = new JsonTextReader(new StringReader(json))) {
                if (!reader.Read()) {
                    throw new JsonReaderException("Payload is empty.");
                }

                JRaw.Create(reader);

                if (reader.Read()) {
                    throw new JsonReaderException("Payload must be a single JSON value.");
                }
            }
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
            public JRaw Payload { get; set; }
        }
    }
}
