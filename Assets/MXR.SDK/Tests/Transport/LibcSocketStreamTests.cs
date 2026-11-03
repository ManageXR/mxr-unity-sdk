using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

using MXR.SDK.Transport;

using NUnit.Framework;

namespace MXR.SDK.Protocol.Tests {
    public class LibcSocketStreamTests {
        const int Fd = 7;
        const int Eintr = 4;
        const int Epipe = 32;
        const int ShutRdwr = 2;

        [Test]
        public void Read_EintrThenSuccess_ReturnsTheSuccessfulCount() {
            var libc = new FakeLibc();
            libc.Reads.Enqueue(IoResult.Fail(Eintr));
            libc.Reads.Enqueue(IoResult.Bytes(new byte[] { 0x10, 0x20, 0x30 }));
            var stream = new LibcSocketStream(Fd, libc);
            var buffer = new byte[5];

            int got = stream.Read(buffer, 1, 4);

            Assert.AreEqual(3, got);
            Assert.AreEqual(0x10, buffer[1]);
            Assert.AreEqual(0x20, buffer[2]);
            Assert.AreEqual(0x30, buffer[3]);
            Assert.AreEqual(0, buffer[0]);
            Assert.AreEqual(2, libc.ReadCalls.Count);
            Assert.AreEqual(4, libc.ReadCalls[0].Count);
            Assert.AreEqual(4, libc.ReadCalls[1].Count);
        }

        [Test]
        public void Read_ShortCount_ReturnsWithoutAnotherCall() {
            var libc = new FakeLibc();
            libc.Reads.Enqueue(IoResult.Bytes(new byte[] { 0xAA, 0xBB }));
            var stream = new LibcSocketStream(Fd, libc);
            var buffer = new byte[8];

            int got = stream.Read(buffer, 0, 8);

            Assert.AreEqual(2, got);
            Assert.AreEqual(1, libc.ReadCalls.Count);
            Assert.AreEqual(0xAA, buffer[0]);
            Assert.AreEqual(0xBB, buffer[1]);
        }

        [Test]
        public void Read_Zero_ReturnsZero() {
            var libc = new FakeLibc();
            libc.Reads.Enqueue(IoResult.Bytes(new byte[0]));
            var stream = new LibcSocketStream(Fd, libc);

            int got = stream.Read(new byte[4], 0, 4);

            Assert.AreEqual(0, got);
            Assert.AreEqual(1, libc.ReadCalls.Count);
        }

        [Test]
        public void Read_OtherErrno_ThrowsAndNamesErrno() {
            var libc = new FakeLibc();
            libc.Reads.Enqueue(IoResult.Fail(Epipe));
            var stream = new LibcSocketStream(Fd, libc);

            var error = Assert.Throws<IOException>(() => stream.Read(new byte[4], 0, 4));

            StringAssert.Contains("errno=" + Epipe, error.Message);
            Assert.AreEqual(1, libc.ReadCalls.Count);
        }

        [Test]
        public void Write_EintrThenSuccess_KeepsBytesAlreadyAccepted() {
            var libc = new FakeLibc();
            libc.Writes.Enqueue(IoResult.Count(2));
            libc.Writes.Enqueue(IoResult.Fail(Eintr));
            libc.Writes.Enqueue(IoResult.Count(2));
            var stream = new LibcSocketStream(Fd, libc);
            var buffer = new byte[] { 0x01, 0x11, 0x22, 0x33, 0x44, 0x55 };

            stream.Write(buffer, 1, 4);

            Assert.AreEqual(3, libc.WriteCalls.Count);
            CollectionAssert.AreEqual(new byte[] { 0x11, 0x22, 0x33, 0x44 }, libc.WriteCalls[0].Bytes);
            Assert.AreEqual(4, libc.WriteCalls[0].Count);
            CollectionAssert.AreEqual(new byte[] { 0x33, 0x44 }, libc.WriteCalls[1].Bytes);
            Assert.AreEqual(2, libc.WriteCalls[1].Count);
            CollectionAssert.AreEqual(new byte[] { 0x33, 0x44 }, libc.WriteCalls[2].Bytes);
            Assert.AreEqual(2, libc.WriteCalls[2].Count);
        }

        [Test]
        public void Write_ShortWrite_CompletesTheBuffer() {
            var libc = new FakeLibc();
            libc.Writes.Enqueue(IoResult.Count(1));
            libc.Writes.Enqueue(IoResult.Count(2));
            var stream = new LibcSocketStream(Fd, libc);

            stream.Write(new byte[] { 0x0A, 0x0B, 0x0C }, 0, 3);

            Assert.AreEqual(2, libc.WriteCalls.Count);
            CollectionAssert.AreEqual(new byte[] { 0x0A, 0x0B, 0x0C }, libc.WriteCalls[0].Bytes);
            CollectionAssert.AreEqual(new byte[] { 0x0B, 0x0C }, libc.WriteCalls[1].Bytes);
        }

