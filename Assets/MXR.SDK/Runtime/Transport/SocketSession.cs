using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using MXR.SDK.Protocol;
using MXR.SDK.Protocol.Framing;
using MXR.SDK.Protocol.Guarantee;
using MXR.SDK.Protocol.Handshake;
using MXR.SDK.Protocol.Subscriptions;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace MXR.SDK.Transport {
    /// <summary>
    /// One socket session with the Admin App.
    /// <see cref="Handshake"/> agrees the codec. <see cref="Subscribe"/> joins a state channel.
    /// <see cref="TryReadNext"/> yields each later ack, snapshot, push, or close.
    /// </summary>
    public sealed class SocketSession : IDisposable {
        const string TAG = "SocketSession";

        // Same close reasons as the Admin App SocketIpcTransport.
        internal const string Epipe = "epipe";
        internal const string Io = "io";
        internal const string EofMidFrame = "eof-mid-frame";
        internal const string EofBeforeHello = "eof-before-hello";
        internal const string PeerEof = "peer-eof";
        internal const string BadEnvelope = "bad-envelope";
        internal const string OversizeFrame = "OVERSIZE_FRAME";
        internal const string ZeroLengthBody = "ZERO_LENGTH_BODY";

        const int EpipeErrno = 32;

        static readonly JsonSerializerSettings ErrorSettings = new JsonSerializerSettings {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        readonly Stream _stream;
        readonly AckTracker _acks;
        readonly ReplyTracker _replies;
        readonly SubscriptionRegistry _registry;
        readonly FrameReader _reader;
        readonly object _writeLock = new object();
        readonly object _idLock = new object();
        readonly Dictionary<long, string> _subscribeIds = new Dictionary<long, string>();
        readonly HashSet<string> _permanentFailures = new HashSet<string>();

        long _outboundId = 1;
        int _protocolVersion = ProtocolVersion.Current;
        IEnvelopeCodec _postHelloCodec;
        HandshakeResult _handshake;
        HandshakeResult _incompatible;
        string _closeReason;
        bool _helloFinished;
        bool _closed;
        bool _closeDelivered;
        bool _disposed;

        /// <summary>
        /// Creates a session over an already connected stream.
        /// Production passes a <see cref="LibcSocketStream"/>. Tests pass any stream.
        /// </summary>
        public SocketSession(Stream stream, AckTracker acks, ReplyTracker replies, SubscriptionRegistry registry) {
            if (stream == null) {
                throw new ArgumentNullException(nameof(stream));
            }

            if (acks == null) {
                throw new ArgumentNullException(nameof(acks));
            }

            if (replies == null) {
                throw new ArgumentNullException(nameof(replies));
            }

            if (registry == null) {
                throw new ArgumentNullException(nameof(registry));
            }

            _stream = stream;
            _acks = acks;
            _replies = replies;
            _registry = registry;
            _reader = new FrameReader(stream);
        }

        /// <summary>
        /// Writes <c>sys/hello</c>, waits for <c>sys/ack</c>, then the correlated reply or error.
        /// The ack is not the hello result. An incompatible result stops the session.
        /// </summary>
        public HandshakeResult Handshake() {
            ThrowIfDisposed();
            if (_handshake != null) {
                throw new InvalidOperationException("Handshake already finished.");
            }

            var clientHello = HandshakeNegotiator.CreateClientHello(HandshakeNegotiator.DefaultClientImpl);
            var helloId = SendHello(clientHello);
            var answer = ReadHelloAnswer(helloId);
            _handshake = FinishHello(clientHello, helloId, answer);
            return _handshake;
        }

        /// <summary>
        /// Joins one state channel. A channel already refused with <c>retryable: false</c> is not sent again.
        /// </summary>
        public void Subscribe(string channel) {
            ThrowIfDisposed();
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }

            if (channel.Length == 0) {
                throw new ArgumentException("Channel is required.", nameof(channel));
            }

            if (_handshake == null || _handshake.Compatibility != Compatibility.Compatible) {
                throw new InvalidOperationException("Subscribe requires a compatible handshake.");
            }

            if (_closed) {
                throw new IOException(_closeReason);
            }

            if (_permanentFailures.Contains(channel)) {
                return;
            }

            if (_registry.Subscribe(channel) != SubscribeResult.Subscribed) {
                return;
            }

            var id = SendSubscribe(channel);
            _subscribeIds[id] = channel;
        }

        /// <summary>
        /// Reads the next inbound ack, snapshot, push, error, or close.
        /// An inbound <c>sys/ack</c> is <see cref="SessionEventKind.PeerAck"/> and never a snapshot.
        /// </summary>
        public bool TryReadNext(out SessionEvent next) {
            next = null;
            if (_disposed && !_closed) {
                return false;
            }

            if (_closeDelivered) {
                return false;
            }

            if (_incompatible != null) {
                next = new SessionEvent(SessionEventKind.Incompatible, null, null, _incompatible.Error);
                _incompatible = null;
                return true;
            }

            if (_closed) {
                _closeDelivered = true;
                next = new SessionEvent(SessionEventKind.Closed, null, null, _closeReason);
                return true;
            }

            var read = ReadFrame();
            var eof = read as FrameRead.Eof;
            if (eof != null) {
                var reason = EofReason(eof.MidFrame);
                Close(reason);
                _closeDelivered = true;
                next = new SessionEvent(SessionEventKind.Closed, null, null, reason);
                return true;
            }

            if (read is FrameRead.Stopped) {
                _closeDelivered = true;
                next = new SessionEvent(SessionEventKind.Closed, null, null, _closeReason);
                return true;
            }

            next = OnFrame(((FrameRead.Decoded)read).Body);
            return true;
        }

        /// <summary>
        /// Handshakes, subscribes to the three state channels, and reads until the socket closes.
        /// </summary>
        public void Run(Action<string> log) {
            if (log == null) {
                throw new ArgumentNullException(nameof(log));
            }

            try {
                var handshake = Handshake();
                if (handshake.Compatibility != Compatibility.Compatible) {
                    log(TAG + " incompatible " + handshake.Error);
                    return;
                }

                Subscribe(StateChannelNames.RuntimeSettings);
                Subscribe(StateChannelNames.DeviceStatus);
                Subscribe(StateChannelNames.DeviceData);

                SessionEvent next;
                while (TryReadNext(out next)) {
                    Log(log, next);
                    if (next.Kind == SessionEventKind.Closed || next.Kind == SessionEventKind.Incompatible) {
                        return;
                    }
                }
            } catch (Exception ex) {
                log(TAG + " " + ex.Message);
            }
        }

        /// <summary>
        /// Closes the stream. <see cref="LibcSocketStream.Close"/> shuts the fd down.
        /// </summary>
        public void Dispose() {
            if (_disposed) {
                return;
            }

            _disposed = true;
            try {
                _stream.Close();
            } catch (IOException) {
            }
        }

        long SendHello(HelloPayload clientHello) {
            var id = NextId();
            var hello = HandshakeNegotiator.CreateClientHelloEnvelope(clientHello, id, _protocolVersion);
            SendTracked(hello, EnvelopeCodecRegistry.HelloCodec);
            return id;
        }

        Envelope ReadHelloAnswer(long helloId) {
            Envelope answer = null;
            var sawAck = false;
            while (answer == null || !sawAck) {
                var envelope = RequireHelloEnvelope();
                if (envelope.Type == ProtocolTypes.Ack && envelope.ReplyTo == helloId) {
                    if (!sawAck) {
                        _acks.CompleteAckFromReceiver(new ReceiverAck(helloId));
                        sawAck = true;
                    }

                    continue;
                }

                if (IsHelloAnswer(envelope, helloId) && answer == null) {
                    answer = envelope;
                } else if (!IsHelloAnswer(envelope, helloId)) {
                    Close(Io);
                    throw new IOException("Unexpected envelope during hello: " + envelope.Type);
                }
            }

            return answer;
        }

        HandshakeResult FinishHello(HelloPayload clientHello, long helloId, Envelope answer) {
            _replies.CompleteReplyFromReceiver(new ReceiverReply(helloId, answer));

            string helloError;
            HelloPayload serverHello;
            if (!HandshakeNegotiator.TryReadServerHello(answer, helloId, out serverHello, out helloError)) {
                AckIfRequired(answer, EnvelopeCodecRegistry.HelloCodec);
                var rejected = HandshakeResult.CreateIncompatible(IncompatibilityReason.AdminAppTooOld, helloError);
                _incompatible = rejected;
                return rejected;
            }

            var result = HandshakeNegotiator.Negotiate(clientHello, serverHello);
            if (result.Compatibility == Compatibility.Compatible) {
                _postHelloCodec = result.PostHelloCodec;
                _protocolVersion = result.EffectiveProtocol;
                _helloFinished = true;
                AckIfRequired(answer, _postHelloCodec);
                return result;
            }

            _incompatible = result;
            AckIfRequired(answer, EnvelopeCodecRegistry.HelloCodec);
            return result;
        }

        long SendSubscribe(string channel) {
            var id = NextId();
            var envelope = new Envelope {
                V = _protocolVersion,
                Type = ProtocolTypes.Subscribe,
                Id = id,
                Channel = channel
            };
            SendTracked(envelope, _postHelloCodec);
            return id;
        }

        Envelope RequireHelloEnvelope() {
            var read = ReadFrame();
            var eof = read as FrameRead.Eof;
            if (eof != null) {
                var reason = EofReason(eof.MidFrame);
                Close(reason);
                throw new IOException(reason);
            }

            if (read is FrameRead.Stopped) {
                throw new IOException(_closeReason);
            }

            var envelope = Decode(((FrameRead.Decoded)read).Body, EnvelopeCodecRegistry.HelloCodec);
            if (envelope == null) {
                throw new IOException(_closeReason);
            }

            return envelope;
        }

        SessionEvent OnFrame(byte[] body) {
            var codec = _postHelloCodec ?? EnvelopeCodecRegistry.HelloCodec;
            var envelope = Decode(body, codec);
            if (envelope == null) {
                _closeDelivered = true;
                return new SessionEvent(SessionEventKind.Closed, null, null, _closeReason);
            }

            if (envelope.Type == ProtocolTypes.Ack) {
                if (envelope.ReplyTo.HasValue) {
                    _acks.CompleteAckFromReceiver(new ReceiverAck(envelope.ReplyTo.Value));
                }

                return new SessionEvent(SessionEventKind.PeerAck, null, null, null);
            }

            AckIfRequired(envelope, codec);

            if (envelope.Type == ProtocolTypes.Reply || envelope.Type == ProtocolTypes.Err) {
                if (envelope.ReplyTo.HasValue) {
                    _replies.CompleteReplyFromReceiver(new ReceiverReply(envelope.ReplyTo.Value, envelope));
                }

                return ApplyAnswer(envelope);
            }

            if (envelope.Type == ProtocolTypes.Push) {
                return new SessionEvent(SessionEventKind.Push, envelope.Channel, envelope.Payload, null);
            }

            Close(Io);
            _closeDelivered = true;
            return new SessionEvent(SessionEventKind.Closed, null, null, Io);
        }

        SessionEvent ApplyAnswer(Envelope envelope) {
            string channel = null;
            if (envelope.ReplyTo.HasValue) {
                _subscribeIds.TryGetValue(envelope.ReplyTo.Value, out channel);
            }

            if (envelope.Type == ProtocolTypes.Reply) {
                if (channel != null) {
                    _registry.MarkSnapshotReceived(channel);
                    _subscribeIds.Remove(envelope.ReplyTo.Value);
                }

                return new SessionEvent(SessionEventKind.Snapshot, channel, envelope.Payload, null);
            }

            ErrorPayload errorPayload;
            var parsed = TryReadError(envelope.Payload, out errorPayload);
            if (channel != null) {
                _registry.MarkSubscribeFailed(channel);
                _subscribeIds.Remove(envelope.ReplyTo.Value);
                if (!parsed || !errorPayload.Retryable) {
                    _permanentFailures.Add(channel);
                }
            }

            return new SessionEvent(SessionEventKind.ChannelFailed, channel, null, FormatChannelError(errorPayload));
        }

        FrameRead ReadFrame() {
            return _reader.ReadFrame(_closed, Close, FailureReason);
        }

        void AckIfRequired(Envelope envelope, IEnvelopeCodec codec) {
            if (!AckFactory.RequiresAck(envelope) || !envelope.Id.HasValue) {
                return;
            }

            // An ack has no id of its own. The peer does not ack it, so it is not tracked.
            WriteEnvelope(AckFactory.CreateAck(envelope.Id.Value, envelope.V), codec);
        }

        void SendTracked(Envelope envelope, IEnvelopeCodec codec) {
            _acks.RegisterSent(envelope);
            _replies.RegisterRequest(envelope);
            WriteEnvelope(envelope, codec);
        }

        void WriteEnvelope(Envelope envelope, IEnvelopeCodec codec) {
            Write(FrameCodec.Encode(codec.Serialize(envelope)));
        }

        void Write(byte[] bytes) {
            IOException failure = null;
            lock (_writeLock) {
                if (_closed) {
                    return;
                }

                try {
                    _stream.Write(bytes, 0, bytes.Length);
                } catch (IOException ex) {
                    failure = ex;
                }
            }

            if (failure != null) {
                Close(FailureReason(failure));
                throw failure;
            }
        }

        void Close(string reason) {
            if (_closed) {
                return;
            }

            _closed = true;
            _closeReason = reason;
            try {
                _stream.Close();
            } catch (IOException) {
            }
        }

        Envelope Decode(byte[] body, IEnvelopeCodec codec) {
            string error;
            Envelope envelope;
            if (!codec.TryDeserialize(body, out envelope, out error)) {
                Close(string.IsNullOrEmpty(error) ? BadEnvelope : error);
                return null;
            }

            return envelope;
        }

        long NextId() {
            lock (_idLock) {
                var id = _outboundId;
                _outboundId++;
                return id;
            }
        }

        string EofReason(bool midFrame) {
            if (midFrame) {
                return EofMidFrame;
            }

            return _helloFinished ? PeerEof : EofBeforeHello;
        }

        static string FailureReason(IOException exception) {
            var message = exception.Message;
            if (message != null && message.IndexOf("errno=" + EpipeErrno, StringComparison.Ordinal) >= 0) {
                return Epipe;
            }

            return Io;
        }

        static bool IsHelloAnswer(Envelope envelope, long helloId) {
            return (envelope.Type == ProtocolTypes.Reply || envelope.Type == ProtocolTypes.Err)
                && envelope.ReplyTo == helloId;
        }

        void ThrowIfDisposed() {
            if (_disposed) {
                throw new ObjectDisposedException(nameof(SocketSession));
            }
        }

        static bool TryReadError(byte[] payload, out ErrorPayload error) {
            error = null;
            if (payload == null || payload.Length == 0) {
                return false;
            }

            try {
                error = JsonConvert.DeserializeObject<ErrorPayload>(Encoding.UTF8.GetString(payload), ErrorSettings);
                return error != null;
            } catch (DecoderFallbackException) {
                return false;
            } catch (JsonException) {
                return false;
            }
        }

        static void Log(Action<string> log, SessionEvent next) {
            if (next.Kind == SessionEventKind.Snapshot) {
                log(TAG + " snapshot " + next.Channel + " " + GetString(next.Payload));
                return;
            }

            if (next.Kind == SessionEventKind.Push) {
                log(TAG + " push " + next.Channel + " " + GetString(next.Payload));
                return;
            }

            if (next.Kind == SessionEventKind.Closed) {
                log(TAG + " closed " + next.Error);
                return;
            }

            if (next.Kind == SessionEventKind.ChannelFailed || next.Kind == SessionEventKind.Incompatible) {
                log(TAG + " " + next.Kind + " " + next.Channel + " " + next.Error);
            }
        }

        static string GetString(byte[] payload) {
            if (payload == null || payload.Length == 0) {
                return "";
            }

            return Encoding.UTF8.GetString(payload);
        }

        static string FormatChannelError(ErrorPayload error) {
            if (error == null || string.IsNullOrEmpty(error.Code)) {
                return "sys/err";
            }

            if (string.IsNullOrEmpty(error.Message)) {
                return error.Code;
            }

            return error.Code + ": " + error.Message;
        }
    }
}
