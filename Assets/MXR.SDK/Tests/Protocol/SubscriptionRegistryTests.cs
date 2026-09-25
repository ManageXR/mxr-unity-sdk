using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using MXR.SDK.Protocol.Subscriptions;

using NUnit.Framework;

namespace MXR.SDK.Protocol.Tests {
    public class SubscriptionRegistryTests {
        SubscriptionRegistry _registry;

        [SetUp]
        public void SetUp() {
            _registry = new SubscriptionRegistry();
        }

        [Test]
        public void StateChannelNames_DefinesAsArray() {
            Assert.AreEqual(3, StateChannelNames.AsArray.Count);
            CollectionAssert.Contains(StateChannelNames.AsArray, StateChannelNames.DeviceStatus);
            CollectionAssert.Contains(StateChannelNames.AsArray, StateChannelNames.RuntimeSettings);
            CollectionAssert.Contains(StateChannelNames.AsArray, StateChannelNames.DeviceData);
        }

        [Test]
        public void StateChannelNames_AsArrayCannotBeModified() {
            Assert.IsNotInstanceOf<string[]>(StateChannelNames.AsArray);
            Assert.Throws<NotSupportedException>(() => ((IList<string>)StateChannelNames.AsArray)[0] = "tampered");
            Assert.AreEqual(StateChannelNames.DeviceStatus, StateChannelNames.AsArray[0]);
        }

        [Test]
        public void Subscribe_AddsChannelAwaitingSnapshot() {
            Assert.AreEqual(SubscribeResult.Subscribed, _registry.Subscribe(StateChannelNames.DeviceStatus));
            Assert.AreEqual(SubscriptionState.AwaitingSnapshot, _registry.GetSubscriptionState(StateChannelNames.DeviceStatus));
            Assert.AreEqual(1, _registry.GetChannelsAwaitingSnapshot().Count);
        }

        [Test]
        public void Subscribe_ReturnsAlreadySubscribedForDuplicateChannel() {
            _registry.Subscribe(StateChannelNames.DeviceStatus);

            Assert.AreEqual(SubscribeResult.AlreadySubscribed, _registry.Subscribe(StateChannelNames.DeviceStatus));
            Assert.AreEqual(1, _registry.GetChannelsAwaitingSnapshot().Count);
        }

        [Test]
        public void Subscribe_AfterSnapshotReceived_ReturnsAlreadySubscribed() {
            _registry.Subscribe(StateChannelNames.DeviceStatus);
            _registry.MarkSnapshotReceived(StateChannelNames.DeviceStatus);

            Assert.AreEqual(SubscribeResult.AlreadySubscribed, _registry.Subscribe(StateChannelNames.DeviceStatus));
            Assert.AreEqual(SubscriptionState.ReceivedSnapshot, _registry.GetSubscriptionState(StateChannelNames.DeviceStatus));
        }

        [Test]
        public void Subscribe_AfterSubscribeFailed_ReturnsSubscribedAndAwaitsAgain() {
            _registry.Subscribe(StateChannelNames.DeviceStatus);
            _registry.MarkSubscribeFailed(StateChannelNames.DeviceStatus);

            Assert.AreEqual(SubscribeResult.Subscribed, _registry.Subscribe(StateChannelNames.DeviceStatus));
            Assert.AreEqual(SubscriptionState.AwaitingSnapshot, _registry.GetSubscriptionState(StateChannelNames.DeviceStatus));
            Assert.AreEqual(0, _registry.GetFailedChannels().Count);
        }

        [Test]
        public void MarkSnapshotReceived_ReturnsReceivedAndClearsAwaitingState() {
            _registry.Subscribe(StateChannelNames.DeviceStatus);

            Assert.AreEqual(SnapshotReceivedResult.Received, _registry.MarkSnapshotReceived(StateChannelNames.DeviceStatus));
            Assert.AreEqual(SubscriptionState.ReceivedSnapshot, _registry.GetSubscriptionState(StateChannelNames.DeviceStatus));
            Assert.AreEqual(0, _registry.GetChannelsAwaitingSnapshot().Count);
        }

        [Test]
        public void MarkSnapshotReceived_ReturnsAlreadyReceivedOnDuplicate() {
            _registry.Subscribe(StateChannelNames.DeviceStatus);
            _registry.MarkSnapshotReceived(StateChannelNames.DeviceStatus);

            Assert.AreEqual(SnapshotReceivedResult.AlreadyReceived, _registry.MarkSnapshotReceived(StateChannelNames.DeviceStatus));
        }

        [Test]
        public void MarkSnapshotReceived_ReturnsNotSubscribedForUnknownChannel() {
            Assert.AreEqual(SnapshotReceivedResult.NotSubscribed, _registry.MarkSnapshotReceived("unknown"));
            Assert.AreEqual(SubscriptionState.NoSubscriptionFound, _registry.GetSubscriptionState("unknown"));
            Assert.AreEqual(0, _registry.GetChannelsAwaitingSnapshot().Count);
        }

