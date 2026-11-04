using System;
using System.IO;

using MXR.SDK.Protocol.Framing;
using MXR.SDK.Transport;

using NUnit.Framework;

namespace MXR.SDK.Transport.Tests {
    public class FrameReaderTests {
        [Test]
        public void ReadFrame_ShortReads_ReturnsTheDecodedBody() {
            var body = new byte[] { 0x10, 0x20, 0x30, 0x40 };
            var pipe = new ChunkedPipe(1);
            pipe.Enqueue(FrameCodec.Encode(body));
            var reader = new FrameReader(pipe);

            var read = reader.ReadFrame(false, delegate { Assert.Fail("A complete frame does not close."); }, Io);

            var decoded = read as FrameRead.Decoded;
            Assert.IsNotNull(decoded);
            CollectionAssert.AreEqual(body, decoded.Body);
        }

        [Test]
        public void ReadFrame_Oversize_ClosesAndStops() {
            var pipe = new ChunkedPipe(4);
            pipe.Enqueue(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });
            string reason = null;
            var reader = new FrameReader(pipe);

            var read = reader.ReadFrame(false, delegate(string closeReason) { reason = closeReason; }, Io);

            Assert.IsInstanceOf<FrameRead.Stopped>(read);
            Assert.AreEqual(FrameReader.OversizeFrame, reason);
        }

        [Test]
        public void ReadFrame_ZeroLengthBody_ClosesAndStops() {
            var pipe = new ChunkedPipe(4);
            pipe.Enqueue(new byte[] { 0x00, 0x00, 0x00, 0x00 });
            string reason = null;
            var reader = new FrameReader(pipe);

            var read = reader.ReadFrame(false, delegate(string closeReason) { reason = closeReason; }, Io);

            Assert.IsInstanceOf<FrameRead.Stopped>(read);
            Assert.AreEqual(FrameReader.ZeroLengthBody, reason);
        }

        [Test]
        public void ReadFrame_EofMidFrame_ReturnsEofWithoutClosing() {
            var pipe = new ChunkedPipe(4);
            pipe.Enqueue(new byte[] { 0x00, 0x00, 0x00 });
            pipe.End();
            var reader = new FrameReader(pipe);

            var read = reader.ReadFrame(false, delegate { Assert.Fail("EOF does not close."); }, Io);

            var eof = read as FrameRead.Eof;
            Assert.IsNotNull(eof);
            Assert.IsTrue(eof.MidFrame);
        }

        [Test]
        public void ReadFrame_EofOnBoundary_ReturnsEofWithoutClosing() {
            var pipe = new ChunkedPipe(4);
            pipe.End();
            var reader = new FrameReader(pipe);

            var read = reader.ReadFrame(false, delegate { Assert.Fail("EOF does not close."); }, Io);

            var eof = read as FrameRead.Eof;
            Assert.IsNotNull(eof);
            Assert.IsFalse(eof.MidFrame);
        }

        static string Io(IOException exception) {
            return "io";
        }

        sealed class ChunkedPipe : Stream {
            readonly MemoryStream _inbound = new MemoryStream();
            readonly int _chunk;
            long _readPos;
            bool _ended;

            public ChunkedPipe(int chunk) {
                _chunk = chunk;
            }

            public void Enqueue(byte[] bytes) {
                _inbound.Position = _inbound.Length;
                _inbound.Write(bytes, 0, bytes.Length);
            }

            public void End() {
                _ended = true;
            }

            public override bool CanRead {
                get { return true; }
            }

            public override bool CanSeek {
                get { return false; }
            }

            public override bool CanWrite {
                get { return false; }
            }

            public override long Length {
                get { throw new NotSupportedException(); }
            }

            public override long Position {
                get { throw new NotSupportedException(); }
                set { throw new NotSupportedException(); }
            }

            public override int Read(byte[] buffer, int offset, int count) {
                var available = (int)(_inbound.Length - _readPos);
                if (available == 0) {
                    if (_ended) {
                        return 0;
                    }

                    throw new InvalidOperationException("Chunked pipe has no bytes.");
                }

                var n = Math.Min(count, Math.Min(available, _chunk));
                _inbound.Position = _readPos;
                var got = _inbound.Read(buffer, offset, n);
                _readPos += got;
                return got;
            }

            public override void Write(byte[] buffer, int offset, int count) {
                throw new NotSupportedException();
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
