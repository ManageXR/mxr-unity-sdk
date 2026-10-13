using MXR.SDK.Protocol;
using MXR.SDK.Protocol.Guarantee;

using NUnit.Framework;

namespace MXR.SDK.Protocol.Tests {
    public class AckFactoryTests {
        [Test]
        public void RequiresAck_TrueForEnvelopeWithId() {
            var envelope = new Envelope {
                V = ProtocolVersion.Current,
                Type = "wifi/connect",
                Id = 41
            };

            Assert.IsTrue(AckFactory.RequiresAck(envelope));
        }

        [Test]
        public void RequiresAck_FalseForSysAck() {
            var envelope = new Envelope {
                V = ProtocolVersion.Current,
                Type = ProtocolTypes.Ack,
                ReplyTo = 41
            };

            Assert.IsFalse(AckFactory.RequiresAck(envelope));
        }

        [Test]
        public void RequiresAck_FalseWhenIdMissing() {
            var envelope = new Envelope {
                V = ProtocolVersion.Current,
                Type = "wifi/connect"
            };

            Assert.IsFalse(AckFactory.RequiresAck(envelope));
        }

        [Test]
        public void CreateAck_ProducesIdLessSysAck() {
            var ack = AckFactory.CreateAck(41, ProtocolVersion.Current);

            Assert.AreEqual(ProtocolVersion.Current, ack.V);
            Assert.AreEqual(ProtocolTypes.Ack, ack.Type);
            Assert.AreEqual(41, ack.ReplyTo);
            Assert.IsNull(ack.Id);
            Assert.IsNull(ack.Channel);
            Assert.IsNull(ack.Payload);
        }

        [Test]
        public void RequiresAck_NullEnvelope_Throws() {
            Assert.Throws<System.ArgumentNullException>(() => AckFactory.RequiresAck(null));
        }
    }
}
