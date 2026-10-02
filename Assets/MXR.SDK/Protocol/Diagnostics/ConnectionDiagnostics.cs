using System;
using System.Collections.Generic;

using MXR.SDK.Protocol.Handshake;

namespace MXR.SDK.Protocol.Diagnostics {
    /// <summary>
    /// Live connection observability for the SDK client.
    /// Exposes two independent signals — transport up vs data ready — plus handshake
    /// fields and per-channel delivery counters.
    /// </summary>
    /// <remarks>
    /// Counters are diagnostics only. They are never sent on the wire, never written
    /// to analytics, and are not stripped in release builds. Thread-safe for transport
    /// reader/writer paths and main-thread reads.
    /// </remarks>
    public sealed class ConnectionDiagnostics {
        private readonly object _sync = new object();
        private readonly Dictionary<string, MutableCounter> _counters = new Dictionary<string, MutableCounter>();

        private TransportState _transportState = TransportState.Disconnected;
        private Compatibility _compatibility = Compatibility.Incompatible;
        private IncompatibleReason _reason = IncompatibleReason.None;
        private string _error;
        private int? _effectiveProtocol;
        private string _selectedFormat;
        private string _remoteImpl;
        private bool _isDataReady;

        /// <summary>
        /// Whether the socket is up.
        /// </summary>
        public TransportState TransportState {
            get {
                lock (_sync) {
                    return _transportState;
                }
            }
        }

        /// <summary>
        /// True only when <see cref="TransportState"/> is connected.
        /// Independent of <see cref="IsDataReady"/>.
        /// </summary>
        public bool IsTransportUp {
            get {
                lock (_sync) {
                    return _transportState == TransportState.Connected;
                }
            }
        }

        /// <summary>
        /// Handshake or pre-bind compatibility. Defaults to incompatible so an unset session
        /// never reads as compatible.
        /// </summary>
        public Compatibility Compatibility {
            get {
                lock (_sync) {
                    return _compatibility;
                }
            }
        }

        /// <summary>
        /// Why the session is incompatible; <see cref="IncompatibleReason.None"/> when compatible.
        /// </summary>
        public IncompatibleReason IncompatibilityReason {
            get {
                lock (_sync) {
                    return _reason;
                }
            }
        }

        /// <summary>
        /// Human-readable reason when <see cref="Compatibility"/> is incompatible.
        /// </summary>
        public string Error {
            get {
                lock (_sync) {
                    return _error;
                }
            }
        }

        /// <summary>
        /// Agreed protocol version. Null until a compatible handshake is applied.
        /// </summary>
        public int? EffectiveProtocol {
            get {
                lock (_sync) {
                    return _effectiveProtocol;
                }
            }
        }

        /// <summary>
        /// Negotiated post-handshake payload format. Null until a compatible handshake is applied.
        /// </summary>
        public string SelectedFormat {
            get {
                lock (_sync) {
                    return _selectedFormat;
                }
            }
        }

        /// <summary>
        /// Server hello <c>impl</c> label. Null until a handshake is applied.
        /// </summary>
        public string RemoteImpl {
            get {
                lock (_sync) {
                    return _remoteImpl;
                }
            }
        }

        /// <summary>
        /// Whether the first snapshot has arrived on every subscribed channel.
        /// Independent of <see cref="IsTransportUp"/>. False until the caller sets it.
        /// </summary>
        public bool IsDataReady {
            get {
                lock (_sync) {
                    return _isDataReady;
                }
            }
        }

        /// <summary>
        /// Returns the delivery counters for a channel. Unknown channels return zeros
        /// and create an entry so later increments accumulate on the same key.
        /// </summary>
        public ChannelDeliveryCounter GetCounter(string channel) {
            ValidateChannel(channel);

            lock (_sync) {
                return GetOrCreateCounter(channel).ToSnapshot();
            }
        }

        /// <summary>
        /// Snapshot of every channel that has a counter entry.
        /// </summary>
        public IReadOnlyDictionary<string, ChannelDeliveryCounter> GetAllCounters() {
            lock (_sync) {
                var copy = new Dictionary<string, ChannelDeliveryCounter>(_counters.Count);
                foreach (var entry in _counters) {
                    copy[entry.Key] = entry.Value.ToSnapshot();
                }

                return copy;
            }
        }

        /// <summary>
        /// Records socket transport state. Does not change readiness or handshake fields.
        /// </summary>
        public void SetTransportState(TransportState state) {
            lock (_sync) {
                _transportState = state;
            }
        }

