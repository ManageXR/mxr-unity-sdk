using System.Text;

using MXR.SDK.Protocol;
using MXR.SDK.Protocol.Handshake;

using NUnit.Framework;

namespace MXR.SDK.Protocol.Tests {
    public class HandshakeNegotiatorTests {
        static HelloPayload ServerHello(
            int protocol,
            int minProtocol,
            string[] formats,
            string impl = "mxr-admin-app/2.0.0"
        ) {
            return new HelloPayload {
                Protocol = protocol,
                MinProtocol = minProtocol,
                Formats = formats,
                Impl = impl
            };
        }

        [Test]
        public void CreateClientHello_UsesDefaultV2Fields() {
            var hello = HandshakeNegotiator.CreateClientHello(HandshakeNegotiator.DefaultClientImpl);

            Assert.AreEqual(ProtocolVersion.Current, hello.Protocol);
            Assert.AreEqual(ProtocolVersion.Current, hello.MinProtocol);
            Assert.AreEqual(new[] { JsonEnvelopeCodec.FormatName }, hello.Formats);
            Assert.AreEqual(HandshakeNegotiator.DefaultClientImpl, hello.Impl);
        }

        [Test]
        public void CreateClientHelloEnvelope_RoundTripsWithHelloCodec() {
            var payload = HandshakeNegotiator.CreateClientHello("mxr-unity-sdk/2.0.0");
            var envelope = HandshakeNegotiator.CreateClientHelloEnvelope(payload, 1, ProtocolVersion.Current);

            Assert.AreEqual(ProtocolTypes.Hello, envelope.Type);
            Assert.AreEqual(1, envelope.Id);
            Assert.AreEqual(ProtocolVersion.Current, envelope.V);

            Assert.IsTrue(
                EnvelopeCodecRegistry.HelloCodec.TryDeserialize(
                    EnvelopeCodecRegistry.HelloCodec.Serialize(envelope),
                    out var decoded,
                    out var error));
            Assert.IsNull(error);
            Assert.AreEqual(ProtocolTypes.Hello, decoded.Type);
            Assert.AreEqual(
                "{\"protocol\":2,\"minProtocol\":2,\"formats\":[\"json\"],\"impl\":\"mxr-unity-sdk/2.0.0\"}",
                Encoding.UTF8.GetString(decoded.Payload));
        }

        [Test]
        public void Negotiate_CompatibleWhenServerMatchesClientHello() {
            var client = HandshakeNegotiator.CreateClientHello(HandshakeNegotiator.DefaultClientImpl);
            var server = ServerHello(2, 2, new[] { JsonEnvelopeCodec.FormatName });

            var result = HandshakeNegotiator.Negotiate(client, server);

            Assert.AreEqual(Compatibility.Compatible, result.Compatibility);
            Assert.IsNull(result.Error);
            Assert.AreEqual(2, result.EffectiveProtocol);
            Assert.AreEqual(JsonEnvelopeCodec.FormatName, result.SelectedFormat);
            Assert.IsInstanceOf<JsonEnvelopeCodec>(result.PostHelloCodec);
            Assert.AreSame(EnvelopeCodecRegistry.ForFormat(JsonEnvelopeCodec.FormatName), result.PostHelloCodec);
        }

        [Test]
        public void Negotiate_CompatibleWhenVersionsOverlapUsesMinimumProtocol() {
            var client = new HelloPayload {
                Protocol = 3,
                MinProtocol = 2,
                Formats = new[] { JsonEnvelopeCodec.FormatName },
                Impl = HandshakeNegotiator.DefaultClientImpl
            };
            var server = ServerHello(2, 2, new[] { JsonEnvelopeCodec.FormatName });

            var result = HandshakeNegotiator.Negotiate(client, server);

            Assert.AreEqual(Compatibility.Compatible, result.Compatibility);
            Assert.IsNull(result.Error);
            Assert.AreEqual(2, result.EffectiveProtocol);
            Assert.AreEqual(JsonEnvelopeCodec.FormatName, result.SelectedFormat);
            Assert.AreSame(EnvelopeCodecRegistry.ForFormat(JsonEnvelopeCodec.FormatName), result.PostHelloCodec);
        }

