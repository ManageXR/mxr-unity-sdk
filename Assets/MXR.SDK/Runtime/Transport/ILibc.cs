using System;

namespace MXR.SDK.Transport {
    /// <summary>
    /// The four libc calls <see cref="LibcSocketStream"/> is allowed to make.
    /// Production binds these to <c>DllImport("libc")</c>. Tests supply a fake.
    /// </summary>
    internal interface ILibc {
        /// <summary>
        /// &gt;= 0 is a byte count. 0 on read is EOF. &lt; 0 means failure and <paramref name="errno"/> is set.
        /// </summary>
        int Read(int fd, IntPtr buf, UIntPtr count, out int errno);

        /// <summary>
        /// &gt;= 0 is the number of bytes accepted, which may be short of <paramref name="count"/>.
        /// &lt; 0 means failure and <paramref name="errno"/> is set.
        /// </summary>
        int Write(int fd, IntPtr buf, UIntPtr count, out int errno);

        int Shutdown(int fd, int how);

        int Close(int fd);
    }
}
