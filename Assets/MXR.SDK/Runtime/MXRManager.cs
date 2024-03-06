using System;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;

namespace MXR.SDK {
    /// <summary>
    /// A helper for MXR SDK integration. This class initializes and 
    /// exposes an <see cref="IMXRSystem"/> as well as provides
    /// an API to get/set/update the HomeScreenState.
    /// </summary>
    public static class MXRManager {
        const string TAG = "[MXRManager]";

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

        [Obsolete("This event isn't reliable for knowing when the SDK is actually initialized. " +
        "It may be removed in future versions and is used with the Init method which is being deprecated. " +
        "Use InitAsync instead.", false)]
        /// <summary>
        /// Fired when <see cref="MXRManager"/> is initialized
        /// </summary>
        public static event Action OnInitialize;

        [Obsolete("This boolean property isn't a reliable source for knowing is the SDK is actually" +
        "initialized. It may be removed in future versions and is used with the Init method " +
        "which is being deprecated. Use InitAsync instead.", false)]
        /// <summary>
        /// Whether <see cref="MXRManager"/> has been initialized
        /// </summary>
        public static bool IsInitialized { get; private set; } = false;

        /// <summary>
        /// Auto-initializes <see cref="System"/> with appropriate implementation
        /// based on the runtime environment and awaits until the system is available 
        /// for the first time.
        /// </summary>
        /// <param name="system"></param>
        /// <returns>The <see cref="IMXRSystem"/> instance in MXRManager</returns>
        async public static Task<IMXRSystem> InitAsync(IMXRSystem system = null) {
            // We switch off the CS0618 warning code here because we're using the Init 
            // method that has been deprecated. Otherwise we'd be printing the warning
            // to the console.
            // We restore it immediately after calling it so that only the warning
            // for this line is ignored.
#pragma warning disable 0618
            var result = Init(system);
#pragma warning restore 0618
            if (result == null)
                return null;

            // InitAsync waits for the system to become available
            if (!System.IsConnectedToAdminApp)
                Debug.unityLogger.Log(LogType.Log, TAG, "Waiting for MXRManager.System to be available.");

            bool isBoundToAdmin = await CanBindToAdminApp();

            // Next we wait for the DeviceStatus and RuntimeSettingsSummary to become non null.
            if (System.DeviceStatus == null)
                Debug.unityLogger.Log(LogType.Log, TAG, "Waiting for MXRManager.System.DeviceStatus to be initialized.");

            if (System.RuntimeSettingsSummary == null)
                Debug.unityLogger.Log(LogType.Log, TAG, "Waiting for MXRManager.System.RuntimeSettingsSummary to be initialized.");

            while (System.DeviceStatus == null || System.RuntimeSettingsSummary == null) {
                Debug.unityLogger.Log("Waiting for DeviceStatus and RuntimeSettingsSummary");
                await Task.Delay(100);
            }
            
            if(isBoundToAdmin == false) {
                isBoundToAdmin = await CanBindToAdminApp();
                if (!isBoundToAdmin) {
                    Debug.unityLogger.LogError(TAG, "Failed to bind to Admin App after two attempts.");
                }
            }

            Debug.unityLogger.Log(LogType.Log, TAG, "MXRManager finished initializing.");
            return result;
        }


        /// <summary>
        /// Attempts to establish a connection to the Admin App within a specified timeout period.
        /// </summary>
        /// <returns>
        /// A Task that represents the asynchronous operation. This Task returns true if the connection
        /// to the Admin App is successfully established within the timeout period, and false if the
        /// operation times out.
        /// </summary>
        private async static Task<bool> CanBindToAdminApp() 
        {
            TimeSpan timeout = TimeSpan.FromSeconds(3);

            using (var cancellationTokenSource = new CancellationTokenSource(timeout)) {
                try {
                    while (!System.IsConnectedToAdminApp) {
                        Debug.unityLogger.Log(LogType.Log, TAG, "Waiting to connect to Admin App");

                        await Task.Delay(100, cancellationTokenSource.Token);
                    }
                    return true;
                } catch (TaskCanceledException) {

                    Debug.unityLogger.LogError(TAG, "Connection to Admin App timed out.");
                    return false;
                }
            }
        }



        [Obsolete("This method is being deprecated and may be removed or made private in future versions." +
        "Use \"await InitAsync\" instead.", false)]
        /// <summary>
        /// Auto-Initializes <see cref="System"/> with an appropriate
        /// implementation based on the runtime environment
        /// </summary>
        /// <param name="system">Implementation to force, optional</param>
        public static IMXRSystem Init(IMXRSystem system = null) {
            if (IsInitialized) {
                Debug.unityLogger.Log(TAG, "MXRManager has already been initialized. Returning MXRManager.System");
                return System;
            }

            // If the method user isn't overriding the system implementation, 
            // create a suitable implementation instance and use that.
            if (system == null) {
#if UNITY_EDITOR
                System = MXREditorSystem.New();
#elif UNITY_ANDROID
                System = new MXRAndroidSystem();
#else
                Debug.unityLogger.Log(LogType.Error, TAG, "MXRManager can only initialize in the Unity Editor and Android runtime.");
                return null;
#endif
            }
            else {
#if UNITY_EDITOR
                if (system is MXREditorSystem)
                    System = system;
                else
                    Debug.unityLogger.LogError(TAG, "Must initialize MXRManager with MXREditorSystem when in the Unity Editor.");
#elif UNITY_ANDROID
                if (system is MXRAndroidSystem)
                    System = system;
                else
                    Debug.unityLogger.LogError(TAG, "Must initialize MXRManager with MXRAndroidSystem when on an Android device.");
#else
                Debug.unityLogger.LogError(TAG, "Must initialize MXRManager with MXREditorSystem when in the Unity Editor " +
                    "or with MXRAndroidSystem when on an Android device.");
                return null;
#endif
            }

            HomeScreenState = new HomeScreenState {
                view = HomeScreenView.LIBRARY,
                data = new HomeScreenData()
            };

            // MXRManager automatically handles homescreen state requests
            // from the system
            System.OnHomeScreenStateRequest += () =>
                System.SendHomeScreenState(HomeScreenState);

            IsInitialized = true;
            OnInitialize?.Invoke();
            return System;
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
