using System;

using UnityEngine;

namespace MXR.SDK {
    public static partial class MXRAndroidUtils {
        /// <summary>
        /// The minimum Admin App version that supports the <see cref="DeviceData"/> features
        /// </summary>
        public static Version MinAdminAppVersionSupportingDeviceData => new Version(1, 7, 74);

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

        public static void LaunchIntentAction(string intentAction) =>
            NativeUtils.SafeCall<bool>("launchIntentAction", intentAction);

        /// <summary>
        /// The package name of the Admin App installed on an Android device
        /// </summary>
        /// <returns>Returns null if unsuccessful</returns>
        public static string GetAdminAppPackageName() {
            if(NativeUtils != null)
                return NativeUtils.SafeCall<string>("getInstalledAdminAppPackageName");
            return null;
        }

        /// <summary>
        /// The version code of the Admin App installed on an Android device
        /// </summary>
        /// <returns>Returns -1 if unsuccessful</returns>
        public static int GetAdminAppVersionCode() {
            if (NativeUtils != null)
                return NativeUtils.SafeCall<int>("getInstalledAdminAppVersionCode");
            return -1;
        }

        /// <summary>
        /// The version name string of the Admin App installed on an Android device
        /// </summary>
        /// <returns>Returns null if unsuccessful</returns>
        public static string GetAdminAppVersionName() {
            if (NativeUtils != null)
                return NativeUtils.SafeCall<string>("getInstalledAdminAppVersionName");
            return null;
        }

        /// <summary>
        /// The <see cref="Version"/> of the Admin App installed on an Android device
        /// </summary>
        /// <returns>
        /// Returns null in the Unity Editor
        /// On Android, returns null if the version name of the installed Admin App could not be retrieved
        /// or if the retrieved value is invalid.
        /// </returns>
        public static Version GetAdminAppVersion() {
            if (Application.isEditor)
                return null;

            var versionName = GetAdminAppVersionName();
            if (versionName == null)
                return null;

            // The version name may have a hyphen, e.g. "1.0.0-test"
            var versionString = versionName.Split('-')[0];
            if (Version.TryParse(versionString, out var version)) 
                return version;
            return null;
        }

        /// <summary>
        /// Whether the Admin App installed on an Android device supports the <see cref="DeviceData"/> features
        /// </summary>
        /// <returns>
        /// Returns true in the Unity Editor.
        /// On Android, returns false if the version of the installed Admin App could not be retrieved 
        /// or if the version is below <see cref="MinAdminAppVersionSupportingDeviceData"/>
        /// </returns>
        public static bool IsDeviceDataSupported {
            get {
                // When on the editor, we simulate DeviceData using deviceData.json included in the samples
                if (Application.isEditor)
                    return true;

                var version = GetAdminAppVersion();
                if (version == null)
                    return false;
                return version >= MinAdminAppVersionSupportingDeviceData;
            }
        }

        /// <summary>
        /// Returns a system property using the android.os.SystemProperties.get method
        /// </summary>
        /// <param name="property">The property to fetch</param>
        public static string GetSystemProperty(string property) {
            if (NativeUtils != null)
                return NativeUtils.SafeCall<string>("getSystemProperty", property);
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

        #region OBSOLETE
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

        [Obsolete("Use GetSystemProperty(\"r.product.name\") instead")]
        public static string GetProductName() =>
            GetSystemProperty("ro.product.name");


        [Obsolete("Use LaunchIntentAction instead.")]
        public static void LaunchAppWithIntentAction(string intentAction) =>
            LaunchIntentAction(intentAction);
        #endregion
    }
}
