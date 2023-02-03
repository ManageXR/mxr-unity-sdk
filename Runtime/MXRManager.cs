using System;

namespace MXR.SDK {
    /// <summary>
    /// A helper for MXR SDK integration. This class initializes and 
    /// exposes an <see cref="IMXRSystem"/> as well as provides
    /// an API to get/set/update the HomeScreenState.
    /// </summary>
    public static class MXRManager {
        /// <summary>
        /// The <see cref="IMXRSystem"/> implementation being used by the SDK
        /// </summary>
        public static IMXRSystem System { get; private set; }

        /// <summary>
        /// The current <see cref="HomeScreenState"/>
        /// NOTE: If modifying the internal fields of this object
        /// be sure to call <see cref="IMXRSystem.SendHomeScreenState(HomeScreenState)"/>
        /// after it. Otherwise, use <see cref="MXRManager.ModifyHomeScreenState(Action{HomeScreenState})"/>
        /// which will automatically do that for you.
        /// instead
        /// </summary>
        public static HomeScreenState HomeScreenState { get; private set; }

        /// <summary>
        /// Fired when <see cref="MXRManager"/> is initialized
        /// </summary>
        public static event Action OnInitialize;

        /// <summary>
        /// Whether <see cref="MXRManager"/> has been initialized
        /// </summary>
        public static bool IsInitialized { get; private set; } = false;

        /// <summary>
        /// Auto-Initializes <see cref="System"/> with an appropriate
        /// implementation based on the runtime environment
        /// </summary>
        /// <param name="system">Implementation to force, optional</param>
        public static void Init(IMXRSystem system = null) {
            if (IsInitialized)
                return;

            HomeScreenState = new HomeScreenState {
                view = HomeScreenView.LIBRARY,
                viewDetails = new HomeScreenViewDetails()
            };

            if (system == null) {
#if UNITY_EDITOR
                System = MXREditorSystem.New();
#elif UNITY_ANDROID
                System = new MXRAndroidSystem();
#endif
            }
            else
                System = system;

            // MXRManager automatically handles homescreen state requests
            // from the system
            System.OnHomeScreenStateRequest += () =>
                System.SendHomeScreenState(HomeScreenState);

            IsInitialized = true;
            OnInitialize?.Invoke();
        }

        /// <summary>
        /// Sets a new object as the home screen state
        /// </summary>
        /// <param name="newState"></param>
        public static void SetHomeScreenState(HomeScreenState newState) {
            HomeScreenState = newState;
            System.SendHomeScreenState(HomeScreenState);
        }

        /// <summary>
        /// Provides access to <see cref="HomeScreenState"/> for modification
        /// </summary>
        /// <param name="modification">Method for modification</param>
        public static void ModifyHomeScreenState(Action<HomeScreenState> modification) {
            modification(HomeScreenState);
            System.SendHomeScreenState(HomeScreenState);
        }
    }
}