        [Test]
        public void MarkSnapshotReceived_AfterSubscribeFailed_ReturnsReceived() {
            _registry.Subscribe(StateChannelNames.DeviceStatus);
            _registry.MarkSubscribeFailed(StateChannelNames.DeviceStatus);

            Assert.AreEqual(SnapshotReceivedResult.Received, _registry.MarkSnapshotReceived(StateChannelNames.DeviceStatus));
            Assert.AreEqual(SubscriptionState.ReceivedSnapshot, _registry.GetSubscriptionState(StateChannelNames.DeviceStatus));
            Assert.AreEqual(0, _registry.GetFailedChannels().Count);
            Assert.IsTrue(_registry.AllSnapshotsReceived);
        }

        [Test]
        public void MarkSubscribeFailed_ReturnsFailedForAwaitingChannel() {
            _registry.Subscribe(StateChannelNames.DeviceStatus);

            Assert.AreEqual(SubscribeFailedResult.Failed, _registry.MarkSubscribeFailed(StateChannelNames.DeviceStatus));
            Assert.AreEqual(SubscriptionState.SubscribeFailed, _registry.GetSubscriptionState(StateChannelNames.DeviceStatus));
            Assert.AreEqual(0, _registry.GetChannelsAwaitingSnapshot().Count);
            CollectionAssert.AreEqual(new[] { StateChannelNames.DeviceStatus }, _registry.GetFailedChannels());
        }

        [Test]
        public void MarkSubscribeFailed_ReturnsAlreadyFailedOnDuplicate() {
            _registry.Subscribe(StateChannelNames.DeviceStatus);
            _registry.MarkSubscribeFailed(StateChannelNames.DeviceStatus);

            Assert.AreEqual(SubscribeFailedResult.AlreadyFailed, _registry.MarkSubscribeFailed(StateChannelNames.DeviceStatus));
        }

        [Test]
        public void MarkSubscribeFailed_ReturnsAlreadyReceivedAndKeepsSnapshot() {
            _registry.Subscribe(StateChannelNames.DeviceStatus);
            _registry.MarkSnapshotReceived(StateChannelNames.DeviceStatus);

            Assert.AreEqual(SubscribeFailedResult.AlreadyReceived, _registry.MarkSubscribeFailed(StateChannelNames.DeviceStatus));
            Assert.AreEqual(SubscriptionState.ReceivedSnapshot, _registry.GetSubscriptionState(StateChannelNames.DeviceStatus));
        }

        [Test]
        public void MarkSubscribeFailed_ReturnsNotSubscribedForUnknownChannel() {
            Assert.AreEqual(SubscribeFailedResult.NotSubscribed, _registry.MarkSubscribeFailed("unknown"));
            Assert.AreEqual(SubscriptionState.NoSubscriptionFound, _registry.GetSubscriptionState("unknown"));
        }

        [Test]
        public void GetSubscriptionState_ReturnsNoSubscriptionFoundForUnknownChannel() {
            Assert.AreEqual(SubscriptionState.NoSubscriptionFound, _registry.GetSubscriptionState("unknown"));
        }

        [Test]
        public void AllSnapshotsReceived_FalseBeforeAnySubscribe() {
            Assert.IsFalse(_registry.AllSnapshotsReceived);
        }

        [Test]
        public void AllSnapshotsReceived_FalseWhileAnyChannelAwaiting() {
            _registry.Subscribe(StateChannelNames.DeviceStatus);
            _registry.Subscribe(StateChannelNames.RuntimeSettings);
            _registry.MarkSnapshotReceived(StateChannelNames.DeviceStatus);

            Assert.IsFalse(_registry.AllSnapshotsReceived);
        }

        [Test]
        public void AllSnapshotsReceived_FalseWhileAnyChannelFailed() {
            _registry.Subscribe(StateChannelNames.DeviceStatus);
            _registry.Subscribe(StateChannelNames.RuntimeSettings);
            _registry.MarkSnapshotReceived(StateChannelNames.DeviceStatus);
            _registry.MarkSubscribeFailed(StateChannelNames.RuntimeSettings);

            Assert.IsFalse(_registry.AllSnapshotsReceived);
            Assert.AreEqual(0, _registry.GetChannelsAwaitingSnapshot().Count);
        }

        [Test]
        public void GetChannelsAwaitingSnapshot_ReportsProgressAcrossBootstrap() {
            foreach (var channel in StateChannelNames.AsArray) {
                _registry.Subscribe(channel);
            }

            Assert.AreEqual(3, _registry.GetChannelsAwaitingSnapshot().Count);

            _registry.MarkSnapshotReceived(StateChannelNames.DeviceStatus);
            _registry.MarkSnapshotReceived(StateChannelNames.RuntimeSettings);

            var awaiting = _registry.GetChannelsAwaitingSnapshot();
            Assert.AreEqual(1, awaiting.Count);
            Assert.AreEqual(StateChannelNames.DeviceData, awaiting[0]);
            Assert.AreEqual(SubscriptionState.ReceivedSnapshot, _registry.GetSubscriptionState(StateChannelNames.DeviceStatus));
            Assert.AreEqual(SubscriptionState.ReceivedSnapshot, _registry.GetSubscriptionState(StateChannelNames.RuntimeSettings));
            Assert.AreEqual(SubscriptionState.AwaitingSnapshot, _registry.GetSubscriptionState(StateChannelNames.DeviceData));
        }