        [Test]
        public void Negotiate_IncompatibleWhenEffectiveProtocolBelowClientMinimum() {
            var client = HandshakeNegotiator.CreateClientHello(HandshakeNegotiator.DefaultClientImpl);
            var server = ServerHello(1, 1, new[] { JsonEnvelopeCodec.FormatName });

            var result = HandshakeNegotiator.Negotiate(client, server);

            Assert.AreEqual(Compatibility.Incompatible, result.Compatibility);
            Assert.AreEqual(IncompatibleReason.AdminAppTooOld, result.Reason);
            Assert.That(result.Error, Does.Contain("Update the Admin App"));
            Assert.IsNull(result.PostHelloCodec);
        }

        [Test]
        public void Negotiate_IncompatibleWhenEffectiveProtocolBelowServerMinimum() {
            var client = new HelloPayload {
                Protocol = 3,
                MinProtocol = 2,
                Formats = new[] { JsonEnvelopeCodec.FormatName },
                Impl = HandshakeNegotiator.DefaultClientImpl
            };
            var server = ServerHello(2, 3, new[] { JsonEnvelopeCodec.FormatName });

            var result = HandshakeNegotiator.Negotiate(client, server);

            Assert.AreEqual(Compatibility.Incompatible, result.Compatibility);
            Assert.AreEqual(IncompatibleReason.SdkTooOld, result.Reason);
            Assert.That(result.Error, Does.Contain("Update the SDK"));
            Assert.IsNull(result.PostHelloCodec);
        }

        [Test]
        public void Negotiate_PicksFirstMutualFormatInClientPreferenceOrder() {
            var client = new HelloPayload {
                Protocol = 2,
                MinProtocol = 2,
                Formats = new[] { "proto3", JsonEnvelopeCodec.FormatName },
                Impl = HandshakeNegotiator.DefaultClientImpl
            };
            var server = ServerHello(2, 2, new[] { JsonEnvelopeCodec.FormatName });

            var result = HandshakeNegotiator.Negotiate(client, server);

            Assert.AreEqual(Compatibility.Compatible, result.Compatibility);
            Assert.AreEqual(JsonEnvelopeCodec.FormatName, result.SelectedFormat);
        }

        [Test]
        public void Negotiate_PicksClientPreferredFormatWhenServerListsADifferentMutualFormatFirst() {
            var client = new HelloPayload {
                Protocol = 2,
                MinProtocol = 2,
                Formats = new[] { JsonEnvelopeCodec.FormatName, "proto3" },
                Impl = HandshakeNegotiator.DefaultClientImpl
            };
            var server = ServerHello(2, 2, new[] { "proto3", JsonEnvelopeCodec.FormatName });

            var result = HandshakeNegotiator.Negotiate(client, server);

            Assert.AreEqual(Compatibility.Compatible, result.Compatibility);
            Assert.AreEqual(JsonEnvelopeCodec.FormatName, result.SelectedFormat);
            Assert.AreSame(EnvelopeCodecRegistry.ForFormat(JsonEnvelopeCodec.FormatName), result.PostHelloCodec);
        }

        [Test]
        public void Negotiate_IncompatibleWhenNoCommonFormat() {
            var client = new HelloPayload {
                Protocol = 2,
                MinProtocol = 2,
                Formats = new[] { "proto3" },
                Impl = HandshakeNegotiator.DefaultClientImpl
            };
            var server = ServerHello(2, 2, new[] { JsonEnvelopeCodec.FormatName });

            var result = HandshakeNegotiator.Negotiate(client, server);

            Assert.AreEqual(Compatibility.Incompatible, result.Compatibility);
            Assert.AreEqual(IncompatibleReason.NoCommonFormat, result.Reason);
            Assert.That(result.Error, Does.StartWith("No common payload format."));
        }