        [Test]
        public void Write_OtherErrno_ThrowsAndNamesErrno() {
            var libc = new FakeLibc();
            libc.Writes.Enqueue(IoResult.Count(1));
            libc.Writes.Enqueue(IoResult.Fail(Epipe));
            var stream = new LibcSocketStream(Fd, libc);

            var error = Assert.Throws<IOException>(() => stream.Write(new byte[] { 0x01, 0x02, 0x03 }, 0, 3));

            StringAssert.Contains("errno=" + Epipe, error.Message);
            CollectionAssert.AreEqual(new byte[] { 0x02, 0x03 }, libc.WriteCalls[1].Bytes);
        }

        [Test]
        public void Close_ShutsDownThenClosesOnce() {
            var libc = new FakeLibc();
            var stream = new LibcSocketStream(Fd, libc);

            stream.Close();
            stream.Close();

            CollectionAssert.AreEqual(new[] { "shutdown", "close" }, libc.Calls);
            Assert.AreEqual(ShutRdwr, libc.ShutdownHow);
            Assert.AreEqual(Fd, libc.ClosedFd);
            Assert.AreEqual(1, libc.ShutdownCount);
            Assert.AreEqual(1, libc.CloseCount);
        }

        [Test]
        public void PInvoke_ReadAndWriteUsePointerSizedSignatures() {
            Type native = typeof(LibcSocketStream).GetNestedType("LibcNative", BindingFlags.NonPublic);
            Assert.IsNotNull(native);

            foreach (string name in new[] { "read", "write" }) {
                MethodInfo method = native.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static);
                Assert.IsNotNull(method, name);
                Assert.AreEqual(typeof(IntPtr), method.ReturnType, name);
                ParameterInfo[] parameters = method.GetParameters();
                Assert.AreEqual(typeof(int), parameters[0].ParameterType, name);
                Assert.AreEqual(typeof(IntPtr), parameters[1].ParameterType, name);
                Assert.AreEqual(typeof(UIntPtr), parameters[2].ParameterType, name);

                var import = (DllImportAttribute)method.GetCustomAttributes(typeof(DllImportAttribute), false)[0];
                Assert.AreEqual("libc", import.Value, name);
                Assert.IsTrue(import.SetLastError, name);
            }
        }

        sealed class IoCall {
            public int Count;
            public byte[] Bytes;
        }

        sealed class IoResult {
            public int Value;
            public int Errno;
            public byte[] Payload;

            public static IoResult Fail(int errno) {
                return new IoResult { Value = -1, Errno = errno };
            }

            public static IoResult Count(int count) {
                return new IoResult { Value = count };
            }

            public static IoResult Bytes(byte[] payload) {
                return new IoResult { Value = payload.Length, Payload = payload };
            }
        }

        sealed class FakeLibc : ILibc {
            public readonly Queue<IoResult> Reads = new Queue<IoResult>();
            public readonly Queue<IoResult> Writes = new Queue<IoResult>();
            public readonly List<IoCall> ReadCalls = new List<IoCall>();
            public readonly List<IoCall> WriteCalls = new List<IoCall>();
            public readonly List<string> Calls = new List<string>();
            public int ShutdownHow = -1;
            public int ShutdownCount;
            public int CloseCount;
            public int ClosedFd = -1;

            public int Read(int fd, IntPtr buf, UIntPtr count, out int errno) {
                Assert.AreEqual(Fd, fd);
                IoResult step = Reads.Dequeue();
                int requested = (int)count.ToUInt32();
                ReadCalls.Add(new IoCall { Count = requested });
                errno = step.Errno;
                if (step.Payload != null && step.Payload.Length > 0) {
                    Marshal.Copy(step.Payload, 0, buf, step.Payload.Length);
                }

                return step.Value;
            }

            public int Write(int fd, IntPtr buf, UIntPtr count, out int errno) {
                Assert.AreEqual(Fd, fd);
                IoResult step = Writes.Dequeue();
                int requested = (int)count.ToUInt32();
                var bytes = new byte[requested];
                if (requested > 0) {
                    Marshal.Copy(buf, bytes, 0, requested);
                }

                WriteCalls.Add(new IoCall { Count = requested, Bytes = bytes });
                errno = step.Errno;
                return step.Value;
            }

            public int Shutdown(int fd, int how) {
                Assert.AreEqual(Fd, fd);
                Calls.Add("shutdown");
                ShutdownHow = how;
                ShutdownCount++;
                return 0;
            }

            public int Close(int fd) {
                Calls.Add("close");
                ClosedFd = fd;
                CloseCount++;
                return 0;
            }
        }
    }
}
