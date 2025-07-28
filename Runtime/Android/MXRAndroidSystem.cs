#if UNITY_ANDROID
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MXR.SDK {
    /// <summary>
    /// A bridge to communicate with the native MXR admin application
    /// </summary>
    public class MXRAndroidSystem : IMXRSystem {
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

        private static readonly string TAG = "[MXRAndroidSystem]";
        private readonly AdminAppMessengerManager messenger;

        /// <summary>
        /// The messenger used for sending and receiving messages
        /// between Unity and Admin App
        /// </summary>
        public AdminAppMessengerManager Messenger => messenger;

        public ScannedWifiNetwork CurrentNetwork {
            get {
                if (Application.isEditor) {
                    return null;
                }

                var currentSsid = WifiConnectionStatus.ssid;
                foreach (var network in WifiNetworks) {
                    if (network.ssid.Equals(currentSsid)) {
                        return network;
                    }
                }

                return null;
            }
        }

        private bool loggingEnabled = true;

        public bool LoggingEnabled {
            get => loggingEnabled;
            set => loggingEnabled = value;
        }

        public bool IsAdminAppInstalled =>
            MXRAndroidUtils.NativeUtils.SafeCall<bool>("isAdminAppInstalled");

        public bool IsConnectedToAdminApp => IsAvailable;
        public bool IsAvailable => messenger.IsBoundToService;

        public DeviceData DeviceData { get; private set; }
        public DeviceStatus DeviceStatus { get; private set; }
        public RuntimeSettingsSummary RuntimeSettingsSummary { get; private set; }
        public List<ScannedWifiNetwork> WifiNetworks { get; private set; }
        public WifiConnectionStatus WifiConnectionStatus { get; private set; }
        public StreamingCodeStatus StreamingCodeStatus { get; private set; }

        public event Action<bool> OnAvailabilityChange;
        public event Action<DeviceData> OnDeviceDataChange;
        public event Action<RuntimeSettingsSummary> OnRuntimeSettingsSummaryChange;
        public event Action<DeviceStatus> OnDeviceStatusChange;
        public event Action<List<ScannedWifiNetwork>> OnWifiNetworksChange;
        public event Action<WifiConnectionStatus> OnWifiConnectionStatusChange;
        public event Action<PlayVideoCommandData> OnPlayVideoCommand;
        public event Action<PauseVideoCommandData> OnPauseVideoCommand;
        public event Action<ResumeVideoCommandData> OnResumeVideoCommand;
        public event Action<LaunchMXRHomeScreenCommandData> OnLaunchMXRHomeScreenCommand;
        public event Action<StreamingCodeStatus> OnStreamingCodeStatusChanged;
        public event Action OnHomeScreenStateRequest;

        private string lastWifiNetworksJSON = string.Empty;
        private string lastWifiConnectionStatusJSON = string.Empty;
        private string lastRuntimeSettingsSummaryJSON = string.Empty;
        private string lastDeviceStatusJSON = string.Empty;
        private string lastDeviceDataJSON = string.Empty;

        private string _cachedJsonDirectory = Path.Combine(Application.persistentDataPath, "ManageXR");

        private string _cachedRuntimeSettingsSummaryPath =>
            Path.Combine(_cachedJsonDirectory, "runtimeSettingsSummary.json");

        private string _cachedDeviceStatusPath =>
            Path.Combine(_cachedJsonDirectory, "deviceStatus.json");

        private string _cachedDeviceDataPath =>
            Path.Combine(_cachedJsonDirectory, "deviceData.json");

        private string _externalRuntimeSettingsSummaryFilePath =>
            MXRStorage.GetFullPath("MightyImmersion/runtimeSettingsSummary.json");

        private string _externalDeviceStatusPath =>
            MXRStorage.GetFullPath("MightyImmersion/deviceStatus.json");

        public MXRAndroidSystem() {
            LogIfEnabled(LogType.Log, "Initializing MXRAndroidSystem");
            Directory.CreateDirectory(_cachedJsonDirectory);

            messenger = new AdminAppMessengerManager();
            OnAvailabilityChange?.Invoke(messenger.IsBoundToService);
            messenger.OnBoundStatusToAdminAppChanged += x =>
                OnAvailabilityChange?.Invoke(x);

            if (MXRAndroidUtils.IsDeviceDataSupported) {
                InitializeDeviceData();
            }

            InitializeRuntimeSettingsSummary();
            InitializeDeviceStatus();
            RefreshWifiConnectionStatus();
            RefreshWifiNetworks();

            // Subscribe to application focus change event and 
            // execute any command passed in extra strings
            Dispatcher.OnPlayerFocusChange += OnPlayerFocusChange;
            messenger.OnMessageFromAdminApp += OnMessageFromAdminApp;
        }

        private void OnPlayerFocusChange(bool x) {
            LogIfEnabled(LogType.Log, "SDK Focus: " + x);

            if (!x) {
                return;
            }

            TryExecuteIntentCommands();
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

        /// <summary>
        /// Unescape json if it is escaped 
        /// Ref: https://stackoverflow.com/a/26406504
        /// </summary>
        private static string UnescapeJsonIfNeeded(string json) {
            if (!json.StartsWith("\"")) {
                return json;
            }

            var token = JToken.Parse(json);
            var obj = JObject.Parse((string)token);
            json = obj.ToString();

            return json;
        }

        private void LogIfEnabled(LogType logType, string message) {
            if (LoggingEnabled) {
                Debug.unityLogger.Log(logType, TAG, message);
            }
        }

        private void LogIfEnabled(Exception exception) {
            if (LoggingEnabled) {
                Debug.unityLogger.Log(LogType.Exception, TAG, exception);
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
        private readonly HashSet<string> executedIntentIds = new();

        private void TryExecuteIntentCommands() {
            LogIfEnabled(LogType.Log, "Checking for intent commands");

            if (!MXRAndroidUtils.HasIntentExtra("intentId")) {
                LogIfEnabled(LogType.Log, "No 'intentId' key found in intent extras.");
                return;
            }

            if (!MXRAndroidUtils.HasIntentExtra("action")) {
                LogIfEnabled(LogType.Log, "No 'action' key found in intent extras.");
                return;
            }

            var action = MXRAndroidUtils.GetIntentStringExtra("action");
            if (!action.Equals("PLAY_VIDEO")) {
                LogIfEnabled(LogType.Log, "Only PLAY_VIDEO command action is supported via intents.");
                return;
            }

            LogIfEnabled(LogType.Log, "PLAY_VIDEO command action found.");

            if (!MXRAndroidUtils.HasIntentExtra("videoId")) {
                LogIfEnabled(LogType.Error, "No 'videoId' key found in intent extras.");
                return;
            }

            var intentId = MXRAndroidUtils.GetIntentStringExtra("intentId");
            var videoId = MXRAndroidUtils.GetIntentStringExtra("videoId");
            var playFromBeginning = MXRAndroidUtils.GetIntentBooleanExtra("playFromBeginning", true);

            var commandObj = new Dictionary<string, object> {
                { "action", "PLAY_VIDEO" }, {
                    "data", new Dictionary<string, object> {
                        { "videoId", videoId },
                        { "playFromBeginning", playFromBeginning }
                    }
                }
            };

            executedIntentIds.Add(intentId);
            ProcessCommandJson(JsonConvert.SerializeObject(commandObj));
        }

        private void ProcessCommandJson(string json) {
            try {
                LogIfEnabled(LogType.Log, $"Command received: {json}");

                var command = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                if (command == null) {
                    LogIfEnabled(LogType.Error, $"Could not deserialize command json string: {json}");
                    return;
                }

                var action = command["action"].ToString();
                var data = command["data"].ToString();

                switch (action) {
                    case Command.LAUNCH_ACTION:
                        var launchCommandData = JsonUtility.FromJson<LaunchMXRHomeScreenCommandData>(data);
                        if (launchCommandData != null) {
                            OnLaunchMXRHomeScreenCommand?.Invoke(launchCommandData);
                        } else {
                            LogIfEnabled(LogType.Error, "Could not deserialize command data string.");
                        }

                        break;
                    case Command.PLAY_VIDEO_ACTION:
                        var playVideoCommandData = JsonUtility.FromJson<PlayVideoCommandData>(data);
                        if (playVideoCommandData != null) {
                            OnPlayVideoCommand?.Invoke(playVideoCommandData);
                        } else {
                            LogIfEnabled(LogType.Error, "Could not deserialize command data string.");
                        }

                        break;
                    case Command.PAUSE_VIDEO_ACTION:
                        var pauseVideoCommandData = JsonUtility.FromJson<PauseVideoCommandData>(data);
                        if (pauseVideoCommandData != null) {
                            OnPauseVideoCommand?.Invoke(pauseVideoCommandData);
                        } else {
                            LogIfEnabled(LogType.Error, "Could not deserialize command data string.");
                        }

                        break;
                    case Command.RESUME_VIDEO_ACTION:
                        var resumeVideoCommandData = JsonUtility.FromJson<ResumeVideoCommandData>(data);
                        if (resumeVideoCommandData != null) {
                            OnResumeVideoCommand?.Invoke(resumeVideoCommandData);
                        } else {
                            LogIfEnabled(LogType.Error, "Could not deserialize command data string.");
                        }

                        break;
                }
            } catch (Exception e) {
                LogIfEnabled(new Exception("Could not process command json", e));
            }
        }

        public void ConnectToWifiNetwork(string ssid, string password) {
            ssid = EscapeStringToJsonString(ssid);
            password = EscapeStringToJsonString(password);

            if (messenger.IsBoundToService) {
                LogIfEnabled(LogType.Log, "ConnectToWifiNetwork called. Invoking over JNI: connectToWifiNetworkAsync");
                messenger.Call<bool>("connectToWifiNetworkAsync", ssid, password);
            } else {
                LogIfEnabled(LogType.Warning,
                    "ConnectToWifiNetwork ignored. System is not available (not bound to messenger).");
            }
        }

        public void ConnectToEnterpriseWifiNetwork(EnterpriseWifiConnectionRequest enterpriseWifiConnectionRequest) {
            if (enterpriseWifiConnectionRequest == null) {
                throw new ArgumentNullException(nameof(enterpriseWifiConnectionRequest));
            }

            if (messenger.IsBoundToService) {
                LogIfEnabled(LogType.Log,
                    "ConnectToEnterpriseWifiNetwork called. Invoking over JNI: connectToEnterpriseWifiNetworkAsync");
                var json = JsonConvert.SerializeObject(enterpriseWifiConnectionRequest, Formatting.None,
                    new StringEnumConverter());
                messenger.Call<bool>("connectToEnterpriseWifiNetworkAsync", json);
            } else {
                LogIfEnabled(LogType.Warning,
                    "ConnectToEnterpriseWifiNetwork ignored. System is not available (not bound to messenger).");
            }
        }

        public void DisableWifi() {
            if (messenger.IsBoundToService) {
                LogIfEnabled(LogType.Log, "DisableWifi called. Invoking over JNI: disableWifiAsync");
                messenger.Call<bool>("disableWifiAsync");
            } else {
                LogIfEnabled(LogType.Warning, "DisableWifi ignored. System is not available (not bound to messenger.");
            }
        }

        public void EnableWifi() {
            if (messenger.IsBoundToService) {
                LogIfEnabled(LogType.Log, "EnableWifi called. Invoking over JNI: enableWifiAsync");
                messenger.Call<bool>("enableWifiAsync");
            } else {
                LogIfEnabled(LogType.Warning, "EnableWifi ignored. System is not available (not bound to messenger.");
            }
        }

        public void ForgetWifiNetwork(string ssid) {
            ssid = EscapeStringToJsonString(ssid);

            if (messenger.IsBoundToService) {
                LogIfEnabled(LogType.Log, "ForgetWifiNetwork called. Invoking over JNI: forgetWifiNetworkAsync");
                messenger.Call<bool>("forgetWifiNetworkAsync", ssid);
            } else {
                LogIfEnabled(LogType.Warning,
                    "ForgetWifiNetwork ignored. System is not available (not bound to messenger.");
            }
        }

        public void RefreshWifiNetworks() {
            if (messenger.IsBoundToService) {
                LogIfEnabled(LogType.Log, "RefreshWifiNetworks called. Invoking over JNI: getWifiNetworksAsync");
                messenger.Call<bool>("getWifiNetworksAsync");
            } else {
                LogIfEnabled(LogType.Warning,
                    "RefreshWifiNetworks ignored. System is not available (not bound to messenger.");
            }
        }

        public void RefreshWifiConnectionStatus() {
            if (messenger.IsBoundToService) {
                LogIfEnabled(LogType.Log,
                    "RefreshWifiConnectionStatus called. Invoking over JNI: getWifiConnectionStatusAsync");
                messenger.Call<bool>("getWifiConnectionStatusAsync");
            } else {
                LogIfEnabled(LogType.Warning,
                    "RefreshWifiConnectionStatus ignored. System is not available (not bound to messenger.");
            }
        }

        public void RefreshRuntimeSettings() {
            if (IsAvailable) {
                LogIfEnabled(LogType.Log, "RefreshRuntimeSettings called. Invoking over JNI: getRuntimeSettingsAsync");
                messenger.Call<bool>("getRuntimeSettingsAsync");
            } else {
                LogIfEnabled(LogType.Warning,
                    "RefreshRuntimeSettings ignored. System is not available (not bound to messenger.");
            }
        }

        public void RefreshDeviceData() {
            if (IsAvailable) {
                LogIfEnabled(LogType.Log, "RefreshDeviceData called. Invoking over JNI: getDeviceDataAsync");
                messenger.Call<bool>("getDeviceDataAsync");
            } else {
                LogIfEnabled(LogType.Warning,
                    "RefreshDeviceData ignored. System is not available (not bound to messenger.)");
            }
        }

        public void RefreshDeviceStatus() {
            if (IsAvailable) {
                LogIfEnabled(LogType.Log, "RefreshDeviceStatus called. Invoking over JNI: getDeviceStatusAsync");
                messenger.Call<bool>("getDeviceStatusAsync");
            } else {
                LogIfEnabled(LogType.Warning,
                    "RefreshDeviceStatus ignored. System is not available (not bound to messenger.");
            }
        }

        public void EnableKioskMode() {
            if (messenger.IsBoundToService) {
                LogIfEnabled(LogType.Log, "EnableKioskMode called. Invoking over JNI: enableKioskModeAsync");
                messenger.Call<bool>("enableKioskModeAsync");
            } else {
                LogIfEnabled(LogType.Warning,
                    "EnableKioskMode ignored. System is not available (not bound to messenger.");
            }
        }

        public void DisableKioskMode() {
            if (messenger.IsBoundToService) {
                LogIfEnabled(LogType.Log, "DisableKioskMode called. Invoking over JNI: disableKioskModeAsync");
                messenger.Call<bool>("disableKioskModeAsync");
            } else {
                LogIfEnabled(LogType.Warning,
                    "DisableKioskMode ignored. System is not available (not bound to messenger.");
            }
        }

        public void OverrideKioskApp(string packageName) {
            packageName = EscapeStringToJsonString(packageName);

            if (messenger.IsBoundToService) {
                LogIfEnabled(LogType.Log, "OverrideKioskApp called. Invoking over JNI: overrideKioskAppAsync");
                messenger.Call<bool>("overrideKioskAppAsync", packageName);
            } else {
                LogIfEnabled(LogType.Warning,
                    "OverrideKioskApp ignored. System is not available (not bound to messenger).");
            }
        }

        public void KillApp(string packageName) {
            packageName = EscapeStringToJsonString(packageName);

            if (messenger.IsBoundToService) {
                LogIfEnabled(LogType.Log, "KillApp called. Invoking over JNI: killApp");
                messenger.Call<bool>("killApp", packageName);
            } else {
                LogIfEnabled(LogType.Warning, "KillApp ignored. System is not available (not bound to messenger).");
            }
        }

        public void RestartApp(string packageName) {
            packageName = EscapeStringToJsonString(packageName);

            if (messenger.IsBoundToService) {
                LogIfEnabled(LogType.Log, "RestartApp called. Invoking over JNI: restartApp");
                messenger.Call<bool>("restartApp", packageName);
            } else {
                LogIfEnabled(LogType.Warning, "restartApp ignored. System is not available (not bound to messenger).");
            }
        }

        public void Shutdown() {
            if (messenger.IsBoundToService) {
                LogIfEnabled(LogType.Log, "Shutdown called. Invoking over JNI: shutdown");
                messenger.Call<bool>("shutdown");
            } else {
                LogIfEnabled(LogType.Warning, "Shutdown ignored. System is not available (not bound to messenger).");
            }
        }

        public void Reboot() {
            if (messenger.IsBoundToService) {
                LogIfEnabled(LogType.Log, "Reboot called. Invoking over JNI: reboot");
                messenger.Call<bool>("reboot");
            } else {
                LogIfEnabled(LogType.Warning, "Reboot ignored. System is not available (not bound to messenger).");
            }
        }

        public void Sync() {
            if (messenger.IsBoundToService) {
                LogIfEnabled(LogType.Log, "Sync called. Invoking over JNI: checkDbAsync");
                messenger.Call<bool>("checkDbAsync");
            } else {
                LogIfEnabled(LogType.Warning, "Sync ignored. System is not available (not bound to messenger.");
            }
        }

        public void SendHomeScreenState(HomeScreenState state) {
            if (messenger.IsBoundToService) {
                try {
                    var stateJson = JsonConvert.SerializeObject(state);
                    messenger.Call<bool>("sendHomeScreenState", stateJson);
                    LogIfEnabled(LogType.Log, "SendHomeScreenState called. Invoking over JNI: sendHomeScreenState");
                } catch (Exception e) {
                    Debug.LogError("An error occured while trying to send homescreen state " + e);
                }
            } else {
                LogIfEnabled(LogType.Warning,
                    "SendHomeScreenState ignored. System is not available (not bound to messenger.");
            }
        }

        public void ExitLauncher() {
            if (messenger.IsBoundToService) {
                LogIfEnabled(LogType.Log, "ExitLauncher called. Invoking over JNI: exitLauncherAsync");
                messenger.Call<bool>("exitLauncherAsync");
            } else {
                LogIfEnabled(LogType.Warning, "ExitLauncher ignored. System is not available (not bound to messenger.");
            }
        }

        public void RequestStreamingCode() {
            if (messenger.IsBoundToService) {
                LogIfEnabled(LogType.Log, "RequestStreamingCode called. Invoking over JNI: requestStreamingCodeAsync");
                messenger.Call<bool>("requestStreamingCodeAsync");
            } else {
                LogIfEnabled(LogType.Warning,
                    "RequestStreamingCode ignored. System is not available (not bound to messenger.");
            }
        }

        #region INITIALIZATION

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
            LogIfEnabled(LogType.Log, "Checking if RuntimeSettingsSummary can be initialized using external json file.");

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

            LogIfEnabled(LogType.Log, "Invoking RefreshRuntimeSettings to initialize RuntimeSettingsSummary using MXR Admin App");
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

        #endregion

        /// <summary>
        /// Returns whether the SDK can read external files, taking into account
        /// whether we need any permissions for it or not.
        /// </summary>
        private bool CanAccessExternalFiles {
            get {
                // If we're on level 29 and below, we don't need external storage manager permissions
                if (!MXRAndroidUtils.NeedsManageExternalStoragePermission) {
                    return true;
                } else {
                    return MXRAndroidUtils.IsExternalStorageManager;
                }
            }
        }

        private string EscapeStringToJsonString(string input) {
            // Escape JSON string. Ref: https://stackoverflow.com/a/26152046
            // Then get rid of the encosing double quotes (") using substring
            var output = JsonConvert.ToString(input);
            output = output.Substring(1, output.Length - 2);

            return output;
        }

        private const string EXTERNAL_READ_WARNING_MSG =
            "On Android 30 and above, request Manage External Storage permission to read external files. " +
            "MXRAndroidUtils.RequestManageAppAllFilesAccessPermission() is provided in the SDK for the same. " +
            "On Android 29, use android:requestLegacyExternalStorage=\"true\" in your AndroidManifest.xml." +
            "Refer to the MXR Unity SDK README for more info.";
    }
}
#endif
