using System;
using System.IO;

using MXR.SDK.Protocol.Framing;

namespace MXR.SDK.Transport {
    /// <summary>
    /// The <c>readFrame</c> / <c>append</c> / <c>consume</c> half of <c>SocketIpcTransport</c>.
    /// A violation or a broken read closes. EOF is returned so the caller can name it.
    /// </summary>
    internal sealed class FrameReader {
        internal const string OversizeFrame = "OVERSIZE_FRAME";
        internal const string ZeroLengthBody = "ZERO_LENGTH_BODY";

        readonly Stream _stream;
        readonly byte[] _scratch = new byte[4096];
        byte[] _pending = new byte[4096];
        int _buffered;

        public FrameReader(Stream stream) {
            _stream = stream;
        }

        public FrameRead ReadFrame(bool closed, Action<string> close, Func<IOException, string> failureReason) {
            while (!closed) {
                if (_buffered > 0) {
                    var frame = FrameCodec.TryDecode(_pending, 0, _buffered);
                    if (frame.Status == FrameDecodeStatus.Violation) {
                        close(ViolationReason(frame.Error));
                        return FrameRead.Stopped.Instance;
                    }

                    if (frame.Status == FrameDecodeStatus.Decoded) {
                        Consume(frame.BytesConsumed);
                        return new FrameRead.Decoded(frame.Body);
                    }
                }

                int read;
                try {
                    read = _stream.Read(_scratch, 0, _scratch.Length);
                } catch (IOException ex) {
                    close(failureReason(ex));
                    return FrameRead.Stopped.Instance;
                }

                if (read <= 0) {
                    return new FrameRead.Eof(_buffered > 0);
                }

                Append(_scratch, read);
            }

            return FrameRead.Stopped.Instance;
        }

        static string ViolationReason(FrameDecodeError error) {
            if (error == FrameDecodeError.OversizeFrame) {
                return OversizeFrame;
            }

            return ZeroLengthBody;
        }

        void Append(byte[] src, int length) {
            var needed = _buffered + length;
            if (needed > _pending.Length) {
                var grown = new byte[Math.Max(_pending.Length * 2, needed)];
                Buffer.BlockCopy(_pending, 0, grown, 0, _buffered);
                _pending = grown;
            }

            Buffer.BlockCopy(src, 0, _pending, _buffered, length);
            _buffered += length;
        }

        void Consume(int length) {
            if (length >= _buffered) {
                _buffered = 0;
                return;
            }

            var rest = _buffered - length;
            Buffer.BlockCopy(_pending, length, _pending, 0, rest);
            _buffered = rest;
        }
    }

    /// <summary>
    /// Same three outcomes as <c>SocketIpcTransport.FrameRead</c>.
    /// </summary>
    internal abstract class FrameRead {
        public sealed class Decoded : FrameRead {
            public Decoded(byte[] body) {
                Body = body;
            }

            public byte[] Body { get; }
        }

        public sealed class Eof : FrameRead {
            public Eof(bool midFrame) {
                MidFrame = midFrame;
            }

            public bool MidFrame { get; }
        }

        public sealed class Stopped : FrameRead {
            public static readonly Stopped Instance = new Stopped();

            Stopped() {
            }
        }
    }
}
