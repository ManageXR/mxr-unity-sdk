using System;

using UnityEngine;

namespace MXR.SDK {
    public static partial class MXRAndroidUtils {
        public static string ResolveClassNameForPackage(string pkgName) {
            if (CurrentActivity != null) {
                var joPackageManager = CurrentActivity.Call<AndroidJavaObject>("getPackageManager");
                var joIntent = joPackageManager.Call<AndroidJavaObject>("getLaunchIntentForPackage", pkgName);
                if (joIntent == null) return null;
                var joComponent = joPackageManager.Call<AndroidJavaObject>("getComponent");
                if (joComponent == null) return null;
                return joComponent.Call<string>("getClassName");
            }
            return null;
        }


        /// <summary>
        /// Returns the SDK version of Android currently running on a device.
        /// Ref: https://developer.android.com/reference/android/os/Build.VERSION#SDK_INT
        /// </summary>
        public static int AndroidSDKAsInt {
            get {
                AndroidJavaClass buildVersion = new AndroidJavaClass("android.os.Build$VERSION");
                return buildVersion.GetStatic<int>("SDK_INT");
            }
        }

        /// <summary>
        /// Returns the SDK version the app is targeting.
        /// </summary>
        public static int TargetSDKLevelAsInt {
            get {
                return CurrentActivity
                    .Call<AndroidJavaObject>("getApplicationContext")
                    .Call<AndroidJavaObject>("getApplicationInfo")
                    .Get<int>("targetSdkVersion");
            }
        }

        /// <summary>
        /// Returns whether the SDK can read external files.
        /// </summary>
        public static bool CanAccessExternalFiles {
            get {
                // If we're on level 29 and below, we don't need external storage manager permissions
                if (!NeedsManageAllFilesPermission)
                    return true;
                else
                    return IsExternalStorageManager;
            }
        }

        /// <summary>
        /// Returns whether the SDK needs MANAGE_ALL_FILES permission for accessing files for proper functioning
        /// </summary>
        public static bool NeedsManageAllFilesPermission => TargetSDKLevelAsInt >= 29 && AndroidSDKAsInt >= 29;

        /// <summary>
        /// Returns whether the SDK has the isExternalStorageManager permission for accessing files for proper functioning.
        /// </summary>
        public static bool IsExternalStorageManager {
            get {
                AndroidJavaClass environment = new AndroidJavaClass("android.os.Environment");
                return environment.CallStatic<bool>("isExternalStorageManager");
            }
        }

        public static string GetInstalledPackageVersionName(string packageName) {
            if (NativeUtils != null)
                NativeUtils.Call<string>("getInstalledPackagedVersionName", packageName);
            return null;
        }

        public static bool IsAppInstalled(string packageName) {
            if (NativeUtils != null)
                return NativeUtils.Call<bool>("isAppInstalled", packageName);
            return false;
        }

        public static void LaunchRuntimeApp(RuntimeApp app) {
            if (string.IsNullOrEmpty(app.className))
                LaunchAppWithPackageName(app.packageName);
            else
                LaunchAppWithPackageAndClassNames(app.packageName, app.className);
        }

        public static void LaunchAppWithPackageName(string packageName) {
            if (NativeUtils != null) 
                NativeUtils.Call<bool>("launchApp", packageName);
        }

        public static void LaunchAppWithPackageAndClassNames(string packageName, string className) {
            if (NativeUtils != null) 
                NativeUtils.Call<bool>("launchAppWithClass", packageName, className);
        }

        public static void LaunchAppWithIntentAction(string intentAction) {
            NativeUtils.Call<bool>("launchIntentAction", intentAction);
        }

        /// <summary>
        /// This function only works on certain device / firmware combinations.
        /// Instead, rely on the AdminAppMessengerManager to send KillApp messages to the admin app.
        /// </summary>
        /// <param name="packageName"></param>
        [Obsolete(@"This function only works on certain device / firmware combinations. 
        Instead, rely on the AdminAppMessengerManager to send KillApp messages to the admin app.", false)]
        public static void KillApp(string packageName) {
            if (NativeUtils != null)
                NativeUtils.Call("killApp", packageName);
        }

        public static string GetAdminAppPackageName() {
            if (NativeUtils != null)
                return NativeUtils.Call<string>("getInstalledAdminAppPackageName");
            return null;
        }

        public static int GetAdminAppVersionCode() {
            if (NativeUtils != null)
                return NativeUtils.Call<int>("getInstalledAdminAppVersionCode");
            return -1;

        }

        /// <summary>
        /// Opens Android UI for users to grant the MANAGE_ALL_FILES permission
        /// </summary>
        public static void RequestManageAllFilesPermission() {
            try {
                // Create an empty intent
                AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent");

                // Set the intent action 
                intent.Call<AndroidJavaObject>("setAction", "android.settings.MANAGE_APP_ALL_FILES_ACCESS_PERMISSION");

                // Set the data for the intent. This causes the Android UI to open the MANAGE_ALL_FILES
                // permission flow for the SDK integrating app
                var uriClass = new AndroidJavaClass("android.net.Uri");
                var data = uriClass.CallStatic<AndroidJavaObject>("parse", "package:" + Application.identifier);
                intent.Call<AndroidJavaObject>("setData", data);

                CurrentActivity.Call("startActivity", intent);

            }
            catch (Exception e) {
                Debug.unityLogger.Log(LogType.Error, "Failed to open MANAGE_ALL_FILES permission flow: " + e.Message);
            }
        }
    }
}
