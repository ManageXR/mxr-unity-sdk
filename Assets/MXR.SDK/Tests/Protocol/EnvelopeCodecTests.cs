using System;
using System.Text;

using MXR.SDK.Protocol;
using MXR.SDK.Protocol.Framing;

using Newtonsoft.Json;

using NUnit.Framework;

namespace MXR.SDK.Protocol.Tests {
    public class EnvelopeCodecTests {
        readonly JsonEnvelopeCodec _json = new JsonEnvelopeCodec();

        [Test]
        public void JsonEnvelopeCodec_Format_IsJson() {
            Assert.AreEqual(JsonEnvelopeCodec.FormatName, _json.Format);
        }

        [Test]
        public void SerializeDeserialize_RoundTripsRequestEnvelope() {
            var payloadJson = "{\"ssid\":\"Lab\",\"password\":\"secret\"}";
            var original = new Envelope {
                V = ProtocolVersion.Current,
                Type = "wifi/connect",
                Id = 41,
                Payload = Encoding.UTF8.GetBytes(payloadJson)
            };

            var serialized = _json.Serialize(original);
            var json = Encoding.UTF8.GetString(serialized);

            Assert.That(json, Does.Contain("\"v\":2"));
            Assert.That(json, Does.Contain("\"type\":\"wifi/connect\""));
            Assert.That(json, Does.Contain("\"id\":41"));
            Assert.That(json, Does.Contain("\"payload\":"));
            Assert.That(json, Does.Contain("\"ssid\":\"Lab\""));

            Assert.IsTrue(_json.TryDeserialize(serialized, out var decoded, out var error));
            Assert.IsNull(error);
            Assert.AreEqual(original.V, decoded.V);
            Assert.AreEqual(original.Type, decoded.Type);
            Assert.AreEqual(original.Id, decoded.Id);
            Assert.IsNull(decoded.ReplyTo);
            Assert.IsNull(decoded.Channel);
            Assert.AreEqual(payloadJson, Encoding.UTF8.GetString(decoded.Payload));
        }

        [Test]
        public void SerializeDeserialize_RoundTripsAckWithoutId() {
            var original = new Envelope {
                V = ProtocolVersion.Current,
                Type = ProtocolTypes.Ack,
                ReplyTo = 41
            };

            var serialized = _json.Serialize(original);
            var json = Encoding.UTF8.GetString(serialized);

            Assert.That(json, Does.Not.Contain("\"id\""));
            Assert.That(json, Does.Contain("\"replyTo\":41"));

            Assert.IsTrue(_json.TryDeserialize(serialized, out var decoded, out _));
            Assert.IsNull(decoded.Id);
            Assert.AreEqual(41, decoded.ReplyTo);
            Assert.IsNull(decoded.Payload);
        }

        [Test]
        public void SerializeDeserialize_RoundTripsHelloPayload() {
            var helloPayload = Encoding.UTF8.GetBytes(
                "{\"protocol\":2,\"minProtocol\":2,\"formats\":[\"json\"],\"impl\":\"mxr-unity-sdk/2.0.0\"}");

            var original = new Envelope {
                V = ProtocolVersion.Current,
                Type = ProtocolTypes.Hello,
                Id = 1,
                Payload = helloPayload
            };

            Assert.IsTrue(EnvelopeCodecRegistry.HelloCodec.TryDeserialize(EnvelopeCodecRegistry.HelloCodec.Serialize(original), out var decoded, out _));
            Assert.AreEqual(ProtocolTypes.Hello, decoded.Type);
            Assert.AreEqual(1, decoded.Id);
            Assert.AreEqual(
                Encoding.UTF8.GetString(helloPayload),
                Encoding.UTF8.GetString(decoded.Payload));
        }

        [Test]
        public void HelloCodec_IsAlwaysJsonEnvelopeCodec() {
            Assert.IsInstanceOf<JsonEnvelopeCodec>(EnvelopeCodecRegistry.HelloCodec);
            Assert.AreSame(EnvelopeCodecRegistry.ForFormat(JsonEnvelopeCodec.FormatName), EnvelopeCodecRegistry.HelloCodec);
        }

        [Test]
        public void ForFormat_UnknownFormat_Throws() {
            var ex = Assert.Throws<ArgumentException>(() => EnvelopeCodecRegistry.ForFormat("proto3"));
            Assert.That(ex.Message, Does.Contain("proto3"));
            Assert.AreEqual("format", ex.ParamName);
        }

