using UnityEngine;

namespace MXR.SDK.Samples {
    /// <summary>
    /// A utility script that provides access to a <see cref="IMXRSystem"/>
    /// that is of <see cref="MXREditorSystem"/> in the Unity Editor
    /// and <see cref="MXRAndroidSystem"/> on Android devices
    /// </summary>
    public static class MXRManager {
        public static IMXRSystem System { get; private set; }

        static bool isInited = false;

        /// <summary>
        /// Initializes <see cref="System"/>
        /// </summary>
        public static void Init() {
            if (isInited)
                return;
            isInited = true;

            if (Application.isEditor)
                System = MXREditorSystem.New();
            else if (Application.platform == RuntimePlatform.Android)
                System = new MXRAndroidSystem();
        }
    }
}
