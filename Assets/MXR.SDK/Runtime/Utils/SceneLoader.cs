using System;
using System.IO;
using System.Collections.Generic;

using Cysharp.Threading.Tasks;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace MXR.SDK {
    /// <summary>
    /// Loads an mxrus file. Provides loading states and access to internal AssetBundles
    /// </summary>
    public class SceneLoader : MonoBehaviour {
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
            /// The instance failed to load an mxrus file. <see cref="bundles"/> is empty.
            /// </summary>
            Error,

            /// <summary>
            /// The instance has successfully load an mxrus file and all asset bundles are available.
            /// </summary>
            Success
        }

        static string defaultExtractsLocation = null;
        /// <summary>
        /// The global folder where .mxrus files will be extracted by default
        /// </summary>
        public static string DefaultExtractsLocation { 
            get {
                if (!string.IsNullOrEmpty(defaultExtractsLocation))
                    return defaultExtractsLocation;
#if UNITY_EDITOR
                defaultExtractsLocation = Application.dataPath.Replace("Assets", "Temp");
#else
                defaultExtractsLocation = Application.persistentDataPath;
#endif
                return defaultExtractsLocation;
            }
            set {
                if (string.IsNullOrEmpty(value))
                    throw new Exception("Cannot set DefaultExtractsLocation to null/empty");
                defaultExtractsLocation = value;
            }
        }

        /// <summary>
        /// The folder where this instance will extract .mxrus files to. Use this to
        /// customization extracts container directories.
        /// </summary>
        public string ExtractLocation { get; private set; }

        /// <summary>
        /// The current state of this instance
        /// </summary>
        public State CurrState { get; private set; }

        /// <summary>
        /// The path to the mxrus file this instance is loading/has failed to load from/has successfully loaded from
        /// Get assigned on invoking <see cref="Load"/>
        /// </summary>
        public string SourceFilePath { get; private set; }

        /// <summary>
        /// Returns whether the mxrus file has a scene that can be loaded
        /// </summary>
        public bool HasScene => !string.IsNullOrEmpty(ScenePath);

        /// <summary>
        /// Gets the path to the scene inside the <see cref="SCENE_ASSETBUNDLE_NAME"/> AssetBundle in the mxrus file
        /// </summary>
        public string ScenePath {
            get {
                if (!bundles.ContainsKey(SCENE_ASSETBUNDLE_NAME)) {
                    Debug.unityLogger.Log(LogType.Error, TAG, "scene asset bundle not loaded");
                    return null;
                }

                var sceneBundle = bundles[SCENE_ASSETBUNDLE_NAME];
                if (sceneBundle.GetAllScenePaths().Length == 0) {
                    Debug.unityLogger.Log(LogType.Error, TAG, "There are no scenes in scene bundle");
                    return null;
                }

                return sceneBundle.GetAllScenePaths()[0];
            }
        }

        /// <summary>
        /// The AssetBundle containing the scene assets. Just loading <see cref="Scene"/>
        /// should be enough to create the scene at runtime, but this can be used to get access
        /// to the individual assets used in the mxrus file.
        /// </summary>
        public AssetBundle Assets =>
            bundles.ContainsKey(ASSETS_ASSETBUNDLE_NAME) ? bundles[ASSETS_ASSETBUNDLE_NAME] : null;

        readonly Dictionary<string, AssetBundle> bundles = new Dictionary<string, AssetBundle>();

        string FileNameWithoutExt => Path.GetFileNameWithoutExtension(SourceFilePath);

        string ExtractDirName => FileNameWithoutExt + TEMP_EXTRACT_DIRNAME_POSTFIX;

        string ExtractDirPath => Path.Combine(ExtractLocation, ExtractDirName);

        [Obsolete("new keyword is not supported on MonoBehaviour. Use static SceneLoader.New method instead", true)]
        public SceneLoader() { }

        /// <summary>
        /// Creates a new instance 
        /// </summary>
        /// <param name="sourceFilePath">The fully resolved path to the mxrus file to load from</param>
        /// <param name="extractDir">
        /// The directory where the .mxrus file is temporarily extracted to
        /// Leave this null or empty to use <see cref="DefaultExtractsLocation"/>
        /// </param>
        /// <returns></returns>
        public static SceneLoader New() {
            var go = new GameObject("SceneLoader");
            DontDestroyOnLoad(go);
            var instance = go.AddComponent<SceneLoader>();
            instance.CurrState = State.Idle;

            return instance;
        }

        /// <summary>
        /// Asynchronously loads an mxrus file
        /// </summary>
        /// <returns></returns>
        public async UniTask Load(string sourceFilePath, string extractLocation = null) {
            UnloadBundles();
            CurrState = State.Loading;

            // Initialize paths and ensure extract location directory
            Debug.unityLogger.Log(LogType.Log, TAG, $"Loading {sourceFilePath}");
            SourceFilePath = sourceFilePath;
            ExtractLocation = string.IsNullOrEmpty(extractLocation) ? DefaultExtractsLocation : extractLocation;

            if (!Directory.Exists(ExtractLocation))
                Directory.CreateDirectory(ExtractLocation);

            // Extract the file to destination path
            Debug.unityLogger.Log(LogType.Log, TAG, $"Extracting {sourceFilePath} to {ExtractDirPath}");
            ZipUtils.ExtractZipFile(SourceFilePath, ExtractDirPath);

            // Attempt to load the bundles from the extract directory
            var bundleNames = new string[] { ASSETS_ASSETBUNDLE_NAME, SCENE_ASSETBUNDLE_NAME, FileNameWithoutExt };
            Debug.unityLogger.Log(LogType.Log, TAG, $"Attempting to load the following asset bundles: {string.Join(", ", bundleNames)}");

            List<string> failedBundleNames = new List<string>();
            foreach (var bundleName in bundleNames) {
                try {
                    var loadedBundle = await LoadAssetBundleAsync(bundleName);
                    bundles.Add(bundleName, loadedBundle);
                    Debug.unityLogger.Log(LogType.Log, TAG, $"Added {bundleName} to Bundles Dictionary");
                }
                catch {
                    failedBundleNames.Add(bundleName);
                }
            }

            // If any bundles have failed to load, the entire load operation is undone
            // and the instance gets into the error state
            DeleteExtractDir();
            if (failedBundleNames.Count == 0)
                CurrState = State.Success;
            else {
                UnloadBundles();
                CurrState = State.Error;
                var msg = $"Failed to load the following asset bundles: {string.Join(", ", failedBundleNames)}";
                Debug.unityLogger.Log(LogType.Error, TAG, msg);
                throw new Exception(msg);
            }
        }

        /// <summary>
        /// Unloads any mxrus file and its AssetBundles that may have been loaded previously
        /// </summary>
        public void Unload() {
            UnloadBundles();
            CurrState = State.Idle;
        }

        void UnloadBundles() {
            foreach (var pair in bundles)
                pair.Value.Unload(true);
            bundles.Clear();
        }

        void DeleteExtractDir() {
            if (Directory.Exists(ExtractDirPath))
                Directory.Delete(ExtractDirPath, recursive: true);
        }

        async UniTask<AssetBundle> LoadAssetBundleAsync(string name) {
            var path = Path.Combine(ExtractDirPath, name);

            if (!File.Exists(path)) {
                Debug.LogError($"File does not exist at path: {path}");
                throw new FileNotFoundException(path);
            }

            Debug.unityLogger.Log(LogType.Log, TAG, "Loading asset bundle at " + path);

            var loadRequest = AssetBundle.LoadFromFileAsync(path);
            await loadRequest;
            if (loadRequest.assetBundle != null) {
                Debug.unityLogger.Log(LogType.Log, TAG, $"AssetBundle {name} loaded successfully.");
                return loadRequest.assetBundle;
            }
            else {
                Debug.unityLogger.Log(LogType.Error, TAG, "Failed to load AssetBundle " + name);
                throw new Exception("Failed to load AssetBundle " + name);
            }
        }
    }
}