        [Test]
        public void ForFormat_Null_Throws() {
            Assert.Throws<ArgumentNullException>(() => EnvelopeCodecRegistry.ForFormat(null));
        }

        [Test]
        public void TryDeserialize_UnknownTopLevelFieldsAreIgnored() {
            var json = "{\"v\":2,\"type\":\"sys/ack\",\"replyTo\":41,\"futureField\":\"ignored\"}";
            var bytes = Encoding.UTF8.GetBytes(json);

            Assert.IsTrue(_json.TryDeserialize(bytes, out var envelope, out var error));

            Assert.IsNull(error);
            Assert.AreEqual(ProtocolTypes.Ack, envelope.Type);
            Assert.AreEqual(41, envelope.ReplyTo);
        }

        [Test]
        public void TryDeserialize_InvalidJson_ReturnsFalse() {
            var bytes = Encoding.UTF8.GetBytes("{not json");

            Assert.IsFalse(_json.TryDeserialize(bytes, out var envelope, out var error));

            Assert.IsNull(envelope);
            Assert.IsNotNull(error);
        }

        [Test]
        public void TryDeserialize_EmptyBytes_ReturnsFalse() {
            Assert.IsFalse(_json.TryDeserialize(new byte[0], out _, out var error));
            Assert.That(error, Does.Contain("empty"));
        }

        [Test]
        public void Encode_TryDecode_RoundTripsWithPostHandshakeCodec() {
            var codec = EnvelopeCodecRegistry.ForFormat("json");
            var original = new Envelope {
                V = ProtocolVersion.Current,
                Type = ProtocolTypes.Subscribe,
                Id = 7,
                Channel = "deviceStatus"
            };

            var frame = EnvelopeCodec.Encode(codec, original);
            var success = EnvelopeCodec.TryDecode(codec, frame, 0, frame.Length, out var decoded, out var error, out var frameResult);

            Assert.IsTrue(success);
            Assert.IsNull(error);
            Assert.IsTrue(frameResult.Success);
            Assert.AreEqual(frame.Length, frameResult.BytesConsumed);
            Assert.AreEqual(original.Type, decoded.Type);
            Assert.AreEqual(original.Id, decoded.Id);
            Assert.AreEqual(original.Channel, decoded.Channel);
        }

        [Test]
        public void TryDecode_FrameError_DoesNotDeserializeEnvelope() {
            // Length prefix declares 2 body bytes but only 1 is present.
            var buffer = new byte[] { 0x00, 0x00, 0x00, 0x02, 0x7B };

            var success = EnvelopeCodec.TryDecode(
                _json,
                buffer,
                0,
                buffer.Length,
                out var envelope,
                out var error,
                out var frameResult);

            Assert.IsFalse(success);
            Assert.IsNull(envelope);
            Assert.IsNull(error);
            Assert.AreEqual(FrameDecodeError.TruncatedFrame, frameResult.Error);
        }

        [Test]
        public void Serialize_NullEnvelope_Throws() {
            Assert.Throws<ArgumentNullException>(() => _json.Serialize(null));
        }

        [Test]
        public void SerializeDeserialize_RoundTripsReplyAndPush() {
            var reply = new Envelope {
                V = ProtocolVersion.Current,
                Type = ProtocolTypes.Reply,
                Id = 88,
                ReplyTo = 41
            };

            Assert.IsTrue(_json.TryDeserialize(_json.Serialize(reply), out var decodedReply, out _));
            Assert.AreEqual(88, decodedReply.Id);
            Assert.AreEqual(41, decodedReply.ReplyTo);

            var push = new Envelope {
                V = ProtocolVersion.Current,
                Type = ProtocolTypes.Push,
                Id = 9,
                Channel = "deviceStatus"
            };

            Assert.IsTrue(_json.TryDeserialize(_json.Serialize(push), out var decodedPush, out _));
            Assert.AreEqual(9, decodedPush.Id);
            Assert.AreEqual("deviceStatus", decodedPush.Channel);
        }

