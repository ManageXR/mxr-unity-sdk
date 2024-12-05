using System.Collections.Generic;
using System.IO;

using Cysharp.Threading.Tasks;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace MXR.SDK {
    /// <summary>
    /// Loads an mxrus file. Provides loading states and access to internal AssetBundles
    /// </summary>
    public class SceneLoader : ISceneLoader {
        const string TAG = "SceneLoader";
        const string ASSETS_ASSETBUNDLE_NAME = "assets";
        const string SCENE_ASSETBUNDLE_NAME = "scene";
        const string TEMP_EXTRACT_DIRNAME_POSTFIX = "-extract";

        /// <summary>
        /// The different states the loader can be in
        /// </summary>
        public enum State {
            /// <summary>
            /// The instance is awaiting load operation.
            /// This is the state the instance starts in.
            /// On invoking <see cref="Unload"/>, the instance resets back to this state.
            /// </summary>
            Idle,

            /// <summary>
            /// The instance is currently loading an mxrus file.
            /// </summary>
            Loading,

            /// <summary>
            /// The instance failed to load an mxrus file. <see cref="_bundles"/> is empty.
            /// </summary>
            Error,

            /// <summary>
            /// The instance has successfully load an mxrus file and all asset bundles are available.
            /// </summary>
            Success
        }

        /// <summary>
        /// The global folder where .mxrus files will be extracted by default
        /// </summary>
        public static string GlobalExtractsLocation { get; set; }

        /// <summary>
        /// The folder where this instance will extract .mxrus files to by default.
        /// In the editor this is the Temp/ directory of the Unity Project
        /// In a build it is the persistent data path
        /// </summary>
        public string ExtractLocation { get; private set; }
             = Application.isEditor ? Application.dataPath.Replace("Assets", "Temp") : Application.persistentDataPath;

        /// <summary>
        /// The current state of this instance
        /// </summary>
        public State CurrentState { get; private set; } = State.Idle;

        /// <summary>
        /// The path to the mxrus file this instance is loading/has failed to load from/has successfully loaded from
        /// Get assigned on invoking <see cref="Load"/>
        /// </summary>
        public string SourceFilePath { get; private set; }

        /// <summary>
        /// Gets the scene inside the <see cref="SCENE_ASSETBUNDLE_NAME"/> AssetBundle in the mxrus file
        /// </summary>
        public Scene? Scene {
            get {
                if (!_bundles.ContainsKey(SCENE_ASSETBUNDLE_NAME)) {
                    Debug.unityLogger.Log(LogType.Error, TAG, "scene asset bundle not loaded");
                    return null;
                }

                var sceneBundle = _bundles[SCENE_ASSETBUNDLE_NAME];
                if (sceneBundle.GetAllScenePaths().Length == 0) {
                    Debug.unityLogger.Log(LogType.Error, TAG, "There are no scenes in scene bundle");
                    return null;
                }
                var path = sceneBundle.GetAllScenePaths()[0];
                return SceneManager.GetSceneByPath(path);
            }
        }

        /// <summary>
        /// The AssetBundle containing the scene assets. Just loading <see cref="Scene"/>
        /// should be enough to create the scene at runtime, but this can be used to get access
        /// to the individual assets used in the mxrus file.
        /// </summary>
        public AssetBundle Assets =>
            _bundles.ContainsKey(ASSETS_ASSETBUNDLE_NAME) ? _bundles[ASSETS_ASSETBUNDLE_NAME] : null;

        private readonly Dictionary<string, AssetBundle> _bundles = new Dictionary<string, AssetBundle>();

        /// <summary>
        /// Asynchronously loads an mxrus file
        /// </summary>
        /// <returns></returns>
        public async UniTask<bool> Load(string sourceFilePath, string extractLocation = null) {
            ExtractLocation = string.IsNullOrEmpty(extractLocation) ? GlobalExtractsLocation : extractLocation;
            if (!Directory.Exists(ExtractLocation))
                Directory.CreateDirectory(ExtractLocation);

            UnloadBundles();
            CurrentState = State.Loading;

            Debug.unityLogger.Log(LogType.Log, TAG, $"Loading {sourceFilePath}");

            // Initialize paths and ensure extract location directory
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(sourceFilePath);
            string extractDirName = fileNameWithoutExt + TEMP_EXTRACT_DIRNAME_POSTFIX;
            string extractDirPath = Path.Combine(ExtractLocation, extractDirName);

            SourceFilePath = sourceFilePath;

            // Extract the file to destination path
            Debug.unityLogger.Log(LogType.Log, TAG, $"Extracting {sourceFilePath} to {extractDirPath}");
            ICompressionUtility compressionUtility = new SharpZipLibCompressionUtility();
            compressionUtility.ExtractToDirectory(sourceFilePath, extractDirPath);

            // Attempt to load the bundles from the extract directory
            var bundleNames = new string[] { ASSETS_ASSETBUNDLE_NAME, SCENE_ASSETBUNDLE_NAME, fileNameWithoutExt };
            Debug.unityLogger.Log(LogType.Log, TAG, $"Attempting to load the following asset bundles: {string.Join(", ", bundleNames)}");

            List<string> failedBundleNames = new List<string>();
            foreach (var bundleName in bundleNames) {
                try {
                    var loadedBundle = await LoadAssetBundleAsync(Path.Combine(extractDirPath, bundleName));
                    _bundles.Add(bundleName, loadedBundle);
                    Debug.unityLogger.Log(LogType.Log, TAG, $"Added {bundleName} to Bundles Dictionary");
                }
                catch {
                    failedBundleNames.Add(bundleName);
                    Debug.unityLogger.Log(LogType.Error, TAG, $"Failed to load AssetBundle {bundleName}");
                }
            }

            // Regardless of whether any bundles failed to load, always delete the extract directory
            if (Directory.Exists(extractDirPath)) {
                Directory.Delete(extractDirPath, recursive: true);
            }

            if (failedBundleNames.Count == 0) {
                CurrentState = State.Success;
                return true;
            }
            else {
                UnloadBundles();
                CurrentState = State.Error;
                var msg = $"Failed to load the following asset bundles: {string.Join(", ", failedBundleNames)}";
                Debug.unityLogger.Log(LogType.Error, TAG, msg);
                return false;
            }
        }

        /// <summary>
        /// Unloads any mxrus file and its AssetBundles that may have been loaded previously
        /// </summary>
        public void Unload() {
            UnloadBundles();
            CurrentState = State.Idle;
        }

        private void UnloadBundles() {
            foreach (var pair in _bundles) {
                pair.Value.Unload(true);
            }
            _bundles.Clear();
        }

        private async UniTask<AssetBundle> LoadAssetBundleAsync(string path) {
            var loadRequest = AssetBundle.LoadFromFileAsync(path);
            loadRequest.allowSceneActivation = false;

            var assetBundle = await loadRequest;

            if (assetBundle != null) {
                return assetBundle;
            }
            else {
                return null;
            }
        }
    }
}