        [Test]
        public void Negotiate_SkipsMutualFormatWithoutCodecAndFallsBackToJson() {
            var client = new HelloPayload {
                Protocol = 2,
                MinProtocol = 2,
                Formats = new[] { "proto3", JsonEnvelopeCodec.FormatName },
                Impl = HandshakeNegotiator.DefaultClientImpl
            };
            var server = ServerHello(2, 2, new[] { "proto3", JsonEnvelopeCodec.FormatName });

            var result = HandshakeNegotiator.Negotiate(client, server);

            Assert.AreEqual(Compatibility.Compatible, result.Compatibility);
            Assert.AreEqual(JsonEnvelopeCodec.FormatName, result.SelectedFormat);
            Assert.AreSame(EnvelopeCodecRegistry.ForFormat(JsonEnvelopeCodec.FormatName), result.PostHelloCodec);
        }

        [Test]
        public void Negotiate_IncompatibleWhenOnlyMutualFormatHasNoCodec() {
            var client = new HelloPayload {
                Protocol = 2,
                MinProtocol = 2,
                Formats = new[] { "proto3" },
                Impl = HandshakeNegotiator.DefaultClientImpl
            };
            var server = ServerHello(2, 2, new[] { "proto3" });

            var result = HandshakeNegotiator.Negotiate(client, server);

            Assert.AreEqual(Compatibility.Incompatible, result.Compatibility);
            Assert.AreEqual(IncompatibleReason.NoCommonFormat, result.Reason);
            Assert.That(result.Error, Does.Not.Contain("Parameter"));
        }

        [Test]
        public void Negotiate_CompatibleWhenServerIsNewerUsesClientProtocol() {
            var client = HandshakeNegotiator.CreateClientHello(HandshakeNegotiator.DefaultClientImpl);
            var server = ServerHello(3, 2, new[] { JsonEnvelopeCodec.FormatName });

            var result = HandshakeNegotiator.Negotiate(client, server);

            Assert.AreEqual(Compatibility.Compatible, result.Compatibility);
            Assert.AreEqual(IncompatibleReason.None, result.Reason);
            Assert.AreEqual(2, result.EffectiveProtocol);
        }

        [Test]
        public void Negotiate_IncompatibleWhenServerFormatsNull() {
            var client = HandshakeNegotiator.CreateClientHello(HandshakeNegotiator.DefaultClientImpl);
            var server = ServerHello(2, 2, null);

            var result = HandshakeNegotiator.Negotiate(client, server);

            Assert.AreEqual(IncompatibleReason.NoCommonFormat, result.Reason);
        }

        [Test]
        public void Negotiate_IncompatibleWhenServerFormatsEmpty() {
            var client = HandshakeNegotiator.CreateClientHello(HandshakeNegotiator.DefaultClientImpl);
            var server = ServerHello(2, 2, new string[0]);

            var result = HandshakeNegotiator.Negotiate(client, server);

            Assert.AreEqual(IncompatibleReason.NoCommonFormat, result.Reason);
        }

        [Test]
        public void Negotiate_IgnoresNullEntriesInServerFormats() {
            var client = HandshakeNegotiator.CreateClientHello(HandshakeNegotiator.DefaultClientImpl);
            var server = ServerHello(2, 2, new[] { null, JsonEnvelopeCodec.FormatName });

            var result = HandshakeNegotiator.Negotiate(client, server);

            Assert.AreEqual(Compatibility.Compatible, result.Compatibility);
            Assert.AreEqual(JsonEnvelopeCodec.FormatName, result.SelectedFormat);
        }

        [Test]
        public void Compatibility_DefaultValueIsIncompatible() {
            Assert.AreEqual(Compatibility.Incompatible, default(Compatibility));
        }

        [Test]
        public void HandshakeResult_CompatibleRequiresCodec() {
            Assert.Throws<System.ArgumentNullException>(() =>
                HandshakeResult.CreateCompatible(2, JsonEnvelopeCodec.FormatName, null));
        }

