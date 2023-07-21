#if UNITY_ANDROID
using System;
using System.Collections.Generic;
using System.IO;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using UnityEngine;

namespace MXR.SDK {
    /// <summary>
    /// A bridge to communicate with the native MXR admin application
    /// </summary>
    public class MXRAndroidSystem : IMXRSystem {
        static readonly string TAG = "[MXRAndroidSystem]";
        readonly AdminAppMessengerManager messenger;

        /// <summary>
        /// The messenger used for sending and receiving messages
        /// between Unity and Admin App
        /// </summary>
        public AdminAppMessengerManager Messenger => messenger;

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

        bool loggingEnabled = true;
        public bool LoggingEnabled {
            get => loggingEnabled;
            set => loggingEnabled = value;
        }

        public bool IsAvailable => messenger.IsBoundToService;

        public DeviceStatus DeviceStatus { get; private set; }
        public RuntimeSettingsSummary RuntimeSettingsSummary { get; private set; }
        public List<ScannedWifiNetwork> WifiNetworks { get; private set; }
        public WifiConnectionStatus WifiConnectionStatus { get; private set; }

        public event Action<bool> OnAvailabilityChange;
        public event Action<RuntimeSettingsSummary> OnRuntimeSettingsSummaryChange;
        public event Action<DeviceStatus> OnDeviceStatusChange;
        public event Action<List<ScannedWifiNetwork>> OnWifiNetworksChange;
        public event Action<WifiConnectionStatus> OnWifiConnectionStatusChange;
        public event Action<PlayVideoCommandData> OnPlayVideoCommand;
        public event Action<PauseVideoCommandData> OnPauseVideoCommand;
        public event Action OnHomeScreenStateRequest;

        string lastWifiNetworksJSON = string.Empty;
        string lastWifiConnectionStatusJSON = string.Empty;
        string lastRuntimeSettingsSummaryJSON = string.Empty;
        string lastDeviceStatusJSON = string.Empty;

