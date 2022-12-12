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

        public static string GetInstalledPackageVersionName(string packageName) {
            if (Plugin != null)
                Plugin.Call<string>("getInstalledPackagedVersionName", packageName);
            return null;
        }

        public static bool IsAppInstalled(string packageName) {
            if (Plugin != null)
                return Plugin.Call<bool>("isAppInstalled", packageName);
            return false;
        }

        public static void LaunchRuntimeApp(RuntimeApp app) {
            if (string.IsNullOrEmpty(app.className))
                LaunchAppWithPackageName(app.packageName);
            else
                LaunchAppWithPackageAndClassNames(app.packageName, app.className);
        }

        public static void LaunchAppWithPackageName(string packageName) {
            if (Plugin != null) {
                Plugin.Call<bool>("launchApp", packageName);
            }
        }

        public static void LaunchAppWithPackageAndClassNames(string packageName, string className) {
            if (Plugin != null) {
                Plugin.Call<bool>("launchAppWithClass", packageName, className);
            }
        }

        public static void LaunchAppWithIntentAction(string intentAction) {
            Plugin.Call<bool>("launchIntentAction", intentAction);
        }

        public static void KillApp(string packageName) {
            if (Plugin != null)
                Plugin.Call("killApp", packageName);
        }

        public static void RestartApp(string packageName) {
            KillApp(packageName);
            LaunchAppWithPackageName(packageName);
        }

        public static string GetAdminAppPackageName() {
            if (Plugin != null)
                return Plugin.Call<string>("getInstalledAdminAppPackageName");
            return null;
        }
    }
}
