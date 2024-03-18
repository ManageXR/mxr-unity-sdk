using System.Collections.Generic;

namespace MXR.SDK {
    /// <summary>
    /// The runtime status of the ManageXR managed device. This class contains
    /// state of about the system and content deployed to this device
    /// </summary>
    [System.Serializable]
    public class DeviceStatus {
        /// <summary>
        /// The device's serial number
        /// </summary>
        public string serial = "";

        /// <summary>
        /// The <see cref="AppInstallStatus"/> for applications for this device
        /// The key(string) is the <see cref="RuntimeApp.packageName"/> of <see cref="RuntimeApp"/>
        /// </summary>
        public Dictionary<string, AppInstallStatus> appStatuses = new Dictionary<string, AppInstallStatus>();

        /// <summary>
        /// The <see cref="FileInstallStatus"/> for videos for this device
        /// The key(string) is the <see cref="Content.id"/> of <see cref="Video"/>
        /// </summary>
        public Dictionary<string, FileInstallStatus> videoStatuses = new Dictionary<string, FileInstallStatus>();

        /// <summary>
        /// Whether the device is locked using a passcode.
        /// </summary>
        public bool locked = false;

        /// <summary>
        /// Status of the device system
        /// </summary>
        public DeviceSystemVersionInstallStatus deviceSystemVersionStatus = new DeviceSystemVersionInstallStatus();

        /// <summary>
        /// Whether the application has Android data permissions, required for local storage
        /// </summary>
        public bool hasAndroidDataPermission = false;

        /// <summary>
        /// Whether the application requires Android data permissions, based on its deployed files.
        /// </summary>
        public bool requiresAndroidDataPermission = false;

        /// <summary>
        /// Whether the application has Android OBB permissions, required for application installation
        /// </summary>
        public bool hasAndroidObbPermission = false;

        /// <summary>
        /// Whether the application requires Android OBB permissions, based on its deployed apps.
        /// </summary>
        public bool requiresAndroidObbPermission = false;        

        public bool picoCvControllerUpdateAvailable;
        public bool picoGuardianHasBeenOpened;

        /// <summary>
        /// The brightness level for the screen
        /// </summary>
        public int screenBrightness;
        
        /// <summary>
        /// The last app open in the foregound that is appropriate to show in the shortcut menu.
        /// </summary>
        public ForegroundAppForShortcutMenu lastForegroundAppForShortcutMenu = new ForegroundAppForShortcutMenu();
        public bool oculusScreencastActive;
        public ForegroundApp currentForegroundApp = new ForegroundApp();
        public ForegroundApp previousForegroundApp = new ForegroundApp();
        public Dictionary<string, FileInstallStatus> fileStatuses = new Dictionary<string, FileInstallStatus>();
        public Timestamp lastCheckIn = new Timestamp();
        public Timestamp lastUpdate = new Timestamp();

        /// <summary>
        /// Whether the device has its mic muted at the system level.
        /// </summary>
        public bool micMuted;
        
        /// <summary>
        /// Returns the <see cref="FileInstallStatus"/> for a <see cref="Video"/>
        /// </summary>
        /// <param name="video"></param>
        /// <returns></returns>
        public FileInstallStatus FileInstallStatusForVideo(Video video) {
            if (video == null) return null;
            if (videoStatuses.TryGetValue(video.id, out FileInstallStatus status))
                return status;
            else
                return null;
        }

        /// <summary>
        /// Returns the <see cref="AppInstallStatus"/> of a <see cref="RuntimeApp"/>
        /// </summary>
        /// <param name="runtimeApp"></param>
        /// <returns></returns>
        public AppInstallStatus AppInstallStatusForRuntimeApp(RuntimeApp runtimeApp) {
            if (runtimeApp == null) return null;
            if (appStatuses.TryGetValue(runtimeApp.packageName, out AppInstallStatus result))
                return result;
            else
                return null;
        }
    }

    /// <summary>
    /// Represents the status of a file on the device
    /// </summary>
    [System.Serializable]
    public class FileInstallStatus {
        [System.Serializable]
        public enum Status {
            QUEUED, DOWNLOADING, COMPLETE, ERROR
        }

        [System.Serializable]
        public enum ManagedFileType {
            FILE, ICON, BRANDING, VIDEO
        }

        public Status status;
        public string id;
        public string path;
        public string name;
        public int progress;
        public long totalBytesDownloaded;
        public long totalDownloadSize;
        public Timestamp timestamp = new Timestamp();
        public string message;
        public int errorCode;
        public ManagedFileType managedFileType;
    }

    [System.Serializable]
    public class ForegroundApp {
        public string packageName;
        public string className;
    }

    [System.Serializable]
    public class ForegroundAppForShortcutMenu : ForegroundApp {
        /// <summary>
        /// The last time this object was updated. This represents the time that this foreground
        /// app was first detected in the foreground.
        /// </summary>
        public Timestamp lastUpdated = new Timestamp();
    }

    /// <summary>
    /// Represents the status of a <see cref="RuntimeApp"/> on the device
    /// </summary>
    [System.Serializable]
    public class AppInstallStatus {
        public enum Status {
            QUEUED,
            SETUP,
            DOWNLOADING,
            PATCHING,
            READY_TO_INSTALL,
            INSTALLING,
            CLEANUP,
            COMPLETE,
            ERROR,
            NO_STATUS
        }
        public enum InstallMethod {
            PATCH_INSTALL,
            FULL_INSTALL,
            FORCE_INSTALL
        }

        public Status status = Status.NO_STATUS;
        public InstallMethod installMethod;
        public string packageName;
        public long progress;
        public long totalBytesDownloaded;
        public long totalDownloadSize;
        public long currentVersion;
        public string currentVersionName;
        public long nextVersion;
        public string nextVersionName;
        public int errorCode;
        public string message;
        public Timestamp timestamp = new Timestamp();

        /// <summary>
        /// Whether we're currently in an update phase
        /// </summary>
        /// <returns></returns>
        public bool IsUpdating() {
            return status == Status.SETUP ||
              status == Status.DOWNLOADING ||
              status == Status.PATCHING ||
              status == Status.READY_TO_INSTALL ||
              status == Status.INSTALLING ||
              status == Status.CLEANUP;
        }

        public bool UpdateIsQueued() {
            return status == Status.QUEUED;
        }

        public bool IsNotUpdating() {
            return status == Status.NO_STATUS || status == Status.COMPLETE || status == Status.ERROR;
        }

        public bool HasError() {
            return status == Status.ERROR;
        }
    }

    [System.Serializable]
    public class DeviceSystemVersionInstallStatus {
        public enum Status {
            UP_TO_DATE,
            DOWNLOADING,
            READY_TO_INSTALL,
            ERROR
        }

        public string currentVersion;
        public string nextVersion;
        public Status status;
        public long progress;
        public long totalBytesDownloaded;
        public long totalDownloadSize;
        public int errorCode;
        public string message;
        public Timestamp timestamp = new Timestamp();
    }

    [System.Serializable]
    public class Timestamp {
        public long nanoseconds;
        public long seconds;
    }
}