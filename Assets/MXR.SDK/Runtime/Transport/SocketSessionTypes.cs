namespace MXR.SDK.Transport {
    /// <summary>
    /// What <see cref="SocketSession.TryReadNext"/> yielded.
    /// </summary>
    public enum SessionEventKind {
        /// <summary>
        /// An inbound <c>sys/ack</c>. This is not a hello result or a snapshot.
        /// </summary>
        PeerAck,

        /// <summary>
        /// The subscribe reply snapshot for one channel.
        /// </summary>
        Snapshot,

        /// <summary>
        /// A <c>state/push</c> on a subscribed channel.
        /// </summary>
        Push,

        /// <summary>
        /// A subscribe was rejected.
        /// </summary>
        ChannelFailed,

        /// <summary>
        /// Hello did not produce a usable session.
        /// </summary>
        Incompatible,

        /// <summary>
        /// The socket is closed. <see cref="SessionEvent.Error"/> is the close reason.
        /// </summary>
        Closed
    }

    /// <summary>
    /// One inbound outcome from <see cref="SocketSession"/>.
    /// </summary>
    public sealed class SessionEvent {
        /// <summary>
        /// Creates a session event.
        /// </summary>
        public SessionEvent(SessionEventKind kind, string channel, byte[] payload, string error) {
            Kind = kind;
            Channel = channel;
            Payload = payload;
            Error = error;
        }

        /// <summary>
        /// Which outcome this is.
        /// </summary>
        public SessionEventKind Kind { get; }

        /// <summary>
        /// Channel for a snapshot, push, or failed subscribe.
        /// </summary>
        public string Channel { get; }

        /// <summary>
        /// Snapshot or push JSON bytes. Opaque at this layer.
        /// </summary>
        public byte[] Payload { get; }

        /// <summary>
        /// Incompatible reason, channel error, or close reason.
        /// </summary>
        public string Error { get; }
    }
}
