using System;

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

        static AndroidJavaObject nativeUtils;

        /// <summary>
        /// Returns an instance of the NativeUtils.java class in the MXR SDK
        /// </summary>
        [Obsolete("This property has been deprecated and may be removed soon. Use NativeUtils instead.")]
        public static AndroidJavaObject Plugin => NativeUtils;

        /// <summary>
        /// Returns an instance of the NativeUtils.java class in the MXR SDK
        /// </summary>
        public static AndroidJavaObject NativeUtils {
            get {
                if (nativeUtils != null) return nativeUtils;
                if (CurrentActivity != null) {
                    var context = CurrentActivity?.Call<AndroidJavaObject>("getApplicationContext");
                    nativeUtils = new AndroidJavaObject("com.mightyimmersion.customlauncher.NativeUtils", context);
                    return nativeUtils;
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
            if (bundle == null) return false;
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
            if (NativeUtils != null)
                NativeUtils.Call("sendBroadcastAction", action);
        }
    }
}
