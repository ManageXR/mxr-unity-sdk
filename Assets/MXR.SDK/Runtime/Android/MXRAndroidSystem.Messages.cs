using System;
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
            public const int CASTING_CODE = 21000;
            public const int PREPARE_FOR_TERMINATION = 24000;
            public const int REQUEST_USER_IDENTITY = 26;
        }

        private void OnMessageFromAdminApp(int what, string json) {
            if (string.IsNullOrEmpty(json)) {
                LogIfEnabled(LogType.Warning, $"Received null or empty JSON for message type {what}");
                return;
            }

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
                case AdminAppMessageTypes.CASTING_CODE:
                    HandleCastingCode(json);
                    break;
                case AdminAppMessageTypes.HANDLE_COMMAND:
                    ProcessCommandJson(json);
                    break;
                case AdminAppMessageTypes.GET_HOME_SCREEN_STATE:
                    try {
                        OnHomeScreenStateRequest?.Invoke();
                    } catch (Exception ex) {
                        LogIfEnabled(LogType.Error, $"Exception in OnHomeScreenStateRequest event: {ex.GetType().Name}: {ex.Message}");
                    }
                    break;
                case AdminAppMessageTypes.PREPARE_FOR_TERMINATION:
                    LogIfEnabled(LogType.Log, $"Termination Notification Received");
                    try {
                        OnTerminationNotification?.Invoke();
                    } catch (Exception ex) {
                        LogIfEnabled(LogType.Error, $"Exception in OnTerminationNotificatiocln event: {ex.GetType().Name}: {ex.Message}");
                    }
                    break;
                case AdminAppMessageTypes.REQUEST_USER_IDENTITY:
                    HandleUserIdentityRequest(json);
                    break;
                default:
                    LogIfEnabled(LogType.Warning, $"Unknown message type received: {what}");
                    break;
            }
        }

        private void HandleWifiNetworks(string json) {
            try {
                if (json.Equals(lastWifiNetworksJSON)) {
                    return;
                }

                var networks = JsonConvert.DeserializeObject<List<ScannedWifiNetwork>>(json);
                if (networks == null) {
                    LogIfEnabled(LogType.Warning, "Failed to deserialize WifiNetworks: result was null");
                    return;
                }

                lastWifiNetworksJSON = json;
                WifiNetworks = networks;
                OnWifiNetworksChange?.Invoke(networks);
                LogIfEnabled(LogType.Log, "WifiNetworks updated with " + WifiNetworks.Count + " networks.");
            } catch (JsonException ex) {
                LogIfEnabled(LogType.Error, $"JSON deserialization error in HandleWifiNetworks: {ex.Message}");
            } catch (Exception ex) {
                LogIfEnabled(LogType.Error, $"Unexpected error in HandleWifiNetworks: {ex.GetType().Name}: {ex.Message}");
            }
        }

        private void HandleWifiConnectionStatus(string json) {
            try {
                if (json.Equals(lastWifiConnectionStatusJSON)) {
                    return;
                }

                var status = JsonConvert.DeserializeObject<WifiConnectionStatus>(json);
                if (status == null) {
                    LogIfEnabled(LogType.Warning, "Failed to deserialize WifiConnectionStatus: result was null");
                    return;
                }

                lastWifiConnectionStatusJSON = json;
                WifiConnectionStatus = status;
                OnWifiConnectionStatusChange?.Invoke(status);
                LogIfEnabled(LogType.Log, "WifiConnectionStatus updated.");
            } catch (JsonException ex) {
                LogIfEnabled(LogType.Error, $"JSON deserialization error in HandleWifiConnectionStatus: {ex.Message}");
            } catch (Exception ex) {
                LogIfEnabled(LogType.Error, $"Unexpected error in HandleWifiConnectionStatus: {ex.GetType().Name}: {ex.Message}");
            }
        }

        private void HandleRuntimeSettingsSummary(string json) {
            try {
                if (json.Equals(lastRuntimeSettingsSummaryJSON)) {
                    return;
                }

                AtomicWriteAllText(_cachedRuntimeSettingsSummaryPath, json);
                var settings = new JsonSerializerSettings {
                    Error = (sender, args) => {
                        LogIfEnabled(LogType.Warning,
                            $"JSON deserialization warning in HandleRuntimeSettingsSummary: Failed to deserialize field '{args.ErrorContext.Path}': {args.ErrorContext.Error.Message}");
                        args.ErrorContext.Handled = true;
                    }
                };
                var summary = JsonConvert.DeserializeObject<RuntimeSettingsSummary>(json, settings);
                if (summary == null) {
                    LogIfEnabled(LogType.Warning, "Failed to deserialize RuntimeSettingsSummary: result was null");
                    return;
                }

                lastRuntimeSettingsSummaryJSON = json;
                RuntimeSettingsSummary = summary;
                OnRuntimeSettingsSummaryChange?.Invoke(summary);
                LogIfEnabled(LogType.Log, "RuntimeSettingsSummary updated.");
            } catch (JsonException ex) {
                LogIfEnabled(LogType.Error, $"JSON deserialization error in HandleRuntimeSettingsSummary: {ex.Message}");
            } catch (Exception ex) {
                LogIfEnabled(LogType.Error, $"Unexpected error in HandleRuntimeSettingsSummary: {ex.GetType().Name}: {ex.Message}");
            }
        }

        private void HandleDeviceStatus(string json) {
            try {
                if (json.Equals(lastDeviceStatusJSON)) {
                    return;
                }

                AtomicWriteAllText(_cachedDeviceStatusPath, json);
                var settings = new JsonSerializerSettings {
                    Error = (sender, args) => {
                        LogIfEnabled(LogType.Warning,
                            $"JSON deserialization warning in HandleDeviceStatus: Failed to deserialize field '{args.ErrorContext.Path}': {args.ErrorContext.Error.Message}");
                        args.ErrorContext.Handled = true;
                    }
                };
                var status = JsonConvert.DeserializeObject<DeviceStatus>(json, settings);
                if (status == null) {
                    LogIfEnabled(LogType.Warning, "Failed to deserialize DeviceStatus: result was null");
                    return;
                }

                lastDeviceStatusJSON = json;
                DeviceStatus = status;
                OnDeviceStatusChange?.Invoke(status);
                LogIfEnabled(LogType.Log, "DeviceStatus updated.");
            } catch (JsonException ex) {
                LogIfEnabled(LogType.Error, $"JSON deserialization error in HandleDeviceStatus: {ex.Message}");
            } catch (Exception ex) {
                LogIfEnabled(LogType.Error, $"Unexpected error in HandleDeviceStatus: {ex.GetType().Name}: {ex.Message}");
            }
        }

        private void HandleDeviceData(string json) {
            try {
                if (json.Equals(lastDeviceDataJSON)) {
                    return;
                }

                AtomicWriteAllText(_cachedDeviceDataPath, json);

                var data = JsonConvert.DeserializeObject<DeviceData>(json);
                if (data == null) {
                    return;
                }

                lastDeviceDataJSON = json;
                DeviceData = data;
                OnDeviceDataChange?.Invoke(data);
                LogIfEnabled(LogType.Log, "DeviceData updated.");
            } catch (JsonException ex) {
                LogIfEnabled(LogType.Error, $"JSON deserialization error in HandleDeviceData: {ex.Message}");
            } catch (Exception ex) {
                LogIfEnabled(LogType.Error, $"Unexpected error in HandleDeviceData: {ex.GetType().Name}: {ex.Message}");
            }
        }

        private void HandleUserIdentityRequest(string json) {
            try {
                var request = JsonConvert.DeserializeObject<UserIdentityRequest>(json);
                if (request == null) {
                    LogIfEnabled(LogType.Warning, "Failed to deserialize UserIdentityRequest: result was null");
                    return;
                }

                OnUserIdentityRequest?.Invoke(request);
                LogIfEnabled(LogType.Log, "UserIdentityRequest received.");
            } catch (JsonException ex) {
                LogIfEnabled(LogType.Error, $"JSON deserialization error in HandleUserIdentityRequest: {ex.Message}");
            } catch (Exception ex) {
                LogIfEnabled(LogType.Error, $"Unexpected error in HandleUserIdentityRequest: {ex.GetType().Name}: {ex.Message}");
            }
        }

        private void HandleCastingCode(string json) {
            try {
                var castingCodeData = JsonConvert.DeserializeObject<CastingCodeStatus>(json);
                if (castingCodeData == null) {
                    return;
                }

                CastingCodeStatus = castingCodeData;
                OnCastingCodeStatusChanged?.Invoke(castingCodeData);
                LogIfEnabled(LogType.Log, "CastingCodeStatus updated.");
            } catch (JsonException ex) {
                LogIfEnabled(LogType.Error, $"JSON deserialization error in HandleCastingCode: {ex.Message}");
            } catch (Exception ex) {
                LogIfEnabled(LogType.Error, $"Unexpected error in HandleCastingCode: {ex.GetType().Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Writes <paramref name="content"/> to <paramref name="path"/> via a temp-file
        /// rename pattern. Direct File.WriteAllText truncates the destination to 0 bytes
        /// before writing the new content; if the process is killed during that window
        /// (Android OOM kill, force-stop, unclean shutdown) the destination is left
        /// empty, and HS reads stale/empty cache on next startup. This pattern writes
        /// to "<path>.tmp" first then moves it into place, so a reader sees either the
        /// old valid file or the new valid file but never a 0-byte intermediate.
        ///
        /// See ENG-2319 / ENG-1990 for context.
        /// </summary>
        private void AtomicWriteAllText(string path, string content) {
            string tmp = path + ".tmp";
            try {
                File.WriteAllText(tmp, content);
                // File.Move does not overwrite existing files in older .NET runtimes,
                // so delete-then-move. The race window between Delete and Move is
                // microseconds and several orders of magnitude smaller than the
                // truncate-then-write window in direct File.WriteAllText.
                if (File.Exists(path)) {
                    File.Delete(path);
                }
                File.Move(tmp, path);
            } catch (Exception ex) {
                LogIfEnabled(LogType.Error, $"AtomicWriteAllText failed for {path}: {ex.GetType().Name}: {ex.Message}");
                // Best-effort cleanup of the temp file so we don't leak it.
                try {
                    if (File.Exists(tmp)) File.Delete(tmp);
                } catch {
                    // Swallow — the only way this can fail is a permissions issue we
                    // can't recover from, and there is nothing useful to do here.
                }
            }
        }

    }
}
