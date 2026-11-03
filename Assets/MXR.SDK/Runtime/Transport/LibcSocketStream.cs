using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

[assembly: InternalsVisibleTo("com.mxr.unity.sdk.transport.tests")]

namespace MXR.SDK.Transport {
    /// <summary>
    /// <see cref="Stream"/> over a raw socket fd. Reads and writes go through libc.
    /// The fd is never wrapped in <see cref="FileStream"/> or <c>SafeFileHandle</c>.
    /// </summary>
    public sealed class LibcSocketStream : Stream {
        const int EINTR = 4;
        const int SHUT_RDWR = 2;

        readonly int _fd;
        readonly ILibc _libc;
        int _closed;

        public LibcSocketStream(int fd)
            : this(fd, new LibcNative()) {
        }

        internal LibcSocketStream(int fd, ILibc libc) {
            if (libc == null) {
                throw new ArgumentNullException(nameof(libc));
            }

            _fd = fd;
            _libc = libc;
        }

        public override bool CanRead {
            get { return _closed == 0; }
        }

        public override bool CanSeek {
            get { return false; }
        }

        public override bool CanWrite {
            get { return _closed == 0; }
        }

        public override long Length {
            get { throw new NotSupportedException(); }
        }

        public override long Position {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public override int Read(byte[] buffer, int offset, int count) {
            ValidateBuffer(buffer, offset, count);
            if (count == 0) {
                return 0;
            }

            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try {
                IntPtr ptr = new IntPtr(handle.AddrOfPinnedObject().ToInt64() + offset);
                while (true) {
                    int errno;
                    int got = _libc.Read(_fd, ptr, new UIntPtr((uint)count), out errno);
                    if (got >= 0) {
                        return got;
                    }

                    if (errno == EINTR) {
                        continue;
                    }

                    throw new IOException("libc read(fd=" + _fd + ") failed, errno=" + errno);
                }
            } finally {
                handle.Free();
            }
        }

        public override void Write(byte[] buffer, int offset, int count) {
            ValidateBuffer(buffer, offset, count);
            if (count == 0) {
                return;
            }

            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try {
                long baseAddr = handle.AddrOfPinnedObject().ToInt64();
                int written = 0;
                while (written < count) {
                    IntPtr ptr = new IntPtr(baseAddr + offset + written);
                    int errno;
                    int put = _libc.Write(_fd, ptr, new UIntPtr((uint)(count - written)), out errno);
                    if (put >= 0) {
                        written += put;
                        continue;
                    }

                    if (errno == EINTR) {
                        continue;
                    }

                    throw new IOException("libc write(fd=" + _fd + ") failed, errno=" + errno);
                }
            } finally {
                handle.Free();
            }
        }

        /// <summary>
        /// Shuts both directions down, then closes the fd. Runs that pair once.
        /// <c>close</c> alone does not wake a thread blocked in <c>read</c>.
        /// </summary>
        public override void Close() {
            if (Interlocked.Exchange(ref _closed, 1) == 0) {
                _libc.Shutdown(_fd, SHUT_RDWR);
                _libc.Close(_fd);
            }

            base.Close();
        }

        public override void Flush() {
        }

        public override long Seek(long offset, SeekOrigin origin) {
            throw new NotSupportedException();
        }

        public override void SetLength(long value) {
            throw new NotSupportedException();
        }

        static void ValidateBuffer(byte[] buffer, int offset, int count) {
            if (buffer == null) {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0) {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (count < 0) {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if ((long)offset + count > buffer.Length) {
                throw new ArgumentException("Offset and count fall outside the buffer.");
            }
        }

        /// <summary>
        /// Production libc binding. <c>ssize_t</c> is <see cref="IntPtr"/> and <c>size_t</c> is <see cref="UIntPtr"/>
        /// so the same signatures match the 32-bit and 64-bit ABIs. Never call <c>setsockopt</c> on this fd.
        /// </summary>
        sealed class LibcNative : ILibc {
            public int Read(int fd, IntPtr buf, UIntPtr count, out int errno) {
                IntPtr result = read(fd, buf, count);
                errno = Marshal.GetLastWin32Error();
                return (int)result.ToInt64();
            }

            public int Write(int fd, IntPtr buf, UIntPtr count, out int errno) {
                IntPtr result = write(fd, buf, count);
                errno = Marshal.GetLastWin32Error();
                return (int)result.ToInt64();
            }

            public int Shutdown(int fd, int how) {
                return shutdown(fd, how);
            }

            public int Close(int fd) {
                return libc_close(fd);
            }

            [DllImport("libc", SetLastError = true)]
            static extern IntPtr read(int fd, IntPtr buf, UIntPtr count);

            [DllImport("libc", SetLastError = true)]
            static extern IntPtr write(int fd, IntPtr buf, UIntPtr count);

            [DllImport("libc", SetLastError = true)]
            static extern int shutdown(int sockfd, int how);

            [DllImport("libc", SetLastError = true, EntryPoint = "close")]
            static extern int libc_close(int fd);
        }
    }
}
