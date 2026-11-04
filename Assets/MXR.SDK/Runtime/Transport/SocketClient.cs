// UNITY_5_3_OR_NEWER wraps every UnityEngine use below. Unity defines that symbol in
// every editor from 5.3 on, and the dotnet test project compiles this file without
// defining it, so Debug, UnityLifecycleEvents, and AndroidJavaObject stay out of
// that build. It is not the SDK's minimum Unity version.
using System;
using System.IO;
using System.Threading.Tasks;

using MXR.SDK.Protocol.Guarantee;
using MXR.SDK.Protocol.Subscriptions;

#if UNITY_5_3_OR_NEWER
using UnityEngine;
#endif

namespace MXR.SDK.Transport {
    /// <summary>
    /// Binds the Admin App when running on Android, then handshakes and keeps
    /// <see cref="SocketSession"/> reading. The Messenger path is left alone.
    /// </summary>
    public sealed class SocketClient : IDisposable {
        /// <summary>
        /// Log text when <c>SOCKET_IPC</c> is off or the bind returns no fd.
        /// </summary>
        public const string SocketNotHandedOver = "the socket was not handed over";

        private readonly IProtocolClock _clock;
        private readonly Action<string> _log;
        private SocketSession _session;
        private Task _run;
        private bool _disposed;

#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject _bootstrap;
        private SocketBootstrapperListener _listener;
#endif

        /// <summary>
        /// Creates a client that can bind the Admin App.
        /// </summary>
        public SocketClient() : this(new UtcProtocolClock(), DefaultLog) { }

        /// <summary>
        /// Creates a client over an already connected stream. Tests use this.
        /// </summary>
        public SocketClient(Stream stream, IProtocolClock clock, Action<string> log)
            : this(clock, log) {
            _session = CreateSession(stream, clock);
        }

        private SocketClient(IProtocolClock clock, Action<string> log) {
            if (clock == null) {
                throw new ArgumentNullException(nameof(clock));
            }

            if (log == null) {
                throw new ArgumentNullException(nameof(log));
            }

            _clock = clock;
            _log = log;
        }

        private static SocketSession CreateSession(Stream stream, IProtocolClock clock) {
            return new SocketSession(
                stream,
                new AckTracker(clock),
                new ReplyTracker(clock),
                new SubscriptionRegistry());
        }

        /// <summary>
        /// Binds <c>SocketBootstrapService</c>. A missing socket is logged and the Messenger path is unchanged.
        /// </summary>
        public void Connect() {
#if UNITY_5_3_OR_NEWER
            UnityLifecycleEvents.Ensure().Destroyed += Dispose;
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
            _listener = new SocketBootstrapperListener(this);
            var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            _bootstrap = new AndroidJavaObject("com.mightyimmersion.customlauncher.SocketBootstrapper");
            _bootstrap.Call("bind", activity, _listener);
#elif UNITY_5_3_OR_NEWER
            Debug.Log("SocketClient: " + SocketNotHandedOver);
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        internal void OnSocketFd(int fd) {
            _session = CreateSession(new LibcSocketStream(fd), _clock);
            Start();
        }

        internal void OnSocketNotHandedOver(string reason) {
            _log("SocketClient: " + reason);
        }
#endif

        /// <summary>
        /// Runs <see cref="RunSession"/> on the thread pool. <see cref="SocketSession.Handshake"/> and
        /// <see cref="SocketSession.TryReadNext"/> block in <c>libc</c> read, so they stay off the player thread.
        /// </summary>
        public Task Start() {
            _run = Task.Run((Action)RunSession);
            return _run;
        }

        /// <summary>
        /// Handshakes, subscribes to the three state channels, and reads until the socket closes.
        /// </summary>
        public void RunSession() {
            _session.Run(_log);
        }

        /// <summary>
        /// Closes the socket, which unblocks the read, and waits for that read to finish.
        /// Also runs when the player object is destroyed.
        /// </summary>
        public void Dispose() {
            if (_disposed) {
                return;
            }

            _disposed = true;
#if UNITY_5_3_OR_NEWER
            if (UnityLifecycleEvents.Current != null) {
                UnityLifecycleEvents.Current.Destroyed -= Dispose;
            }
#endif
            if (_session != null) {
                _session.Dispose();
            }

            if (_run != null && !_run.IsCompleted) {
                try {
                    _run.Wait(TimeSpan.FromSeconds(1));
                } catch (AggregateException) {
                }
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            if (_bootstrap != null) {
                _bootstrap.Dispose();
                _bootstrap = null;
            }
#endif
        }

        private static void DefaultLog(string message) {
#if UNITY_5_3_OR_NEWER
            Debug.Log(message);
#endif
        }
    }
}
