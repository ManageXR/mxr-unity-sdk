using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace MXR.SDK.Protocol.Handshake {
    /// <summary>
    /// Builds client hello envelopes, reads server hello replies, and negotiates protocol version and payload format.
    /// </summary>
    public static class HandshakeNegotiator {
        static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        static readonly JsonSerializerSettings HelloPayloadSerializerSettings = new JsonSerializerSettings {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            MaxDepth = JsonEnvelopeCodec.MaxParseDepth
        };

        /// <summary>
        /// Default client implementation label when none is supplied by the caller.
        /// </summary>
        public const string DefaultClientImpl = "mxr-unity-sdk/2.0.0";

        /// <summary>
        /// Builds the client hello payload for the first envelope on a new connection.
        /// </summary>
        public static HelloPayload CreateClientHello(string impl) {
            if (impl == null) {
                throw new ArgumentNullException(nameof(impl));
            }

            return new HelloPayload {
                Protocol = ProtocolVersion.Current,
                MinProtocol = ProtocolVersion.Current,
                Formats = new[] { JsonEnvelopeCodec.FormatName },
                Impl = impl
            };
        }

        /// <summary>
        /// Wraps a hello payload in a <see cref="ProtocolTypes.Hello"/> envelope.
        /// The hello exchange is always JSON on the wire.
        /// </summary>
        public static Envelope CreateClientHelloEnvelope(HelloPayload payload, long id, int v) {
            if (payload == null) {
                throw new ArgumentNullException(nameof(payload));
            }

            return new Envelope {
                V = v,
                Type = ProtocolTypes.Hello,
                Id = id,
                Payload = SerializeHelloPayload(payload)
            };
        }

        /// <summary>
        /// Reads the server hello from the peer's answer to our hello.
        /// The hello payload is always JSON. Unknown payload fields are ignored.
        /// </summary>
        /// <param name="reply">Envelope the peer sent in answer to our hello.</param>
        /// <param name="helloId">Id of the hello envelope we sent.</param>
        /// <param name="hello">Server hello when this returns true.</param>
        /// <param name="error">Why the answer is not a usable server hello when this returns false.</param>
        public static bool TryReadServerHello(Envelope reply, long helloId, out HelloPayload hello, out string error) {
            hello = null;
            error = null;

            if (reply == null) {
                throw new ArgumentNullException(nameof(reply));
            }

            if (reply.ReplyTo != helloId) {
                error = $"Answer correlates to {FormatReplyTo(reply.ReplyTo)}, not hello id {helloId}.";
                return false;
            }

            if (reply.Type == ProtocolTypes.Err) {
                error = FormatHelloSysErr(reply.Payload);
                return false;
            }

            if (reply.Type != ProtocolTypes.Reply) {
                error = $"Expected {ProtocolTypes.Reply} for hello, got '{reply.Type}'.";
                return false;
            }

            if (reply.Payload == null || reply.Payload.Length == 0) {
                error = "Server hello payload is empty.";
                return false;
            }

            ServerHelloWire wire;
            try {
                wire = DeserializeServerHello(reply.Payload);
            } catch (DecoderFallbackException ex) {
                error = $"Server hello payload is not valid UTF-8: {ex.Message}";
                return false;
            } catch (JsonException ex) {
                error = $"Server hello payload is not valid JSON: {ex.Message}";
                return false;
            }

            if (wire == null) {
                error = "Server hello payload is not a JSON object.";
                return false;
            }

            if (!wire.Protocol.HasValue) {
                error = "Server hello is missing 'protocol'.";
                return false;
            }

            if (!wire.MinProtocol.HasValue) {
                error = "Server hello is missing 'minProtocol'.";
                return false;
            }

            if (wire.Formats == null) {
                error = "Server hello is missing 'formats'.";
                return false;
            }

            if (wire.Protocol.Value < wire.MinProtocol.Value) {
                error = $"Server hello protocol {wire.Protocol.Value} is below minProtocol {wire.MinProtocol.Value}.";
                return false;
            }

            hello = new HelloPayload {
                Protocol = wire.Protocol.Value,
                MinProtocol = wire.MinProtocol.Value,
                Formats = wire.Formats,
                Impl = wire.Impl ?? string.Empty
            };
            return true;
        }

        /// <summary>
        /// Negotiates effective protocol version, payload format, and post-handshake codec.
        /// </summary>
        public static HandshakeResult Negotiate(HelloPayload client, HelloPayload server) {
            if (client == null) {
                throw new ArgumentNullException(nameof(client));
            }

            if (server == null) {
                throw new ArgumentNullException(nameof(server));
            }

            var effectiveProtocol = Math.Min(client.Protocol, server.Protocol);
            if (server.Protocol < server.MinProtocol) {
                return HandshakeResult.CreateIncompatible(
                    IncompatibleReason.AdminAppTooOld,
                    $"Admin App hello protocol {server.Protocol} is below its minProtocol {server.MinProtocol}. Update the Admin App.");
            }

            if (effectiveProtocol < client.MinProtocol) {
                return HandshakeResult.CreateIncompatible(
                    IncompatibleReason.AdminAppTooOld,
                    $"Admin App speaks protocol {server.Protocol}; this SDK requires at least {client.MinProtocol}. Update the Admin App.");
            }

            if (effectiveProtocol < server.MinProtocol) {
                return HandshakeResult.CreateIncompatible(
                    IncompatibleReason.SdkTooOld,
                    $"This SDK speaks protocol {client.Protocol}; the Admin App requires at least {server.MinProtocol}. Update the SDK.");
            }

            if (!TrySelectFormat(client.Formats, server.Formats, out var selectedFormat, out var postHelloCodec)) {
                return HandshakeResult.CreateIncompatible(
                    IncompatibleReason.NoCommonFormat,
                    $"No common payload format. SDK offers [{JoinFormats(client.Formats)}]; Admin App offers [{JoinFormats(server.Formats)}].");
            }

            return HandshakeResult.CreateCompatible(
                effectiveProtocol,
                selectedFormat,
                postHelloCodec);
        }

        static bool TrySelectFormat(
            IReadOnlyList<string> clientFormats,
            IReadOnlyList<string> serverFormats,
            out string selectedFormat,
            out IEnvelopeCodec codec
        ) {
            selectedFormat = null;
            codec = null;

            if (clientFormats == null || serverFormats == null || clientFormats.Count == 0 || serverFormats.Count == 0) {
                return false;
            }

            var serverFormatsSet = new HashSet<string>();
            foreach (var format in serverFormats) {
                if (format != null) {
                    serverFormatsSet.Add(format);
                }
            }

            foreach (var format in clientFormats) {
                if (format == null || !serverFormatsSet.Contains(format)) {
                    continue;
                }

                if (EnvelopeCodecRegistry.TryForFormat(format, out codec)) {
                    selectedFormat = format;
                    return true;
                }
            }

            return false;
        }

        static string FormatHelloSysErr(byte[] payload) {
            const string generic = "Admin App rejected the hello with sys/err.";
            if (payload == null || payload.Length == 0) {
                return generic;
            }

            try {
                var errorPayload = DeserializeErrorPayload(payload);
                if (errorPayload != null && !string.IsNullOrEmpty(errorPayload.Code)) {
                    return $"Admin App rejected the hello with sys/err ({errorPayload.Code}).";
                }
            } catch (DecoderFallbackException) {
            } catch (JsonException) {
            }

            return generic;
        }

        static string JoinFormats(IReadOnlyList<string> formats) {
            return formats == null ? string.Empty : string.Join(", ", formats);
        }

        static string FormatReplyTo(long? replyTo) {
            return replyTo.HasValue ? $"id {replyTo.Value}" : "no id";
        }

        static byte[] SerializeHelloPayload(HelloPayload payload) {
            var json = JsonConvert.SerializeObject(payload, HelloPayloadSerializerSettings);
            return StrictUtf8.GetBytes(json);
        }

        static ServerHelloWire DeserializeServerHello(byte[] payload) {
            return DeserializeJson<ServerHelloWire>(payload);
        }

        static ErrorPayload DeserializeErrorPayload(byte[] payload) {
            return DeserializeJson<ErrorPayload>(payload);
        }

        static T DeserializeJson<T>(byte[] payload) {
            using (var stream = new MemoryStream(payload, writable: false))
            using (var textReader = new StreamReader(stream, StrictUtf8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: false))
            using (var jsonReader = new JsonTextReader(textReader) { MaxDepth = JsonEnvelopeCodec.MaxParseDepth }) {
                var serializer = JsonSerializer.Create(HelloPayloadSerializerSettings);
                return serializer.Deserialize<T>(jsonReader);
            }
        }

        sealed class ServerHelloWire {
            [JsonProperty("protocol")]
            public int? Protocol { get; set; }

            [JsonProperty("minProtocol")]
            public int? MinProtocol { get; set; }

            [JsonProperty("formats")]
            public string[] Formats { get; set; }

            [JsonProperty("impl")]
            public string Impl { get; set; }
        }
    }
}
