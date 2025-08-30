using System;

using UnityEngine;

namespace MXR.SDK {
    public static partial class MXRAndroidUtils {
        #region JAVA OBJECTS
        public const int MIN_ADMIN_APP_VERSION_FOR_LAUNCH_WEB_URL_INTENT = 10724;

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
            var intent = CurrentActivity.SafeCall<AndroidJavaObject>("getIntent");
            var bundle = intent.SafeCall<AndroidJavaObject>("getExtras");
            if(bundle == null) return false;
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
        /// Opens the browser from a resume action if the admin app version is sufficient. Otherwise, 
        /// opens the browser via Unity's Application.OpenURL. We pass the url through an intent to the
        /// android app to open the browser if the admin app fails as well.
        /// </summary>
        public static void OpenBrowserFromResume(String url) {        
            if (GetAdminAppVersionCode() < MIN_ADMIN_APP_VERSION_FOR_LAUNCH_WEB_URL_INTENT) {
                Application.OpenURL(url);
                return;
            }

            var intent = new AndroidJavaObject("android.content.Intent",
                "com.mightyimmersion.mightylibrary.intent.action.LAUNCH_BROWSER_FROM_RESUME");

            intent.Call<AndroidJavaObject>("putExtra", "url", url);

            intent.SafeCall<AndroidJavaObject>("setPackage", GetAdminAppPackageName());
            CurrentActivity.SafeCall("sendBroadcast", intent);
        }

        /// <summary>
        /// Returns an instance of the NativeUtils.java class in the MXR SDK
        /// </summary>
        [Obsolete("This property has been deprecated and may be removed soon. Use NativeUtils instead.")]
        public static AndroidJavaObject Plugin => NativeUtils;
    }
}
