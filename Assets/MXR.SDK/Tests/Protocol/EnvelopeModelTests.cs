using System.Text;

using MXR.SDK.Protocol;

using NUnit.Framework;

namespace MXR.SDK.Protocol.Tests {
    public class EnvelopeModelTests {

        [Test]
        public void ProtocolVersion_Current_IsSocketGenerationTwo() {
            Assert.AreEqual(2, ProtocolVersion.Current);
        }

        [Test]
        public void ProtocolTypes_MatchWireConstants() {
            Assert.AreEqual("sys/hello", ProtocolTypes.Hello);
            Assert.AreEqual("sys/ack", ProtocolTypes.Ack);
            Assert.AreEqual("sys/reply", ProtocolTypes.Reply);
            Assert.AreEqual("sys/err", ProtocolTypes.Err);
            Assert.AreEqual("state/subscribe", ProtocolTypes.Subscribe);
            Assert.AreEqual("state/push", ProtocolTypes.Push);
        }

        [Test]
        public void Envelope_StoresOpaquePayloadBytes() {
            var json = "{\"ssid\":\"Lab\"}";
            var bytes = Encoding.UTF8.GetBytes(json);

            var envelope = new Envelope {
                V = ProtocolVersion.Current,
                Type = "wifi/connect",
                Id = 41,
                Payload = bytes
            };

            Assert.AreEqual(ProtocolVersion.Current, envelope.V);
            Assert.AreEqual("wifi/connect", envelope.Type);
            Assert.AreEqual(41, envelope.Id);
            Assert.AreEqual(json, Encoding.UTF8.GetString(envelope.Payload));
        }

        [Test]
        public void Envelope_OptionalFieldsCanBeUnset() {
            var envelope = new Envelope {
                V = ProtocolVersion.Current,
                Type = ProtocolTypes.Ack,
                ReplyTo = 41
            };

            Assert.IsNull(envelope.Id);
            Assert.AreEqual(41, envelope.ReplyTo);
            Assert.IsNull(envelope.Channel);
            Assert.IsNull(envelope.Payload);
        }

        [Test]
        public void Envelope_TypeDefaultsToEmptyString() {
            var envelope = new Envelope();

            Assert.AreEqual(string.Empty, envelope.Type);
        }

        [Test]
        public void Envelope_ReplyErrAndPushCarryExpectedFields() {
            var reply = new Envelope {
                V = ProtocolVersion.Current,
                Type = ProtocolTypes.Reply,
                Id = 88,
                ReplyTo = 41
            };

            Assert.AreEqual(88, reply.Id);
            Assert.AreEqual(41, reply.ReplyTo);
            Assert.IsNull(reply.Channel);

            var err = new Envelope {
                V = ProtocolVersion.Current,
                Type = ProtocolTypes.Err,
                Id = 55,
                ReplyTo = 41
            };

            Assert.AreEqual(55, err.Id);
            Assert.AreEqual(41, err.ReplyTo);
            Assert.IsNull(err.Channel);

            var push = new Envelope {
                V = ProtocolVersion.Current,
                Type = ProtocolTypes.Push,
                Id = 9,
                Channel = "deviceStatus"
            };

            Assert.AreEqual(9, push.Id);
            Assert.AreEqual("deviceStatus", push.Channel);
            Assert.IsNull(push.ReplyTo);
        }

        [Test]
        public void Envelope_StateSubscribeCarriesChannel() {
            var envelope = new Envelope {
                V = ProtocolVersion.Current,
                Type = ProtocolTypes.Subscribe,
                Id = 7,
                Channel = "deviceStatus"
            };

            Assert.AreEqual("deviceStatus", envelope.Channel);
        }

        [Test]
        public void HelloPayload_DefaultsToEmptyFormatsAndImpl() {
            var hello = new HelloPayload();

            Assert.AreEqual(0, hello.Formats.Count);
            Assert.AreEqual(string.Empty, hello.Impl);
        }

        [Test]
        public void HelloPayload_StoresHandshakeFields() {
            var hello = new HelloPayload {
                Protocol = 2,
                MinProtocol = 2,
                Formats = new[] { "json" },
                Impl = "mxr-unity-sdk/2.0.0"
            };

            Assert.AreEqual(2, hello.Protocol);
            Assert.AreEqual(2, hello.MinProtocol);
            Assert.AreEqual(new[] { "json" }, hello.Formats);
            Assert.AreEqual("mxr-unity-sdk/2.0.0", hello.Impl);
        }

        [Test]
        public void ErrorPayload_StoresRetryableFlag() {
            var error = new ErrorPayload {
                Code = "wifi/invalidNetwork",
                Message = "ssid is empty",
                Retryable = false
            };

            Assert.AreEqual("wifi/invalidNetwork", error.Code);
            Assert.AreEqual("ssid is empty", error.Message);
            Assert.IsFalse(error.Retryable);
        }

        [Test]
        public void ErrorPayload_RetryableWhenTrue() {
            var error = new ErrorPayload {
                Code = "telemetry/rateLimited",
                Message = "busy",
                Retryable = true
            };

            Assert.IsTrue(error.Retryable);
        }
    }
}
