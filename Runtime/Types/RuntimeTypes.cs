using System;
using System.Collections.Generic;

using UnityEngine;

namespace MXR.SDK {
    /// <summary>
    /// The runtime settings for the ManageXR device. This class contains information
    /// about the ManageXR configuration assigned to this device.
    /// </summary>
    [Serializable]
    public class RuntimeSettingsSummary {
        /// <summary>
        /// Settings ID
        /// </summary>
        public string id;

        /// <summary>
        /// The name of the device as set on the ManageXR portal
        /// </summary>
        public string deviceName;

        /// <summary>
        /// The apps deployed to the device
        /// </summary>
        public Dictionary<string, RuntimeApp> apps = new Dictionary<string, RuntimeApp>();

        /// <summary>
        /// The WebXR websites allowed on this device
        /// </summary>
        public Dictionary<string, WebXRApp> webXRApps = new Dictionary<string, WebXRApp>();

        /// <summary>
        /// The videos deployed to this devices
        /// </summary>
        public Dictionary<string, Video> videos = new Dictionary<string, Video>();

        /// <summary>
        /// Configuration, Settings and Customization for the launcher
        /// </summary>
        public CustomLauncherSettings customLauncherSettings = new CustomLauncherSettings();

        /// <summary>
        /// Whether the kiosk mode is enabled
        /// </summary>
        public bool kioskModeEnabled = true;

        /// <summary>
        /// Helper property for whether the guardian settings are hidden
        /// </summary>
        public bool IsGaurdianHidden => TryGet(x => x.customLauncherSettings.hiddenSettings.guardian, false);

        /// <summary>
        /// Helper property for whether the cast settings are hidden
        /// </summary>
        public bool IsCastHidden => TryGet(x => x.customLauncherSettings.hiddenSettings.cast, false);

        /// <summary>
        /// Helper property for whether the bluetooth settings are hidden
        public bool IsBluetoothHidden => TryGet(x => x.customLauncherSettings.hiddenSettings.bluetooth, false);

        /// <summary>
        /// Helper property for whether the wifi settings are hidden
        /// </summary>
        public bool IsWifiHidden => TryGet(x => x.customLauncherSettings.hiddenSettings.wifi, false);

        /// <summary>
        /// Helper property for whether the controller settings are hidden
        /// </summary>
        public bool AreControllerSettingsHidden => TryGet(x => x.customLauncherSettings.hiddenSettings.controller, false);

        /// <summary>
        /// Gets a <see cref="RuntimeApp"/> for a given package name, if present in the settings
        /// </summary>
        /// <param name="packageName"></param>
        /// <returns></returns>
        public RuntimeApp GetRuntimeAppFromPackageName(string packageName) {
            if (string.IsNullOrEmpty(packageName))
                return null;
            if (apps.TryGetValue(packageName, out RuntimeApp result))
                return result;
            return null;
        }

        T TryGet<T>(Func<RuntimeSettingsSummary, T> getter, T fallback) {
            try {
                return getter(this);
            }
            catch (Exception e) {
                Debug.LogError(e);
                return fallback;
            }
        }
    }

    /// <summary>
    /// Represents the homescreen settings for the ManageXR controlled device
    /// Used for customising the home screen experience
    /// </summary>
    [Serializable]
    public class CustomLauncherSettings {
        /// <summary>
        /// The admin PIN to access the admin settings. 
        /// </summary>
        public string adminPin = "5112";

        /// <summary>
        /// The settings that are not to be made editable in the launcher
        /// </summary>
        public HiddenSettings hiddenSettings = new HiddenSettings();

        /// <summary>
        /// Whether the guardian/boundary settings should be opened on launch
        /// </summary>
        public bool openGuardianOnLaunch;

        /// <summary>
        /// The settings for the splash screen
        /// </summary>
        [Obsolete("Splash settings are currently not supported.")]
        public SplashSettings splashSettings = new SplashSettings();

        /// <summary>
        /// The background 360 image to be displayed in the homescreen
        /// </summary>
        public CustomLauncherImage backgroundFile = new CustomLauncherImage();

        /// <summary>
        /// Secondary background image, used by the official ManageXR homescreen app to
        /// change the 360 background when the user is in the app shortcut menu
        /// </summary>
        public CustomLauncherImage shortcutMenuBackgroundFile = new CustomLauncherImage();

        /// <summary>
        /// The <see cref="CustomLauncherImage"/> to be shown on top of the homescreen panels
        /// </summary>
        public CustomLauncherImage topLogoFile = new CustomLauncherImage();

        /// <summary>
        /// The <see cref="CustomLauncherImage"/> to be shown at the bottom of the homescreen panels
        /// </summary>
        public CustomLauncherImage bottomLogoFile = new CustomLauncherImage();

        /// <summary>
        /// The theme to be used for the homescreen
        /// </summary>
        public CustomHomeScreenColors colors = new CustomHomeScreenColors();
    }

    /// <summary>
    /// The theming data for the homescreen
    /// </summary>
    [Serializable]
    public class CustomHomeScreenColors {
        public string menuBackground;
        public string menuText;
        public string buttonPrimary;
        public string buttonPrimaryText;
        public string buttonSecondary;
        public string buttonSecondaryText;
        public string input;
        public string inputText;
        public string alert;
        public string success;
    }

    /// <summary>
    /// Settings configured to be hidden in the homescreen
    /// </summary>
    [Serializable]
    public class HiddenSettings {
        public bool bluetooth;
        public bool cast;
        public bool controller;
        public bool guardian;
        public bool wifi;
    }

    /// <summary>
    /// Splash screen settings for the homescreen
    /// Future feature. Currently not supported.
    /// </summary>
    [Serializable]
    [Obsolete("Splash settings are currently not supported.")]
    public class SplashSettings {
        public float duration;
        public string bottomColorHex;
        public string topColorHex;
        public float imageScale;
    }

    /// <summary>
    /// Represents an image that has been downloaded to disk 
    /// using the admin app and is available through a disk path
    /// </summary>
    [System.Serializable]
    public class CustomLauncherImage {
        public string id = string.Empty;
        public string name = string.Empty;
        public string path = string.Empty;

        public bool IsValid() {
            return !string.IsNullOrEmpty(id)
                && !string.IsNullOrEmpty(name)
                && !string.IsNullOrEmpty(path);
        }
    }
}