using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace MXR.SDK {
    /// <summary>
    /// Handles the initialization and setup of the MXR system, including loading initial state,
    /// establishing connections with the Admin App, and setting up event handlers.
    /// </summary>
    public partial class MXRAndroidSystem {
        private const string EXTERNAL_READ_WARNING_MSG =
            "On Android 30 and above, request Manage External Storage permission to read external files. " +
            "MXRAndroidUtils.RequestManageAppAllFilesAccessPermission() is provided in the SDK for the same. " +
            "On Android 29, use android:requestLegacyExternalStorage=\"true\" in your AndroidManifest.xml." +
            "Refer to the MXR Unity SDK README for more info.";

        private async void InitializeRuntimeSettingsSummary() {
            bool InitFromFile(string path) {
                if (DeserializeFromFile(path, out var contents, out RuntimeSettingsSummary runtimeSettingsSummary)) {
                    lastRuntimeSettingsSummaryJSON = contents;
                    RuntimeSettingsSummary = runtimeSettingsSummary;
                    OnRuntimeSettingsSummaryChange?.Invoke(RuntimeSettingsSummary);
                    return true;
                } else {
                    return false;
                }
            }

            string filePath;

            // Method 1: Try to initialize using the external json file
            LogIfEnabled(LogType.Log,
                "Checking if RuntimeSettingsSummary can be initialized using external json file.");

            if (CanAccessExternalFiles) {
                filePath = _externalRuntimeSettingsSummaryFilePath;

                if (InitFromFile(filePath)) {
                    LogIfEnabled(LogType.Log, "Initialized RuntimeSettingsSummary using external json file. ");
                    return;
                }
            }

            // Method 2: Try to initialize using the cached json file
            LogIfEnabled(LogType.Warning, "RuntimeSettingsSummary cannot initialize using external json file. "
                                          + "Trying to initialize it using the cached json file. This is not an error. "
                                          + EXTERNAL_READ_WARNING_MSG);

            filePath = _cachedRuntimeSettingsSummaryPath;

            if (InitFromFile(filePath)) {
                LogIfEnabled(LogType.Log, "Initialized RuntimeSettingsSummary using cached json file. ");
                return;
            }

            // Method 3: If initialization using both external and cached json fails, we wait for the SDK to get connected
            // to the admin app and then request a RuntimeSettings refresh
            var msg = "Cannot initialize RuntimeSettingsSummary using any json file.";
            if (!IsConnectedToAdminApp) {
                msg += "Waiting for MXR Admin App connection to send it a RuntimeSettingsSummary refresh request.";
            }

            LogIfEnabled(LogType.Warning, msg);

            while (!IsConnectedToAdminApp) {
                await Task.Delay(100);
            }

            LogIfEnabled(LogType.Log,
                "Invoking RefreshRuntimeSettings to initialize RuntimeSettingsSummary using MXR Admin App");
            RefreshRuntimeSettings();
        }

        private async void InitializeDeviceData() {
            bool InitFromFile(string path) {
                if (DeserializeFromFile(path, out var contents, out DeviceData deviceData)) {
                    lastDeviceDataJSON = contents;
                    DeviceData = deviceData;
                    OnDeviceDataChange?.Invoke(deviceData);
                    return true;
                } else {
                    return false;
                }
            }

            var filePath = _cachedDeviceDataPath;

            // Method 1: Try to initialize using the cached json file
            LogIfEnabled(LogType.Log, "Checking if DeviceData can be initialized using the cached json file.");
            if (InitFromFile(filePath)) {
                LogIfEnabled(LogType.Log, "Initialized DeviceData using cached json file. ");
                return;
            }

            // Method 2: If initialization using cached json fails, we wait for the SDK to get connected
            // to the admin app and then request a DeviceData refresh
            var msg = "Cannot initialize DeviceData using cached json file.";
            if (!IsConnectedToAdminApp) {
                msg += "Waiting for MXR Admin App connection to send it a DeviceData refresh request.";
            }

            LogIfEnabled(LogType.Warning, msg);
            while (!IsConnectedToAdminApp) {
                await Task.Delay(100);
            }

            LogIfEnabled(LogType.Log, "Invoking RefreshDeviceData to initialize DeviceData using MXR Admin App");
            RefreshDeviceData();
        }

        private async void InitializeDeviceStatus() {
            bool InitFromFile(string path) {
                if (DeserializeFromFile(path, out var contents, out DeviceStatus deviceStatus)) {
                    lastDeviceStatusJSON = contents;
                    DeviceStatus = deviceStatus;
                    OnDeviceStatusChange?.Invoke(DeviceStatus);
                    return true;
                } else {
                    return false;
                }
            }

            string filePath;

            // Method 1: Try to initialize using the external json file
            LogIfEnabled(LogType.Log, "Checking if DeviceStatus can be initialized using external json file.");
            if (CanAccessExternalFiles) {
                filePath = _externalDeviceStatusPath;

                if (InitFromFile(filePath)) {
                    LogIfEnabled(LogType.Log, "Initialized DeviceStatus using external json file. ");
                    return;
                }
            }

            // Method 2: Try to initialize using the cached json file
            LogIfEnabled(LogType.Log, "DeviceStatus cannot initialize using external json file. "
                                      + "Trying to initialize it using the cached json file. This is not an error. "
                                      + EXTERNAL_READ_WARNING_MSG);

            filePath = _cachedDeviceStatusPath;

            if (InitFromFile(filePath)) {
                LogIfEnabled(LogType.Log, "Initialized DeviceStatus using cached json file. ");
                return;
            }

            // Method 3: If initialization using both external and cached json fails, we wait for the SDK to get connected
            // to the admin app and then request a DeviceStatus refresh
            var msg = "Cannot initialize DeviceStatus using any json file.";
            if (!IsConnectedToAdminApp) {
                msg += "Waiting for MXR Admin App connection to send it a DeviceStatus refresh request.";
            }

            LogIfEnabled(LogType.Warning, msg);
            while (!IsConnectedToAdminApp) {
                await Task.Delay(100);
            }

            LogIfEnabled(LogType.Log, "Invoking RefreshDeviceStatus to initialize DeviceStatus using MXR Admin App");
            RefreshDeviceStatus();
        }

        private bool DeserializeFromFile<T>(string filePath, out string contents, out T value) {
            try {
                if (!File.Exists(filePath)) {
                    contents = string.Empty;
                    value = default;
                    return false;
                }

                contents = File.ReadAllText(filePath);
                value = JsonConvert.DeserializeObject<T>(contents);
                return true;
            } catch (Exception e) {
                LogIfEnabled(e);
                contents = null;
                value = default;
                return false;
            }
        }
    }
}
