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

        /// <summary>
        /// Returns if the Android intent extras bundle has a key with the given name
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool HasIntentExtra(string key) {
            var intent = CurrentActivity.Call<AndroidJavaObject>("getIntent");
            var bundle = intent.Call<AndroidJavaObject>("getExtras");
            return bundle.Call<bool>("containsKey", key);
        }

        /// <summary>
        /// Returns a boolean from the Android intent extras 
        /// </summary>
        /// <param name="key">The key to read the boolean from</param>
        /// <param name="defaultValue">The default value in case the key doesn't exist</param>
        /// <returns></returns>
        public static bool GetIntentBooleanExtra(string key, bool defaultValue) {
            var intent = CurrentActivity.Call<AndroidJavaObject>("getIntent");
            return intent.Call<bool>("getBooleanExtra", key, defaultValue);
        }

        /// <summary>
        /// Returns a string from the Android intent extras
        /// </summary>
        /// <param name="key">The key to read the string from</param>
        /// <returns></returns>
        public static string GetIntentStringExtra(string key) {
            var intent = CurrentActivity.Call<AndroidJavaObject>("getIntent");
            return intent.Call<string>("getStringExtra", key);
        }

        public static void SendBroadcastAction(string action) {
            if (Plugin != null)
                Plugin.Call("sendBroadcastAction", action);
        }
    }
}
