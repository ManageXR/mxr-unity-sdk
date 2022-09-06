using Newtonsoft.Json;

using System;
using System.Collections.Generic;

using UnityEngine;

namespace MXR.SDK {
    /// <summary>
    /// A bridge to communicate with the native MXR admin application
    /// </summary>
    public class MXRAndroidSystem : IMXRSystem {
        readonly AdminAppMessengerManager messenger;

        public ScannedWifiNetwork CurrentNetwork {
            get {
                if (Application.isEditor) return null;
                var currentSsid = WifiConnectionStatus.ssid;
                foreach (var network in WifiNetworks)
                    if (network.ssid.Equals(currentSsid))
                        return network;
                return null;
            }
        }

        public List<ScannedWifiNetwork> WifiNetworks { get; private set; }
            = new List<ScannedWifiNetwork>();
        public event Action<List<ScannedWifiNetwork>> OnWifiNetworksChange;

        public WifiConnectionStatus WifiConnectionStatus { get; private set; }
            = new WifiConnectionStatus();
        public event Action<WifiConnectionStatus> OnWifiConnectionStatusChange;

        public RuntimeSettingsSummary RuntimeSettingsSummary { get; private set; }
            = new RuntimeSettingsSummary();
        public event Action<RuntimeSettingsSummary> OnRuntimeSettingsSummaryChange;

        public DeviceStatus DeviceStatus { get; private set; }
            = new DeviceStatus();
        public event Action<DeviceStatus> OnDeviceStatusChange;

        public MXRAndroidSystem() {
            messenger = new AdminAppMessengerManager();
            int WIFI_NETWORKS = 1000;
            int WIFI_CONNECTION_STATUS = 3000;
            int RUNTIME_SETTINGS = 4000;
            int DEVICE_STATUS = 5000;

            string lastWifiNetworksJSON = string.Empty;
            string lastWifiConnectionStatusJSON = string.Empty;
            string lastRuntimeSettingsSummaryJSON = string.Empty;
            string lastDeviceStatusJSON = string.Empty;

            messenger.OnMessageFromAdminApp += (what, json) => {
                if (what == WIFI_NETWORKS) {
                    if (json.Equals(lastWifiNetworksJSON)) return;

                    List<ScannedWifiNetwork> networks = JsonConvert.DeserializeObject<List<ScannedWifiNetwork>>(json);
                    if (networks != null) {
                        WifiNetworks = networks;
                        OnWifiNetworksChange?.Invoke(networks);
                    }
                }
                else if (what == WIFI_CONNECTION_STATUS) {
                    if (json.Equals(lastWifiConnectionStatusJSON)) return;

                    WifiConnectionStatus status = JsonConvert.DeserializeObject<WifiConnectionStatus>(json);
                    if (status != null) {
                        WifiConnectionStatus = status;
                        OnWifiConnectionStatusChange?.Invoke(status);
                    }
                }
                else if (what == RUNTIME_SETTINGS) {
                    // TODO: Test if this works
                    if (json.Equals(lastRuntimeSettingsSummaryJSON)) return;

                    RuntimeSettingsSummary summary = JsonConvert.DeserializeObject<RuntimeSettingsSummary>(json);
                    if (summary != null) {
                        RuntimeSettingsSummary = summary;
                        OnRuntimeSettingsSummaryChange?.Invoke(summary);
                    }
                }
                else if (what == DEVICE_STATUS) {
                    // TODO: Test if this works
                    if (json.Equals(lastDeviceStatusJSON)) return;

                    DeviceStatus status = JsonConvert.DeserializeObject<DeviceStatus>(json);
                    if (status != null) {
                        DeviceStatus = status;
                        OnDeviceStatusChange?.Invoke(status);
                    }
                }
            };
        }

        public void ConnectToWifiNetwork(string ssid, string password) {
            if (messenger.IsBoundToService)
                messenger.Native?.Call<bool>("connectToWifiNetworkAsync", ssid, password);
        }

        public void DisableWifi() {
            if (messenger.IsBoundToService)
                messenger.Native?.Call<bool>("disableWifiAsync");
        }

        public void EnableWifi() {
            if (messenger.IsBoundToService)
                messenger.Native?.Call<bool>("enableWifiAsync");
        }

        public void ForgetWifiNetwork(string ssid) {
            if (messenger.IsBoundToService)
                messenger.Native?.Call<bool>("forgetWifiNetworkAsync", ssid);
        }

        public void RefreshWifiNetworks() {
            if (messenger.IsBoundToService)
                messenger.Native?.Call<bool>("getWifiNetworksAsync");
        }

        public void RefreshWifiConnectionStatus() {
            if (messenger.IsBoundToService)
                messenger.Native?.Call<bool>("getWifiConnectionStatusAsync");
        }

        public void RefreshRuntimeSettings() {
            if (messenger.IsBoundToService)
                messenger.Native?.Call<bool>("getRuntimeSettingsAsync");
        }

        public void RefreshDeviceStatus() {
            if (messenger.IsBoundToService)
                messenger.Native?.Call<bool>("getDeviceStatusAsync");
        }

        public void EnableKioskMode() {
            if (messenger.IsBoundToService)
                messenger.Native?.Call<bool>("enableKioskModeAsync");
        }

        public void DisableKioskMode() {
            if (messenger.IsBoundToService)
                messenger.Native?.Call<bool>("disableKioskModeAsync");
        }

        public void ExitLauncher() {
            if (messenger.IsBoundToService)
                messenger.Native?.Call<bool>("exitLauncherAsync");
        }

        public void Sync() {
            if (messenger.IsBoundToService)
                messenger.Native?.Call<bool>("checkDbAsync");
        }
    }
}
