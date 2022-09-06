using System;

using UnityEngine;

namespace MXR.SDK {
    internal class AdminAppMessengerManager {
        public bool IsBoundToService { get; private set; }
        public event Action<bool> OnBoundStatusToAdminAppChanged;

        public event Action<int, string> OnMessageFromAdminApp;

        public AndroidJavaObject Native { get; private set; }

        public AdminAppMessengerManager() {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext");
            Native = new AndroidJavaObject(
                "com.mightyimmersion.customlauncher.AdminAppMessengerManager", 
                context, 
                new AdminAppMessengerListener(this)
            );
        }

        public class AdminAppMessengerListener : AndroidJavaProxy {
            readonly AdminAppMessengerManager bridge;

            public AdminAppMessengerListener(AdminAppMessengerManager bridge)
            : base("com.mightyimmersion.customlauncher.AdminAppMessengerManager$AdminAppMessengerListener") {
                this.bridge = bridge;
            }

            public void onBindStatusToAdminAppChanged(bool bound) {
                if (bridge.IsBoundToService != bound) {
                    bridge.IsBoundToService = bound;
                    bridge.OnBoundStatusToAdminAppChanged?.Invoke(bound);
                }
            }

            public void onMessageFromAdminApp(int what, string json) {
                bridge.OnMessageFromAdminApp?.Invoke(what, json);
            }
        }
    }

}
