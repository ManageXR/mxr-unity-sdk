using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace MXR.SDK {
    /// <summary>
    /// Handles message processing and communication with the MXR Admin App.
    /// Contains logic for receiving and processing various message types including WiFi status,
    /// device data, runtime settings, and streaming codes.
    /// </summary>
    public partial class MXRAndroidSystem {
        /// <summary>
        /// These should be in parity with the `AdminAppMessageTypes.java` class.
        /// Pleasure ensure parity before making any modifications.
        /// </summary>
        private static class AdminAppMessageTypes {
            public const int WIFI_NETWORKS = 1000;
            public const int WIFI_CONNECTION_STATUS = 3000;
            public const int RUNTIME_SETTINGS_SUMMARY = 4000;
            public const int DEVICE_DATA = 19000;
            public const int DEVICE_STATUS = 5000;
            public const int HANDLE_COMMAND = 6000;
            public const int GET_HOME_SCREEN_STATE = 15000;
            public const int STREAMING_CODE = 21000;
        }

        private void OnMessageFromAdminApp(int what, string json) {
            json = UnescapeJsonIfNeeded(json);

            switch (what) {
                case AdminAppMessageTypes.WIFI_NETWORKS:
                    HandleWifiNetworks(json);
                    break;
                case AdminAppMessageTypes.WIFI_CONNECTION_STATUS:
                    HandleWifiConnectionStatus(json);
                    break;
                case AdminAppMessageTypes.RUNTIME_SETTINGS_SUMMARY:
                    HandleRuntimeSettingsSummary(json);
                    break;
                case AdminAppMessageTypes.DEVICE_STATUS:
                    HandleDeviceStatus(json);
                    break;
                case AdminAppMessageTypes.DEVICE_DATA:
                    HandleDeviceData(json);
                    break;
                case AdminAppMessageTypes.STREAMING_CODE:
                    HandleStreamingCode(json);
                    break;
                case AdminAppMessageTypes.HANDLE_COMMAND:
                    ProcessCommandJson(json);
                    break;
                case AdminAppMessageTypes.GET_HOME_SCREEN_STATE:
                    OnHomeScreenStateRequest?.Invoke();
                    break;
            }
        }

        private void HandleWifiNetworks(string json) {
            if (json.Equals(lastWifiNetworksJSON)) {
                return;
            }

            var networks = JsonConvert.DeserializeObject<List<ScannedWifiNetwork>>(json);
            if (networks == null) {
                return;
            }

            lastDeviceStatusJSON = json;
            WifiNetworks = networks;
            OnWifiNetworksChange?.Invoke(networks);
            LogIfEnabled(LogType.Log, "WifiNetworks updated with " + WifiNetworks.Count + " networks.");
        }

        private void HandleWifiConnectionStatus(string json) {
            if (json.Equals(lastWifiConnectionStatusJSON)) {
                return;
            }

            var status = JsonConvert.DeserializeObject<WifiConnectionStatus>(json);
            if (status == null) {
                return;
            }

            lastWifiConnectionStatusJSON = json;
            WifiConnectionStatus = status;
            OnWifiConnectionStatusChange?.Invoke(status);
            LogIfEnabled(LogType.Log, "WifiConnectionStatus updated.");
        }

        private void HandleRuntimeSettingsSummary(string json) {
            if (json.Equals(lastRuntimeSettingsSummaryJSON)) {
                return;
            }

            File.WriteAllText(_cachedRuntimeSettingsSummaryPath, json);

            var summary = JsonConvert.DeserializeObject<RuntimeSettingsSummary>(json);
            if (summary == null) {
                return;
            }

            lastRuntimeSettingsSummaryJSON = json;
            RuntimeSettingsSummary = summary;
            OnRuntimeSettingsSummaryChange?.Invoke(summary);
            LogIfEnabled(LogType.Log, "RuntimeSettingsSummary updated.");
        }

        private void HandleDeviceStatus(string json) {
            if (json.Equals(lastDeviceStatusJSON)) {
                return;
            }

            File.WriteAllText(_cachedDeviceStatusPath, json);

            var status = JsonConvert.DeserializeObject<DeviceStatus>(json);
            if (status == null) {
                return;
            }

            lastDeviceStatusJSON = json;
            DeviceStatus = status;
            OnDeviceStatusChange?.Invoke(status);
            LogIfEnabled(LogType.Log, "DeviceStatus updated.");
        }

        private void HandleDeviceData(string json) {
            if (json.Equals(lastDeviceDataJSON)) {
                return;
            }

            File.WriteAllText(_cachedDeviceDataPath, json);

            var data = JsonConvert.DeserializeObject<DeviceData>(json);
            if (data == null) {
                return;
            }

            lastDeviceDataJSON = json;
            DeviceData = data;
            OnDeviceDataChange?.Invoke(data);
            LogIfEnabled(LogType.Log, "DeviceData updated.");
        }

        private void HandleStreamingCode(string json) {
            var streamingCodeData = JsonConvert.DeserializeObject<StreamingCodeStatus>(json);
            if (streamingCodeData == null) {
                return;
            }

            StreamingCodeStatus = streamingCodeData;
            OnStreamingCodeStatusChanged?.Invoke(streamingCodeData);
            LogIfEnabled(LogType.Log, "StreamingCodeStatus updated.");
        }
    }
}
