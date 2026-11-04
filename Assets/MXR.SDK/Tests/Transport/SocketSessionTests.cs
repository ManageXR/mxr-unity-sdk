using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using MXR.SDK.Protocol;
using MXR.SDK.Protocol.Framing;
using MXR.SDK.Protocol.Guarantee;
using MXR.SDK.Protocol.Handshake;
using MXR.SDK.Protocol.Subscriptions;
using MXR.SDK.Transport;

using NUnit.Framework;

namespace MXR.SDK.Transport.Tests {
    public class SocketSessionTests {
        ManualProtocolClock _clock;
        AckTracker _acks;
        ReplyTracker _replies;
        SubscriptionRegistry _registry;
        ScriptedPipe _pipe;
        SocketSession _session;
        IEnvelopeCodec _codec;
        long _serverId;

        [SetUp]
        public void SetUp() {
            _clock = new ManualProtocolClock(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            _acks = new AckTracker(_clock);
            _replies = new ReplyTracker(_clock);
            _registry = new SubscriptionRegistry();
            _pipe = new ScriptedPipe();
            _session = new SocketSession(_pipe, _acks, _replies, _registry);
            _codec = EnvelopeCodecRegistry.HelloCodec;
            _serverId = 500;
        }

        [TearDown]
        public void TearDown() {
            _session.Dispose();
        }

        [Test]
        public void Handshake_AckThenReply_DoesNotTreatAckAsHello() {
            var serverHello = ServerHelloPayload("mighty-admin-app/13");
            var registeredOnWrite = false;
            _pipe.OnWrite = delegate {
                if (registeredOnWrite) {
                    return;
                }

                registeredOnWrite = true;
                Assert.AreEqual(1, _acks.GetPendingAcks().Count);
                Assert.AreEqual(1, _replies.GetPendingReplies().Count);
            };

            _pipe.Enqueue(Frame(Ack(1)));
            _pipe.Enqueue(Frame(Reply(1, 500, serverHello)));

            var result = _session.Handshake();

            Assert.IsTrue(registeredOnWrite);
            Assert.AreEqual(Compatibility.Compatible, result.Compatibility);
            Assert.AreEqual(ProtocolVersion.Current, result.EffectiveProtocol);
            Assert.AreSame(EnvelopeCodecRegistry.ForFormat(JsonEnvelopeCodec.FormatName), result.PostHelloCodec);
            Assert.AreEqual(0, _acks.GetPendingAcks().Count);
            Assert.AreEqual(0, _replies.GetPendingReplies().Count);

            var written = DecodeAll(_pipe.Written());
            Assert.AreEqual(ProtocolTypes.Hello, written[0].Type);
            Assert.AreEqual(1, written[0].Id);
            Assert.AreEqual(ProtocolTypes.Ack, written[1].Type);
            Assert.AreEqual(500, written[1].ReplyTo);
            Assert.IsNull(written[1].Id);
        }

        [Test]
        public void Handshake_IncompatibleErr_StopsBeforeSubscribe() {
            _pipe.Enqueue(Frame(Ack(1)));
            _pipe.Enqueue(Frame(Err(1, 500, "sys/badHello", "nope", false)));

            var result = _session.Handshake();

            Assert.AreEqual(Compatibility.Incompatible, result.Compatibility);
            StringAssert.Contains("sys/badHello", result.Error);
            Assert.Throws<InvalidOperationException>(() => _session.Subscribe(StateChannelNames.RuntimeSettings));

            SessionEvent next;
            Assert.IsTrue(_session.TryReadNext(out next));
            Assert.AreEqual(SessionEventKind.Incompatible, next.Kind);
            StringAssert.Contains("sys/badHello", next.Error);

            var written = DecodeAll(_pipe.Written());
            Assert.AreEqual(ProtocolTypes.Ack, written[1].Type);
            Assert.AreEqual(500, written[1].ReplyTo);
        }

        [Test]
        public void Subscribe_ThreeChannels_SnapshotThenPush_AcksEveryServerId() {
            CompatibleHello();

            var channels = new[] {
                StateChannelNames.RuntimeSettings,
                StateChannelNames.DeviceStatus,
                StateChannelNames.DeviceData
            };
            var serverIds = new List<long>();
            serverIds.Add(500);

            foreach (var channel in channels) {
                var snapshot = Json("{\"channel\":\"" + channel + "\",\"snapshot\":true}");
                var push = Json("{\"channel\":\"" + channel + "\",\"push\":1}");
                _session.Subscribe(channel);
                var sent = LastOfType(DecodeAll(_pipe.Written()), ProtocolTypes.Subscribe);
                Assert.AreEqual(channel, sent.Channel);
                Assert.IsTrue(PendingAck(_acks, sent.Id.Value));
                Assert.IsTrue(PendingReply(_replies, sent.Id.Value));

                var replyId = ++_serverId;
                serverIds.Add(replyId);
                _pipe.Enqueue(Frame(Ack(sent.Id.Value)));
                _pipe.Enqueue(Frame(Reply(sent.Id.Value, replyId, snapshot)));

                SessionEvent ackEvent;
                Assert.IsTrue(_session.TryReadNext(out ackEvent));
                Assert.AreEqual(SessionEventKind.PeerAck, ackEvent.Kind);
                Assert.IsNull(ackEvent.Payload);

                SessionEvent snapshotEvent;
                Assert.IsTrue(_session.TryReadNext(out snapshotEvent));
                Assert.AreEqual(SessionEventKind.Snapshot, snapshotEvent.Kind);
                Assert.AreEqual(channel, snapshotEvent.Channel);
                CollectionAssert.AreEqual(snapshot, snapshotEvent.Payload);
                Assert.AreEqual(SubscriptionState.ReceivedSnapshot, _registry.GetSubscriptionState(channel));

                var pushId = ++_serverId;
                serverIds.Add(pushId);
                _pipe.Enqueue(Frame(Push(channel, pushId, push)));
                SessionEvent pushEvent;
                Assert.IsTrue(_session.TryReadNext(out pushEvent));
                Assert.AreEqual(SessionEventKind.Push, pushEvent.Kind);
                Assert.AreEqual(channel, pushEvent.Channel);
                CollectionAssert.AreEqual(push, pushEvent.Payload);
            }

            var written = DecodeAll(_pipe.Written());
            foreach (var channel in channels) {
                Assert.AreEqual(1, CountSubscribe(written, channel));
            }

            foreach (var serverId in serverIds) {
                Assert.IsTrue(HasAck(written, serverId), "missing ack for " + serverId);
            }
            Assert.AreEqual(0, _acks.GetPendingAcks().Count);
            Assert.AreEqual(0, _replies.GetPendingReplies().Count);
        }

        [Test]
        public void Subscribe_UnknownChannel_IsNotSentAgain() {
            CompatibleHello();

            _session.Subscribe("nope");
            var sent = LastOfType(DecodeAll(_pipe.Written()), ProtocolTypes.Subscribe);
            var replyId = ++_serverId;
            _pipe.Enqueue(Frame(Ack(sent.Id.Value)));
            _pipe.Enqueue(Frame(Err(sent.Id.Value, replyId, "state/unknownChannel", "unsupported channel nope", false)));

            SessionEvent ackEvent;
            Assert.IsTrue(_session.TryReadNext(out ackEvent));
            Assert.AreEqual(SessionEventKind.PeerAck, ackEvent.Kind);

            SessionEvent failed;
            Assert.IsTrue(_session.TryReadNext(out failed));
            Assert.AreEqual(SessionEventKind.ChannelFailed, failed.Kind);
            Assert.AreEqual("nope", failed.Channel);
            StringAssert.Contains("state/unknownChannel", failed.Error);
            Assert.AreEqual(SubscriptionState.SubscribeFailed, _registry.GetSubscriptionState("nope"));

            var writesBeforeRetry = _pipe.Written().Length;
            _session.Subscribe("nope");
            Assert.AreEqual(writesBeforeRetry, _pipe.Written().Length);
            Assert.AreEqual(1, CountSubscribe(DecodeAll(_pipe.Written()), "nope"));
            Assert.IsTrue(HasAck(DecodeAll(_pipe.Written()), replyId));
        }

        [Test]
        public void TryReadNext_EofOnBoundaryAfterHello_IsCleanPeerClose() {
            CompatibleHello();
            _pipe.End();

            SessionEvent next;
            Assert.IsTrue(_session.TryReadNext(out next));
            Assert.AreEqual(SessionEventKind.Closed, next.Kind);
            Assert.AreEqual(SocketSession.PeerEof, next.Error);
        }

        [Test]
        public void Handshake_EofBeforeHello_NamesTheClose() {
            _pipe.End();

            var error = Assert.Throws<IOException>(() => _session.Handshake());
            Assert.AreEqual(SocketSession.EofBeforeHello, error.Message);
        }

        void CompatibleHello() {
            _pipe.Enqueue(Frame(Ack(1)));
            _pipe.Enqueue(Frame(Reply(1, 500, ServerHelloPayload("mighty-admin-app/13"))));
            var result = _session.Handshake();
            Assert.AreEqual(Compatibility.Compatible, result.Compatibility);
        }

        byte[] Frame(Envelope envelope) {
            return FrameCodec.Encode(_codec.Serialize(envelope));
        }

        static Envelope Ack(long replyTo) {
            return new Envelope {
                V = ProtocolVersion.Current,
                Type = ProtocolTypes.Ack,
                ReplyTo = replyTo
            };
        }

        static Envelope Reply(long replyTo, long id, byte[] payload) {
            return new Envelope {
                V = ProtocolVersion.Current,
                Type = ProtocolTypes.Reply,
                Id = id,
                ReplyTo = replyTo,
                Payload = payload
            };
        }

        static Envelope Err(long replyTo, long id, string code, string message, bool retryable) {
            var json = "{\"code\":\"" + code + "\",\"message\":\"" + message + "\",\"retryable\":" + (retryable ? "true" : "false") + "}";
            return new Envelope {
                V = ProtocolVersion.Current,
                Type = ProtocolTypes.Err,
                Id = id,
                ReplyTo = replyTo,
                Payload = Encoding.UTF8.GetBytes(json)
            };
        }

        static Envelope Push(string channel, long id, byte[] payload) {
            return new Envelope {
                V = ProtocolVersion.Current,
                Type = ProtocolTypes.Push,
                Id = id,
                Channel = channel,
                Payload = payload
            };
        }

        static byte[] ServerHelloPayload(string impl) {
            var hello = HandshakeNegotiator.CreateClientHello(impl);
            return HandshakeNegotiator.CreateClientHelloEnvelope(hello, 1, ProtocolVersion.Current).Payload;
        }

        static byte[] Json(string json) {
            return Encoding.UTF8.GetBytes(json);
        }

        static List<Envelope> DecodeAll(byte[] bytes) {
            var codec = EnvelopeCodecRegistry.HelloCodec;
            var frames = new List<Envelope>();
            var offset = 0;
            while (offset < bytes.Length) {
                var decoded = FrameCodec.TryDecode(bytes, offset, bytes.Length - offset);
                Assert.IsTrue(decoded.Success, decoded.Error.ToString());
                string error;
                Envelope envelope;
                Assert.IsTrue(codec.TryDeserialize(decoded.Body, out envelope, out error), error);
                frames.Add(envelope);
                offset += decoded.BytesConsumed;
            }

            return frames;
        }

        static Envelope LastOfType(List<Envelope> frames, string type) {
            Envelope found = null;
            foreach (var frame in frames) {
                if (frame.Type == type) {
                    found = frame;
                }
            }

            Assert.IsNotNull(found, type);
            return found;
        }

        static int CountSubscribe(List<Envelope> frames, string channel) {
            var count = 0;
            foreach (var frame in frames) {
                if (frame.Type == ProtocolTypes.Subscribe && frame.Channel == channel) {
                    count++;
                }
            }

            return count;
        }

        static bool PendingAck(AckTracker acks, long id) {
            foreach (var pending in acks.GetPendingAcks()) {
                if (pending.SentId == id) {
                    return true;
                }
            }

            return false;
        }

        static bool PendingReply(ReplyTracker replies, long id) {
            foreach (var pending in replies.GetPendingReplies()) {
                if (pending.RequestId == id) {
                    return true;
                }
            }

            return false;
        }

        static bool HasAck(List<Envelope> frames, long replyTo) {
            foreach (var frame in frames) {
                if (frame.Type == ProtocolTypes.Ack && frame.ReplyTo == replyTo) {
                    return true;
                }
            }

            return false;
        }

        sealed class ScriptedPipe : Stream {
            readonly object _gate = new object();
            readonly MemoryStream _inbound = new MemoryStream();
            readonly MemoryStream _outbound = new MemoryStream();
            long _readPos;
            bool _readClosed;
            bool _writeClosed;

            public Action OnWrite;

            public void Enqueue(byte[] frame) {
                lock (_gate) {
                    _inbound.Position = _inbound.Length;
                    _inbound.Write(frame, 0, frame.Length);
                }
            }

            public void End() {
                lock (_gate) {
                    _readClosed = true;
                }
            }

            public byte[] Written() {
                lock (_gate) {
                    return _outbound.ToArray();
                }
            }

            public override bool CanRead {
                get { return true; }
            }

            public override bool CanSeek {
                get { return false; }
            }

            public override bool CanWrite {
                get { return !_writeClosed; }
            }

            public override long Length {
                get { throw new NotSupportedException(); }
            }

            public override long Position {
                get { throw new NotSupportedException(); }
                set { throw new NotSupportedException(); }
            }

            public override int Read(byte[] buffer, int offset, int count) {
                lock (_gate) {
                    var available = (int)(_inbound.Length - _readPos);
                    if (available == 0) {
                        if (_readClosed) {
                            return 0;
                        }

                        throw new InvalidOperationException("Scripted pipe has no bytes.");
                    }

                    var n = Math.Min(count, available);
                    _inbound.Position = _readPos;
                    var got = _inbound.Read(buffer, offset, n);
                    _readPos += got;
                    return got;
                }
            }

            public override void Write(byte[] buffer, int offset, int count) {
                Action callback;
                lock (_gate) {
                    if (_writeClosed) {
                        throw new IOException("closed");
                    }

                    _outbound.Write(buffer, offset, count);
                    callback = OnWrite;
                }

                if (callback != null) {
                    callback();
                }
            }

            public override void Flush() {
            }

            public override long Seek(long offset, SeekOrigin origin) {
                throw new NotSupportedException();
            }

            public override void SetLength(long value) {
                throw new NotSupportedException();
            }

            public override void Close() {
                lock (_gate) {
                    _readClosed = true;
                    _writeClosed = true;
                }

                base.Close();
            }
        }
    }
}
