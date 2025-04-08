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
                Debug.unityLogger.LogWarning(TAG, "Not running on a Pico device. Ignoring MXRPicoUtils.OpenSystemUpdateUI()");
        }

        /// <summary>
        /// Opens the Pico General Settings UI.
        /// Ignored when called on a non Pico device
        /// </summary>
        public static void OpenSettingsUI() {
            if (MXRAndroidUtils.IsPicoDevice)
                LaunchIntentAction("pui.settings.action.GENERAL_SETTINGS");
            else
                Debug.unityLogger.LogWarning(TAG, "Not running on a Pico device. Ignoring MXRPicoUtils.OpenSettingsUI()");
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
                    Debug.unityLogger.LogWarning(TAG, "Not running on a Pico device. MXRPicoUtils.PUIVersion returning 0.0.0");
                    return "0.0.0";
                }
            }
        }

        /// <summary>
        /// Returns if current Pico UI version is 5.x.x 
        /// Always returns false on non Pico device
        /// </summary>
        public static bool IsPUI5 {
            get {
                if(MXRAndroidUtils.IsPicoDevice)
                    return PUIVersion.StartsWith("5");
                else {
                    Debug.unityLogger.LogWarning(TAG, "Not running on a Pico device. MXRPicoUtils.IsPUI5 returning false");
                    return false;
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
                    Debug.unityLogger.LogWarning(TAG, "Not running on a Pico device. MXRPicoUtils.IsPUI4 returning false");
                    return false;
                }
            }
        }

        /// <summary>
        /// Returns if current Pico UI version requires reboot on kiosk app change,
        //  meaning it is between 5.0.0 and 5.11.0
        /// Always returns false on non Pico device
        /// </summary>
        public static bool IsKioskRebootRequiredForPUI {
            get {
                if (!MXRAndroidUtils.IsPicoDevice) {
                    Debug.unityLogger.LogWarning(TAG, "Not running on a Pico device. MXRPicoUtils.IsKioskRebootRequiredforPUI returning false");
                    return false;
                }

                if (IsPUI4) return false;

                // PUI versions are Semver, or sometimes have a revison (5.9.5.0). Parsing directly
                // should consistently handle this case. If it doesn't, we'd rather return false because
                // of how we rely on these functions downstream.
                if (Version.TryParse(PUIVersion, out var version)) {
                    if (version.Major == 5 && version.Minor < 11)
                        return true;
                }

                return false;
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
