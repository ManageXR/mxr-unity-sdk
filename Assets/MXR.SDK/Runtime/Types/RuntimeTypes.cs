using Newtonsoft.Json;

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
        /// Device ID
        /// </summary>
        public string id;

        /// <summary>
        /// The name of the device as set on the ManageXR portal
        /// </summary>
        public string deviceName; 

        /// <summary>
        /// Whether device passcode has been enabled on the configuration this device
        /// uses. 
        /// </summary>
        public bool isPasscodeEnabled;

        /// <summary>
        /// The ManageXR organization this device is under
        /// </summary>
        public Organization organization = new Organization();

        /// <summary>
        /// The ManageXR configuration this device is currently assigned 
        /// </summary>
        public Configuration configuration = new Configuration();

        /// <summary>
        /// The kiosk app to run IF <see cref="deviceExperienceMode"/> is set to
        /// <see cref="DeviceExperienceMode.KIOSK"/>. 
        /// </summary>
        public RuntimeApp kioskApp = null;

        /// <summary>
        /// The current "mode" set as the device experience. Refer to managexr.com
        /// for what these different product offerings mean.
        /// Defaults to HOME_SCREEN for legacy reasons.
        /// </summary>
        public DeviceExperienceMode deviceExperienceMode = DeviceExperienceMode.HOME_SCREEN;

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
        /// Whether the kiosk mode is currently enabled.
        /// </summary>
        public bool kioskModeEnabled = true;

        /// <summary>
        /// Whether the mic is force muted at the system level.
        /// </summary>
        public bool muteMic = true;

        /// <summary>
        /// Use when <see cref="deviceExperienceMode"/> is set to <see cref="DeviceExperienceMode.KIOSK"/>,
        /// in this scenario, the homescreen is launched on pressing the home (or equivalent) button 
        /// on the controller.
        /// This field describes if the shortcut menu is to be shown when the homescreen opens.
        /// </summary>
        public bool enableKioskShortcutMenu = false;

        /// <summary>
        /// A list of features enabled for this deployment
        /// </summary>
        public List<string> featureFlags = new List<string>();

        /// <summary>
        /// Use to determine if, once we are in the android 12 permissions popup, we should allow
        /// the user to exit or not.
        /// </summary>
        public bool forceAndroid12PermissionsAccept = false;

        /// <summary>
        /// Disables gaze input mode when no controllers or handtracking is available. 
        /// Instead, the headset button will be used to trigger clicks.
        /// </summary>
        public bool disableGazeInput = false;

        /// <summary>
        /// Helper property for whether the guardian settings are hidden
        /// </summary>
        [JsonIgnore]
        public bool IsGaurdianHidden => TryGet(x => x.customLauncherSettings.hiddenSettings.guardian, false);

        /// <summary>
        /// Helper property for whether the cast settings are hidden
        /// </summary>
        [JsonIgnore]
        public bool IsCastHidden => TryGet(x => x.customLauncherSettings.hiddenSettings.cast, false);

        /// <summary>
        /// Helper property for whether the cast settings are hidden
        /// </summary>
        [JsonIgnore]
        public bool IsBrightnessHidden => TryGet(x => x.customLauncherSettings.hiddenSettings.brightness, false);

        /// <summary>
        /// Helper property for whether the bluetooth settings are hidden
        [JsonIgnore]
        public bool IsBluetoothHidden => TryGet(x => x.customLauncherSettings.hiddenSettings.bluetooth, false);

        /// <summary>
        /// Helper property for whether the wifi settings are hidden
        /// </summary>
        [JsonIgnore]
        public bool IsWifiHidden => TryGet(x => x.customLauncherSettings.hiddenSettings.wifi, false);

        /// <summary>
        /// Helper property for whether the force passthrough setting is active or not
        /// </summary>
        [JsonIgnore]
        public bool IsPassthroughForced => TryGet(x => x.customLauncherSettings.backgroundSettings.forcePassthrough, false);


        /// <summary>
        /// Helper property for whether the controller settings are hidden
        /// </summary>
        [JsonIgnore]
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
        /// The different language types recognised by the SDK. 
        /// </summary>
        [Serializable]
        public enum DisplayLanguage {
            enUS,
            frFR,
            deDE,
            esES,
            ukUA,
            ptPT,
            svSE
        }

        /// <summary>
        /// The display language type used in the home screen
        /// </summary>
        public DisplayLanguage displayLanguage = DisplayLanguage.enUS;

        /// <summary>
        /// Whether the shortcut menu should NOT be shown when the user comes back to the
        /// homescreen app when <see cref="RuntimeSettingsSummary.deviceExperienceMode"/>
        /// is set to <see cref="DeviceExperienceMode.HOME_SCREEN"/>
        /// </summary>
        public bool disableShortcutMenu = false;

        /// <summary>
        /// The settings that are not to be made editable in the launcher
        /// </summary>
        public HiddenSettings hiddenSettings = new HiddenSettings();

        /// <summary>
        /// Customization settings for the library panel
        /// </summary>
        public LibrarySettings librarySettings = new LibrarySettings();

        /// <summary>
        /// The settings used to configure background setting properties
        /// </summary>
        public BackgroundSettings backgroundSettings = new BackgroundSettings();

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
    /// Settings configured to be hidden in the homescreen.
    /// </summary>
    [Serializable]
    public class HiddenSettings {
        public bool bluetooth;
        public bool cast;
        public bool controller;
        public bool guardian;
        public bool wifi;
        public bool passthrough;
        public bool brightness;
    }
    /// <summary>
    /// Settings used to configure Background Settings.
    /// </summary>
    [Serializable]
    public class BackgroundSettings {
        public bool forcePassthrough;
    }

    /// <summary>
    /// Customization settings for the library user interface
    /// </summary>
    [Serializable]
    public class LibrarySettings {
        /// <summary>
        /// Customization settings for the content cards shown in the library
        /// </summary>
        public CardSettings cardSettings = new CardSettings();

        /// <summary>
        /// Customization settings for the panel that displays the library
        /// </summary>
        public PanelSettings panelSettings = new PanelSettings();

        /// <summary>
        /// Customization settings for the content card grid in the library
        /// </summary>
        public GridSettings gridSettings = new GridSettings();
    }

    /// <summary>
    /// Customization settings for the content card grid in the library
    /// </summary>
    [Serializable]
    public class GridSettings {
        /// <summary>
        /// The number of cards shown in a single row of the library content card grid
        /// </summary>
        public int cardsPerRow = 4;

        /// <summary>
        /// The alignment of the cards in the library content card grid
        /// </summary>
        public CardAlignment alignment = CardAlignment.LEFT;
    }

    [Serializable]
    public enum CardAlignment {
        LEFT,
        CENTER,
        RIGHT
    }

    /// <summary>
    /// Customization settings for the content cards shown in the library
    /// </summary>
    [Serializable]
    public class CardSettings {
        /// <summary>
        /// Whether the content cards should show the title text
        /// </summary>
        public bool showTitle = true;

        /// <summary>
        /// Whether the content cards should show the content type text ("App", "Video", WebXR")
        /// </summary>
        public bool showContentType = true;
    }

    /// <summary>
    /// Customization settings for the panel that displays the library
    /// </summary>
    [Serializable]
    public class PanelSettings {
        public CategoriesPosition categoriesPosition = CategoriesPosition.TOP;
    }

    /// <summary>
    /// The location of categories in the library panel
    /// </summary>
    [Serializable]
    public enum CategoriesPosition {
        /// <summary>
        /// Categories are not shown in the library panel
        /// </summary>
        NONE,

        /// <summary>
        /// Categories are shown at the top of the library panel
        /// </summary>
        TOP
    }

    /// <summary>
    /// Represents the mode the device/homescreen app is in.
    /// </summary>
    [Serializable]
    public enum DeviceExperienceMode {
        /// <summary>
        /// When the home screen has been disabled via the ManageXR web dashboard.
        /// </summary>
        DEFAULT,

        /// <summary>
        /// When a content has been configured to be the only running content
        /// on this device. The homescreen doesn't show the library in this mode
        /// </summary>
        KIOSK,

        /// <summary>
        /// Mode when the ManageXR library is visible along with settings/options
        /// in the homescreen.
        /// </summary>
        HOME_SCREEN
    }

    /// <summary>
    /// Represents the current expiration behavior for the app.
    /// </summary>
    [Serializable]
    public enum ExpirationBehavior {
        /// <summary>
        /// When the app is not disabled or deleted 
        /// </summary>
        NONE,

        /// <summary>
        /// When the app is in a disabled state
        /// </summary>
        DISABLE_APP,

        /// <summary>
        /// When the app is in a deleted state
        /// </summary>
        DELETE_APP
    }

    /// <summary>
    /// Splash screen settings for the homescreen
    /// Future feature. Currently not supported.
    /// </summary>
    [Serializable]
    [Obsolete("Splash settings are currently not available for configuration on the MXR dashboard")]
    public class SplashSettings {
        public float duration;
        public string bottomColorHex;
        public string topColorHex;
        public float imageScale;
    }

    /// <summary>
    /// Represents a configuration created on ManageXR
    /// </summary>
    [Serializable]
    public class Configuration {
        /// <summary>
        /// The ID of the configuration
        /// </summary>
        public string id;

        /// <summary>
        /// Name of the organization
        /// </summary>
        public string name;
    }

    /// <summary>
    /// Represents an orgnization created on ManageXR
    /// </summary>
    public class Organization {
        /// <summary>
        /// The ID of the organization
        /// </summary>
        public string id;

        /// <summary>
        /// The name of the organization
        /// </summary>
        public string name;
    }

    /// <summary>
    /// Represents an image that has been downloaded to disk 
    /// using the admin app and is available through a disk path
    /// </summary>
    [System.Serializable]
    public class CustomLauncherImage {
        /// <summary>
        /// Unique identifier of the image
        /// </summary>
        public string id = string.Empty;

        /// <summary>
        /// Name of the image
        /// </summary>
        public string name = string.Empty;

        /// <summary>
        /// Local path to the image 
        /// </summary>
        public string path = string.Empty;

        public bool IsValid() {
            return !string.IsNullOrEmpty(id)
                && !string.IsNullOrEmpty(name)
                && !string.IsNullOrEmpty(path);
        }
    }
}
