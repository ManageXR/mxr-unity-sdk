using System;

using MXR.SDK.Protocol;

namespace MXR.SDK.Protocol.Guarantee {
    /// <summary>
    /// Builds id-less <c>sys/ack</c> envelopes for the receiver-side delivered guarantee.
    /// </summary>
    public static class AckFactory {
        /// <summary>
        /// Returns whether the receiver must send a transport ack for this envelope.
        /// </summary>
        public static bool RequiresAck(Envelope envelope) {
            if (envelope == null) {
                throw new ArgumentNullException(nameof(envelope));
            }

            if (envelope.Type == ProtocolTypes.Ack) {
                return false;
            }

            return envelope.Id.HasValue;
        }

        /// <summary>
        /// Creates an id-less <c>sys/ack</c> correlating to a prior peer id.
        /// </summary>
        public static Envelope CreateAck(long replyTo, int protocolVersion) {
            return new Envelope {
                V = protocolVersion,
                Type = ProtocolTypes.Ack,
                ReplyTo = replyTo
            };
        }
    }
}