        [Test]
        public void HandshakeResult_IncompatibleRequiresReason() {
            Assert.Throws<System.ArgumentException>(() =>
                HandshakeResult.CreateIncompatible(IncompatibleReason.None, "error"));
        }

        [Test]
        public void TryReadServerHello_ReadsSysReplyPayload() {
            var reply = ServerHelloReply(1, "{\"protocol\":2,\"minProtocol\":2,\"formats\":[\"json\"],\"impl\":\"mxr-admin-app/2.0.0\"}");

            Assert.IsTrue(HandshakeNegotiator.TryReadServerHello(reply, 1, out var hello, out var error));
            Assert.IsNull(error);
            Assert.AreEqual(2, hello.Protocol);
            Assert.AreEqual(2, hello.MinProtocol);
            CollectionAssert.AreEqual(new[] { JsonEnvelopeCodec.FormatName }, hello.Formats);
            Assert.AreEqual("mxr-admin-app/2.0.0", hello.Impl);
        }

        [Test]
        public void TryReadServerHello_RoundTripsThroughHelloCodec() {
            var sent = ServerHelloReply(1, "{\"protocol\":2,\"minProtocol\":2,\"formats\":[\"json\"],\"impl\":\"aa\"}");
            var bytes = EnvelopeCodecRegistry.HelloCodec.Serialize(sent);
            Assert.IsTrue(EnvelopeCodecRegistry.HelloCodec.TryDeserialize(bytes, out var received, out _));

            Assert.IsTrue(HandshakeNegotiator.TryReadServerHello(received, 1, out var hello, out _));
            Assert.AreEqual(Compatibility.Compatible, HandshakeNegotiator.Negotiate(
                HandshakeNegotiator.CreateClientHello(HandshakeNegotiator.DefaultClientImpl),
                hello).Compatibility);
        }

        [Test]
        public void TryReadServerHello_IgnoresUnknownFields() {
            var reply = ServerHelloReply(1, "{\"protocol\":2,\"minProtocol\":2,\"formats\":[\"json\"],\"impl\":\"aa\",\"future\":{\"x\":1}}");

            Assert.IsTrue(HandshakeNegotiator.TryReadServerHello(reply, 1, out var hello, out _));
            Assert.AreEqual(2, hello.Protocol);
        }

        [Test]
        public void TryReadServerHello_DefaultsMissingImplToEmpty() {
            var reply = ServerHelloReply(1, "{\"protocol\":2,\"minProtocol\":2,\"formats\":[\"json\"]}");

            Assert.IsTrue(HandshakeNegotiator.TryReadServerHello(reply, 1, out var hello, out _));
            Assert.AreEqual(string.Empty, hello.Impl);
        }

        [TestCase("{\"minProtocol\":2,\"formats\":[\"json\"]}", "'protocol'")]
        [TestCase("{\"protocol\":2,\"formats\":[\"json\"]}", "'minProtocol'")]
        [TestCase("{\"protocol\":2,\"minProtocol\":2}", "'formats'")]
        public void TryReadServerHello_FailsWhenRequiredFieldMissing(string payload, string field) {
            var reply = ServerHelloReply(1, payload);

            Assert.IsFalse(HandshakeNegotiator.TryReadServerHello(reply, 1, out var hello, out var error));
            Assert.IsNull(hello);
            Assert.That(error, Does.Contain(field));
        }

        [TestCase("{\"protocol\":\"two\",\"minProtocol\":2,\"formats\":[\"json\"]}")]
        [TestCase("[1,2,3]")]
        [TestCase("not json")]
        public void TryReadServerHello_FailsOnMalformedPayload(string payload) {
            var reply = ServerHelloReply(1, payload);

            Assert.IsFalse(HandshakeNegotiator.TryReadServerHello(reply, 1, out var hello, out var error));
            Assert.IsNull(hello);
            Assert.IsNotNull(error);
        }

