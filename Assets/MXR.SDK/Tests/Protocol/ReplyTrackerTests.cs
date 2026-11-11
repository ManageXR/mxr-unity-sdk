using System;
using System.Threading.Tasks;

using MXR.SDK.Protocol;
using MXR.SDK.Protocol.Guarantee;

using NUnit.Framework;

namespace MXR.SDK.Protocol.Tests {
    public class ReplyTrackerTests {
        ManualProtocolClock _clock;
        ReplyTracker _tracker;

        [SetUp]
        public void SetUp() {
            _clock = new ManualProtocolClock(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            _tracker = new ReplyTracker(_clock);
        }

        [Test]
        public void FireAndForget_HasNoRegisteredRepliesUntilRegisterRequestCalled() {
            Assert.AreEqual(0, _tracker.GetPendingReplies().Count);
            Assert.AreEqual(0, _tracker.GetExpiredReplies().Count);
            Assert.AreEqual(0, _tracker.GetLateReplies().Count);
        }

        [Test]
        public void RegisterRequest_TracksRequestWithId() {
            var request = RequestEnvelope(41, "wifi/connect");

            Assert.AreEqual(ReplyRegisterResult.Registered, _tracker.RegisterRequest(request));
            Assert.AreEqual(1, _tracker.GetPendingReplies().Count);
        }

        [Test]
        public void RegisterRequestAndCompleteReplyFromReceiver_CompletesWithSysReply() {
            _tracker.RegisterRequest(RequestEnvelope(41, "wifi/connect"));

            var match = _tracker.CompleteReplyFromReceiver(ReceiverReplyEnvelope(
                41,
                ProtocolTypes.Reply,
                88));

            Assert.AreEqual(ReplyCompleteResult.Completed, match.Result);
            Assert.AreEqual(41, match.RequestId);
            Assert.AreEqual(88, match.Reply.Id);
            Assert.IsFalse(match.IsError);
            Assert.AreEqual(0, _tracker.GetPendingReplies().Count);
        }

        [Test]
        public void CompleteReplyFromReceiver_CompletesWithSysErr() {
            _tracker.RegisterRequest(RequestEnvelope(41, "wifi/connect"));

            var match = _tracker.CompleteReplyFromReceiver(ReceiverReplyEnvelope(
                41,
                ProtocolTypes.Err,
                55));

            Assert.AreEqual(ReplyCompleteResult.Completed, match.Result);
            Assert.IsTrue(match.IsError);
        }

        [Test]
        public void CompleteReplyFromReceiver_OrphanReplyWhenRequestWasNeverRegistered() {
            var match = _tracker.CompleteReplyFromReceiver(ReceiverReplyEnvelope(
                99,
                ProtocolTypes.Reply,
                88));

            Assert.AreEqual(ReplyCompleteResult.OrphanReply, match.Result);
        }

        [Test]
        public void RegisterRequest_DuplicateIdWhilePending() {
            _tracker.RegisterRequest(RequestEnvelope(41, "wifi/connect"));

            Assert.AreEqual(ReplyRegisterResult.DuplicateId, _tracker.RegisterRequest(RequestEnvelope(41, "wifi/connect")));
            Assert.AreEqual(1, _tracker.GetPendingReplies().Count);
        }

        [Test]
        public void CompleteReplyFromReceiver_AlreadyCompletedOnSecondReply() {
            _tracker.RegisterRequest(RequestEnvelope(41, "wifi/connect"));
            _tracker.CompleteReplyFromReceiver(ReceiverReplyEnvelope(41, ProtocolTypes.Reply, 88));

            var match = _tracker.CompleteReplyFromReceiver(ReceiverReplyEnvelope(41, ProtocolTypes.Reply, 89));

            Assert.AreEqual(ReplyCompleteResult.AlreadyCompleted, match.Result);
        }

        [Test]
        public void CompleteReplyFromReceiver_LateReplyWhenDeadlinePassed() {
            _tracker.RegisterRequest(RequestEnvelope(41, "wifi/connect"), TimeSpan.FromSeconds(1));
            _clock.Advance(TimeSpan.FromSeconds(2));

            var match = _tracker.CompleteReplyFromReceiver(ReceiverReplyEnvelope(41, ProtocolTypes.Reply, 88));

            Assert.AreEqual(ReplyCompleteResult.LateReply, match.Result);
            Assert.AreEqual(0, _tracker.GetExpiredReplies().Count);
            Assert.AreEqual(1, _tracker.GetLateReplies().Count);
            Assert.AreEqual(41, _tracker.GetLateReplies()[0].RequestId);
        }

        [Test]
        public void CompleteReplyFromReceiver_InvalidReplyForDomainEnvelope() {
            var match = _tracker.CompleteReplyFromReceiver(new ReceiverReply(
                41,
                new Envelope {
                    V = ProtocolVersion.Current,
                    Type = "wifi/connect",
                    Id = 88,
                    ReplyTo = 41
                }));

            Assert.AreEqual(ReplyCompleteResult.InvalidReply, match.Result);
        }

        [Test]
        public void CompleteReplyFromReceiver_InvalidReplyWhenReplyToMismatches() {
            var match = _tracker.CompleteReplyFromReceiver(new ReceiverReply(
                41,
                new Envelope {
                    V = ProtocolVersion.Current,
                    Type = ProtocolTypes.Reply,
                    Id = 88,
                    ReplyTo = 42
                }));

            Assert.AreEqual(ReplyCompleteResult.InvalidReply, match.Result);
        }

        [Test]
        public void RegisterRequest_NoIdWhenIdMissing() {
            var request = new Envelope {
                V = ProtocolVersion.Current,
                Type = "wifi/connect"
            };

            Assert.AreEqual(ReplyRegisterResult.NoId, _tracker.RegisterRequest(request));
        }

        [Test]
        public void GetExpiredReplies_ReturnsPendingRequestsPastDeadline() {
            _tracker.RegisterRequest(RequestEnvelope(41, "wifi/connect"), TimeSpan.FromSeconds(5));
            _tracker.RegisterRequest(RequestEnvelope(42, "wifi/connect"), TimeSpan.FromSeconds(10));

            _clock.Advance(TimeSpan.FromSeconds(6));

            var expired = _tracker.GetExpiredReplies();

            Assert.AreEqual(1, expired.Count);
            Assert.AreEqual(41, expired[0].RequestId);
            Assert.AreEqual(new DateTime(2026, 1, 1, 0, 0, 5, DateTimeKind.Utc), expired[0].DeadlineUtc);
            Assert.AreEqual(1, _tracker.GetPendingReplies().Count);
        }

        [Test]
        public void Reset_ClearsPendingCompletedAndLateState() {
            _tracker.RegisterRequest(RequestEnvelope(41, "wifi/connect"), TimeSpan.FromSeconds(1));
            _clock.Advance(TimeSpan.FromSeconds(2));
            _tracker.CompleteReplyFromReceiver(ReceiverReplyEnvelope(41, ProtocolTypes.Reply, 88));

            _tracker.Reset();

            Assert.AreEqual(0, _tracker.GetPendingReplies().Count);
            Assert.AreEqual(0, _tracker.GetExpiredReplies().Count);
            Assert.AreEqual(0, _tracker.GetLateReplies().Count);
            Assert.AreEqual(ReplyCompleteResult.OrphanReply, _tracker.CompleteReplyFromReceiver(
                ReceiverReplyEnvelope(41, ProtocolTypes.Reply, 88)).Result);
        }

        [Test]
        public void RegisterRequest_ClearsLateStateForSameId() {
            _tracker.RegisterRequest(RequestEnvelope(41, "wifi/connect"), TimeSpan.FromSeconds(1));
            _clock.Advance(TimeSpan.FromSeconds(2));
            _tracker.CompleteReplyFromReceiver(ReceiverReplyEnvelope(41, ProtocolTypes.Reply, 88));
            Assert.AreEqual(1, _tracker.GetLateReplies().Count);

            Assert.AreEqual(ReplyRegisterResult.Registered, _tracker.RegisterRequest(RequestEnvelope(41, "wifi/connect")));
            Assert.AreEqual(0, _tracker.GetLateReplies().Count);
        }

        [Test]
        public void RegisterRequest_NullEnvelope_Throws() {
            Assert.Throws<ArgumentNullException>(() => _tracker.RegisterRequest(null));
        }

        [Test]
        public void RegisterRequest_IsNotRequestForSysReply() {
            var envelope = new Envelope {
                V = ProtocolVersion.Current,
                Type = ProtocolTypes.Reply,
                Id = 7,
                ReplyTo = 41
            };

            Assert.AreEqual(ReplyRegisterResult.IsNotRequest, _tracker.RegisterRequest(envelope));
            Assert.AreEqual(0, _tracker.GetPendingReplies().Count);
        }

        [Test]
        public void RegisterRequest_IsNotRequestForSysErr() {
            var envelope = new Envelope {
                V = ProtocolVersion.Current,
                Type = ProtocolTypes.Err,
                Id = 7,
                ReplyTo = 41
            };

            Assert.AreEqual(ReplyRegisterResult.IsNotRequest, _tracker.RegisterRequest(envelope));
        }

        [Test]
        public void RegisterRequest_IsNotRequestForSysAck() {
            var envelope = new Envelope {
                V = ProtocolVersion.Current,
                Type = ProtocolTypes.Ack,
                Id = 7,
                ReplyTo = 41
            };

            Assert.AreEqual(ReplyRegisterResult.IsNotRequest, _tracker.RegisterRequest(envelope));
        }

        [Test]
        public void PruneCompleted_DropsOldIdsOutsideRetentionWindow() {
            for (long id = 1; id <= GuaranteeDefaults.CompletedRetentionWindow + 2; id++) {
                _tracker.RegisterRequest(RequestEnvelope(id, "wifi/connect"));
                _tracker.CompleteReplyFromReceiver(ReceiverReplyEnvelope(id, ProtocolTypes.Reply, id + 1000));
            }

            Assert.AreEqual(
                ReplyCompleteResult.OrphanReply,
                _tracker.CompleteReplyFromReceiver(ReceiverReplyEnvelope(1, ProtocolTypes.Reply, 1001)).Result);
        }

        [Test]
        public void RegisterRequestAndCompleteReplyFromReceiver_AreThreadSafe() {
            var tracker = new ReplyTracker(_clock);
            Parallel.For(0, 200, i => {
                var id = i + 1L;
                tracker.RegisterRequest(RequestEnvelope(id, "wifi/connect"));
                tracker.CompleteReplyFromReceiver(ReceiverReplyEnvelope(id, ProtocolTypes.Reply, id + 1000));
            });

            Assert.AreEqual(0, tracker.GetPendingReplies().Count);
        }

        [Test]
        public void CompleteReplyFromReceiver_NullEnvelope_ThrowsWithReplyParamName() {
            var ex = Assert.Throws<ArgumentNullException>(() =>
                _tracker.CompleteReplyFromReceiver(new ReceiverReply(41, null)));

            Assert.AreEqual("reply", ex.ParamName);
        }

        [Test]
        public void GetExpiredReplies_UsesGuaranteeDefaultsReplyDeadlineWhenNotOverridden() {
            _tracker.RegisterRequest(RequestEnvelope(41, "wifi/connect"));

            _clock.Advance(GuaranteeDefaults.ReplyDeadline);

            var expired = _tracker.GetExpiredReplies();

            Assert.AreEqual(1, expired.Count);
            Assert.AreEqual(41, expired[0].RequestId);
            Assert.AreEqual(
                new DateTime(2026, 1, 1, 0, 0, 10, DateTimeKind.Utc),
                expired[0].DeadlineUtc);
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
