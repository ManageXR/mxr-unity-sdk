using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using System;
using System.IO;
using Newtonsoft.Json;

namespace MXR.SDK {
    /// <summary>
    /// Simulates MXR system on editor using locally available json files.
    /// Allows testing the application/integration in the editor.
    /// </summary>
    public class MXREditorSystem : MonoBehaviour, IMXRSystem {
        public float syncFrequency = 1;

        public ScannedWifiNetwork CurrentNetwork { get; private set; }

        public DeviceStatus DeviceStatus { get; private set; }

        public RuntimeSettingsSummary RuntimeSettingsSummary { get; private set; }

        public WifiConnectionStatus WifiConnectionStatus { get; private set; }

        public List<ScannedWifiNetwork> WifiNetworks { get; private set; }

        public event Action<DeviceStatus> OnDeviceStatusChange;
        public event Action<RuntimeSettingsSummary> OnRuntimeSettingsSummaryChange;
        public event Action<WifiConnectionStatus> OnWifiConnectionStatusChange;
        public event Action<List<ScannedWifiNetwork>> OnWifiNetworksChange;

        public void ConnectToWifiNetwork(string ssid, string password) {
            foreach(var network in WifiNetworks) {
                if (network.ssid.Equals(ssid)) {
                    CurrentNetwork = network;
                    WifiConnectionStatus.ssid = ssid;
                    WriteWifiConnectionStatus();
                }
            }
        }

        public void DisableKioskMode() {
            RuntimeSettingsSummary.kioskModeEnabled = false;
            WriteRuntimeSettings();
        }

        public void EnableKioskMode() {
            RuntimeSettingsSummary.kioskModeEnabled = true;
            WriteRuntimeSettings();
        }

        public void DisableWifi() {
            WifiNetworks.Clear();
            CurrentNetwork = null;
            WifiConnectionStatus.wifiIsEnabled = false;
            WriteWifiConnectionStatus();
        }

        public void EnableWifi() {
            WifiConnectionStatus.wifiIsEnabled = true;
            WriteWifiConnectionStatus();
        }

        public void ExitLauncher() { }

        public void ForgetWifiNetwork(string ssid) { }

        string lastDeviceStatus = string.Empty;
        public void RefreshDeviceStatus() {
            try {
                string json = File.ReadAllText(GetFilePath("deviceStatus.json"));
                if (json != null && !lastDeviceStatus.Equals(json)) {
                    var obj = JsonConvert.DeserializeObject<DeviceStatus>(json);
                    if (obj == null) return;
                    DeviceStatus = obj;
                    OnDeviceStatusChange?.Invoke(obj);
                    lastDeviceStatus = json;
                }
            }
            catch (JsonReaderException e) {
                Debug.Log($"Error reading json {e}. Skipping...");
            }
        }

        string lastRuntimeSettingsSummaryJson = string.Empty;
        public void RefreshRuntimeSettings() {
            try {
                string json = File.ReadAllText(GetFilePath("runtimeSettingsSummary.json"));
                if (json != null && !lastRuntimeSettingsSummaryJson.Equals(json)) {
                    var obj = JsonConvert.DeserializeObject<RuntimeSettingsSummary>(json);
                    if (obj == null) return;
                    RuntimeSettingsSummary = obj;
                    OnRuntimeSettingsSummaryChange?.Invoke(obj);
                    lastRuntimeSettingsSummaryJson = json;
                }
            }
            catch (JsonReaderException e) {
                Debug.Log($"Error reading json {e}. Skipping...");
            }
        }

        void WriteRuntimeSettings() {
            string path = GetFilePath("runtimeSettingsSummary.json");
            var json = JsonConvert.SerializeObject(RuntimeSettingsSummary);
            File.WriteAllText(path, json);
        }

        string lastWifiConnectionStatusJson = string.Empty;
        public void RefreshWifiConnectionStatus() {
            try {
                string json = File.ReadAllText(GetFilePath("wifiConnectionStatus.json"));
                if (json != null && !lastWifiConnectionStatusJson.Equals(json)) {
                    var obj = JsonConvert.DeserializeObject<WifiConnectionStatus>(json);
                    if (obj == null) return;
                    WifiConnectionStatus = obj;
                    OnWifiConnectionStatusChange?.Invoke(obj);
                    lastWifiConnectionStatusJson = json;
                }
            }
            catch (JsonReaderException e) {
                Debug.Log($"Error reading json {e}. Skipping...");
            }
        }

        void WriteWifiConnectionStatus() {
            string path = GetFilePath("wifiConnectionStatus.json");
            var json = JsonConvert.SerializeObject(WifiConnectionStatus);
            File.WriteAllText(path, json);
        }

        string lastWifiNetworksJson = string.Empty;
        public void RefreshWifiNetworks() {
            try {
                string json = File.ReadAllText(GetFilePath("wifiNetworks.json"));
                if (json != null && !lastWifiNetworksJson.Equals(json)) {
                    var obj = JsonConvert.DeserializeObject<List<ScannedWifiNetwork>>(json);
                    if (obj == null) return;
                    WifiNetworks = obj;
                    OnWifiNetworksChange?.Invoke(obj);
                    lastWifiNetworksJson = json;
                }
            }
            catch (JsonReaderException e) {
                Debug.Log($"Error reading json {e}. Skipping...");
            }
        }

        public void Sync() {
            RefreshDeviceStatus();
            RefreshRuntimeSettings();
            RefreshWifiConnectionStatus();
            RefreshWifiNetworks();
        }

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
            coroutine = StartCoroutine(ManualUpdate());
        }

        void OnDisable() {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }

        IEnumerator ManualUpdate() {
            while(true) {
                Sync();
                yield return new WaitForSeconds(syncFrequency);
            }
        }

        string GetFilePath(string fileName) {
            return Path.Combine(Application.dataPath.Replace("Assets", "Files/MightyImmersion"), fileName);
        }
    }
}