        [Test]
        public void TryDecode_EnvelopeJsonError_ReturnsFrameSuccessAndError() {
            var frame = FrameCodec.Encode(Encoding.UTF8.GetBytes("{"));

            var success = EnvelopeCodec.TryDecode(
                _json,
                frame,
                0,
                frame.Length,
                out var envelope,
                out var error,
                out var frameResult);

            Assert.IsFalse(success);
            Assert.IsNull(envelope);
            Assert.IsNotNull(error);
            Assert.IsTrue(frameResult.Success);
        }

        [Test]
        public void TryDeserialize_NullBytes_ReturnsFalse() {
            Assert.IsFalse(_json.TryDeserialize(null, out _, out var error));
            Assert.That(error, Does.Contain("empty"));
        }

        [TestCase("[1,2,3]")]
        [TestCase("\"hello\"")]
        [TestCase("{\"nested\":{\"ok\":true}}")]
        public void SerializeDeserialize_RoundTripsEmbeddedPayload(string payloadJson) {
            var original = new Envelope {
                V = ProtocolVersion.Current,
                Type = "test/payload",
                Id = 1,
                Payload = Encoding.UTF8.GetBytes(payloadJson)
            };

            Assert.IsTrue(_json.TryDeserialize(_json.Serialize(original), out var decoded, out var error), error);
            Assert.AreEqual(payloadJson, Encoding.UTF8.GetString(decoded.Payload));
        }

        [Test]
        public void TryDecode_NonZeroOffsetAndSequentialFrames_DecodesEnvelopes() {
            var firstEnvelope = new Envelope {
                V = ProtocolVersion.Current,
                Type = ProtocolTypes.Ack,
                ReplyTo = 1
            };
            var secondEnvelope = new Envelope {
                V = ProtocolVersion.Current,
                Type = ProtocolTypes.Ack,
                ReplyTo = 2
            };

            var firstFrame = EnvelopeCodec.Encode(_json, firstEnvelope);
            var secondFrame = EnvelopeCodec.Encode(_json, secondEnvelope);

            var junkPrefixLength = 3;
            var offsetBuffer = new byte[junkPrefixLength + firstFrame.Length];
            offsetBuffer[0] = 0xFF;
            Buffer.BlockCopy(firstFrame, 0, offsetBuffer, junkPrefixLength, firstFrame.Length);

            Assert.IsTrue(EnvelopeCodec.TryDecode(
                _json,
                offsetBuffer,
                junkPrefixLength,
                firstFrame.Length,
                out var offsetDecoded,
                out _,
                out _));
            Assert.AreEqual(1, offsetDecoded.ReplyTo);

            var sequentialBuffer = new byte[firstFrame.Length + secondFrame.Length];
            Buffer.BlockCopy(firstFrame, 0, sequentialBuffer, 0, firstFrame.Length);
            Buffer.BlockCopy(secondFrame, 0, sequentialBuffer, firstFrame.Length, secondFrame.Length);

            Assert.IsTrue(EnvelopeCodec.TryDecode(
                _json,
                sequentialBuffer,
                0,
                sequentialBuffer.Length,
                out var firstDecoded,
                out _,
                out var firstResult));
            Assert.AreEqual(1, firstDecoded.ReplyTo);

            Assert.IsTrue(EnvelopeCodec.TryDecode(
                _json,
                sequentialBuffer,
                firstResult.BytesConsumed,
                sequentialBuffer.Length - firstResult.BytesConsumed,
                out var secondDecoded,
                out _,
                out _));
            Assert.AreEqual(2, secondDecoded.ReplyTo);
        }

        [Test]
        public void Serialize_InvalidPayloadJson_Throws() {
            var envelope = new Envelope {
                V = ProtocolVersion.Current,
                Type = "wifi/connect",
                Id = 41,
                Payload = Encoding.UTF8.GetBytes("not json")
            };

            Assert.Throws<JsonReaderException>(() => _json.Serialize(envelope));
        }

        [Test]
        public void TryDeserialize_NonObjectRoot_ReturnsFalse() {
            Assert.IsFalse(_json.TryDeserialize(Encoding.UTF8.GetBytes("null"), out _, out var nullError));
            Assert.That(nullError, Does.Contain("JSON object"));

            Assert.IsFalse(_json.TryDeserialize(Encoding.UTF8.GetBytes("[]"), out _, out var arrayError));
            Assert.IsNotNull(arrayError);

            Assert.IsFalse(_json.TryDeserialize(Encoding.UTF8.GetBytes("\"hello\""), out _, out var stringError));
            Assert.IsNotNull(stringError);
        }

    }
}
