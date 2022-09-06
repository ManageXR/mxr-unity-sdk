namespace MXR.SDK {
    public static partial class MXRAndroidUtils {
        /// <summary>
        /// Open Android Bluetooth settings
        /// </summary>
        public static void LaunchBluetoothSettings() {
            var action = "android.settings.BLUETOOTH_SETTINGS";
            Plugin.Call<bool>("launchIntentAction", action);
        }
    }
}
