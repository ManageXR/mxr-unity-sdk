using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Newtonsoft.Json;

namespace MXR.SDK {
    /// <summary>
    /// Simulates MXR system on editor using locally available json files.
    /// Allows testing the application/integration in the editor.
    /// </summary>
    public class MXREditorSystem : MonoBehaviour, IMXRSystem {
        const string TAG = "[MXREditorSystem]";

        // ================================================
        #region INITIALIZATION AND LOOP
        // ================================================
        // The time duration between data sync in seconds
        public float syncTimestep = 1;

        [Obsolete("Use `MXREditorSystem.New()` instead of `new MXREditorSystem()`", true)]
        public MXREditorSystem() { }

        public static MXREditorSystem New() {
            var go = new GameObject("MXREditor");
            DontDestroyOnLoad(go);
            var instance = go.AddComponent<MXREditorSystem>();
            return instance;
        }

        Coroutine coroutine;
        void OnEnable() {
            isAvailable = Directory.Exists(MXRStorage.MXRRootDirectory);
            OnAvailabilityChange?.Invoke(isAvailable);
            coroutine = StartCoroutine(Loop());
        }

        void OnDisable() {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }

        IEnumerator Loop() {
            while (true) {
                var dirExists = Directory.Exists(MXRStorage.MXRRootDirectory);
                if (isAvailable != dirExists) {
                    isAvailable = dirExists;
                    OnAvailabilityChange?.Invoke(dirExists);
                }

                Sync();

                yield return new WaitForSeconds(syncTimestep);
            }
        }
        #endregion

        public void ExecuteCommand(Command command) {
            var data = command.data;

            switch (command.action) {
                case Command.PLAY_VIDEO_ACTION:
                    if (LoggingEnabled)
                        Debug.unityLogger.Log(LogType.Log, TAG, "Play Video Command received. " + JsonUtility.ToJson(command));
                    var playVideoCommandData = JsonUtility.FromJson<PlayVideoCommandData>(command.data);
                    if (playVideoCommandData != null)
                        OnPlayVideoCommand?.Invoke(playVideoCommandData);
                    break;
                case Command.PAUSE_VIDEO_ACTION:
                    if (LoggingEnabled)
                        Debug.unityLogger.Log(LogType.Log, TAG, "Pause Video Command received.");
                    var pauseVideoCommandData = JsonUtility.FromJson<PauseVideoCommandData>(command.data);
                    if (pauseVideoCommandData != null)
                        OnPauseVideoCommand?.Invoke(pauseVideoCommandData);
                    break;
            }
        }

        // ================================================
        #region INTERFACE IMPLEMENTATION
        // ================================================
        // INTERFACE PROPERTIES
        bool loggingEnabled = true;
        public bool LoggingEnabled {
            get => loggingEnabled;
            set => loggingEnabled = value;
        }

        bool isAvailable;
        public bool IsAvailable => isAvailable;

        public ScannedWifiNetwork CurrentNetwork {
            get {
                foreach (var network in WifiNetworks) {
                    if (network.ssid.Equals(WifiConnectionStatus.ssid))
                        return network;
                }
                return null;
            }
            private set {
                if (value == null) {
                    WifiConnectionStatus.ssid = string.Empty;
                    WriteWifiConnectionStatus();
                    return;
                }

                foreach (var network in WifiNetworks) {
                    if (network.ssid.Equals(value.ssid)) {
                        WifiConnectionStatus.ssid = value.ssid;
                        WifiConnectionStatus.capabilities = value.capabilities;
                        WifiConnectionStatus.signalStrength = value.signalStrength;
                        WifiConnectionStatus.networkSecurityType = value.networkSecurityType;

                        // TODO: Provide tooling to simulate these too
                        WifiConnectionStatus.state = WifiConnectionStatus.State.CONNECTED;
                        WifiConnectionStatus.hasInternetAccess = true;
                        WifiConnectionStatus.requiresCaptivePortal = false;

                        WriteWifiConnectionStatus();
                    }
                }
            }
        }
        public DeviceStatus DeviceStatus { get; private set; }
        public RuntimeSettingsSummary RuntimeSettingsSummary { get; private set; }
        public WifiConnectionStatus WifiConnectionStatus { get; private set; }
        public List<ScannedWifiNetwork> WifiNetworks { get; private set; }

