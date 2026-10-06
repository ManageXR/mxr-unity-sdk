using System;
using System.Threading.Tasks;

using MXR.SDK.Protocol;
using MXR.SDK.Protocol.Guarantee;

using NUnit.Framework;

namespace MXR.SDK.Protocol.Tests {
    public class AckTrackerTests {
        ManualProtocolClock _clock;
        AckTracker _tracker;

        [SetUp]
        public void SetUp() {
            _clock = new ManualProtocolClock(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            _tracker = new AckTracker(_clock);
        }

        [Test]
        public void RegisterSent_TracksEnvelopeWithId() {
            var envelope = SentEnvelope(41, "wifi/connect");

            Assert.AreEqual(AckRegisterResult.Registered, _tracker.RegisterSent(envelope));
            Assert.AreEqual(1, _tracker.GetPendingAcks().Count);
        }

        [Test]
        public void RegisterSent_TracksFireAndForgetEnvelopeWithId() {
            var envelope = SentEnvelope(41, "telemetry/event");

            Assert.AreEqual(AckRegisterResult.Registered, _tracker.RegisterSent(envelope));
            Assert.AreEqual(1, _tracker.GetPendingAcks().Count);
        }

        [Test]
        public void RegisterSentAndCompleteAckFromReceiver_CompletesPendingId() {
            _tracker.RegisterSent(SentEnvelope(41, "wifi/connect"));

            Assert.AreEqual(AckCompleteResult.Completed, _tracker.CompleteAckFromReceiver(new ReceiverAck(41)));
            Assert.AreEqual(0, _tracker.GetPendingAcks().Count);
            Assert.AreEqual(0, _tracker.GetPendingAcks().Count);
            Assert.AreEqual(0, _tracker.GetExpiredAcks().Count);
            Assert.AreEqual(0, _tracker.GetLateAcks().Count);
        }

        [Test]
        public void CompleteAckFromReceiver_OrphanAckWhenIdWasNeverRegistered() {
            Assert.AreEqual(AckCompleteResult.OrphanAck, _tracker.CompleteAckFromReceiver(new ReceiverAck(99)));
        }

        [Test]
        public void RegisterSent_DuplicateIdWhilePending() {
            _tracker.RegisterSent(SentEnvelope(41, "wifi/connect"));

            Assert.AreEqual(AckRegisterResult.DuplicateId, _tracker.RegisterSent(SentEnvelope(41, "wifi/connect")));
            Assert.AreEqual(1, _tracker.GetPendingAcks().Count);
        }

        [Test]
        public void CompleteAckFromReceiver_AlreadyCompletedOnSecondAck() {
            _tracker.RegisterSent(SentEnvelope(41, "wifi/connect"));
            _tracker.CompleteAckFromReceiver(new ReceiverAck(41));

            Assert.AreEqual(AckCompleteResult.AlreadyCompleted, _tracker.CompleteAckFromReceiver(new ReceiverAck(41)));
        }

        [Test]
        public void CompleteAckFromReceiver_LateAckWhenDeadlinePassed() {
            _tracker.RegisterSent(SentEnvelope(41, "wifi/connect"), TimeSpan.FromSeconds(1));
            _clock.Advance(TimeSpan.FromSeconds(2));

            Assert.AreEqual(AckCompleteResult.LateAck, _tracker.CompleteAckFromReceiver(new ReceiverAck(41)));
            Assert.AreEqual(0, _tracker.GetExpiredAcks().Count);
            Assert.AreEqual(1, _tracker.GetLateAcks().Count);
            Assert.AreEqual(41, _tracker.GetLateAcks()[0].SentId);
        }

        [Test]
        public void RegisterSent_IsAckForSysAckWithoutId() {
            var envelope = new Envelope {
                V = ProtocolVersion.Current,
                Type = ProtocolTypes.Ack,
                ReplyTo = 41
            };

            Assert.AreEqual(AckRegisterResult.IsAck, _tracker.RegisterSent(envelope));
            Assert.AreEqual(0, _tracker.GetPendingAcks().Count);
        }

        [Test]
        public void RegisterSent_IsAckForSysAckEvenWhenIdPresent() {
            var envelope = new Envelope {
                V = ProtocolVersion.Current,
                Type = ProtocolTypes.Ack,
                Id = 7,
                ReplyTo = 41
            };

            Assert.AreEqual(AckRegisterResult.IsAck, _tracker.RegisterSent(envelope));
        }

        [Test]
        public void RegisterSent_NoIdWhenIdMissingOnNonAckEnvelope() {
            var envelope = new Envelope {
                V = ProtocolVersion.Current,
                Type = "wifi/connect"
            };

            Assert.AreEqual(AckRegisterResult.NoId, _tracker.RegisterSent(envelope));
        }

        [Test]
        public void GetExpiredAcks_ReturnsPendingIdsPastDeadline() {
            _tracker.RegisterSent(SentEnvelope(41, "wifi/connect"), TimeSpan.FromSeconds(5));
            _tracker.RegisterSent(SentEnvelope(42, "wifi/connect"), TimeSpan.FromSeconds(10));

            _clock.Advance(TimeSpan.FromSeconds(6));

            var expired = _tracker.GetExpiredAcks();

            Assert.AreEqual(1, expired.Count);
            Assert.AreEqual(41, expired[0].SentId);
            Assert.AreEqual(new DateTime(2026, 1, 1, 0, 0, 5, DateTimeKind.Utc), expired[0].DeadlineUtc);
            Assert.AreEqual(1, _tracker.GetPendingAcks().Count);
            Assert.AreEqual(1, _tracker.GetPendingAcks().Count);
        }

        [Test]
        public void GetExpiredAcks_DoesNotRemoveExpiredFromPending() {
            _tracker.RegisterSent(SentEnvelope(41, "wifi/connect"), TimeSpan.FromSeconds(1));
            _clock.Advance(TimeSpan.FromSeconds(2));

            Assert.AreEqual(1, _tracker.GetExpiredAcks().Count);
            Assert.AreEqual(1, _tracker.GetExpiredAcks().Count);
            Assert.AreEqual(AckCompleteResult.LateAck, _tracker.CompleteAckFromReceiver(new ReceiverAck(41)));
        }

        [Test]
        public void Reset_ClearsPendingCompletedAndLateState() {
            _tracker.RegisterSent(SentEnvelope(41, "wifi/connect"), TimeSpan.FromSeconds(1));
            _clock.Advance(TimeSpan.FromSeconds(2));
            _tracker.CompleteAckFromReceiver(new ReceiverAck(41));

            _tracker.Reset();

            Assert.AreEqual(0, _tracker.GetPendingAcks().Count);
            Assert.AreEqual(0, _tracker.GetExpiredAcks().Count);
            Assert.AreEqual(0, _tracker.GetLateAcks().Count);
            Assert.AreEqual(AckCompleteResult.OrphanAck, _tracker.CompleteAckFromReceiver(new ReceiverAck(41)));
        }

        [Test]
        public void RegisterSent_AllowsSameIdAfterCompleted() {
            _tracker.RegisterSent(SentEnvelope(41, "wifi/connect"));
            _tracker.CompleteAckFromReceiver(new ReceiverAck(41));

            Assert.AreEqual(AckRegisterResult.Registered, _tracker.RegisterSent(SentEnvelope(41, "wifi/connect")));
        }

        [Test]
        public void RegisterSent_ClearsLateStateForSameId() {
            _tracker.RegisterSent(SentEnvelope(41, "wifi/connect"), TimeSpan.FromSeconds(1));
            _clock.Advance(TimeSpan.FromSeconds(2));
            _tracker.CompleteAckFromReceiver(new ReceiverAck(41));
            Assert.AreEqual(1, _tracker.GetLateAcks().Count);

            Assert.AreEqual(AckRegisterResult.Registered, _tracker.RegisterSent(SentEnvelope(41, "wifi/connect")));
            Assert.AreEqual(0, _tracker.GetLateAcks().Count);
        }

        [Test]
        public void RegisterSent_NullEnvelope_Throws() {
            Assert.Throws<ArgumentNullException>(() => _tracker.RegisterSent(null));
        }

        [Test]
        public void PruneCompleted_DropsOldIdsOutsideRetentionWindow() {
            for (long id = 1; id <= GuaranteeDefaults.CompletedRetentionWindow + 2; id++) {
                _tracker.RegisterSent(SentEnvelope(id, "wifi/connect"));
                _tracker.CompleteAckFromReceiver(new ReceiverAck(id));
            }

            Assert.AreEqual(
                AckCompleteResult.OrphanAck,
                _tracker.CompleteAckFromReceiver(new ReceiverAck(1)));
        }

        [Test]
        public void RegisterSentAndCompleteAckFromReceiver_AreThreadSafe() {
            var tracker = new AckTracker(_clock);
            Parallel.For(0, 200, i => {
                var id = i + 1L;
                tracker.RegisterSent(SentEnvelope(id, "wifi/connect"));
                tracker.CompleteAckFromReceiver(new ReceiverAck(id));
            });

            Assert.AreEqual(0, tracker.GetPendingAcks().Count);
        }

        [Test]
        public void GetExpiredAcks_UsesGuaranteeDefaultsAckDeadlineWhenNotOverridden() {
            _tracker.RegisterSent(SentEnvelope(41, "wifi/connect"));

            _clock.Advance(GuaranteeDefaults.AckDeadline);

            var expired = _tracker.GetExpiredAcks();

            Assert.AreEqual(1, expired.Count);
            Assert.AreEqual(41, expired[0].SentId);
            Assert.AreEqual(
                new DateTime(2026, 1, 1, 0, 0, 5, DateTimeKind.Utc),
                expired[0].DeadlineUtc);
        }

        static Envelope SentEnvelope(long id, string type) {
            return new Envelope {
                V = ProtocolVersion.Current,
                Type = type,
                Id = id
            };
        }
    }
}
