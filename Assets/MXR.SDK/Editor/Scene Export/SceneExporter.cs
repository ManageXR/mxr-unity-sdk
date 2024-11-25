using System.IO;
using System.Linq;

using UnityEditor;
using UnityEditor.Build.Reporting;

using UnityEngine;

namespace MXR.SDK.Editor {
    public static class SceneExporter {
        private const string SCENE_ASSETBUNDLE_NAME = "scene";
        private const string ASSETS_ASSETBUNDLE_NAME = "assets";
        private const string BUILD_REPORT_FILE_NAME = "build_report.txt";
        private const string FAILED_BUILD_REPORT_FILE_NAME = "failed_mxrus_build_report.txt";
        private readonly static string FAILED_BUILD_REPORT_FILE_PATH = Path.Combine(Application.dataPath.Replace("Assets", "Temp"), FAILED_BUILD_REPORT_FILE_NAME);

        /// <summary>
        /// Exports a scene into a zip arachive containing three asset bundles and their manifests:
        /// - "assets" containing the asset dependencies such as textures, models, materials, shaders
        /// - "scene" containing the scene
        /// - a third generated by Unity named after the output directory with some metadata
        /// </summary>
        /// <param name="scenePath">The fully resolved path to the scene in the project</param>
        /// <param name="outputFilePath">The fully resolved path to where the output archive will be saved</param>
        /// <param name="buildTarget">The target platform to build for</param>
        /// <param name="deleteExportDir">Whether the intermediate directory containing the asset bundles and manifests should be deleted after the export.</param>
        public static BuildReport ExportScene(string scenePath, string outputFilePath, BuildTarget buildTarget = BuildTarget.Android, bool deleteExportDir = true) {
            // Setup export directory
            var exportDir = GetExportDirectory(outputFilePath);
            EnsureEmptyDirectory(exportDir);

            // Delete any previous failed build report
            if (File.Exists(FAILED_BUILD_REPORT_FILE_PATH)) {
                File.Delete(FAILED_BUILD_REPORT_FILE_PATH);
            }

            // Perform asset bundle build and get the related outputs
            var dependencies = GetRelevantDependencies(scenePath);
            var manifest = BuildAssetBundles(dependencies, exportDir, buildTarget);
            var buildReport = Utils.GetLatestBuildReport();
            
            // If build failed, write failed report and show an error dialog
            if (!DidBuildSucceed(manifest, buildReport)) {
                if(buildReport != null) {
                    File.WriteAllText(FAILED_BUILD_REPORT_FILE_PATH, buildReport.ToPrettyString());
                    EditorUtility.DisplayDialog("Error", $"AssetBundle build failed. See {FAILED_BUILD_REPORT_FILE_PATH}", "OK");
                }
                else
                    EditorUtility.DisplayDialog("Error", "AssetBundle build failed", "OK");

                return buildReport;
            }

            // Write the build report inside the export directory
            File.WriteAllText(Path.Combine(exportDir, BUILD_REPORT_FILE_NAME), buildReport.ToPrettyString());

            // Compress export directory to the output file path
            ICompressionUtility zipCompression = new SharpZipLibCompressionUtility();
            zipCompression.CompressDirectory(exportDir, outputFilePath);

            // Delete the export directory if required
            if (deleteExportDir) {
                Directory.Delete(exportDir, recursive: true);
            }

            // Show a success dialog with information about the export paths
            var successMessage = $"Exported mxrus file to {outputFilePath}";
            if (!deleteExportDir) {
                successMessage += $"\n\nExported intermediate files to Directory {exportDir}";
            }
            EditorUtility.DisplayDialog("Success", successMessage, "OK");

            return buildReport;
        }

        private static string[] GetRelevantDependencies(string scenePath) {
            return AssetDatabase.GetDependencies(new string[] { scenePath })
                .Where(x => !x.EndsWith(".cs"))
                .Where(x => !x.EndsWith(".shader"))
                .ToArray();
        }

        private static string GetExportDirectory(string outputFilePath) {
            // Create a export directory for the asset bundle export.
            // If the mxrus file destination is /exports/myscene.mxrus
            // the temporary directory would be /exports/myscene/
            var outputDir = Path.GetDirectoryName(outputFilePath);
            var exportDirName = Path.GetFileNameWithoutExtension(outputFilePath);
            return Path.Combine(outputDir, exportDirName);
        }

        private static void EnsureEmptyDirectory(string exportDir) {
            if (Directory.Exists(exportDir)) {
                Directory.Delete(exportDir, recursive: true);
            }
            Directory.CreateDirectory(exportDir);
        }

        private static AssetBundleManifest BuildAssetBundles(string[] dependencies, string exportDir, BuildTarget buildTarget) {
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

        private static bool DidBuildSucceed(AssetBundleManifest manifest, BuildReport buildReport) {
            return manifest != null && buildReport != null && buildReport.summary.result == BuildResult.Succeeded;
        }
    }
}
