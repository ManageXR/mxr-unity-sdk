using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEditor;
using UnityEditor.Build.Reporting;

using UnityEngine;
using UnityEngine.SceneManagement;

using Object = UnityEngine.Object;

namespace MXR.SDK.Editor {
    public class SceneExportWindow : EditorWindow {
        const int H1 = 16;
        const int H2 = 14;
        const int H3 = 12;

        const int WIDTH = 800;
        const int HEIGHT = 600;
        const string EXTENSION = "mxrus";

        string exportPath;
        bool keepExportDir;
        List<SceneExportViolation> violations;
        BuildReport buildReport;
        Vector2 scrollPos;

        [MenuItem("Tools/MXR/Scene Exporter")]
        public static void OpenWindow() {
            var window = (SceneExportWindow)GetWindow(typeof(SceneExportWindow));
            window.Show();
            window.titleContent = new GUIContent("MXR Scene Exporter");
            window.minSize = new Vector2(WIDTH, HEIGHT);
            window.maxSize = new Vector2(WIDTH, HEIGHT);
        }

        void OnGUI() {
            // The entire window has a vertical scroll view
            EditorGUILayout.BeginVertical();
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Width(WIDTH), GUILayout.Height(HEIGHT));

            if (buildReport == null)
                OnGUI_DisplayExportWizard();
            else
                OnGUI_DisplayBuildReport();

            // End the scroll view
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // ================================================
        // EXPORT WIZARD
        // ================================================
        void OnGUI_DisplayExportWizard() {
            // Early out if the current scene isn't saved
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene == null || string.IsNullOrEmpty(activeScene.path)) {
                Label("You're on an unsaved scene. Please save the scene to export.", Color.red, H1);
                Label("Ignore this message if an export is under progress", H3);
                return;
            }

            Label(
                "This tool allows you to export your Unity scene as .mxrus files." +
                "\n\nThese files can then be deployed via the ManageXR dashboard to customize your homescreen environment.",
                H2
            );

            GUILayout.Space(20);
            if (GUILayout.Button("Validate Scene", GUILayout.MaxWidth(EditorGUIUtility.currentViewWidth)))
                violations = new SceneExportValidator().Validate();

            if (violations == null)
                return;

            if (violations.Count == 0)
                Label("Scene validated. No issues found. You can export this scene");

            if (violations.Where(x => !x.IsWarning).Count() == 0) {
                GUILayout.Space(10);
                keepExportDir = GUILayout.Toggle(keepExportDir, "Keep intermediate export directory");
                GUILayout.Space(10);

                if (GUILayout.Button("Export Scene")) {
                    // Validate again before attempting export in case violating changes
                    // were made on the scene after successful validation previously
                    violations = new SceneExportValidator().Validate();
                    if (violations.Count > 0) {
                        // If there are export preventing violations, return
                        if (violations.Where(x => !x.IsWarning).Count() > 0) 
                            return;

                        // If there are violations that don't prevent export,
                        // confirm if the user still wants to go ahead.
                        if (violations.Where(x => x.IsWarning).Count() > 0) {
                            var resp = EditorUtility.DisplayDialog(
                                "MXR Scene Export",
                                "Your scene has some issues. Are you sure you want to export?",
                                "Yes", "No"
                            );
                            if (!resp)
                                return;
                        }
                    }

                    // Proceed with the export
                    violations = null;
                    var directory = Application.dataPath.Replace("Assets", "");
                    var defaultName = Path.GetFileNameWithoutExtension(activeScene.path);
                    exportPath = EditorUtility.SaveFilePanel("Export scene", directory, defaultName, EXTENSION);
                    if (!string.IsNullOrEmpty(exportPath))
                        buildReport = SceneExporter.ExportScene(activeScene.path, exportPath, BuildTarget.Android, !keepExportDir);
                    else
                        Debug.Log("Export path selection cancelled");
                }
            }

            ShowViolationsSummary();
        }

        void ShowViolationsSummary() {
            if (violations == null || violations.Count == 0)
                return;

            // Draw a horizontal divider with 20 padding
            EditorGUILayout.Space(20);
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, Color.gray);
            EditorGUILayout.Space(20);

            // Heading
            Label("Your scene has some issues.", H1);
            EditorGUILayout.Space(5);
            Label("The ones highlighted RED must be addressed for export.\n" +
            $"The {(EditorGUIUtility.isProSkin ? "white" : "black")} ones may be ignored but it is recommended you fix them.", H2);
            EditorGUILayout.Space(20);

