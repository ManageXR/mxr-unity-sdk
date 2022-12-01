#if UNITY_ANDROID
using System;
using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

        public bool IsAvailable => messenger.IsBoundToService;

        public event Action<List<ScannedWifiNetwork>> OnWifiNetworksChange;
        public List<ScannedWifiNetwork> WifiNetworks { get; private set; }
            = new List<ScannedWifiNetwork>();

        public event Action<WifiConnectionStatus> OnWifiConnectionStatusChange;
        public WifiConnectionStatus WifiConnectionStatus { get; private set; }
            = new WifiConnectionStatus();

        public event Action<RuntimeSettingsSummary> OnRuntimeSettingsSummaryChange;
        public RuntimeSettingsSummary RuntimeSettingsSummary { get; private set; }
            = new RuntimeSettingsSummary();

        public event Action<PlayVideoCommandData> OnPlayVideoCommand;

        public event Action<PauseVideoCommandData> OnPauseVideoCommand;

        public event Action OnHomeScreenStateRequest;

        public DeviceStatus DeviceStatus { get; private set; }
            = new DeviceStatus();
        public event Action<DeviceStatus> OnDeviceStatusChange;

        string lastWifiNetworksJSON = string.Empty;
        string lastWifiConnectionStatusJSON = string.Empty;
        string lastRuntimeSettingsSummaryJSON = string.Empty;
        string lastDeviceStatusJSON = string.Empty;

        public MXRAndroidSystem() {
            messenger = new AdminAppMessengerManager();

            RefreshRuntimeSettings();
            RefreshDeviceStatus();

            int WIFI_NETWORKS = 1000;
            int WIFI_CONNECTION_STATUS = 3000;
            int RUNTIME_SETTINGS = 4000;
            int DEVICE_STATUS = 5000;
            int HANDLE_COMMAND = 6000;
            int GET_HOME_SCREEN_STATE = 15000;

            // Subscribe to application focus change event and 
            // execute any command passed in extra strings
            Dispatcher.OnPlayerFocusChange += x => {
                if(x) {
                    var intent = MXRAndroidUtils.CurrentActivity.Call<AndroidJavaObject>("getIntent");
                    var commandJson = intent.Call<string>("getStringExtra", "command");

                    // TODO: Will this cause the same command to be executed
                    // everytime? Should the same consecutive command be ignored by the SDK
                    // or will the admin app clear extra strings itself?
                    if (!string.IsNullOrEmpty(commandJson))
                        ProcessCommandJson(commandJson);
                }
            };

            messenger.OnMessageFromAdminApp += (what, json) => {
                // Unescape json if it is escaped 
                // Ref: https://stackoverflow.com/a/26406504
                if (json.StartsWith("\"")) {
                    JToken token = JToken.Parse(json);
                    JObject obj = JObject.Parse((string)token);
                    json = obj.ToString();
                }

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
                    if (json.Equals(lastRuntimeSettingsSummaryJSON)) return;

                    RuntimeSettingsSummary summary = JsonConvert.DeserializeObject<RuntimeSettingsSummary>(json);
                    if (summary != null) {
                        RuntimeSettingsSummary = summary;
                        OnRuntimeSettingsSummaryChange?.Invoke(summary);
                    }
                }
                else if (what == DEVICE_STATUS) {
                    if (json.Equals(lastDeviceStatusJSON)) return;

                    DeviceStatus status = JsonConvert.DeserializeObject<DeviceStatus>(json);
                    if (status != null) {
                        DeviceStatus = status;
                        OnDeviceStatusChange?.Invoke(status);
                    }
                }
                else if (what == HANDLE_COMMAND) {
                    ProcessCommandJson(json);
                }
                else if (what == GET_HOME_SCREEN_STATE) {
                    OnHomeScreenStateRequest?.Invoke();
                }
            };
        }

        void ProcessCommandJson(string json) {
            try {
                var command = JsonUtility.FromJson<Command>(json);
                if (command == null)
                    Debug.LogError("Could not deserialize command json " + json);
                else {
                    switch (command.action) {
                        case CommandAction.PLAY_VIDEO:
                            var playVideoCommandData = JsonUtility.FromJson<PlayVideoCommandData>(command.data);
                            if (playVideoCommandData != null)
                                OnPlayVideoCommand?.Invoke(playVideoCommandData);
                            break;
                        case CommandAction.PAUSE_VIDEO:
                            var pauseVideoCommandData = JsonUtility.FromJson<PauseVideoCommandData>(command.data);
                            if (pauseVideoCommandData != null)
                                OnPauseVideoCommand?.Invoke(pauseVideoCommandData);
                            break;
                    }
                }
            }
            catch (Exception e) {
                Debug.LogError("Could not deserialize command json " + e);
            }
        }

        public void ConnectToWifiNetwork(string ssid, string password) {
            // Escape JSON string. Ref: https://stackoverflow.com/a/26152046
            // Then get rid of the encosing double quotes (") using substring
            ssid = JsonConvert.ToString(ssid);
            ssid = ssid.Substring(1, ssid.Length - 2);
            password = JsonConvert.ToString(password);
            password = password.Substring(1, password.Length - 2);

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
            // Escape JSON string. Ref: https://stackoverflow.com/a/26152046
            // Then get rid of the encosing double quotes (") using substring
            ssid = JsonConvert.ToString(ssid);
            ssid = ssid.Substring(1, ssid.Length - 2);

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
            if (IsAvailable)
                messenger.Native?.Call<bool>("getRuntimeSettingsAsync");
        }

        public void RefreshDeviceStatus() {
            if (IsAvailable)
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

        public void Sync() {
            if (messenger.IsBoundToService)
                messenger.Native?.Call<bool>("checkDbAsync");
        }

        public void SendHomeScreenState(HomeScreenState state) {
            if (messenger.IsBoundToService) {
                Debug.Log("SendHomeScreenState called.");
                // TODO: This might look something like this
                //messenger.Native?.Call<bool>("sendHomeScreenState", "state json");
            }
        }

        public void ExitLauncher() {
            if (messenger.IsBoundToService)
                messenger.Native?.Call<bool>("exitLauncherAsync");
        }
    }
}
#endif