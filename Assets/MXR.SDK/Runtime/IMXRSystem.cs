using System;
using System.Collections.Generic;

namespace MXR.SDK {
    /// <summary>
    /// Exposes properties, events and methods to the ManageXR 
    /// admin/system application
    /// </summary>
    public interface IMXRSystem {
        /// <summary>
        /// The current status of the device
        /// </summary>
        DeviceStatus DeviceStatus { get; }

        /// <summary>
        /// The current ManageXR settings for this device
        /// </summary>
        RuntimeSettingsSummary RuntimeSettingsSummary { get; }

        /// <summary>
        /// The current WiFi status of the device
        /// </summary>
        WifiConnectionStatus WifiConnectionStatus { get; }

        /// <summary>
        /// WiFi networks currently available
        /// </summary>
        List<ScannedWifiNetwork> WifiNetworks { get; }

        /// <summary>
        /// WiFi network currently connected to (if any)
        /// </summary>
        ScannedWifiNetwork CurrentNetwork { get; }

        /// <summary>
        /// Fired when the ManageXR status updates
        /// </summary>
        event Action<DeviceStatus> OnDeviceStatusChange;

        /// <summary>
        /// Event fired when the ManageXR settings updates
        /// </summary>
        event Action<RuntimeSettingsSummary> OnRuntimeSettingsSummaryChange;

        /// <summary>
        /// Event fired when the wifi connection status updates
        /// </summary>
        event Action<WifiConnectionStatus> OnWifiConnectionStatusChange;

        /// <summary>
        /// Event fired when the available wifi networks update
        /// </summary>
        event Action<List<ScannedWifiNetwork>> OnWifiNetworksChange;

        /// <summary>
        /// Disable Kiosk mode on the device
        /// </summary>
        void DisableKioskMode();

        /// <summary>
        /// Enable Kiosk mode on the device
        /// </summary>
        void EnableKioskMode();

        /// <summary>
        /// Syncs the device with the configuration
        /// on the ManageXR dashboard
        /// </summary>
        void Sync();

        /// <summary>
        /// Refreshes <see cref="DeviceStatus"/>
        /// Once called, the <see cref="OnDeviceStatusChange"/>
        /// event is also invoked.
        /// </summary>
        void RefreshDeviceStatus();

        /// <summary>
        /// Refreshes <see cref="RuntimeSettingsSummary"/>
        /// Once called, the <see cref="OnRuntimeSettingsSummaryChange"/>
        /// event is also invoked.
        /// </summary>
        void RefreshRuntimeSettings();

        /// <summary>
        /// Refreshes <see cref="WifiConnectionStatus"/>
        /// Once called, the <see cref="OnWifiConnectionStatusChange"/>
        /// event is also invoked.
        /// </summary>
        void RefreshWifiConnectionStatus();

        /// <summary>
        /// Refreshes <see cref="WifiNetworks"/>
        /// Once called, the <see cref="OnWifiNetworksChange"/>
        /// event is also invoked.
        /// </summary>
        void RefreshWifiNetworks();

        /// <summary>
        /// Connects to a wifi network
        /// </summary>
        /// <param name="ssid">The SSID of the network to connect to</param>
        /// <param name="password">The password to use to attempt to connect</param>
        void ConnectToWifiNetwork(string ssid, string password);

        /// <summary>
        /// Disable the wifi device
        /// </summary>
        void DisableWifi();

        /// <summary>
        /// Enable the wifi device
        /// </summary>
        void EnableWifi();

        /// <summary>
        /// Forgets a wifi network. Once forgotten,
        /// connecting to the network would require
        /// the password again.
        /// </summary>
        /// <param name="ssid">
        /// The SSID of the network to forget
        /// </param>
        void ForgetWifiNetwork(string ssid);

        /// <summary>
        /// Exit the launcher
        /// </summary>
        void ExitLauncher();
    }
}