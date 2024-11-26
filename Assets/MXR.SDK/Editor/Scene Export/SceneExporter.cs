using System.IO;
using System.Linq;
using System.Text;

using UnityEditor;
using UnityEngine;

namespace MXR.SDK.Editor {
    public static class SceneExporter {
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
            // We get all the dependencies of the scene except scripts
            Debug.Log("Getting dependencies of scene at " + scenePath);
            var dependencies = AssetDatabase.GetDependencies(new string[] { scenePath });
            if(dependencies.Count() > 0) {
                StringBuilder sb = new StringBuilder();
                sb.Append("Dependency list:");
                foreach (var dependency in dependencies)
                    sb.Append(dependency).Append("\n");
                Debug.Log(sb.ToString());
            }

            var scenes = dependencies.Where(x => x.EndsWith(".unity")).ToArray();
            AssetBundleBuild sceneBuild = new AssetBundleBuild();
            sceneBuild.assetBundleName = "scene";
            sceneBuild.assetNames = scenes;

            var assets = dependencies.Where(x => !x.EndsWith(".unity")).ToArray();
            AssetBundleBuild assetsBuild = new AssetBundleBuild();
            assetsBuild.assetBundleName = "assets";
            assetsBuild.assetNames = assets;

            var outputDir = Path.GetDirectoryName(outputFilePath);
            var exportDirName = Path.GetFileNameWithoutExtension(outputFilePath);
            var exportDir = Path.Combine(outputDir, exportDirName);

            if (Directory.Exists(exportDir))
                Directory.Delete(exportDir, recursive: true);
            Directory.CreateDirectory(exportDir);

            var manifest = BuildPipeline.BuildAssetBundles(
                exportDir,
                new AssetBundleBuild[] { sceneBuild, assetsBuild },
                BuildAssetBundleOptions.None,
                buildTarget
            );
            if (manifest == null) {
                Debug.LogError("Asset bundle build failed");
            } else {
                ZipUtils.CompressDirectory(exportDir, outputFilePath);
                if (deleteExportDir)
                    Directory.Delete(exportDir, recursive: true);
                else
                    Debug.Log("Exported intermediate asset bundles and manifests to " + exportDir);
                Debug.Log("Exported .mxrus to " + outputFilePath);
            }
        }
    }
}