using UnityEngine;

namespace MXR.SDK {
    public static partial class MXRAndroidUtils {
        static AndroidJavaObject currentActivity;
        public static AndroidJavaObject CurrentActivity {
            get {
                if (currentActivity != null) return currentActivity;
                AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                return currentActivity;
            }
        }

        static AndroidJavaObject plugin;
        public static AndroidJavaObject Plugin {
            get {
                if (plugin != null) return plugin;
                if (CurrentActivity != null) {
                    var context = CurrentActivity?.Call<AndroidJavaObject>("getApplicationContext");
                    plugin = new AndroidJavaObject("com.mightyimmersion.customlauncher.NativeUtils", context);
                    return plugin;
                }
                return null;
            }
        }
    }
}
