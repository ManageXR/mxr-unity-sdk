using System;
using System.Collections.Generic;

namespace MXR.SDK {
    /// <summary>
    /// Exposes properties, events and methods to the ManageXR 
    /// admin/system application
    /// </summary>
    public interface IMXRSystem {
        /// <summary>
        /// Whether the system will log messages to the Unity console
        /// </summary>
        bool LoggingEnabled { get; set; }

        /// <summary>
        /// Whether the ManageXR Admin App is installed on this device.
        /// If not, the device cannot communicate with the ManageXR service.
        /// </summary>
        bool IsAdminAppInstalled { get; }

        /// <summary>
        /// Whether the system is available/bound/online for use.
        /// If false, the SDK will not work.
        /// </summary>
        bool IsConnectedToAdminApp { get; }

        /// <summary>
        /// Whether the system is available/bound/online for use.
        /// If false, the SDK will not work.
        /// </summary>
        [Obsolete("This property has been deprecated and may soon be removed. Please use IsConnectedToAdminApp instead.", false)]
        bool IsAvailable { get; }

        /// <summary>
        /// Data associated with this device
        /// </summary>
        DeviceData DeviceData { get; }

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
        /// Fired when the availability of system changes.
        /// </summary>
        event Action<bool> OnAvailabilityChange;

        /// <summary>
        /// Fired when the device data changes
        /// </summary>
        event Action<DeviceData> OnDeviceDataChange;

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
        /// Event fired when a Play Video command is received
        /// </summary>
        event Action<PlayVideoCommandData> OnPlayVideoCommand;

        /// <summary>
        /// Event fired when a Pause Video command is received
        /// </summary>
        event Action<PauseVideoCommandData> OnPauseVideoCommand;

        /// <summary>
        /// Event fired when a Resume Video command is received
        /// </summary>
        event Action<ResumeVideoCommandData> OnResumeVideoCommand;        

        /// <summary>
        /// Event fired when the admin app requests for 
        /// <see cref="HomeScreenState"/> 
        /// </summary>
        event Action OnHomeScreenStateRequest;

        /// <summary>
        /// Disable Kiosk mode on the device
        /// </summary>
        void DisableKioskMode();

        /// <summary>
        /// Enable Kiosk mode on the device
        /// </summary>
        void EnableKioskMode();


        /// <summary>
        /// Kills the running application with packageName.
        /// </summary>
        void KillApp(string packageName);

        /// <summary>
        /// Kills and then restarts the running application with packageName.
        /// </summary>
        void RestartApp(string packageName);

        /// <summary>
        /// Syncs the device with the configuration
        /// on the ManageXR dashboard
        /// </summary>
        void Sync();

        /// <summary>
        /// Requests the admin app to refresh <see cref="DeviceData"/>
        /// If the device data has changed, <see cref="OnDeviceDataChange"/> is fired.
        /// </summary>
        void RefreshDeviceData();

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

        ///// <summary>
        ///// Connects to an Enterpirse wifi network
        ///// </summary>
        ///// <param name="ssid">The payload for the Enterprise Wifi Connection Request</param>
        void ConnectToEnterpriseWifiNetwork(EnterpriseWifiConnectionRequest enterpriseWifiConnectionRequest);

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
        /// Sends the <see cref="HomeScreenState"/> to the system.
        /// </summary>
        /// <param name="state">The state to be sent</param>
        void SendHomeScreenState(HomeScreenState state);

        /// <summary>
        /// Exit the launcher
        /// </summary>
        void ExitLauncher();
    }
}