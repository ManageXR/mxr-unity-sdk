using System.IO;
using System.Linq;
using System.Text;

using UnityEditor;
using UnityEditor.Build.Reporting;

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
        public static BuildReport ExportScene(string scenePath, string outputFilePath, BuildTarget buildTarget = BuildTarget.Android, bool deleteExportDir = true) {
            // Get all the dependencies of the scene
            Debug.unityLogger.Log(LogType.Log, TAG, "Getting dependencies of scene at " + scenePath);

            var dependencies = AssetDatabase.GetDependencies(new string[] { scenePath })
                .Where(x => !x.EndsWith(".cs"))
                .Where(x => !x.EndsWith(".shader"));

            if (dependencies.Count() > 0) {
                StringBuilder sb = new StringBuilder();
                sb.Append("Dependency list:");
                foreach (var dependency in dependencies)
                    sb.Append(dependency).Append("\n");
                Debug.unityLogger.Log(LogType.Log, TAG, sb.ToString());
            }

            // Identify the scenes and create an asset bundle build object
            AssetBundleBuild sceneBuild = new AssetBundleBuild {
                assetBundleName = SCENE_ASSETBUNDLE_NAME,
                assetNames = dependencies.Where(x => x.EndsWith(".unity")).ToArray()
            };

            // Identify the assets and create an asset bundle build object
            AssetBundleBuild assetsBuild = new AssetBundleBuild {
                assetBundleName = ASSETS_ASSETBUNDLE_NAME,
                assetNames = dependencies.Where(x => !x.EndsWith(".unity")).ToArray()
            };

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
                BuildAssetBundleOptions.AssetBundleStripUnityVersion | BuildAssetBundleOptions.ForceRebuildAssetBundle,
                buildTarget
            );

            string message;
            var buildReport = GetLatestBuildReport();
            if (manifest == null) {
                message = "mxrus file export failed. ";
                if (buildReport != null)
                    message += buildReport.GetStepsSummary();
                Debug.unityLogger.Log(LogType.Error, TAG, message);
                return null;
            }

            File.WriteAllText(Path.Combine(exportDir, "files.txt"), string.Join("\n", dependencies));

            // Compress the export directory to a .mxrus file and delete the export 
            // directory if required.
            ZipUtils.CompressDirectory(exportDir, outputFilePath);
            if (deleteExportDir)
                Directory.Delete(exportDir, recursive: true);
            else
                Debug.unityLogger.Log(LogType.Log, TAG, "Exported asset bundles and manifests to " + exportDir);

            message = $"Exported .mxrus to {outputFilePath}. ";
            if (buildReport != null)
                message += buildReport.GetStepsSummary();
            Debug.unityLogger.Log(LogType.Log, TAG, message);
            return buildReport;
        }

        public static BuildReport GetLatestBuildReport() {
            try {
                // Get the build report from the Library directory
                // We use this because BuildReport.GetLatestReport is not supported on
                // several Unity editors that the MXR SDK may be used in.
                var source = Path.Combine("Library", "LastBuild.buildreport");
                var dest = Path.Combine("Assets", "LastBuild.buildreport");
                File.Copy(source, dest, true);
                AssetDatabase.ImportAsset(dest);
                var report = AssetDatabase.LoadAssetAtPath<BuildReport>(dest);
                File.Delete(dest);
                File.Delete(dest + ".meta");
                return report;
            }
            catch {
                return null;
            }
        }

        public static string GetStepsSummary(this BuildReport buildReport) {
            string message = $"Asset bundle build summary:";
            if (buildReport != null) {
                foreach (var step in buildReport.steps) {
                    message += $"\n{step.name} (time:{step.duration.ToString(@"hh\:mm\:ss")})";
                    foreach (var msg in step.messages) {
                        message += $"\n  {msg.content}";
                    }
                }
            }
            return message;
        }
    }
}