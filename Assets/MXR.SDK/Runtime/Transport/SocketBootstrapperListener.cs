// Device player only. The editor and the dotnet test build do not compile this proxy.
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine;

namespace MXR.SDK.Transport {
    /// <summary>
    /// Receives <c>SocketBootstrapper.Listener</c> callbacks from Java.
    /// </summary>
    internal sealed class SocketBootstrapperListener : AndroidJavaProxy {
        private readonly SocketClient _client;

        public SocketBootstrapperListener(SocketClient client)
            : base("com.mightyimmersion.customlauncher.SocketBootstrapper$Listener") {
            _client = client;
        }

        public void onSocketFd(int fd) {
            _client.OnSocketFd(fd);
        }

        public void onSocketNotHandedOver(string reason) {
            _client.OnSocketNotHandedOver(reason);
        }
    }
}
#endif