        // INTERFACE EVENTS
        public event Action<bool> OnAvailabilityChange;
        public event Action<DeviceStatus> OnDeviceStatusChange;
        public event Action<RuntimeSettingsSummary> OnRuntimeSettingsSummaryChange;
        public event Action<WifiConnectionStatus> OnWifiConnectionStatusChange;
        public event Action<List<ScannedWifiNetwork>> OnWifiNetworksChange;
        public event Action<PlayVideoCommandData> OnPlayVideoCommand;
        public event Action<PauseVideoCommandData> OnPauseVideoCommand;
        public event Action OnHomeScreenStateRequest;

        // INTERFACE METHODS
        public void DisableKioskMode() {
            try {
                if (RuntimeSettingsSummary.kioskModeEnabled == false) return;
                
                RuntimeSettingsSummary.kioskModeEnabled = false;
                WriteRuntimeSettings();
                if (LoggingEnabled)
                    Debug.unityLogger.Log(LogType.Log, TAG, "Disabled Kiosk Mode");
            }
            catch (Exception e) {
                RuntimeSettingsSummary.kioskModeEnabled = true;
                if (LoggingEnabled)
                    Debug.unityLogger.Log(LogType.Error, TAG, "Could not disable Kiosk Mode. " + e);
            }
        }

        public void EnableKioskMode() {
            try {
                if (RuntimeSettingsSummary.kioskModeEnabled) return;

                RuntimeSettingsSummary.kioskModeEnabled = true;
                WriteRuntimeSettings();
                if (LoggingEnabled)
                    Debug.unityLogger.Log(LogType.Log, TAG, "Enabled Kiosk Mode");
            }
            catch(Exception e) {
                RuntimeSettingsSummary.kioskModeEnabled = false;
                if (LoggingEnabled)
                    Debug.unityLogger.Log(LogType.Error, TAG, "Could not enable Kiosk Mode. " + e);
            }
        }

        public void KillApp(string packageName) {
            if (LoggingEnabled)
                Debug.unityLogger.Log(LogType.Log, TAG, "Killed App");
        }

        public void RestartApp(string packageName) {
            if (LoggingEnabled)
                Debug.unityLogger.Log(LogType.Log, TAG, "Restarted App");
        }

        public void EnableWifi() {
            try {
                if (WifiConnectionStatus.wifiIsEnabled) return;

                WifiConnectionStatus.wifiIsEnabled = true;
                WriteWifiConnectionStatus();
                if (LoggingEnabled)
                    Debug.unityLogger.Log(LogType.Log, TAG, "Enabled Wifi");
            }
            catch(Exception e){
                WifiConnectionStatus.wifiIsEnabled = false;
                if (LoggingEnabled)
                    Debug.unityLogger.Log(LogType.Error, TAG, "Could not enable Wifi. " + e);
            }
        }

        public void DisableWifi() {
            try {
                if (WifiConnectionStatus.wifiIsEnabled == false) return;

                lastWifiConnectionStatusJson = string.Empty;
                lastWifiNetworksJson = string.Empty;
                WifiNetworks.Clear();
                CurrentNetwork = null;
                WifiConnectionStatus.wifiIsEnabled = false;
                WriteWifiConnectionStatus();
                if (LoggingEnabled)
                    Debug.unityLogger.Log(LogType.Log, TAG, "Disabled Kiosk Mode");
            }
            catch(Exception e) {
                if (LoggingEnabled)
                    Debug.unityLogger.Log(LogType.Error, TAG, "Could not disable Wifi. " + e);
            }
        }

        public void RequestHomeScreenState() {
            OnHomeScreenStateRequest?.Invoke();
        }

