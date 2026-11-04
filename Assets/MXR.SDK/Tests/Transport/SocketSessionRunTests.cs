using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using MXR.SDK.Protocol;
using MXR.SDK.Protocol.Framing;
using MXR.SDK.Protocol.Guarantee;
using MXR.SDK.Protocol.Handshake;
using MXR.SDK.Protocol.Subscriptions;

using NUnit.Framework;

namespace MXR.SDK.Transport.Tests {
    public class SocketSessionRunTests {
        [Test]
        public void Run_ThreeSnapshotsThenPush_LogsEach() {
            var pipe = new ScriptedPipe();
            pipe.Enqueue(Frame(Ack(1)));
            pipe.Enqueue(Frame(Reply(1, 500, ServerHelloPayload())));
            pipe.Enqueue(Frame(Ack(2)));
            pipe.Enqueue(Frame(Reply(2, 501, Json("{\"runtime\":1}"))));
            pipe.Enqueue(Frame(Ack(3)));
            pipe.Enqueue(Frame(Reply(3, 502, Json("{\"status\":2}"))));
            pipe.Enqueue(Frame(Ack(4)));
            pipe.Enqueue(Frame(Reply(4, 503, Json("{\"data\":3}"))));
            pipe.Enqueue(Frame(Push(StateChannelNames.RuntimeSettings, 504, Json("{\"runtime\":9}"))));
            pipe.End();

            var lines = new List<string>();
            var session = Open(pipe);
            session.Run(lines.Add);

            CollectionAssert.AreEqual(new[] {
                "SocketSession snapshot runtimeSettings {\"runtime\":1}",
                "SocketSession snapshot deviceStatus {\"status\":2}",
                "SocketSession snapshot deviceData {\"data\":3}",
                "SocketSession push runtimeSettings {\"runtime\":9}",
                "SocketSession closed " + SocketSession.PeerEof
            }, lines);

            var written = Encoding.UTF8.GetString(pipe.Written());
            Assert.AreEqual(1, Count(written, "\"channel\":\"runtimeSettings\""));
            Assert.AreEqual(1, Count(written, "\"channel\":\"deviceStatus\""));
            Assert.AreEqual(1, Count(written, "\"channel\":\"deviceData\""));
        }

        [Test]
        public void Run_EofBeforeHello_LogsTheClose() {
            var pipe = new ScriptedPipe();
            pipe.End();
            var lines = new List<string>();
            var session = Open(pipe);

            session.Run(lines.Add);

            Assert.AreEqual(1, lines.Count);
            StringAssert.Contains(SocketSession.EofBeforeHello, lines[0]);
        }

        private static SocketSession Open(ScriptedPipe pipe) {
            var clock = new UtcProtocolClock();
            return new SocketSession(pipe, new AckTracker(clock), new ReplyTracker(clock), new SubscriptionRegistry());
        }

        private static int Count(string text, string value) {
            var count = 0;
            var index = 0;
            while (true) {
                index = text.IndexOf(value, index, StringComparison.Ordinal);
                if (index < 0) {
                    return count;
                }

                count++;
                index += value.Length;
            }
        }

        private static byte[] Frame(Envelope envelope) {
            return FrameCodec.Encode(EnvelopeCodecRegistry.HelloCodec.Serialize(envelope));
        }

        private static Envelope Ack(long replyTo) {
            return new Envelope {
                V = ProtocolVersion.Current,
                Type = ProtocolTypes.Ack,
                ReplyTo = replyTo
            };
        }

        private static Envelope Reply(long replyTo, long id, byte[] payload) {
            return new Envelope {
                V = ProtocolVersion.Current,
                Type = ProtocolTypes.Reply,
                Id = id,
                ReplyTo = replyTo,
                Payload = payload
            };
        }

        private static Envelope Push(string channel, long id, byte[] payload) {
            return new Envelope {
                V = ProtocolVersion.Current,
                Type = ProtocolTypes.Push,
                Id = id,
                Channel = channel,
                Payload = payload
            };
        }

        private static byte[] ServerHelloPayload() {
            var hello = HandshakeNegotiator.CreateClientHello("mighty-admin-app/13");
            return HandshakeNegotiator.CreateClientHelloEnvelope(hello, 1, ProtocolVersion.Current).Payload;
        }

        private static byte[] Json(string json) {
            return Encoding.UTF8.GetBytes(json);
        }

        private sealed class ScriptedPipe : Stream {
            private readonly object _gate = new object();
            private readonly MemoryStream _inbound = new MemoryStream();
            private readonly MemoryStream _outbound = new MemoryStream();
            private long _readPos;
            private bool _readClosed;

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
                get { return true; }
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
                lock (_gate) {
                    _outbound.Write(buffer, offset, count);
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
        }
    }
}
