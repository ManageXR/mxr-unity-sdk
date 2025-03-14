using System.IO;
using System.Linq;
using System.Text;

using UnityEditor;
using UnityEngine;

namespace MXR.SDK.Editor {
    public static class SceneExporter {
        const string TAG = "SceneExporter";
        const string ASSETS_ASSETBUNDLE_NAME = "assets";
        const string SCENE_ASSETBUNDLE_NAME = "scene";

        /// <summary>
        /// Exports a scene into a zip arachive containing three asset bundles and their manifests:
        /// - "assets" containing the asset dependencies such as textures, models, materials, shaders
        /// - "scene" containing the scene
        /// - a third named after the output path with general metadata
        /// </summary>
        /// <param name="scenePath">The fully resolved path to the scene in the project</param>
        /// <param name="outputFilePath">The fully resolved path to where the output archive will be saved</param>
        /// <param name="buildTarget">The target platform to build for</param>
        /// <param name="deleteExportDir">Whether the intermediate directory containing the asset bundles and manifests should be deleted after the export.</param>
        public static void ExportScene(string scenePath, string outputFilePath, BuildTarget buildTarget = BuildTarget.Android, bool deleteExportDir = true) {
            // Get all the dependencies of the scene
            Debug.unityLogger.Log(LogType.Log, TAG, "Getting dependencies of scene at " + scenePath);
            var dependencies = AssetDatabase.GetDependencies(new string[] { scenePath });
            if(dependencies.Count() > 0) {
                StringBuilder sb = new StringBuilder();
                sb.Append("Dependency list:");
                foreach (var dependency in dependencies)
                    sb.Append(dependency).Append("\n");
                Debug.unityLogger.Log(LogType.Log, TAG, sb.ToString());
            }

            // Identify the scenes and create an asset bundle build object
            var scenes = dependencies.Where(x => x.EndsWith(".unity")).ToArray();
            AssetBundleBuild sceneBuild = new AssetBundleBuild();
            sceneBuild.assetBundleName = SCENE_ASSETBUNDLE_NAME;
            sceneBuild.assetNames = scenes;

            // Identify the assets and create an asset bundle build object
            var assets = dependencies.Where(x => !x.EndsWith(".unity")).ToArray();
            AssetBundleBuild assetsBuild = new AssetBundleBuild();
            assetsBuild.assetBundleName = ASSETS_ASSETBUNDLE_NAME;
            assetsBuild.assetNames = assets;

            // Create a export directory for the asset bundle export.
            // If the mxrus file destination is /exports/myscene.mxrus
            // the temporary directory would be /exports/myscene/
            var outputDir = Path.GetDirectoryName(outputFilePath);
            var exportDirName = Path.GetFileNameWithoutExtension(outputFilePath);
            var exportDir = Path.Combine(outputDir, exportDirName);

            if (Directory.Exists(exportDir))
                Directory.Delete(exportDir, recursive: true);
            Directory.CreateDirectory(exportDir);

            // Build and export the asset bundles to the export directory
            var manifest = BuildPipeline.BuildAssetBundles(
                exportDir,
                new AssetBundleBuild[] { sceneBuild, assetsBuild },
                BuildAssetBundleOptions.None,
                buildTarget
            );


            if (manifest == null) {
                Debug.unityLogger.Log(LogType.Error, TAG, "Asset bundle build failed");
                return;
            }
            
            // Compress the export directory to a .mxrus file and delete the export 
            // directory if required.
            ZipUtils.CompressDirectory(exportDir, outputFilePath);
            if (deleteExportDir)
                Directory.Delete(exportDir, recursive: true);
            else
                Debug.unityLogger.Log(LogType.Log, TAG, "Exported asset bundles and manifests to " + exportDir);

            Debug.unityLogger.Log(LogType.Log, TAG, "Exported .mxrus to " + outputFilePath);
        }
    }
}