        public void ConnectToWifiNetwork(string ssid, string password) {
            // Escape JSON string. Ref: https://stackoverflow.com/a/26152046
            // Then get rid of the encosing double quotes (") using substring
            ssid = JsonConvert.ToString(ssid);
            ssid = ssid.Substring(1, ssid.Length - 2);

            if (LoggingEnabled)
                Debug.unityLogger.Log(LogType.Log, TAG, "Connecting to Wifi Network with SSID " + ssid + " using password " + password);

            // If a network with the given SSID is available
            // just set it as the current network
            foreach (var network in WifiNetworks) {
                if (network.ssid.Equals(ssid)) {
                    CurrentNetwork = network;
                    if (LoggingEnabled)
                        Debug.unityLogger.Log(LogType.Log, TAG, "Connected to Wifi Network with SSID " + ssid);
                }
            }
        }

        /// <summary>
        /// Currently, in editor, we can only forget the current network.
        /// TODO: Add savedWifiNetworks.json to simulate this function better.
        /// </summary>
        /// <param name="ssid">The SSID to forget</param>
        public void ForgetWifiNetwork(string ssid) {
            // Escape JSON string. Ref: https://stackoverflow.com/a/26152046
            // Then get rid of the encosing double quotes (") using substring
            ssid = JsonConvert.ToString(ssid);
            ssid = ssid.Substring(1, ssid.Length - 2);

            if (CurrentNetwork != null && CurrentNetwork.ssid.Equals(ssid)) {
                CurrentNetwork = null;
                if (LoggingEnabled)
                    Debug.unityLogger.Log(LogType.Log, TAG, "Forgot Wifi Network with SSID " + ssid);
            }
        }

        public void Sync() {
            if (LoggingEnabled)
                Debug.unityLogger.Log(LogType.Log, TAG, "Syncing...");

            // Refresh the data from all the json files
            // this class uses
            RefreshDeviceStatus();
            RefreshRuntimeSettings();
            RefreshWifiConnectionStatus();
            RefreshWifiNetworks();
        }

        string lastRuntimeSettingsSummaryJson = string.Empty;
        public void RefreshRuntimeSettings() {
            try {
                if (LoggingEnabled)
                    Debug.unityLogger.Log(LogType.Log, TAG, "Refreshing RuntimeSettingsSummary from runtimeSettingsSummary.json");
                if (TryRefresh("runtimeSettingsSummary.json", ref lastRuntimeSettingsSummaryJson, out RuntimeSettingsSummary summary)) {
                    RuntimeSettingsSummary = summary;
                    OnRuntimeSettingsSummaryChange?.Invoke(summary);
                }
            }
            catch (Exception e) {
                if (LoggingEnabled)
                    Debug.unityLogger.Log(LogType.Error, TAG, "Could not refresh Wifi Networks from wifiNetworks.json. " + e);
                RuntimeSettingsSummary = null;
                OnRuntimeSettingsSummaryChange?.Invoke(RuntimeSettingsSummary);
            }
        }

        string lastDeviceStatus = string.Empty;
        public void RefreshDeviceStatus() {
            try {
                if (LoggingEnabled)
                    Debug.unityLogger.Log(LogType.Log, TAG, "Refreshing DeviceStatus from deviceStatus.json");
                if (TryRefresh("deviceStatus.json", ref lastDeviceStatus, out DeviceStatus status)) {
                    DeviceStatus = status;
                    OnDeviceStatusChange?.Invoke(status);
                }
            }
            catch (Exception e) {
                if (LoggingEnabled)
                    Debug.unityLogger.Log(LogType.Error, TAG, "Could not refresh DeviceStatus from deviceStatus.json. " + e);
                DeviceStatus = null;
                OnDeviceStatusChange?.Invoke(DeviceStatus);
            }
        }

