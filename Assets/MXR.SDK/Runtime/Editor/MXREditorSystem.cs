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
            coroutine = StartCoroutine(Loop());
        }

        void OnDisable() {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }

        IEnumerator Loop() {
            while (true) {
                Sync();
                yield return new WaitForSeconds(syncTimestep);
            }
        }
        #endregion

        public void ExecuteCommand(Command command) {
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

        // ================================================
        #region INTERFACE IMPLEMENTATION
        // ================================================

        // INTERFACE PROPERTIES
        public bool IsAvailable => Directory.Exists(MXRStorage.MXRRootDirectory);

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
        public event Action<DeviceStatus> OnDeviceStatusChange;
        public event Action<RuntimeSettingsSummary> OnRuntimeSettingsSummaryChange;
        public event Action<WifiConnectionStatus> OnWifiConnectionStatusChange;
        public event Action<List<ScannedWifiNetwork>> OnWifiNetworksChange;
        public event Action<PlayVideoCommandData> OnPlayVideoCommand;
        public event Action<PauseVideoCommandData> OnPauseVideoCommand;
        public event Action OnHomeScreenStateRequest;

        // INTERFACE METHODS
        public void DisableKioskMode() {
            RuntimeSettingsSummary.kioskModeEnabled = false;
            WriteRuntimeSettings();
        }

        public void EnableKioskMode() {
            RuntimeSettingsSummary.kioskModeEnabled = true;
            WriteRuntimeSettings();
        }

        public void EnableWifi() {
            WifiConnectionStatus.wifiIsEnabled = true;
            WriteWifiConnectionStatus();
        }

        public void DisableWifi() {
            lastWifiConnectionStatusJson = string.Empty;
            lastWifiNetworksJson = string.Empty;
            WifiNetworks.Clear();
            CurrentNetwork = null;
            WifiConnectionStatus.wifiIsEnabled = false;
            WriteWifiConnectionStatus();
        }

        public void RequestHomeScreenState() {
            OnHomeScreenStateRequest?.Invoke();
        }

        public void ConnectToWifiNetwork(string ssid, string password) {
            // Escape JSON string. Ref: https://stackoverflow.com/a/26152046
            // Then get rid of the encosing double quotes (") using substring
            ssid = JsonConvert.ToString(ssid);
            ssid = ssid.Substring(1, ssid.Length - 2);

            // If a network with the given SSID is available
            // just set it as the current network
            foreach (var network in WifiNetworks) {
                if (network.ssid.Equals(ssid))
                    CurrentNetwork = network;
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

            if (CurrentNetwork != null && CurrentNetwork.ssid.Equals(ssid))
                CurrentNetwork = null;
        }

        public void Sync() {
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
                if (TryRefresh("runtimeSettingsSummary.json", ref lastRuntimeSettingsSummaryJson, out RuntimeSettingsSummary summary)) {
                    RuntimeSettingsSummary = summary;
                    OnRuntimeSettingsSummaryChange?.Invoke(summary);
                }
            }
            catch {
                if (RuntimeSettingsSummary != null)
                    OnWifiNetworksChange?.Invoke(null);
                RuntimeSettingsSummary = null;
            }
        }

        string lastDeviceStatus = string.Empty;
        public void RefreshDeviceStatus() {
            try {
                if (TryRefresh("deviceStatus.json", ref lastDeviceStatus, out DeviceStatus status)) {
                    DeviceStatus = status;
                    OnDeviceStatusChange?.Invoke(status);
                }
            }
            catch {
                if (DeviceStatus != null)
                    OnDeviceStatusChange?.Invoke(null);
                DeviceStatus = null;
            }
        }

        string lastWifiNetworksJson = string.Empty;
        public void RefreshWifiNetworks() {
            try {
                if (TryRefresh("wifiNetworks.json", ref lastWifiNetworksJson, out List<ScannedWifiNetwork> networks)) {
                    WifiNetworks = networks;
                    OnWifiNetworksChange?.Invoke(networks);
                }
            }
            catch {
                if (WifiNetworks != null)
                    OnWifiNetworksChange?.Invoke(null);
                WifiNetworks = null;
            }
        }

        string lastWifiConnectionStatusJson = string.Empty;
        public void RefreshWifiConnectionStatus() {
            try {
                if (TryRefresh("wifiConnectionStatus.json", ref lastWifiConnectionStatusJson, out WifiConnectionStatus status)) {
                    WifiConnectionStatus = status;
                    OnWifiConnectionStatusChange?.Invoke(status);
                }
            }
            catch {
                if (WifiConnectionStatus != null)
                    OnWifiConnectionStatusChange?.Invoke(null);
                WifiConnectionStatus = null;
            }
        }

        public void SendHomeScreenState(HomeScreenState state) {
            Debug.Log("SendHomeScreenState " + JsonUtility.ToJson(state) + 
            "\nEditor mode doesn't send HomeScreenState anywhere, only prints it to console.");
        }

        public void ExitLauncher() {
            Debug.Log("Editor mode doesn't support ExitLauncher(). Safely ignored...");
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
                Debug.LogError($"Error reading json {e}. Skipping...");
                throw new Exception($"Error reading json {e}. Skipping...");
            }
        }

        void WriteRuntimeSettings() {
            string path = GetFilePath("runtimeSettingsSummary.json");
            var json = JsonConvert.SerializeObject(RuntimeSettingsSummary);
            File.WriteAllText(path, json);
        }

        void WriteWifiConnectionStatus() {
            string path = GetFilePath("wifiConnectionStatus.json");
            var json = JsonConvert.SerializeObject(WifiConnectionStatus);
            File.WriteAllText(path, json);
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
