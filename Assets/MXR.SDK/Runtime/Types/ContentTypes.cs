using System.Collections.Generic;
using Newtonsoft.Json;

namespace MXR.SDK {
    /// <summary>
    /// Base class for different kinds of deployable content supported
    /// </summary>
    [System.Serializable]
    public class Content {
        /// <summary>
        /// Represents a requirement for the content
        /// </summary>
        public enum Requirement {
            /// <summary>
            /// Represents a requirement that is undefined
            /// </summary>
            UNDEFINED,

            /// <summary>
            /// Represents a requirement that is optional (or suggested)
            /// </summary>
            OPTIONAL,

            /// <summary>
            /// Represents a requirement that is mandatory
            /// </summary>
            MANDATORY
        }

        /// <summary>
        /// Unique identifier of the content
        /// </summary>
        public string id;

        /// <summary>
        /// Title of the content
        /// </summary>
        public string title;

        /// <summary>
        /// Categories associated with the content
        /// </summary>
        public List<string> categories = new List<string>();

        /// <summary>
        /// Description of the content
        /// </summary>
        public string description;

        /// <summary>
        /// Whether the content has been configured to be hidden
        /// on the ManageXR web dashboard. To be used by the
        /// home screen implementing the SDK
        /// </summary>
        public bool hidden;

        /// <summary>
        /// Local path of the icon. This may or may not exist.
        /// Admin app downloads the icons to local storage.
        /// Begins with /MightyImmersion. <see cref="MXRStorage.GetFullPath(string)"/>
        /// can be used to get the full path
        /// </summary>
        public string iconPath;

        /// <summary>
        /// URL of the remotely stored icon file
        /// </summary>
        public string iconUrl;

        /// <summary>
        /// The sort order configured on the ManageXR web dashboard for this content.
        /// To be used by the home screen implementing the SDK
        /// </summary>
        public int sortOrder;

        /// <summary>
        /// ID of the ManageXR organization this device is set up for
        /// </summary>
        public string organizationId;

        /// <summary>
        /// Whether this content has been configured to not launch when it's
        /// being updated. To be used by the home screen implementing the SDK
        /// </summary>
        public bool blockLaunchWhileUpdating = false;

        /// <summary>
        /// Internet requirements of this content. 
        /// Values: "mandatory", "optional". May be null or empty.
        /// Use this to show appropriate notice to the user when content is launched
        /// </summary>
        public Requirement internetRequired = Requirement.UNDEFINED;

        /// <summary>
        /// Helper property to check if <see cref="internetRequired"/> is "mandatory"
        /// </summary>
        [JsonIgnore] public bool InternetMandatory => internetRequired == Requirement.MANDATORY;

        /// <summary>
        /// Helper property to check if <see cref="internetRequired"/> is "optional"
        /// </summary>
        [JsonIgnore] public bool InternetOptional => internetRequired == Requirement.OPTIONAL;

        /// <summary>
        /// Helper property to check if <see cref="internetRequired"/> is null or empty
        /// </summary>
        [JsonIgnore] public bool InternetUndefined => internetRequired == Requirement.UNDEFINED;

        /// <summary>
        /// Controller requirements for this content.
        /// Values: "mandatory", "optional". May be null or empty.
        /// Use this to show appropriate notice to the user when content is launched
        /// </summary>
        public Requirement controllersRequired = Requirement.UNDEFINED;

        /// <summary>
        /// Helper property to check if <see cref="controllersRequired"/> is "mandatory"
        /// </summary>
        [JsonIgnore] public bool ControllersMandatory => controllersRequired == Requirement.MANDATORY;

        /// <summary>
        /// Helper property to check if <see cref="controllersRequired"/> is "optional"
        /// </summary>
        [JsonIgnore] public bool ControllersOptional => controllersRequired == Requirement.OPTIONAL;

        /// <summary>
        /// Helper property to check if <see cref="controllersRequired"/> is null or empty
        /// </summary>
        [JsonIgnore] public bool ControllersUndefined => controllersRequired == Requirement.UNDEFINED;
    }

    /// <summary>
    /// WebXR, Website content
    /// </summary>
    [System.Serializable]
    public class WebXRApp : Content {
        /// <summary>
        /// The URL where the content is hosted
        /// </summary>
        public string url;
    }

    /// <summary>
    /// Video content that has been deployed on the ManageXR web dashboard
    /// </summary>
    [System.Serializable]
    public class Video : Content {
        public VideoType type;
        public VideoDisplay display;
        public VideoMapping mapping;
        public VideoPacking packing;

        // local path to video file (it may or may not exist)
        public string videoPath;

        public enum VideoType {
            _360,
            _180,
            _2D,
        }

        public enum VideoMapping {
            NONE,
            EQUIRECTANGULAR,
            CUBEMAP,
        }

        public enum VideoDisplay {
            MONO,
            STEREO,
        }

        public enum VideoPacking {
            NONE,
            TOP_BOTTOM,
            LEFT_RIGHT,
        }
    }

    /// <summary>
    /// Android app that has been deployed via the ManageXR web dashboard
    /// </summary>
    [System.Serializable]
    public class RuntimeApp : Content {
        /// <summary>
        /// Parameters to be used when the app is launched.
        /// To be used by the home screen implementing the SDK
        /// </summary>
        public Dictionary<string, object> launchParams = new Dictionary<string, object>();

        /// <summary>
        /// Whether the app will be forcibly installed by the ManageXR Admin App
        /// </summary>
        public bool forceInstall;

        /// <summary>
        /// Version code of the application 
        /// </summary>
        public int versionCode;

        /// <summary>
        /// Version name of the application 
        /// </summary>
        public string versionName;

        /// <summary>
        /// Package name of the application
        /// </summary>
        public string packageName;

        /// <summary>
        /// Indicates whether the application is currently in an expired state.
        /// This occurs in shared applications when they reach the end of their shared time limit.
        /// </summary>
        public bool isExpired;

        /// <summary>
        /// Class name of the application
        /// </summary>
        public string className;

        public bool debug_isNotInstalled;

        /// <summary>
        /// The expiration behavior of a shared app
        /// An apps expiration behavior can be NONE,  DISABLE_APP and DELETED_APP
        /// </summary>
        public ExpirationBehavior expirationBehavior;

        /// <summary>
        /// The date, in MS, in which a shared app has expired 
        /// </summary>
        public long expirationTimestamp;
    }
}