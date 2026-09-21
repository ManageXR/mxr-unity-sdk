using System;
using System.Collections.Generic;

namespace MXR.SDK.Protocol.Subscriptions {
    /// <summary>
    /// Tracks snapshot bootstrap progress for subscribed channels on the current connection.
    /// Thread-safe for transport reader/writer paths.
    /// </summary>
    public sealed class SubscriptionRegistry {
        readonly object _sync = new object();
        readonly Dictionary<string, SubscriptionState> _states = new Dictionary<string, SubscriptionState>();

        /// <summary>
        /// Records that a channel subscribe is about to be sent and is awaiting its snapshot reply.
        /// A channel whose previous subscribe failed is re-armed and returns <see cref="SubscribeResult.Subscribed"/>.
        /// </summary>
        public SubscribeResult Subscribe(string channel) {
            ValidateChannel(channel);

            lock (_sync) {
                if (_states.TryGetValue(channel, out var state) && state != SubscriptionState.SubscribeFailed) {
                    return SubscribeResult.AlreadySubscribed;
                }

                _states[channel] = SubscriptionState.AwaitingSnapshot;
                return SubscribeResult.Subscribed;
            }
        }

        /// <summary>
        /// Records that the subscribe reply snapshot arrived for a channel.
        /// </summary>
        public SnapshotReceivedResult MarkSnapshotReceived(string channel) {
            ValidateChannel(channel);

            lock (_sync) {
                if (!_states.TryGetValue(channel, out var state)) {
                    return SnapshotReceivedResult.NotSubscribed;
                }

                if (state == SubscriptionState.ReceivedSnapshot) {
                    return SnapshotReceivedResult.AlreadyReceived;
                }

                _states[channel] = SubscriptionState.ReceivedSnapshot;
                return SnapshotReceivedResult.Received;
            }
        }

        /// <summary>
        /// Records that a channel subscribe was rejected or its reply deadline passed.
        /// </summary>
        public SubscribeFailedResult MarkSubscribeFailed(string channel) {
            ValidateChannel(channel);

            lock (_sync) {
                if (!_states.TryGetValue(channel, out var state)) {
                    return SubscribeFailedResult.NotSubscribed;
                }

                switch (state) {
                    case SubscriptionState.ReceivedSnapshot:
                        return SubscribeFailedResult.AlreadyReceived;
                    case SubscriptionState.SubscribeFailed:
                        return SubscribeFailedResult.AlreadyFailed;
                }

                _states[channel] = SubscriptionState.SubscribeFailed;
                return SubscribeFailedResult.Failed;
            }
        }

        /// <summary>
        /// Snapshot bootstrap progress for a channel on the current connection.
        /// </summary>
        public SubscriptionState GetSubscriptionState(string channel) {
            ValidateChannel(channel);

            lock (_sync) {
                return _states.TryGetValue(channel, out var state)
                    ? state
                    : SubscriptionState.NoSubscriptionFound;
            }
        }

        /// <summary>
        /// Whether at least one channel is subscribed and every subscribed channel has its snapshot.
        /// False before any subscribe, after <see cref="Reset"/>, and while any channel is awaiting or failed.
        /// </summary>
        public bool AllSnapshotsReceived {
            get {
                lock (_sync) {
                    if (_states.Count == 0) {
                        return false;
                    }

                    foreach (var state in _states.Values) {
                        if (state != SubscriptionState.ReceivedSnapshot) {
                            return false;
                        }
                    }

                    return true;
                }
            }
        }

        /// <summary>
        /// Returns subscribed channels still waiting for their first snapshot.
        /// </summary>
        public IReadOnlyList<string> GetChannelsAwaitingSnapshot() {
            return GetChannelsIn(SubscriptionState.AwaitingSnapshot);
        }

        /// <summary>
        /// Returns channels whose subscribe failed and have not received a snapshot since.
        /// </summary>
        public IReadOnlyList<string> GetFailedChannels() {
            return GetChannelsIn(SubscriptionState.SubscribeFailed);
        }

        /// <summary>
        /// Clears all subscription bootstrap state for the current connection.
        /// </summary>
        public void Reset() {
            lock (_sync) {
                _states.Clear();
            }
        }

        IReadOnlyList<string> GetChannelsIn(SubscriptionState target) {
            lock (_sync) {
                var channels = new List<string>();
                foreach (var entry in _states) {
                    if (entry.Value == target) {
                        channels.Add(entry.Key);
                    }
                }

                return channels;
            }
        }

        static void ValidateChannel(string channel) {
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }

            if (channel.Length == 0) {
                throw new ArgumentException("Channel is required.", nameof(channel));
            }
        }
    }
}