            var violationTypes = violations.Select(x => x.Type).Distinct().ToArray();

            for (int i = 0; i < violationTypes.Count(); i++) {
                // Print the violation description
                var first = violations.First(x => x.Type == violationTypes[i]);
                if (first.IsWarning)
                    Label((i + 1) + ". " + first.Description, H2);
                else
                    Label((i + 1) + ". " + first.Description, Color.red, H2);

                EditorGUILayout.Space(10);

                // Show the relevant objects of the violation
                foreach (var violation in violations.Where(x => x.Type == violationTypes[i])) {
                    if (violation.Object)
                        EditorGUILayout.ObjectField(violation.Object, typeof(GameObject), true);
                    EditorGUILayout.Space(10);
                }
                EditorGUILayout.Space(10);
            }
        }

        // ================================================
        // BUILD REPORT
        // ================================================
        List<string> allAssetTypes = new List<string> {
            "Texture2D",
            "Mesh",
            "Shader",
            "Material"
        };
        int currAssetType;
        void OnGUI_DisplayBuildReport() {
            if (GUILayout.Button("Back"))
                buildReport = null;

            GUILayout.Space(10);
            GUILayout.Label("Total size " + GetFormattedSizeString(buildReport.summary.totalSize));
            GUILayout.Label("Time to export " + buildReport.summary.totalTime.ToString(@"hh\:mm\:ss"));
            GUILayout.Label("Below is a summary of assets that were packed with the scene export. " +
            "You can use this to reduce the export size.");
            GUILayout.Space(10);

            // Show a toolbar that allows the user to filter the packed assets by type
            currAssetType = GUILayout.Toolbar(currAssetType, allAssetTypes.ToArray());

            var packedAssetsInfo = buildReport.packedAssets.SelectMany(x => x.contents).OrderByDescending(x => x.packedSize);
            HashSet<string> processed = new HashSet<string>();
            foreach (var info in packedAssetsInfo) {
                var path = info.sourceAssetPath;
                // packedAssetsInfo often contains the same asset more than once
                // so we keep track of the ones we're already iterated on and skip them
                if (processed.Contains(path))
                    continue;
                processed.Add(path);

                // We only show a few asset types that impact export size meaningfully
                if (info.type.Name != allAssetTypes[currAssetType])
                    continue;

                // Start a row for a single asset
                EditorGUILayout.BeginHorizontal();

                // Draw the icon
                GUILayout.Label(AssetDatabase.GetCachedIcon(path), GUILayout.MaxHeight(16), GUILayout.Width(20));

                // Show the file name, which on clicking highlights the file in the Project window
                var fileName = string.IsNullOrEmpty(path) ? "Unknown" : Path.GetFileName(path);
                var buttonWidth = GUILayout.MaxWidth(EditorGUIUtility.currentViewWidth - 110);
                if (GUILayout.Button(new GUIContent(Path.GetFileName(fileName), path), GUI.skin.label, buttonWidth))
                    EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(path));

                // Write the asset size
                GUILayout.Label(GetFormattedSizeString(info.packedSize));

                EditorGUILayout.EndHorizontal();
            }
        }

        // ================================================
        // UTILS
        // ================================================
        string GetFormattedSizeString(ulong bytes) {
            ulong oneKB = 1024;
            ulong oneMB = oneKB * 1024;
            ulong oneGB = oneMB * 1024;

            if ((decimal)bytes > oneGB)
                return ((decimal)bytes / oneGB).ToString("F3") + " GB";
            else if ((decimal)bytes > oneMB)
                return ((decimal)bytes / oneMB).ToString("F3") + " MB";
            else if ((decimal)bytes > oneKB)
                return ((decimal)bytes / oneKB).ToString("F3") + " KB";
            else
                return bytes + " B";
        }

        void Label(string msg, int size = H3) {
            var style = new GUIStyle() {
                normal = new GUIStyleState { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black },
                wordWrap = true,
                fontSize = size
            };
            GUILayout.Label(msg, style);
        }

        void Label(string msg, Color color, int size = H3) {
            var style = new GUIStyle() {
                normal = new GUIStyleState { textColor = color == null ? (EditorGUIUtility.isProSkin ? Color.white : Color.black) : color },
                wordWrap = true,
                fontSize = size
            };
            GUILayout.Label(msg, style);
        }
    }
}