        string lastWifiNetworksJson = string.Empty;
        public void RefreshWifiNetworks() {
            try {
                if (LoggingEnabled)
                    Debug.unityLogger.Log(LogType.Log, TAG, "Refreshing WifiNetworks from wifiNetworks.json");
                if (TryRefresh("wifiNetworks.json", ref lastWifiNetworksJson, out List<ScannedWifiNetwork> networks)) {
                    WifiNetworks = networks;
                    OnWifiNetworksChange?.Invoke(networks);
                }
            }
            catch (Exception e) {
                if (LoggingEnabled)
                    Debug.unityLogger.Log(LogType.Error, TAG, "Could not refresh Wifi Networks from wifiNetworks.json. " + e);
                WifiNetworks = null;
                OnWifiNetworksChange?.Invoke(WifiNetworks);
            }
        }

        string lastWifiConnectionStatusJson = string.Empty;
        public void RefreshWifiConnectionStatus() {
            try {
                if (LoggingEnabled)
                    Debug.unityLogger.Log(LogType.Log, TAG, "Refreshing WifiConnectionStatus from wifiConnectionStatus.json");
                if (TryRefresh("wifiConnectionStatus.json", ref lastWifiConnectionStatusJson, out WifiConnectionStatus status)) {
                    WifiConnectionStatus = status;
                    OnWifiConnectionStatusChange?.Invoke(status);
                }
            }
            catch (Exception e) {
                if (LoggingEnabled)
                    Debug.unityLogger.Log(LogType.Error, TAG, "Could not refresh WifiConnectionStatus from wifiConnectionStatus.json. " + e);
                WifiConnectionStatus = null;
                OnWifiConnectionStatusChange?.Invoke(WifiConnectionStatus);
            }
        }

        public void SendHomeScreenState(HomeScreenState state) {
            if (LoggingEnabled)
                Debug.unityLogger.Log(LogType.Warning, TAG, "SendHomeScreenState " + JsonUtility.ToJson(state) +
                    "\nEditor mode doesn't send HomeScreenState anywhere, only prints it to console.");
        }

        public void ExitLauncher() {
            if (LoggingEnabled)
                Debug.unityLogger.Log(LogType.Log, TAG, "Editor mode doesn't support ExitLauncher(). Safely ignored...");
        }
        #endregion

        // ================================================
        #region I/O HELPERS
        // ================================================
        bool TryRefresh<T>(string fileName, ref string lastJSON, out T serialized) {
            try {
                string contents = File.ReadAllText(GetFilePath(fileName));
                if (contents != null && !lastJSON.Equals(contents)) {
                    var obj = JsonConvert.DeserializeObject<T>(contents);
                    if (obj == null) {
                        serialized = default;
                        return false;
                    }
                    serialized = obj;
                    lastJSON = contents;
                    return true;
                }
                serialized = default;
                return false;
            }
            catch (JsonReaderException e) {
                throw new Exception($"Error reading json {e}. Skipping...");
            }
        }

        void WriteRuntimeSettings() {
            try {
                string path = GetFilePath("runtimeSettingsSummary.json");
                var json = JsonConvert.SerializeObject(RuntimeSettingsSummary);
                File.WriteAllText(path, json);
            }
            catch(Exception e) {
                if (LoggingEnabled)
                    Debug.unityLogger.Log(LogType.Error, TAG, e);
            }
        }

        void WriteWifiConnectionStatus() {
            try {
                string path = GetFilePath("wifiConnectionStatus.json");
                var json = JsonConvert.SerializeObject(WifiConnectionStatus);
                File.WriteAllText(path, json);
            }
            catch(Exception e) {
                if (LoggingEnabled)
                    Debug.unityLogger.Log(LogType.Error, TAG, e);
            }
        }

        string GetFilePath(string fileName) {
            var filesPath = Path.Combine(Application.dataPath.Replace("Assets", "Files"));
            if (!Directory.Exists(filesPath))
                throw new DirectoryNotFoundException("Ensure Files/ directory inside Unity project");
            var mightyDir = Path.Combine(filesPath, "MightyImmersion");
            if (!Directory.Exists(mightyDir))
                throw new DirectoryNotFoundException("Ensure Files/MightyImmersion directory inside Unity project");
            return Path.Combine(mightyDir, fileName);
        }
        #endregion
    }
}
