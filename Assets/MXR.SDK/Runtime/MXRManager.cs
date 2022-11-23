using System;

#if UNITY_EDITOR
using MXR.SDK.Editor;
#endif

namespace MXR.SDK {
    /// <summary>
    /// The helper for MXR operations. This class initializes and 
    /// exposes an <see cref="IMXRSystem"/> as well as provides
    /// an API to get/set/update the HomeScreenState. 
    /// </summary>
    public class MXRManager {
        public static IMXRSystem System { get; private set; }
        public static HomeScreenState HomeScreenState { get; private set; }

        public static event Action OnInitialize;
        public static bool IsInitialized { get; private set; } = false;

        /// <summary>
        /// Initializes <see cref="System"/> with an appropriate
        /// implementation based on the runtime environment
        /// </summary>
        public static void Init(IMXRSystem system = null) {
            if (IsInitialized)
                return;
            IsInitialized = true;

            HomeScreenState = new HomeScreenState();

            if (system == null) {
#if UNITY_EDITOR
                System = MXREditorSystem.New();
                MXRCommandSimulator.SetSystem(System);
#elif UNITY_ANDROID
                System = new MXRAndroidSystem();
#endif
            }
            else
                System = system;

            System.OnHomeScreenStateRequest += () =>
                System.SendHomeScreenState(HomeScreenState);
        }

        public static void SetHomeScreenState(HomeScreenState newState) {
            HomeScreenState = newState;
            System.SendHomeScreenState(HomeScreenState);
        }

        public static void ModifyHomeScreenState(Action<HomeScreenState> modification) {
            modification(HomeScreenState);
            System.SendHomeScreenState(HomeScreenState);
        }
    }
}
