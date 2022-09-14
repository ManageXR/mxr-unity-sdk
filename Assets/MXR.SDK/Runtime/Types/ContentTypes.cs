using System.Collections.Generic;

namespace MXR.SDK {
    [System.Serializable]
    public class Content {
        public string id;
        public string title;
        public List<string> categories = new List<string>();
        public string description;
        public bool hidden;
        public string iconPath;
        public string iconUrl;
        public int sortOrder;
        public string organizationId;
    }

    [System.Serializable]
    public class WebXRApp : Content {
        public string url;
    }

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
            None,
            Equirectangular,
            Cubemap,
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

    [System.Serializable]
    public class RuntimeApp : Content {
        public Dictionary<string, object> launchParams = new Dictionary<string, object>();
        public bool forceInstall;
        public int versionCode;
        public string versionName;
        public string packageName;
        public string className;
        public bool debug_isNotInstalled;
    }
}