        [Test]
        public void BootstrapFlow_BecomesReadyWhenAllSnapshotsReceived() {
            foreach (var channel in StateChannelNames.AsArray) {
                Assert.AreEqual(SubscribeResult.Subscribed, _registry.Subscribe(channel));
            }

            Assert.IsFalse(_registry.AllSnapshotsReceived);

            foreach (var channel in StateChannelNames.AsArray) {
                Assert.AreEqual(SnapshotReceivedResult.Received, _registry.MarkSnapshotReceived(channel));
            }

            Assert.IsTrue(_registry.AllSnapshotsReceived);
            Assert.AreEqual(0, _registry.GetChannelsAwaitingSnapshot().Count);
        }

        [Test]
        public void BootstrapFlow_FailedChannelRecoversAfterResubscribe() {
            foreach (var channel in StateChannelNames.AsArray) {
                _registry.Subscribe(channel);
            }

            _registry.MarkSnapshotReceived(StateChannelNames.DeviceStatus);
            _registry.MarkSnapshotReceived(StateChannelNames.RuntimeSettings);
            _registry.MarkSubscribeFailed(StateChannelNames.DeviceData);
            Assert.IsFalse(_registry.AllSnapshotsReceived);

            Assert.AreEqual(SubscribeResult.Subscribed, _registry.Subscribe(StateChannelNames.DeviceData));
            Assert.AreEqual(SnapshotReceivedResult.Received, _registry.MarkSnapshotReceived(StateChannelNames.DeviceData));

            Assert.IsTrue(_registry.AllSnapshotsReceived);
        }

        [Test]
        public void Reset_ClearsAllStateAndReadiness() {
            _registry.Subscribe(StateChannelNames.DeviceStatus);
            _registry.MarkSnapshotReceived(StateChannelNames.DeviceStatus);
            _registry.Subscribe(StateChannelNames.RuntimeSettings);
            _registry.MarkSubscribeFailed(StateChannelNames.RuntimeSettings);

            _registry.Reset();

            Assert.AreEqual(SubscriptionState.NoSubscriptionFound, _registry.GetSubscriptionState(StateChannelNames.DeviceStatus));
            Assert.AreEqual(SubscriptionState.NoSubscriptionFound, _registry.GetSubscriptionState(StateChannelNames.RuntimeSettings));
            Assert.AreEqual(0, _registry.GetChannelsAwaitingSnapshot().Count);
            Assert.AreEqual(0, _registry.GetFailedChannels().Count);
            Assert.IsFalse(_registry.AllSnapshotsReceived);
            Assert.AreEqual(SubscribeResult.Subscribed, _registry.Subscribe(StateChannelNames.DeviceStatus));
        }

        [Test]
        public void SubscribeAndMarkSnapshotReceived_AreThreadSafe() {
            var channels = new string[200];
            for (var i = 0; i < channels.Length; i++) {
                channels[i] = "channel" + i;
            }

            Parallel.For(0, channels.Length, i => {
                _registry.Subscribe(channels[i]);
                _registry.GetChannelsAwaitingSnapshot();
                _registry.MarkSnapshotReceived(channels[i]);
                _ = _registry.AllSnapshotsReceived;
            });

            Assert.AreEqual(0, _registry.GetChannelsAwaitingSnapshot().Count);
            Assert.IsTrue(_registry.AllSnapshotsReceived);
        }

        [Test]
        public void Subscribe_NullChannel_Throws() {
            Assert.Throws<ArgumentNullException>(() => _registry.Subscribe(null));
        }

        [Test]
        public void Subscribe_EmptyChannel_Throws() {
            Assert.Throws<ArgumentException>(() => _registry.Subscribe(string.Empty));
        }

        [Test]
        public void MarkSnapshotReceived_NullChannel_Throws() {
            Assert.Throws<ArgumentNullException>(() => _registry.MarkSnapshotReceived(null));
        }

        [Test]
        public void MarkSnapshotReceived_EmptyChannel_Throws() {
            Assert.Throws<ArgumentException>(() => _registry.MarkSnapshotReceived(string.Empty));
        }

        [Test]
        public void MarkSubscribeFailed_NullChannel_Throws() {
            Assert.Throws<ArgumentNullException>(() => _registry.MarkSubscribeFailed(null));
        }

        [Test]
        public void MarkSubscribeFailed_EmptyChannel_Throws() {
            Assert.Throws<ArgumentException>(() => _registry.MarkSubscribeFailed(string.Empty));
        }

        [Test]
        public void GetSubscriptionState_NullChannel_Throws() {
            Assert.Throws<ArgumentNullException>(() => _registry.GetSubscriptionState(null));
        }
    }
}