        public MXRAndroidSystem() {
            if (LoggingEnabled)
                Debug.unityLogger.Log(LogType.Log, TAG, "Initializing MXRAndroidSystem");

            messenger = new AdminAppMessengerManager();
            OnAvailabilityChange?.Invoke(messenger.IsBoundToService);
            messenger.OnBoundStatusToAdminAppChanged += x => 
                OnAvailabilityChange?.Invoke(x);

            TryInitializeRuntimeSettingsSummary();
            TryInitializeDeviceStatus();
            RefreshWifiConnectionStatus();
            RefreshWifiNetworks();

            int WIFI_NETWORKS = 1000;
            int WIFI_CONNECTION_STATUS = 3000;
            int RUNTIME_SETTINGS_SUMMARY = 4000;
            int DEVICE_STATUS = 5000;
            int HANDLE_COMMAND = 6000;
            int GET_HOME_SCREEN_STATE = 15000;

            // Subscribe to application focus change event and 
            // execute any command passed in extra strings
            Dispatcher.OnPlayerFocusChange += x => {
                if (LoggingEnabled)
                    Debug.unityLogger.Log(LogType.Log, TAG, "SDK Focus: " + x);
                if (!x) return;

                TryExecuteIntentCommands();
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
                        lastDeviceStatusJSON = json;
                        WifiNetworks = networks;
                        OnWifiNetworksChange?.Invoke(networks);
                        if (LoggingEnabled)
                            Debug.unityLogger.Log(LogType.Log, TAG, "WifiNetworks updated with " + WifiNetworks.Count + " networks.");
                    }
                }
                else if (what == WIFI_CONNECTION_STATUS) {
                    if (json.Equals(lastWifiConnectionStatusJSON)) return;

                    WifiConnectionStatus status = JsonConvert.DeserializeObject<WifiConnectionStatus>(json);
                    if (status != null) {
                        lastWifiConnectionStatusJSON = json;
                        WifiConnectionStatus = status;
                        OnWifiConnectionStatusChange?.Invoke(status);
                        if (LoggingEnabled)
                            Debug.unityLogger.Log(LogType.Log, TAG, "WifiConnectionStatus updated.");
                    }
                }
                else if (what == RUNTIME_SETTINGS_SUMMARY) {
                    if (json.Equals(lastRuntimeSettingsSummaryJSON)) return;

                    RuntimeSettingsSummary summary = JsonConvert.DeserializeObject<RuntimeSettingsSummary>(json);
                    if (summary != null) {
                        lastRuntimeSettingsSummaryJSON = json;
                        RuntimeSettingsSummary = summary;
                        OnRuntimeSettingsSummaryChange?.Invoke(summary);
                        if (LoggingEnabled)
                            Debug.unityLogger.Log(LogType.Log, TAG, "RuntimeSettingsSummary updated.");
                    }
                }
                else if (what == DEVICE_STATUS) {
                    if (json.Equals(lastDeviceStatusJSON)) return;

                    DeviceStatus status = JsonConvert.DeserializeObject<DeviceStatus>(json);
                    if (status != null) {
                        lastDeviceStatusJSON = json;
                        DeviceStatus = status;
                        OnDeviceStatusChange?.Invoke(status);
                        if (LoggingEnabled)
                            Debug.unityLogger.Log(LogType.Log, TAG, "DeviceStatus updated.");
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

        // When receiving commands through the Android intent, we get
        // the values in a flattened manner. Schema:
        // {intentID=VALUE,action=VALUE, videoId=VALUE, playFromBeginning=VALUE}
        // 
        // This is different from how the messenger manager sends it, the root has action:string and 
        // data:object where the latter has the string:videoId and 
        // bool:playFromBeginning. Schema:
        // {action=VALUE, data={videoId=VALUE, fromFromBeginning=VALUE}}
        //
        // We currently only handle the PLAY_VIDEO action through intents
        // so we first check if the "action" and "videoId" keys are present
        // in the intent bundle and then create a command object as a dictionary,
        // serialize it to json, and send it for processing. We attempt to read the
        // "playFromBeginning" boolean, but if not found, we default to true.
        readonly HashSet<string> executedIntentIds = new HashSet<string>();
        void TryExecuteIntentCommands() {
            if (LoggingEnabled)
                Debug.unityLogger.Log(LogType.Log, TAG, "Checking for intent commands");

            if (!MXRAndroidUtils.HasIntentExtra("intentId")) {
                if (LoggingEnabled)
                    Debug.unityLogger.Log(LogType.Log, TAG, "No 'intentId' key found in intent extras.");
                return;
            }

            if (!MXRAndroidUtils.HasIntentExtra("action")) {
                if (LoggingEnabled)
                    Debug.unityLogger.Log(LogType.Log, TAG, "No 'action' key found in intent extras.");
                return;
            }

            var action = MXRAndroidUtils.GetIntentStringExtra("action");
            if (!action.Equals("PLAY_VIDEO")) {
                if (LoggingEnabled)
                    Debug.unityLogger.Log(LogType.Log, TAG, 
                        "Only PLAY_VIDEO command action is supported via intents.");
                return;
            }
            else if (LoggingEnabled)
                    Debug.unityLogger.Log(LogType.Log, TAG, "PLAY_VIDEO command action found.");

            if (!MXRAndroidUtils.HasIntentExtra("videoId")) {
                if (LoggingEnabled)
                    Debug.unityLogger.Log(LogType.Error, TAG, "No 'videoId' key found in intent extras.");
                return;
            }

            var intentId = MXRAndroidUtils.GetIntentStringExtra("intentId");
            var videoId = MXRAndroidUtils.GetIntentStringExtra("videoId");
            var playFromBeginning = MXRAndroidUtils.GetIntentBooleanExtra("playFromBeginning", true);

            var commandObj = new Dictionary<string, object> {
                {"action", "PLAY_VIDEO" },
                {"data", new Dictionary<string, object> {
                    {"videoId", videoId },
                    {"playFromBeginning", playFromBeginning }
                } }
            };

            executedIntentIds.Add(intentId);
            ProcessCommandJson(JsonConvert.SerializeObject(commandObj));
        }

        void ProcessCommandJson(string json) {
            try {
                if (LoggingEnabled)
                    Debug.unityLogger.Log(LogType.Log, TAG, "Command received: " + json);

                var command = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                if (command == null) {
                    if (LoggingEnabled)
                        Debug.unityLogger.Log(LogType.Error, TAG, "Could not deserialize command json string." + json);
                    return;
                }

                var action = command["action"].ToString();
                var data = command["data"].ToString();

                switch (action) {
                    case Command.PLAY_VIDEO_ACTION:
                        var playVideoCommandData = JsonUtility.FromJson<PlayVideoCommandData>(data);
                        if (playVideoCommandData != null)
                            OnPlayVideoCommand?.Invoke(playVideoCommandData);
                        else if (LoggingEnabled)
                            Debug.unityLogger.Log(LogType.Error, TAG, "Could not deserialize command data string.");
                        break;
                    case Command.PAUSE_VIDEO_ACTION:
                        var pauseVideoCommandData = JsonUtility.FromJson<PauseVideoCommandData>(data);
                        if (pauseVideoCommandData != null)
                            OnPauseVideoCommand?.Invoke(pauseVideoCommandData);
                        else if (LoggingEnabled)
                            Debug.unityLogger.Log(LogType.Error, TAG, "Could not deserialize command data string.");
                        break;
                }
            }
            catch (Exception e) {
                if (LoggingEnabled)
                    Debug.unityLogger.Log(LogType.Exception, TAG, new Exception("Could not process command json", e));
            }
        }

        public void ConnectToWifiNetwork(string ssid, string password) {
            ssid = EscapeStringToJsonString(ssid);
            password = EscapeStringToJsonString(password);

            if (messenger.IsBoundToService) {
                if (LoggingEnabled)
                    Debug.unityLogger.Log(LogType.Log, TAG, "ConnectToWifiNetwork called. Invoking over JNI: connectToWifiNetworkAsync");
                messenger.Call<bool>("connectToWifiNetworkAsync", ssid, password);
            }
            else if (LoggingEnabled)
                Debug.unityLogger.Log(LogType.Warning, TAG, "ConnectToWifiNetwork ignored. System is not available (not bound to messenger.");
        }

        public void DisableWifi() {
            if (messenger.IsBoundToService) {
                if (LoggingEnabled)
                    Debug.unityLogger.Log(LogType.Log, TAG, "DisableWifi called. Invoking over JNI: disableWifiAsync");
                messenger.Call<bool>("disableWifiAsync");
            }
            else if (LoggingEnabled)
                Debug.unityLogger.Log(LogType.Warning, TAG, "DisableWifi ignored. System is not available (not bound to messenger.");
        }

        public void EnableWifi() {
            if (messenger.IsBoundToService) {
                if (LoggingEnabled)
                    Debug.unityLogger.Log(LogType.Log, TAG, "EnableWifi called. Invoking over JNI: enableWifiAsync");
                messenger.Call<bool>("enableWifiAsync");
            }
            else if (LoggingEnabled)
                Debug.unityLogger.Log(LogType.Warning, TAG, "EnableWifi ignored. System is not available (not bound to messenger.");
        }

        public void ForgetWifiNetwork(string ssid) {
            ssid = EscapeStringToJsonString(ssid);

            if (messenger.IsBoundToService) {
                if (LoggingEnabled)
                    Debug.unityLogger.Log(LogType.Log, TAG, "ForgetWifiNetwork called. Invoking over JNI: forgetWifiNetworkAsync");
                messenger.Call<bool>("forgetWifiNetworkAsync", ssid);
            }
            else if (LoggingEnabled)
                Debug.unityLogger.Log(LogType.Warning, TAG, "ForgetWifiNetwork ignored. System is not available (not bound to messenger.");
        }

        public void RefreshWifiNetworks() {
            if (messenger.IsBoundToService) {
                if (LoggingEnabled)
                    Debug.unityLogger.Log(LogType.Log, TAG, "RefreshWifiNetworks called. Invoking over JNI: getWifiNetworksAsync");
                messenger.Call<bool>("getWifiNetworksAsync");
            }
            else if (LoggingEnabled)
                Debug.unityLogger.Log(LogType.Warning, TAG, "RefreshWifiNetworks ignored. System is not available (not bound to messenger.");
        }

        public void RefreshWifiConnectionStatus() {
            if (messenger.IsBoundToService) {
                if (LoggingEnabled)
                    Debug.unityLogger.Log(LogType.Log, TAG, "RefreshWifiConnectionStatus called. Invoking over JNI: getWifiConnectionStatusAsync");
                messenger.Call<bool>("getWifiConnectionStatusAsync");
            }
            else if (LoggingEnabled)
                Debug.unityLogger.Log(LogType.Warning, TAG, "RefreshWifiConnectionStatus ignored. System is not available (not bound to messenger.");
        }

        public void RefreshRuntimeSettings() {
            if (IsAvailable) {
                if (LoggingEnabled)
                    Debug.unityLogger.Log(LogType.Log, TAG, "RefreshRuntimeSettings called. Invoking over JNI: getRuntimeSettingsAsync");
                messenger.Call<bool>("getRuntimeSettingsAsync");
            }
            else if (LoggingEnabled)
                Debug.unityLogger.Log(LogType.Warning, TAG, "RefreshRuntimeSettings ignored. System is not available (not bound to messenger.");
        }

        public void RefreshDeviceStatus() {
            if (IsAvailable) {
                if (LoggingEnabled)
                    Debug.unityLogger.Log(LogType.Log, TAG, "RefreshDeviceStatus called. Invoking over JNI: getDeviceStatusAsync");
                messenger.Call<bool>("getDeviceStatusAsync");
            }
            else if (LoggingEnabled)
                Debug.unityLogger.Log(LogType.Warning, TAG, "RefreshDeviceStatus ignored. System is not available (not bound to messenger.");
        }

        public void EnableKioskMode() {
            if (messenger.IsBoundToService) {
                if (LoggingEnabled)
                    Debug.unityLogger.Log(LogType.Log, TAG, "EnableKioskMode called. Invoking over JNI: enableKioskModeAsync");
                messenger.Call<bool>("enableKioskModeAsync");
            }
            else if (LoggingEnabled)
                Debug.unityLogger.Log(LogType.Warning, TAG, "EnableKioskMode ignored. System is not available (not bound to messenger.");
        }

        public void DisableKioskMode() {
            if (messenger.IsBoundToService) {
                if (LoggingEnabled)
                    Debug.unityLogger.Log(LogType.Log, TAG, "DisableKioskMode called. Invoking over JNI: disableKioskModeAsync");
                messenger.Call<bool>("disableKioskModeAsync");
            }
            else if (LoggingEnabled)
                Debug.unityLogger.Log(LogType.Warning, TAG, "DisableKioskMode ignored. System is not available (not bound to messenger.");
        }

        public void KillApp(string packageName) {
            packageName = EscapeStringToJsonString(packageName);

            if (messenger.IsBoundToService) {
                if (LoggingEnabled)
                    Debug.unityLogger.Log(LogType.Log, TAG, "KillApp called. Invoking over JNI: killApp");
                messenger.Call<bool>("killApp", packageName);
            }
            else if (LoggingEnabled)
                Debug.unityLogger.Log(LogType.Warning, TAG, "KillApp ignored. System is not available (not bound to messenger).");
        }

        public void RestartApp(string packageName) {
            packageName = EscapeStringToJsonString(packageName);

            if (messenger.IsBoundToService) {
                if (LoggingEnabled)
                    Debug.unityLogger.Log(LogType.Log, TAG, "RestartApp called. Invoking over JNI: restartApp");
                messenger.Call<bool>("restartApp", packageName);
            }
            else if (LoggingEnabled)
                Debug.unityLogger.Log(LogType.Warning, TAG, "restartApp ignored. System is not available (not bound to messenger).");
        }

        public void Sync() {
            if (messenger.IsBoundToService) {
                if (LoggingEnabled)
                    Debug.unityLogger.Log(LogType.Log, TAG, "Sync called. Invoking over JNI: checkDbAsync");
                messenger.Call<bool>("checkDbAsync");
            }
            else if (LoggingEnabled)
                Debug.unityLogger.Log(LogType.Warning, TAG, "Sync ignored. System is not available (not bound to messenger.");
        }

        public void SendHomeScreenState(HomeScreenState state) {
            if(messenger.IsBoundToService) {
                try {
                    var stateJson = JsonConvert.SerializeObject(state);
                    messenger.Call<bool>("sendHomeScreenState", stateJson);
                    if (LoggingEnabled)
                        Debug.unityLogger.Log(LogType.Log, TAG, "SendHomeScreenState called. Invoking over JNI: sendHomeScreenState");
                }
                catch(Exception e) {
                    Debug.LogError("An error occured while trying to send homescreen state " + e);
                }
            }
            else if (LoggingEnabled)
                Debug.unityLogger.Log(LogType.Warning, TAG, "SendHomeScreenState ignored. System is not available (not bound to messenger.");
        }

        public void ExitLauncher() {
            if (messenger.IsBoundToService) {
                if (LoggingEnabled)
                    Debug.unityLogger.Log(LogType.Log, TAG, "ExitLauncher called. Invoking over JNI: exitLauncherAsync");
                messenger.Call<bool>("exitLauncherAsync");
            }
            else if (LoggingEnabled)
                Debug.unityLogger.Log(LogType.Warning, TAG, "ExitLauncher ignored. System is not available (not bound to messenger.");
        }

        #region INITIALIZATION 
        void TryInitializeRuntimeSettingsSummary() {
            // On Android30 and above, we check if we have storage access. Otherwise file read attempt will fail.
            if(MXRAndroidUtils.AndroidSDKAsInt >= 30 && !MXRAndroidUtils.IsExternalStorageManager) {
                if (LoggingEnabled)
                    Debug.unityLogger.LogWarning(TAG, "Not initializing RuntimeSettingsSummary using local json, " 
                    + "the SDK will fallback to trying to initialize it using the Android Messenger. This is not an error. "
                    + EXTERNAL_STORAGE_MANAGER_WARNING_MSG);
                return;
            }

            var subPath = "MightyImmersion/runtimeSettingsSummary.json";
            if (DeserializeFromFile(subPath, out string contents, out RuntimeSettingsSummary runtimeSettingsSummary)) {
                lastRuntimeSettingsSummaryJSON = contents;
                RuntimeSettingsSummary = runtimeSettingsSummary;
                OnRuntimeSettingsSummaryChange?.Invoke(RuntimeSettingsSummary);
            }
            else {
                var msg = "Could not deserialize RuntimeSettingsSummary from " + subPath +
                    "Invoking RefreshRuntimeSettingsSummary to request from AdminAppMessengerManager";
                if (LoggingEnabled) Debug.unityLogger.Log(LogType.Warning, TAG, msg);
                RefreshRuntimeSettings();
            }
        }

        void TryInitializeDeviceStatus() {
            // On Android30 and above, we check if we have storage access. Otherwise file read attempt will fail.
            if (MXRAndroidUtils.AndroidSDKAsInt >= 30 && !MXRAndroidUtils.IsExternalStorageManager) {
                if (LoggingEnabled)
                    Debug.unityLogger.LogWarning(TAG, "Not initializing DeviceStatus using local json, "
                    + "the SDK will fallback to trying to initialize it using the Android Messenger. This is not an error. "
                    + EXTERNAL_STORAGE_MANAGER_WARNING_MSG);
                return;
            }

            var subPath = "MightyImmersion/deviceStatus.json";
            if (DeserializeFromFile(subPath, out string contents, out DeviceStatus deviceStatus)) {
                lastDeviceStatusJSON = contents;
                DeviceStatus = deviceStatus;
                OnDeviceStatusChange?.Invoke(DeviceStatus);
            }
            else {
                var msg = "Could not deserialize DeviceStatus from " + subPath +
                    "Invoking RefreshDeviceStatus to request from AdminAppMessengerManager";
                if (LoggingEnabled) Debug.unityLogger.Log(LogType.Warning, TAG, msg);
                RefreshDeviceStatus();
            }
        }

        bool DeserializeFromFile<T>(string subPath, out string contents, out T value) {
            try {
                var filePath = MXRStorage.GetFullPath(subPath);
                contents = File.ReadAllText(filePath);
                value = JsonConvert.DeserializeObject<T>(contents);
                return true;
            }
            catch (Exception e) {
                if (LoggingEnabled)
                    Debug.unityLogger.Log(LogType.Error, TAG, e);
                contents = null;
                value = default;
                return false;
            }
        }
        #endregion

        string EscapeStringToJsonString(string input) {
            // Escape JSON string. Ref: https://stackoverflow.com/a/26152046
            // Then get rid of the encosing double quotes (") using substring
            string output = JsonConvert.ToString(input);
            output = output.Substring(1, output.Length - 2);

            return output;
        }

        const string EXTERNAL_STORAGE_MANAGER_WARNING_MSG =
            "On Android 30 and above, request Manage All Files permission to read local files. " +
            "A helper method MXRAndroidUtils.RequestManageAllFilesPermission() is provided in the SDK for the same. ";
    }
}
#endif