        [Test]
        public void TryReadServerHello_FailsOnInvalidUtf8() {
            var reply = new Envelope {
                V = ProtocolVersion.Current,
                Type = ProtocolTypes.Reply,
                Id = 88,
                ReplyTo = 1,
                Payload = new byte[] { 0x7B, 0xFF, 0x7D }
            };

            Assert.IsFalse(HandshakeNegotiator.TryReadServerHello(reply, 1, out _, out var error));
            Assert.That(error, Does.Contain("UTF-8"));
        }

        [Test]
        public void TryReadServerHello_FailsOnEmptyPayload() {
            var reply = ServerHelloReply(1, null);

            Assert.IsFalse(HandshakeNegotiator.TryReadServerHello(reply, 1, out _, out var error));
            Assert.That(error, Does.Contain("empty"));
        }

        [Test]
        public void TryReadServerHello_FailsWhenReplyToDoesNotMatchHelloId() {
            var reply = ServerHelloReply(2, "{\"protocol\":2,\"minProtocol\":2,\"formats\":[\"json\"]}");

            Assert.IsFalse(HandshakeNegotiator.TryReadServerHello(reply, 1, out _, out var error));
            Assert.That(error, Does.Contain("hello id 1"));
        }

        [Test]
        public void TryReadServerHello_FailsWhenAnswerIsSysErr() {
            var reply = new Envelope {
                V = ProtocolVersion.Current,
                Type = ProtocolTypes.Err,
                Id = 55,
                ReplyTo = 1,
                Payload = Encoding.UTF8.GetBytes("{\"code\":\"sys/badHello\",\"message\":\"nope\",\"retryable\":false}")
            };

            Assert.IsFalse(HandshakeNegotiator.TryReadServerHello(reply, 1, out _, out var error));
            Assert.That(error, Does.Contain("sys/err"));
        }

        [Test]
        public void TryReadServerHello_FailsWhenAnswerIsNotSysReply() {
            var reply = new Envelope {
                V = ProtocolVersion.Current,
                Type = ProtocolTypes.Hello,
                Id = 1,
                ReplyTo = 1
            };

            Assert.IsFalse(HandshakeNegotiator.TryReadServerHello(reply, 1, out _, out var error));
            Assert.That(error, Does.Contain(ProtocolTypes.Reply));
        }

        [Test]
        public void TryReadServerHello_NullReply_Throws() {
            Assert.Throws<System.ArgumentNullException>(() =>
                HandshakeNegotiator.TryReadServerHello(null, 1, out _, out _));
        }

        static Envelope ServerHelloReply(long replyTo, string payloadJson) {
            return new Envelope {
                V = ProtocolVersion.Current,
                Type = ProtocolTypes.Reply,
                Id = 88,
                ReplyTo = replyTo,
                Payload = payloadJson == null ? null : Encoding.UTF8.GetBytes(payloadJson)
            };
        }

        [Test]
        public void CreateClientHello_NullImpl_Throws() {
            Assert.Throws<System.ArgumentNullException>(() => HandshakeNegotiator.CreateClientHello(null));
        }

        [Test]
        public void CreateClientHelloEnvelope_NullPayload_Throws() {
            Assert.Throws<System.ArgumentNullException>(() =>
                HandshakeNegotiator.CreateClientHelloEnvelope(null, 1, ProtocolVersion.Current));
        }

        [Test]
        public void Negotiate_NullClient_Throws() {
            Assert.Throws<System.ArgumentNullException>(() =>
                HandshakeNegotiator.Negotiate(null, ServerHello(2, 2, new[] { JsonEnvelopeCodec.FormatName })));
        }

        [Test]
        public void Negotiate_NullServer_Throws() {
            Assert.Throws<System.ArgumentNullException>(() =>
                HandshakeNegotiator.Negotiate(
                    HandshakeNegotiator.CreateClientHello(HandshakeNegotiator.DefaultClientImpl),
                    null));
        }
    }
}
