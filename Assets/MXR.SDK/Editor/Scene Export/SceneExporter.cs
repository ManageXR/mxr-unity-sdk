using System.IO;
using System.Linq;

using UnityEditor;
using UnityEditor.Build.Reporting;

using UnityEngine;

namespace MXR.SDK.Editor {
    public static class SceneExporter {
        const string SCENE_ASSETBUNDLE_NAME = "scene";
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
        public static BuildReport ExportScene(string scenePath, string outputFilePath, BuildTarget buildTarget = BuildTarget.Android, bool deleteExportDir = true) {
            // Setup export directory
            var exportDir = GetExportDirectory(outputFilePath);
            EnsureEmptyDirectory(exportDir);

            var dependencies = GetRelevantDependencies(scenePath);

            // Perform asset bundle build
            var manifest = BuildAssetBundles(dependencies, exportDir, buildTarget);
            var buildReport = Utils.GetLatestBuildReport();

            // Early out if build failed
            if (manifest == null) {
                Debug.LogError("Asset bundle build failed");
                EditorUtility.DisplayDialog("Error", "Asset bundle build failed", "OK");
                return buildReport;
            }

            // Create files.txt containing the dependencies inside the export directory
            File.WriteAllText(Path.Combine(exportDir, "build_report.txt"), buildReport.ToPrettyString());

            // Compress export directory to the output file path
            ICompressionUtility zipCompression = new SharpZipLibCompressionUtility();
            zipCompression.CompressDirectory(exportDir, outputFilePath);

            // Delete the export directory if required
            if (deleteExportDir)
                Directory.Delete(exportDir, recursive: true);

            // Show a success dialog with information about the export paths
            var successMessage = $"Exported mxrus file to {outputFilePath}";
            if (!deleteExportDir)
                successMessage += $"\n\nand intermediate files to Directory {exportDir}";
            EditorUtility.DisplayDialog("Success", successMessage, "OK");

            return buildReport;
        }

        static string[] GetRelevantDependencies(string scenePath) {
            return AssetDatabase.GetDependencies(new string[] { scenePath })
                .Where(x => !x.EndsWith(".cs"))
                .Where(x => !x.EndsWith(".shader"))
                .ToArray();
        }

        static string GetExportDirectory(string outputFilePath) {
            // Create a export directory for the asset bundle export.
            // If the mxrus file destination is /exports/myscene.mxrus
            // the temporary directory would be /exports/myscene/
            var outputDir = Path.GetDirectoryName(outputFilePath);
            var exportDirName = Path.GetFileNameWithoutExtension(outputFilePath);
            return Path.Combine(outputDir, exportDirName);
        }

        static void EnsureEmptyDirectory(string exportDir) {
            if (Directory.Exists(exportDir))
                Directory.Delete(exportDir, recursive: true);
            Directory.CreateDirectory(exportDir);
        }

        static AssetBundleManifest BuildAssetBundles(string[] dependencies, string exportDir, BuildTarget buildTarget) {
            // Identify the scenes and create an asset bundle build object
            AssetBundleBuild sceneBundleBuild = new AssetBundleBuild {
                assetBundleName = SCENE_ASSETBUNDLE_NAME,
                assetNames = dependencies.Where(x => x.EndsWith(".unity")).ToArray()
            };

            // Identify the assets and create an asset bundle build object
            AssetBundleBuild assetsBundleBuild = new AssetBundleBuild {
                assetBundleName = ASSETS_ASSETBUNDLE_NAME,
                assetNames = dependencies.Where(x => !x.EndsWith(".unity")).ToArray()
            };

            // Build and export the asset bundles to the export directory
            return BuildPipeline.BuildAssetBundles(
                exportDir,
                new AssetBundleBuild[] { sceneBundleBuild, assetsBundleBuild },
                BuildAssetBundleOptions.AssetBundleStripUnityVersion | BuildAssetBundleOptions.ForceRebuildAssetBundle,
                buildTarget
            );
        }
    }
}