        /// <summary>
        /// Records a completed hello exchange.
        /// Compatible copies protocol, format, and remote impl. Incompatible sets
        /// compatibility, reason, and error, and leaves protocol and format null.
        /// </summary>
        public void ApplyHandshake(HandshakeResult result, HelloPayload serverHello) {
            if (result == null) {
                throw new ArgumentNullException(nameof(result));
            }

            if (serverHello == null) {
                throw new ArgumentNullException(nameof(serverHello));
            }

            lock (_sync) {
                _compatibility = result.Compatibility;
                _reason = result.Reason;
                _remoteImpl = serverHello.Impl;

                if (result.Compatibility == Compatibility.Compatible) {
                    _error = null;
                    _effectiveProtocol = result.EffectiveProtocol;
                    _selectedFormat = result.SelectedFormat;
                } else {
                    _error = result.Error;
                    _effectiveProtocol = null;
                    _selectedFormat = null;
                }
            }
        }

        /// <summary>
        /// Records that the session is incompatible, including pre-bind rejection
        /// when no hello is exchanged.
        /// </summary>
        /// <remarks>
        /// Clears protocol, format, and remote impl. The compiled-in minimum version
        /// (and whether it is a <c>versionCode</c> or a semantic version) is a runtime
        /// bootstrap concern, not a protocol constant.
        /// </remarks>
        public void SetIncompatible(IncompatibleReason reason, string error) {
            if (reason == IncompatibleReason.None) {
                throw new ArgumentException("An incompatible result needs a reason.", nameof(reason));
            }

            if (error == null) {
                throw new ArgumentNullException(nameof(error));
            }

            lock (_sync) {
                _compatibility = Compatibility.Incompatible;
                _reason = reason;
                _error = error;
                _effectiveProtocol = null;
                _selectedFormat = null;
                _remoteImpl = null;
            }
        }

        /// <summary>
        /// Records whether subscribed channels have their first snapshots.
        /// Does not change transport state.
        /// </summary>
        public void SetDataReady(bool ready) {
            lock (_sync) {
                _isDataReady = ready;
            }
        }

        /// <summary>
        /// Increments the sent tally for a channel. Creates the channel entry if needed.
        /// </summary>
        public void RecordSent(string channel) {
            ValidateChannel(channel);

            lock (_sync) {
                GetOrCreateCounter(channel).Sent++;
            }
        }

        /// <summary>
        /// Increments the acked tally for a channel. Creates the channel entry if needed.
        /// </summary>
        public void RecordAcked(string channel) {
            ValidateChannel(channel);

            lock (_sync) {
                GetOrCreateCounter(channel).Acked++;
            }
        }

        /// <summary>
        /// Increments the replied tally for a channel. Creates the channel entry if needed.
        /// </summary>
        public void RecordReplied(string channel) {
            ValidateChannel(channel);

            lock (_sync) {
                GetOrCreateCounter(channel).Replied++;
            }
        }

        /// <summary>
        /// Increments the permanent-reject tally for a channel. Creates the channel entry if needed.
        /// </summary>
        public void RecordRejectedPermanent(string channel) {
            ValidateChannel(channel);

            lock (_sync) {
                GetOrCreateCounter(channel).RejectedPermanent++;
            }
        }

        /// <summary>
        /// Increments the retryable-reject tally for a channel. Creates the channel entry if needed.
        /// </summary>
        public void RecordRejectedRetryable(string channel) {
            ValidateChannel(channel);

            lock (_sync) {
                GetOrCreateCounter(channel).RejectedRetryable++;
            }
        }

        /// <summary>
        /// Clears transport, readiness, handshake fields, and counters for a new session.
        /// </summary>
        public void Reset() {
            lock (_sync) {
                _transportState = TransportState.Disconnected;
                _compatibility = Compatibility.Incompatible;
                _reason = IncompatibleReason.None;
                _error = null;
                _effectiveProtocol = null;
                _selectedFormat = null;
                _remoteImpl = null;
                _isDataReady = false;
                _counters.Clear();
            }
        }

        private MutableCounter GetOrCreateCounter(string channel) {
            if (!_counters.TryGetValue(channel, out var counter)) {
                counter = new MutableCounter();
                _counters[channel] = counter;
            }

            return counter;
        }

        private static void ValidateChannel(string channel) {
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }

            if (channel.Length == 0) {
                throw new ArgumentException("Channel is required.", nameof(channel));
            }
        }

        private sealed class MutableCounter {
            public int Sent;
            public int Acked;
            public int Replied;
            public int RejectedPermanent;
            public int RejectedRetryable;

            public ChannelDeliveryCounter ToSnapshot() {
                return new ChannelDeliveryCounter(
                    Sent,
                    Acked,
                    Replied,
                    RejectedPermanent,
                    RejectedRetryable);
            }
        }
    }
}
