using System;

using UnityEngine;

namespace MXR.SDK {
    public static partial class MXRAndroidUtils {
        #region JAVA OBJECTS
        /// <summary>
        /// JNI to call com.unity.player.UnityPlayer.currentActivity()
        /// </summary>
        public static AndroidJavaObject CurrentActivity {
            get {
                if (currentActivity == null) { 
                    AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    currentActivity = unityPlayer.SafeGetStatic<AndroidJavaObject>("currentActivity");
                }
                return currentActivity;
            }
        }
        static AndroidJavaObject currentActivity;

        public static AndroidJavaObject PackageManager {
            get {
                if(packageManager == null)
                    packageManager = CurrentActivity.SafeCall<AndroidJavaObject>("getPackageManager");
                return packageManager;
            }
        }
        static AndroidJavaObject packageManager;

        /// <summary>
        /// JNI to call <see cref="CurrentActivity"/>.getApplicationContext()
        /// </summary>
        public static AndroidJavaObject ApplicationContext {
            get {
                if (applicationContext == null) 
                    applicationContext = CurrentActivity.SafeCall<AndroidJavaObject>("getApplicationContext");
                return applicationContext;
            }
        }
        static AndroidJavaObject applicationContext;

        /// <summary>
        /// JNI to call <see cref="ApplicationContext"/>.getApplicationInfo()
        /// </summary>
        public static AndroidJavaObject ApplicationInfo {
            get {
                if (applicationInfo == null)
                    applicationInfo = ApplicationContext.SafeCall<AndroidJavaObject>("getApplicationInfo");
                return applicationInfo;
            }
        }
        static AndroidJavaObject applicationInfo;

        /// <summary>
        /// Returns an instance of the NativeUtils.java class in the MXR SDK
        /// </summary>
        public static AndroidJavaObject NativeUtils {
            get {
                if(nativeUtils == null) 
                    nativeUtils = new AndroidJavaObject("com.mightyimmersion.customlauncher.NativeUtils", ApplicationContext);
                return nativeUtils;
            }
        }
        static AndroidJavaObject nativeUtils;
        #endregion

        /// <summary>
        /// Returns if the Android intent extras bundle has a key with the given name
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool HasIntentExtra(string key) {
            if (CurrentActivity == null)
            {
                Debug.unityLogger.Log(LogType.Error, "CurrentActivity is null, on HasIntentExtra. Intent ID " + key );
                return false;
            }

            var intent = CurrentActivity.SafeCall<AndroidJavaObject>("getIntent");
            if (intent == null)
            {
                Debug.unityLogger.Log(LogType.Error, "intent is null, on HasIntentExtra. Intent ID " + key );
                return false;
            }

            var bundle = intent.SafeCall<AndroidJavaObject>("getExtras");
            if (bundle == null)
            {
                Debug.unityLogger.Log(LogType.Error, "bundle is null, on HasIntentExtra. Intent ID " + key );
                return false;
            }

            return bundle.SafeCall<bool>("containsKey", key);
        }

        /// <summary>
        /// Returns a boolean from the Android intent extras 
        /// </summary>
        /// <param name="key">The key to read the boolean from</param>
        /// <param name="defaultValue">The default value in case the key doesn't exist</param>
        /// <returns></returns>
        public static bool GetIntentBooleanExtra(string key, bool defaultValue) {
            var intent = CurrentActivity.SafeCall<AndroidJavaObject>("getIntent");
            return intent.SafeCall<bool>("getBooleanExtra", key, defaultValue);
        }

        /// <summary>
        /// Returns a string from the Android intent extras
        /// </summary>
        /// <param name="key">The key to read the string from</param>
        /// <returns></returns>
        public static string GetIntentStringExtra(string key) {
            var intent = CurrentActivity.SafeCall<AndroidJavaObject>("getIntent");
            return intent.SafeCall<string>("getStringExtra", key);
        }

        /// <summary>
        /// Sends a broadcast action to NativeUtils
        /// </summary>
        /// <param name="action"></param>
        public static void SendBroadcastAction(string action) {
            if (NativeUtils?.SafeCall("sendBroadcastAction", action) == false)
                Debug.unityLogger.Log(LogType.Error, "Could not broadcast action " + action);
        }

        /// <summary>
        /// Returns an instance of the NativeUtils.java class in the MXR SDK
        /// </summary>
        [Obsolete("This property has been deprecated and may be removed soon. Use NativeUtils instead.")]
        public static AndroidJavaObject Plugin => NativeUtils;
    }
}
