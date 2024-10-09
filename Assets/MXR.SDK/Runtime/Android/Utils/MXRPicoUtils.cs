using UnityEngine;

namespace MXR.SDK {
    /// <summary>
    /// Pico runtime utilities
    /// </summary>
    public static class MXRPicoUtils {
        const string TAG = "MXRPicoUtils";

        /// <summary>
        /// Opens the Pico system update UI.
        /// Ignored when called on a non Pico device
        /// </summary>
        public static void OpenSystemUpdateUI() {
            if (MXRAndroidUtils.IsPicoDevice)
                LaunchIntentAction("com.mightyimmersion.mightylibrary.intent.action.OPEN_SYSTEM_UPDATE");
            else
                Debug.unityLogger.LogWarning(TAG, "Not running on a Pico device. Ignoring PicoUtils.OpenSystemUpdateUI()");
        }

        /// <summary>
        /// Returns Pico's PUI version. 
        /// Always returns "0.0.0" on non Pico device
        /// </summary>
        public static string PUIVersion {
            get {
                if (MXRAndroidUtils.IsPicoDevice)
                    return MXRAndroidUtils.AndroidOSBuild.SafeGetStatic<string>("DISPLAY");
                else {
                    Debug.unityLogger.LogWarning(TAG, "Not running on a Pico device. PicoUtils.PUIVersion returning 0.0.0");
                    return "0.0.0";
                }
            }
        }

        /// <summary>
        /// Returns if current Pico UI version is 4.x.x 
        /// Always returns false on non Pico device
        /// </summary>
        public static bool IsPUI4 {
            get {
                if(MXRAndroidUtils.IsPicoDevice)
                    return PUIVersion.StartsWith("4");
                else {
                    Debug.unityLogger.LogWarning(TAG, "Not running on a Pico device. PicoUtils.IsPUI4 returning false");
                    return false;
                }
            }
        }

        /// <summary>
        /// Launches an intent using the given action string
        /// </summary>
        /// <param name="action">The intent action</param>
        internal static void LaunchIntentAction(string action) {
            if (MXRAndroidUtils.IsPicoNeo3)
                LaunchIntentActionViaPVRAdapter(action);
            else
                MXRAndroidUtils.LaunchIntentAction(action);
        }

        /// <summary>
        /// Launches an intent using Pico's com.pvr.adapter using the given action string
        /// </summary>
        /// <param name="action">The intent action</param>
        internal static void LaunchIntentActionViaPVRAdapter(string action) {
            var intent = new AndroidJavaObject("android.content.Intent", "pvr.intent.action.ADAPTER");
            intent.Call<AndroidJavaObject>("setPackage", "com.pvr.adapter");
            intent.Call<AndroidJavaObject>("putExtra", "way", 2);
            intent.Call<AndroidJavaObject>("putExtra", "args", new string[] { action });
            MXRAndroidUtils.CurrentActivity.Call<AndroidJavaObject>("startService", intent);
        }
    }
}
