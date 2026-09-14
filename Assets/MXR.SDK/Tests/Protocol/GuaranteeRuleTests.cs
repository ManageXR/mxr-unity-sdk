using System;

using MXR.SDK.Protocol;
using MXR.SDK.Protocol.Guarantee;

using NUnit.Framework;

namespace MXR.SDK.Protocol.Tests {
    public class GuaranteeRuleTests {
        ManualProtocolClock _clock;
        AckTracker _ackTracker;
        ReplyTracker _replyTracker;

        [SetUp]
        public void SetUp() {
            _clock = new ManualProtocolClock(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            _ackTracker = new AckTracker(_clock);
            _replyTracker = new ReplyTracker(_clock);
        }

        [Test]
        public void AwaitedSend_AckThenReply_CompletesBothTrackers() {
            const long requestId = 41;
            var request = RequestEnvelope(requestId, "wifi/connect");

            Assert.AreEqual(AckRegisterResult.Registered, _ackTracker.RegisterSent(request));
            Assert.AreEqual(ReplyRegisterResult.Registered, _replyTracker.RegisterRequest(request));
            Assert.AreEqual(1, _ackTracker.GetPendingAcks().Count);
            Assert.AreEqual(1, _replyTracker.GetPendingReplies().Count);

            Assert.AreEqual(
                AckCompleteResult.Completed,
                _ackTracker.CompleteAckFromReceiver(new ReceiverAck(requestId)));
            Assert.AreEqual(0, _ackTracker.GetPendingAcks().Count);
            Assert.AreEqual(1, _replyTracker.GetPendingReplies().Count);

            var match = _replyTracker.CompleteReplyFromReceiver(ReceiverReplyEnvelope(
                requestId,
                ProtocolTypes.Reply,
                88));

            Assert.AreEqual(ReplyCompleteResult.Completed, match.Result);
            Assert.AreEqual(requestId, match.RequestId);
            Assert.AreEqual(0, _replyTracker.GetPendingReplies().Count);
        }

        static Envelope RequestEnvelope(long id, string type) {
            return new Envelope {
                V = ProtocolVersion.Current,
                Type = type,
                Id = id
            };
        }

        static ReceiverReply ReceiverReplyEnvelope(long replyTo, string type, long replyId) {
            return new ReceiverReply(replyTo, new Envelope {
                V = ProtocolVersion.Current,
                Type = type,
                Id = replyId,
                ReplyTo = replyTo
            });
        }
    }
}
