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

        public bool IsAdminAppInstalled {
            get {
                return MXRAndroidUtils.NativeUtils.Call<bool>("isAdminAppInstalled");
            }
        }

        public bool IsConnectedToAdminApp => IsAvailable;
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

        string _cachedJsonDirectory = Path.Combine(Application.persistentDataPath, "ManageXR");

        string _cachedRuntimeSettingsSummaryPath =>
            Path.Combine(_cachedJsonDirectory, "runtimeSettingsSummary.json");

        string _cachedDeviceStatusPath =>
            Path.Combine(_cachedJsonDirectory, "deviceStatus.json");

        string _externalRuntimeSettingsSummaryFilePath =>
            MXRStorage.GetFullPath("MightyImmersion/runtimeSettingsSummary.json");

        string _externalDeviceStatusPath =>
            MXRStorage.GetFullPath("MightyImmersion/deviceStatus.json");

        public MXRAndroidSystem() {
            if (LoggingEnabled)
                Debug.unityLogger.Log(LogType.Log, TAG, "Initializing MXRAndroidSystem");

            Directory.CreateDirectory(_cachedJsonDirectory);

            messenger = new AdminAppMessengerManager();
            OnAvailabilityChange?.Invoke(messenger.IsBoundToService);
            messenger.OnBoundStatusToAdminAppChanged += x =>
                OnAvailabilityChange?.Invoke(x);

            InitializeRuntimeSettingsSummary();
            InitializeDeviceStatus();
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

                    File.WriteAllText(_cachedRuntimeSettingsSummaryPath, json);

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

                    File.WriteAllText(_cachedDeviceStatusPath, json);

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
                    Debug.unityLogger.Log(LogType.Log, TAG, "Only PLAY_VIDEO command action is supported via intents.");
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

        public void ConnectToEnterpriseWifiNetwork(EnterpriseWifiConnectionRequest enterpriseWifiConnectionRequest) {

            if (enterpriseWifiConnectionRequest == null)
                throw new ArgumentNullException(nameof(enterpriseWifiConnectionRequest));

            if (messenger.IsBoundToService) {
                if (LoggingEnabled)
                    Debug.unityLogger.Log(LogType.Log, TAG, "ConnectToEnterpriseWifiNetwork called. Invoking over JNI: connectToEnterpriseWifiNetworkAsync");
                string json = JsonConvert.SerializeObject(enterpriseWifiConnectionRequest, Formatting.None, new StringEnumConverter());
                messenger.Call<bool>("connectToEnterpriseWifiNetworkAsync", json);
            }
            else if (LoggingEnabled)
                Debug.unityLogger.Log(LogType.Warning, TAG, "ConnectToEnterpriseWifiNetwork ignored. System is not available (not bound to messenger.");
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
            if (messenger.IsBoundToService) {
                try {
                    var stateJson = JsonConvert.SerializeObject(state);
                    messenger.Call<bool>("sendHomeScreenState", stateJson);
                    if (LoggingEnabled)
                        Debug.unityLogger.Log(LogType.Log, TAG, "SendHomeScreenState called. Invoking over JNI: sendHomeScreenState");
                }
                catch (Exception e) {
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
        async void InitializeRuntimeSettingsSummary() {
            bool InitFromFile(string path) {
                if (DeserializeFromFile(path, out string contents, out RuntimeSettingsSummary runtimeSettingsSummary)) {
                    lastRuntimeSettingsSummaryJSON = contents;
                    RuntimeSettingsSummary = runtimeSettingsSummary;
                    OnRuntimeSettingsSummaryChange?.Invoke(RuntimeSettingsSummary);
                    return true;
                }
                else 
                    return false;
            }

            string filePath;

            // Method 1: Try to initialize using the external json file
            if (LoggingEnabled)
                Debug.unityLogger.Log(TAG, "Checking if RuntimeSettingsSummary can be initialized using external json file.");
            if (CanAccessExternalFiles) {
                filePath = _externalRuntimeSettingsSummaryFilePath;

                if (InitFromFile(filePath)) {
                    if (LoggingEnabled)
                        Debug.unityLogger.Log(TAG, "Initialized RuntimeSettingsSummary using external json file. ");
                    return;
                }
            }

            // Method 2: Try to initialize using the cached json file
            if (LoggingEnabled)
                Debug.unityLogger.LogWarning(TAG, "RuntimeSettingsSummary cannot initialize using external json file. "
                + "Trying to initialize it using the cached json file. This is not an error. "
                + EXTERNAL_STORAGE_MANAGER_WARNING_MSG);
            filePath = _cachedRuntimeSettingsSummaryPath;

            if (InitFromFile(filePath)) {
                if (LoggingEnabled)
                    Debug.unityLogger.Log(TAG, "Initialized RuntimeSettingsSummary using cached json file. ");
                return;
            }

            // Method 3: If initialization using both external and cached json fails, we wait for the SDK to get connected
            // to the admin app and then request a RuntimeSettings refresh
            string msg = "Cannot initialize RuntimeSettingsSummary using any json file.";
            if (!IsConnectedToAdminApp)
                msg += "Waiting for MXR Admin App connection to send it a RuntimeSettingsSummary refresh request.";
            if (LoggingEnabled)
                Debug.unityLogger.LogWarning(TAG, msg);

            while (!IsConnectedToAdminApp)
                await Task.Delay(100);

            if (LoggingEnabled)
                Debug.unityLogger.Log("Invoking RefreshRuntimeSettings to initialize RuntimeSettingsSummary using MXR Admin App");
            RefreshRuntimeSettings();
        }

        async void InitializeDeviceStatus() {
            bool InitFromFile(string path) {
                if (DeserializeFromFile(path, out string contents, out DeviceStatus deviceStatus)) {
                    lastDeviceStatusJSON = contents;
                    DeviceStatus = deviceStatus;
                    OnDeviceStatusChange?.Invoke(DeviceStatus);
                    return true;
                }
                else 
                    return false;
            }

            string filePath;

            // Method 1: Try to initialize using the external json file
            if (LoggingEnabled)
                Debug.unityLogger.Log(TAG, "Checking if DeviceStatus can be initialized using external json file.");
            if (CanAccessExternalFiles) {
                filePath = _externalDeviceStatusPath;

                if (InitFromFile(filePath)) {
                    if (LoggingEnabled)
                        Debug.unityLogger.Log(TAG, "Initialized DeviceStatus using external json file. ");
                    return;
                }
            }

            // Method 2: Try to initialize using the cached json file
            if (LoggingEnabled)
                Debug.unityLogger.LogWarning(TAG, "DeviceStatus cannot initialize using external json file. "
                + "Trying to initialize it using the cached json file. This is not an error. "
                + EXTERNAL_STORAGE_MANAGER_WARNING_MSG);
            filePath = _cachedDeviceStatusPath;

            if (InitFromFile(filePath)) {
                if (LoggingEnabled)
                    Debug.unityLogger.Log(TAG, "Initialized DeviceStatus using cached json file. ");
                return;
            }

            // Method 3: If initialization using both external and cached json fails, we wait for the SDK to get connected
            // to the admin app and then request a DeviceStatus refresh
            string msg = "Cannot initialize DeviceStatus using any json file.";
            if (!IsConnectedToAdminApp)
                msg += "Waiting for MXR Admin App connection to send it a DeviceStatus refresh request.";
            if (LoggingEnabled)
                Debug.unityLogger.LogWarning(TAG, msg);

            while (!IsConnectedToAdminApp)
                await Task.Delay(100);

            if (LoggingEnabled)
                Debug.unityLogger.Log("Invoking RefreshDeviceStatus to initialize DeviceStatus using MXR Admin App");
            RefreshDeviceStatus();
        }

        bool DeserializeFromFile<T>(string filePath, out string contents, out T value) {
            try {
                if (!File.Exists(filePath)) {
                    contents = string.Empty;
                    value = default;
                    return false;
                }
                else {
                    contents = File.ReadAllText(filePath);
                    value = JsonConvert.DeserializeObject<T>(contents);
                    return true;
                }
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

        /// <summary>
        /// Returns whether the SDK can read external files, taking into account
        /// whether we need any permissions for it or not.
        /// </summary>
        bool CanAccessExternalFiles {
            get {
                // If we're on level 29 and below, we don't need external storage manager permissions
                if (!MXRAndroidUtils.NeedsManageAllFilesPermission)
                    return true;
                else
                    return MXRAndroidUtils.IsExternalStorageManager;
            }
        }

        string EscapeStringToJsonString(string input) {
            // Escape JSON string. Ref: https://stackoverflow.com/a/26152046
            // Then get rid of the encosing double quotes (") using substring
            string output = JsonConvert.ToString(input);
            output = output.Substring(1, output.Length - 2);

            return output;
        }

        const string EXTERNAL_STORAGE_MANAGER_WARNING_MSG =
            "On Android 30 and above, request Manage All Files permission to read local files. " +
            "A helper method MXRAndroidUtils.RequestManageAllFilesPermission() is provided in the SDK for the same. " +
            "Refer to the MXR Unity SDK README for more info.";
    }
}
#endif