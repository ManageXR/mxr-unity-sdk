using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MXR.SDK {
    /// <summary>
    /// Main Android system implementation for the MXR SDK that handles communication between Unity and the MXR Admin App.
    /// This class serves as the primary interface for device management, system settings, and runtime operations.
    /// </summary>
    /// <remarks>
    /// This class is split into multiple partial files to organize distinct areas of functionality:
    /// - Messages: Handles incoming message processing from the Admin App
    /// - Commands: Handles commands coming from the Admin App (likely triggered from the Web Console).
    /// - State: Manages system state and data storage
    /// - Initialization: Handles system startup and configuration
    /// - Storage: Manages file operations and data persistence
    /// </remarks>
    public partial class MXRAndroidSystem : IMXRSystem {
        private static readonly string TAG = $"[{nameof(MXRAndroidSystem)}]";
        private readonly AdminAppMessengerManager _messenger;

        /// <summary>
        /// The messenger used for sending and receiving messages
        /// between Unity and Admin App
        /// </summary>
        public AdminAppMessengerManager Messenger => _messenger;

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
        public bool IsAvailable => _messenger.IsBoundToService;

        public DeviceData DeviceData { get; private set; }
        public DeviceStatus DeviceStatus { get; private set; }
        public RuntimeSettingsSummary RuntimeSettingsSummary { get; private set; }
        public List<ScannedWifiNetwork> WifiNetworks { get; private set; }
        public WifiConnectionStatus WifiConnectionStatus { get; private set; }
        public CastingCodeStatus CastingCodeStatus { get; private set; }

        public event Action<bool> OnAvailabilityChange;
        public event Action<DeviceData> OnDeviceDataChange;
        public event Action<DeviceStatus> OnDeviceStatusChange;
        public event Action<RuntimeSettingsSummary> OnRuntimeSettingsSummaryChange;
        public event Action<WifiConnectionStatus> OnWifiConnectionStatusChange;
        public event Action<List<ScannedWifiNetwork>> OnWifiNetworksChange;
        public event Action<PlayVideoCommandData> OnPlayVideoCommand;
        public event Action<PauseVideoCommandData> OnPauseVideoCommand;
        public event Action<ResumeVideoCommandData> OnResumeVideoCommand;
        public event Action<CastingCodeStatus> OnCastingCodeStatusChanged;
        public event Action<LaunchMXRHomeScreenCommandData> OnLaunchMXRHomeScreenCommand;
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

            _messenger = new AdminAppMessengerManager();
            OnAvailabilityChange?.Invoke(_messenger.IsBoundToService);
            _messenger.OnBoundStatusToAdminAppChanged += x =>
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
            _messenger.OnMessageFromAdminApp += OnMessageFromAdminApp;
        }

        private void OnPlayerFocusChange(bool x) {
            LogIfEnabled(LogType.Log, "SDK Focus: " + x);

            if (!x) {
                return;
            }

            TryExecuteIntentCommands();
        }

        /// <summary>
        /// Unescape json if it is escaped 
        /// Ref: https://stackoverflow.com/a/26406504
        /// </summary>
        private static string UnescapeJsonIfNeeded(string json) {
            try {
                if (string.IsNullOrEmpty(json) || !json.StartsWith("\"")) {
                    return json;
                }

                var token = JToken.Parse(json);
                var obj = JObject.Parse((string)token);
                json = obj.ToString();

                return json;
            } catch (Exception ex) {
                Debug.unityLogger.LogError("MXRAndroidSystem",
                    $"Failed to unescape JSON: {ex.GetType().Name}: {ex.Message}. Returning original JSON.");
                return json;
            }
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

        public void ConnectToWifiNetwork(string ssid, string password) {
            ssid = EscapeStringToJsonString(ssid);
            password = EscapeStringToJsonString(password);

            if (_messenger.IsBoundToService) {
                LogIfEnabled(LogType.Log, "ConnectToWifiNetwork called. Invoking over JNI: connectToWifiNetworkAsync");
                _messenger.Call<bool>("connectToWifiNetworkAsync", ssid, password);
            } else {
                LogIfEnabled(LogType.Warning,
                    "ConnectToWifiNetwork ignored. System is not available (not bound to messenger).");
            }
        }

        public void ConnectToEnterpriseWifiNetwork(EnterpriseWifiConnectionRequest enterpriseWifiConnectionRequest) {
            if (enterpriseWifiConnectionRequest == null) {
                throw new ArgumentNullException(nameof(enterpriseWifiConnectionRequest));
            }

            if (_messenger.IsBoundToService) {
                LogIfEnabled(LogType.Log,
                    "ConnectToEnterpriseWifiNetwork called. Invoking over JNI: connectToEnterpriseWifiNetworkAsync");
                var json = JsonConvert.SerializeObject(enterpriseWifiConnectionRequest, Formatting.None,
                    new StringEnumConverter());
                _messenger.Call<bool>("connectToEnterpriseWifiNetworkAsync", json);
            } else {
                LogIfEnabled(LogType.Warning,
                    "ConnectToEnterpriseWifiNetwork ignored. System is not available (not bound to messenger).");
            }
        }

        public void DisableWifi() {
            if (_messenger.IsBoundToService) {
                LogIfEnabled(LogType.Log, "DisableWifi called. Invoking over JNI: disableWifiAsync");
                _messenger.Call<bool>("disableWifiAsync");
            } else {
                LogIfEnabled(LogType.Warning, "DisableWifi ignored. System is not available (not bound to messenger.");
            }
        }

        public void EnableWifi() {
            if (_messenger.IsBoundToService) {
                LogIfEnabled(LogType.Log, "EnableWifi called. Invoking over JNI: enableWifiAsync");
                _messenger.Call<bool>("enableWifiAsync");
            } else {
                LogIfEnabled(LogType.Warning, "EnableWifi ignored. System is not available (not bound to messenger.");
            }
        }

        public void ForgetWifiNetwork(string ssid) {
            ssid = EscapeStringToJsonString(ssid);

            if (_messenger.IsBoundToService) {
                LogIfEnabled(LogType.Log, "ForgetWifiNetwork called. Invoking over JNI: forgetWifiNetworkAsync");
                _messenger.Call<bool>("forgetWifiNetworkAsync", ssid);
            } else {
                LogIfEnabled(LogType.Warning,
                    "ForgetWifiNetwork ignored. System is not available (not bound to messenger.");
            }
        }

        public void RefreshWifiNetworks() {
            if (_messenger.IsBoundToService) {
                LogIfEnabled(LogType.Log, "RefreshWifiNetworks called. Invoking over JNI: getWifiNetworksAsync");
                _messenger.Call<bool>("getWifiNetworksAsync");
            } else {
                LogIfEnabled(LogType.Warning,
                    "RefreshWifiNetworks ignored. System is not available (not bound to messenger.");
            }
        }

        public void RefreshWifiConnectionStatus() {
            if (_messenger.IsBoundToService) {
                LogIfEnabled(LogType.Log,
                    "RefreshWifiConnectionStatus called. Invoking over JNI: getWifiConnectionStatusAsync");
                _messenger.Call<bool>("getWifiConnectionStatusAsync");
            } else {
                LogIfEnabled(LogType.Warning,
                    "RefreshWifiConnectionStatus ignored. System is not available (not bound to messenger.");
            }
        }

        public void RefreshRuntimeSettings() {
            if (IsAvailable) {
                LogIfEnabled(LogType.Log, "RefreshRuntimeSettings called. Invoking over JNI: getRuntimeSettingsAsync");
                _messenger.Call<bool>("getRuntimeSettingsAsync");
            } else {
                LogIfEnabled(LogType.Warning,
                    "RefreshRuntimeSettings ignored. System is not available (not bound to messenger.");
            }
        }

        public void RefreshDeviceData() {
            if (IsAvailable) {
                LogIfEnabled(LogType.Log, "RefreshDeviceData called. Invoking over JNI: getDeviceDataAsync");
                _messenger.Call<bool>("getDeviceDataAsync");
            } else {
                LogIfEnabled(LogType.Warning,
                    "RefreshDeviceData ignored. System is not available (not bound to messenger.)");
            }
        }

        public void RefreshDeviceStatus() {
            if (IsAvailable) {
                LogIfEnabled(LogType.Log, "RefreshDeviceStatus called. Invoking over JNI: getDeviceStatusAsync");
                _messenger.Call<bool>("getDeviceStatusAsync");
            } else {
                LogIfEnabled(LogType.Warning,
                    "RefreshDeviceStatus ignored. System is not available (not bound to messenger.");
            }
        }

        public void EnableKioskMode() {
            if (_messenger.IsBoundToService) {
                LogIfEnabled(LogType.Log, "EnableKioskMode called. Invoking over JNI: enableKioskModeAsync");
                _messenger.Call<bool>("enableKioskModeAsync");
            } else {
                LogIfEnabled(LogType.Warning,
                    "EnableKioskMode ignored. System is not available (not bound to messenger.");
            }
        }

        public void DisableKioskMode() {
            if (_messenger.IsBoundToService) {
                LogIfEnabled(LogType.Log, "DisableKioskMode called. Invoking over JNI: disableKioskModeAsync");
                _messenger.Call<bool>("disableKioskModeAsync");
            } else {
                LogIfEnabled(LogType.Warning,
                    "DisableKioskMode ignored. System is not available (not bound to messenger.");
            }
        }

        public void OverrideKioskApp(string packageName) {
            packageName = EscapeStringToJsonString(packageName);

            if (_messenger.IsBoundToService) {
                LogIfEnabled(LogType.Log, "OverrideKioskApp called. Invoking over JNI: overrideKioskAppAsync");
                _messenger.Call<bool>("overrideKioskAppAsync", packageName);
            } else {
                LogIfEnabled(LogType.Warning,
                    "OverrideKioskApp ignored. System is not available (not bound to messenger).");
            }
        }

        public void KillApp(string packageName) {
            packageName = EscapeStringToJsonString(packageName);

            if (_messenger.IsBoundToService) {
                LogIfEnabled(LogType.Log, "KillApp called. Invoking over JNI: killApp");
                _messenger.Call<bool>("killApp", packageName);
            } else {
                LogIfEnabled(LogType.Warning, "KillApp ignored. System is not available (not bound to messenger).");
            }
        }

        public void RestartApp(string packageName) {
            packageName = EscapeStringToJsonString(packageName);

            if (_messenger.IsBoundToService) {
                LogIfEnabled(LogType.Log, "RestartApp called. Invoking over JNI: restartApp");
                _messenger.Call<bool>("restartApp", packageName);
            } else {
                LogIfEnabled(LogType.Warning, "restartApp ignored. System is not available (not bound to messenger).");
            }
        }

        public void Shutdown() {
            if (_messenger.IsBoundToService) {
                LogIfEnabled(LogType.Log, "Shutdown called. Invoking over JNI: shutdown");
                _messenger.Call<bool>("shutdown");
            } else {
                LogIfEnabled(LogType.Warning, "Shutdown ignored. System is not available (not bound to messenger).");
            }
        }

        public void Reboot() {
            if (_messenger.IsBoundToService) {
                LogIfEnabled(LogType.Log, "Reboot called. Invoking over JNI: reboot");
                _messenger.Call<bool>("reboot");
            } else {
                LogIfEnabled(LogType.Warning, "Reboot ignored. System is not available (not bound to messenger).");
            }
        }

        public void Sync() {
            if (_messenger.IsBoundToService) {
                LogIfEnabled(LogType.Log, "Sync called. Invoking over JNI: checkDbAsync");
                _messenger.Call<bool>("checkDbAsync");
            } else {
                LogIfEnabled(LogType.Warning, "Sync ignored. System is not available (not bound to messenger.");
            }
        }

        public void SendHomeScreenState(HomeScreenState state) {
            if (_messenger.IsBoundToService) {
                try {
                    var stateJson = JsonConvert.SerializeObject(state);
                    _messenger.Call<bool>("sendHomeScreenState", stateJson);
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
            if (_messenger.IsBoundToService) {
                LogIfEnabled(LogType.Log, "ExitLauncher called. Invoking over JNI: exitLauncherAsync");
                _messenger.Call<bool>("exitLauncherAsync");
            } else {
                LogIfEnabled(LogType.Warning, "ExitLauncher ignored. System is not available (not bound to messenger.");
            }
        }

        public void RequestCastingCode() {
            if (_messenger.IsBoundToService) {
                LogIfEnabled(LogType.Log, "RequestCastingCode called. Invoking over JNI: requestCastingCodeAsync");
                _messenger.Call<bool>("requestCastingCodeAsync");
            } else {
                LogIfEnabled(LogType.Warning,
                    "RequestCastingCode ignored. System is not available (not bound to messenger.");
            }
        }

        public void StopCasting() {
            if (_messenger.IsBoundToService) {
                LogIfEnabled(LogType.Log, "StopCasting called. Invoking over JNI: stopCastingAsync");
                _messenger.Call<bool>("stopCastingAsync");
            } else {
                LogIfEnabled(LogType.Warning,
                    "StopCasting ignored. System is not available (not bound to messenger.");
            }
        }

        public void UploadDeviceLogs() {
            if (_messenger.IsBoundToService) {
                LogIfEnabled(LogType.Log, "UploadDeviceLogs called. Invoking over JNI: uploadDeviceLogsAsync");
                _messenger.Call<bool>("uploadDeviceLogsAsync");
            } else {
                LogIfEnabled(LogType.Warning,
                    "UploadDeviceLogs ignored. System is not available (not bound to messenger.");
            }
        }

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
    }
}
