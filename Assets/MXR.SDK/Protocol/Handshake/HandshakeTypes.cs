using System;

namespace MXR.SDK.Protocol.Handshake {
    /// <summary>
    /// Whether the hello exchange produced a usable session.
    /// </summary>
    public enum Compatibility {
        /// <summary>
        /// No common protocol version or payload format.
        /// </summary>
        Incompatible,

        /// <summary>
        /// Protocol version and payload format were agreed.
        /// </summary>
        Compatible
    }

    /// <summary>
    /// Why a handshake is incompatible. Tells support which side needs an update.
    /// </summary>
    public enum IncompatibleReason {
        /// <summary>
        /// The handshake is compatible.
        /// </summary>
        None,

        /// <summary>
        /// The Admin App protocol is below the SDK minimum. Update the Admin App.
        /// </summary>
        AdminAppTooOld,

        /// <summary>
        /// The SDK protocol is below the Admin App minimum. Update the SDK.
        /// </summary>
        SdkTooOld,

        /// <summary>
        /// The peers share no payload format this SDK can decode.
        /// </summary>
        NoCommonFormat
    }

    /// <summary>
    /// Outcome of negotiating a client and server hello.
    /// </summary>
    public sealed class HandshakeResult {
        /// <summary>
        /// Whether the peers can proceed after hello.
        /// </summary>
        public Compatibility Compatibility { get; }

        /// <summary>
        /// Why the handshake is incompatible; <see cref="IncompatibleReason.None"/> when compatible.
        /// </summary>
        public IncompatibleReason Reason { get; }

        /// <summary>
        /// Human-readable reason when <see cref="Compatibility"/> is incompatible.
        /// </summary>
        public string Error { get; }

        /// <summary>
        /// Agreed protocol version for the connection.
        /// </summary>
        public int EffectiveProtocol { get; }

        /// <summary>
        /// Negotiated post-handshake payload format name.
        /// </summary>
        public string SelectedFormat { get; }

        /// <summary>
        /// Codec to use for envelopes after hello. Non-null when compatible.
        /// </summary>
        public IEnvelopeCodec PostHelloCodec { get; }

        HandshakeResult(
            Compatibility compatibility,
            IncompatibleReason reason,
            string error,
            int effectiveProtocol,
            string selectedFormat,
            IEnvelopeCodec postHelloCodec
        ) {
            Compatibility = compatibility;
            Reason = reason;
            Error = error;
            EffectiveProtocol = effectiveProtocol;
            SelectedFormat = selectedFormat;
            PostHelloCodec = postHelloCodec;
        }

        /// <summary>
        /// Creates a compatible handshake result.
        /// </summary>
        public static HandshakeResult CreateCompatible(
            int effectiveProtocol,
            string selectedFormat,
            IEnvelopeCodec postHelloCodec
        ) {
            if (selectedFormat == null) {
                throw new ArgumentNullException(nameof(selectedFormat));
            }

            if (postHelloCodec == null) {
                throw new ArgumentNullException(nameof(postHelloCodec));
            }

            return new HandshakeResult(
                Compatibility.Compatible,
                IncompatibleReason.None,
                null,
                effectiveProtocol,
                selectedFormat,
                postHelloCodec);
        }

        /// <summary>
        /// Creates an incompatible handshake result.
        /// </summary>
        public static HandshakeResult CreateIncompatible(IncompatibleReason reason, string error) {
            if (reason == IncompatibleReason.None) {
                throw new ArgumentException("An incompatible result needs a reason.", nameof(reason));
            }

            if (error == null) {
                throw new ArgumentNullException(nameof(error));
            }

            return new HandshakeResult(
                Compatibility.Incompatible,
                reason,
                error,
                0,
                null,
                null);
        }
    }
}
