using System;

using UnityEngine;

namespace MXR.SDK {
    public static partial class MXRAndroidUtils {
        /// <summary>
        /// Gets the class name using the package name of an app
        /// </summary>
        /// <param name="pkgName"></param>
        /// <returns></returns>
        public static string ResolveClassNameForPackage(string pkgName) {               
            var joIntent = PackageManager.SafeCall<AndroidJavaObject>("getLaunchIntentForPackage", pkgName);
            if (joIntent == null) return null;
            var joComponent = PackageManager.SafeCall<AndroidJavaObject>("getComponent");
            if (joComponent == null) return null;
            return joComponent.SafeCall<string>("getClassName");
        }

        /// <summary>
        /// Returns the SDK version of Android currently running on a device.
        /// Ref: https://developer.android.com/reference/android/os/Build.VERSION#SDK_INT
        /// </summary>
        public static int AndroidSDKAsInt {
            get {
                if(!androidSDKAsInt.HasValue) {
                    AndroidJavaClass buildVersion = new AndroidJavaClass("android.os.Build$VERSION");
                    androidSDKAsInt = buildVersion.SafeGetStatic<int>("SDK_INT");
                }
                return androidSDKAsInt.Value;
            }
        }
        static int? androidSDKAsInt;

        /// <summary>
        /// Returns the SDK version the app is targeting.
        /// </summary>
        public static int TargetSDKLevelAsInt {
            get {
                if(!targetSDKLevelAsInt.HasValue) 
                    targetSDKLevelAsInt = ApplicationInfo.SafeGet<int>("targetSdkVersion");
                return targetSDKLevelAsInt.Value;
            }
        }
        static int? targetSDKLevelAsInt;

        /// <summary>
        /// Returns whether the SDK needs MANAGE_EXTERNAL_STORAGE permission for 
        /// accessing files for proper functioning. This will return true
        /// if the device OS SDK level and the builds target SDK are both
        /// 30 and above which is where Scoped Storage for Android was introduced.
        /// </summary>
        public static bool NeedsManageExternalStoragePermission => 
            TargetSDKLevelAsInt > 29 && AndroidSDKAsInt > 29;

        /// <summary>
        /// Returns whether the MANAGE_EXTERNAL_STORAGE permission has been granted
        /// for accessing files for proper functioning of the SDK.
        /// </summary>
        public static bool IsExternalStorageManager {
            get {
                AndroidJavaClass environment = new AndroidJavaClass("android.os.Environment");
                return environment.SafeCallStatic<bool>("isExternalStorageManager");
            }
        }

        public static string GetInstalledPackageVersionName(string packageName) {
            if (NativeUtils != null)
                return NativeUtils.SafeCall<string>("getInstalledPackagedVersionName", packageName);
            return null;
        }

        public static bool IsAppInstalled(string packageName) {
            if (NativeUtils != null)
                return NativeUtils.SafeCall<bool>("isAppInstalled", packageName);
            return false;
        }

        public static void LaunchRuntimeApp(RuntimeApp app) {
            if (string.IsNullOrEmpty(app.className))
                LaunchAppWithPackageName(app.packageName);
            else
                LaunchAppWithPackageAndClassNames(app.packageName, app.className);
        }

        public static void LaunchAppWithPackageName(string packageName) =>
            NativeUtils.SafeCall<bool>("launchApp", packageName);

        public static void LaunchAppWithPackageAndClassNames(string packageName, string className) =>
            NativeUtils.SafeCall<bool>("launchAppWithClass", packageName, className);

        public static void LaunchAppWithIntentAction(string intentAction) =>
            NativeUtils.SafeCall<bool>("launchIntentAction", intentAction);

        public static string GetAdminAppPackageName() =>
            NativeUtils.SafeCall<string>("getInstalledAdminAppPackageName");

        public static int GetAdminAppVersionCode() {
            if (NativeUtils != null)
                return NativeUtils.SafeCall<int>("getInstalledAdminAppVersionCode");
            return -1;
        }

        public static string GetProductName() {
            if (NativeUtils != null)
                return NativeUtils.SafeCall<string>("getSystemProperty", "ro.product.name");
            return "";
        }

        /// <summary>
        /// Opens Android system dialog for users to grant MANAGE_APP_ALL_FILES_ACCESS_PERMISSION.
        /// Note that if the AndroidManifest.xml of the Unity project doesn't have the 
        /// MANAGE_EXTERNAL_STORAGE permission as described in the SDK README, 
        /// the toggle button in the system dialog may be disabled.
        /// </summary>
        public static void RequestManageAppAllFilesAccessPermission() {
            try {
                // Create an empty intent
                AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent");

                // Set the intent action 
                intent.SafeCall<AndroidJavaObject>("setAction", "android.settings.MANAGE_APP_ALL_FILES_ACCESS_PERMISSION");

                // Set the data for the intent. This causes the Android UI to open the MANAGE_ALL_FILES
                // permission flow for the SDK integrating app
                var uriClass = new AndroidJavaClass("android.net.Uri");
                var data = uriClass.SafeCallStatic<AndroidJavaObject>("parse", "package:" + Application.identifier);
                intent.SafeCall<AndroidJavaObject>("setData", data);

                if (CurrentActivity.SafeCall("startActivity", intent) == false)
                    Debug.unityLogger.Log(LogType.Error, "Could not open android.settings.MANAGE_APP_ALL_FILES_ACCESS_PERMISSION activity.");

            }
            catch (Exception e) {
                string err = "Failed to open MANAGE_ALL_FILES permission system dialog: " + e.Message;
                Debug.unityLogger.Log(LogType.Error, err);
            }
        }

        [Obsolete("Use RequestManageAppAllFilesAccessPermission instead. " +
        "This method may be removed in the future.")]
        public static void RequestManageAllFilesPermission() =>
            RequestManageAppAllFilesAccessPermission();

        /// <summary>
        /// This function only works on certain device / firmware combinations.
        /// Instead, rely on the AdminAppMessengerManager to send KillApp messages to the admin app.
        /// </summary>
        /// <param name="packageName"></param>
        [Obsolete(@"This function only works on certain device / firmware combinations. 
        Instead, rely on the AdminAppMessengerManager to send KillApp messages to the admin app.", false)]
        public static void KillApp(string packageName) {
            if (NativeUtils?.SafeCall("killApp", packageName) == false)
                Debug.unityLogger.Log(LogType.Error, "Could not kill app " + packageName);
        }
    }
}
