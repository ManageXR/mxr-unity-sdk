using System;
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
        private const int FONT_SIZE_H1 = 16;
        private const int FONT_SIZE_H2 = 14;
        private const int FONT_SIZE_H3 = 12;

        private const int WINDOW_WIDTH = 800;
        private const int WINDOW_HEIGHT = 600;
        private const string EXPORT_FILE_EXTENSION = "mxrus";

        private string _exportPath;
        private bool _keepExportDir;
        private List<SceneExportViolation> _violations;

        private BuildReport _buildReport;
        private Type[] _typesInBuild;
        private PackedAssetInfo[] _packedAssetInfos;

        private Vector2 _scrollPos;
        private int _currAssetType;
        private Page _page;

        private enum Page {
            EXPORT_WIZARD,
            BUILD_REPORT
        }

        [MenuItem("Tools/MXR/Scene Exporter")]
        public static void OpenWindow() {
            var window = (SceneExportWindow)GetWindow(typeof(SceneExportWindow));
            window.Show();
            window.titleContent = new GUIContent("MXR Scene Exporter");
            window.minSize = new Vector2(WINDOW_WIDTH, WINDOW_HEIGHT);
            window.maxSize = new Vector2(WINDOW_WIDTH, WINDOW_HEIGHT);
        }

        private void OnGUI() {
            // The entire window has a vertical scroll view
            EditorGUILayout.BeginVertical();
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Width(WINDOW_WIDTH), GUILayout.Height(WINDOW_HEIGHT));

            if (_page == Page.EXPORT_WIZARD)
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
        private void OnGUI_DisplayExportWizard() {
            // Early out if the current scene isn't saved
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene == null || string.IsNullOrEmpty(activeScene.path)) {
                Label("You're on an unsaved scene. Please save the scene to export.", Color.red, FONT_SIZE_H1);
                Label("Ignore this message if an export is under progress", FONT_SIZE_H3);
                return;
            }

            Label(
                "This tool allows you to export your Unity scene as .mxrus files." +
                "\n\nThese files can then be deployed via the ManageXR dashboard to customize your homescreen environment.",
                FONT_SIZE_H2
            );

            GUILayout.Space(20);
            if (_buildReport != null) {
                if (GUILayout.Button("Last Build Report", GUILayout.MaxWidth(EditorGUIUtility.currentViewWidth)))
                    _page = Page.BUILD_REPORT;
            }

            GUILayout.Space(20);
            if (GUILayout.Button("Validate Scene", GUILayout.MaxWidth(EditorGUIUtility.currentViewWidth)))
                _violations = new SceneExportValidator().Validate();

            // If violations is null, the scene is yet to be validated. We don't go past the validate button.
            if (_violations == null)
                return;

            if (_violations.Count == 0)
                Label("Scene validated. No issues found. You can export this scene");

            if (_violations.Where(x => !x.IsWarning).Count() == 0) {
                GUILayout.Space(10);
                _keepExportDir = GUILayout.Toggle(_keepExportDir, "Keep intermediate export directory");
                GUILayout.Space(10);

                if (GUILayout.Button("Export Scene")) {
                    // Validate again before attempting export in case violating changes
                    // were made on the scene after successful validation previously
                    _violations = new SceneExportValidator().Validate();
                    if (_violations.Count > 0) {
                        // If there are export preventing violations, return
                        if (_violations.Where(x => !x.IsWarning).Count() > 0)
                            return;

                        // If there are violations that don't prevent export,
                        // confirm if the user still wants to go ahead.
                        if (_violations.Where(x => x.IsWarning).Count() > 0) {
                            var resp = EditorUtility.DisplayDialog(
                                "MXR Scene Export",
                                "Your scene has some issues. Are you sure you want to export?",
                                "Yes", "No"
                            );
                            if (!resp)
                                return;
                        }
                    }

                    // Proceed with showing the export popup
                    _violations = null;
                    var directory = Application.dataPath.Replace("Assets", "");
                    var defaultName = Path.GetFileNameWithoutExtension(activeScene.path);
                    _exportPath = EditorUtility.SaveFilePanel("Export scene", directory, defaultName, EXPORT_FILE_EXTENSION);
                    
                    // If the user selects a file destination, perform the export
                    if (!string.IsNullOrEmpty(_exportPath)) {
                        _buildReport = SceneExporter.ExportScene(activeScene.path, _exportPath, BuildTarget.Android, !_keepExportDir);
                        _currAssetType = 0;

                        // If the build was successful, we extract some data out for GUI rendering.
                        // OnGUI is called very frequently so we cache the values we need.
                        if (_buildReport.summary.result == BuildResult.Succeeded) {
                            // Get all the PackedAssetInfo across all the packedAssets in descending order of their size.
                            // This is for the window to easily show largest assets first.
                            _packedAssetInfos = _buildReport.packedAssets.SelectMany(x => x.contents).OrderByDescending(x => x.packedSize).ToArray();

                            // Get all the distinct types of the packedAssetInfo objects in the descending order of the sum of their occurences
                            // This is for the window to show the types that contribute more to the build higher up in the dropdown.
                            _typesInBuild = _packedAssetInfos.Select(x => x.type)
                                .Distinct()
                                .OrderByDescending(x => _packedAssetInfos.Where(y => y.type == x).Sum(z => (double)z.packedSize))
                                .ToArray();
                        }
                        _page = Page.BUILD_REPORT;
                    }
                    else {
                        Debug.Log("Export path selection cancelled");
                    }
                }
            }

            ShowViolationsSummary();
        }

        private void ShowViolationsSummary() {
            if (_violations == null || _violations.Count == 0)
                return;

            // Draw a horizontal divider with 20 padding
            EditorGUILayout.Space(20);
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, Color.gray);
            EditorGUILayout.Space(20);

            // Heading
            Label("Your scene has some issues.", FONT_SIZE_H1);
            EditorGUILayout.Space(5);
            Label("The ones highlighted RED must be addressed for export.\n" +
            $"The {(EditorGUIUtility.isProSkin ? "white" : "black")} ones may be ignored but it is recommended you fix them.", FONT_SIZE_H2);
            EditorGUILayout.Space(20);

            var violationTypes = _violations.Select(x => x.Type).Distinct().ToArray();

            for (int i = 0; i < violationTypes.Count(); i++) {
                // Print the violation description
                var first = _violations.First(x => x.Type == violationTypes[i]);
                if (first.IsWarning)
                    Label((i + 1) + ". " + first.Description, FONT_SIZE_H2);
                else
                    Label((i + 1) + ". " + first.Description, Color.red, FONT_SIZE_H2);

                EditorGUILayout.Space(10);

                // Show the relevant objects of the violation
                foreach (var violation in _violations.Where(x => x.Type == violationTypes[i])) {
                    if (violation.Object)
                        EditorGUILayout.ObjectField(violation.Object, violation.Object.GetType(), true);
                    EditorGUILayout.Space(10);
                }
                EditorGUILayout.Space(10);
            }
        }

        // ================================================
        // BUILD REPORT
        // ================================================
        private void OnGUI_DisplayBuildReport() {
            if (GUILayout.Button("Back"))
                _page = Page.EXPORT_WIZARD;

            if (_buildReport == null)
                _page = Page.EXPORT_WIZARD;

            GUILayout.Space(10);
            if (_buildReport.summary.result != BuildResult.Succeeded) {
                GUILayout.Label("Build was unsuccessful.");
                return;
            }

            GUILayout.Label("Total size " + Utils.GetFormattedSizeString(_buildReport.summary.totalSize));
            GUILayout.Label("Time to export " + _buildReport.summary.totalTime.ToString(@"hh\:mm\:ss"));
            GUILayout.Label("Below is a summary of assets that were packed with the scene export. " +
            "You can use this to reduce the export size.");
            GUILayout.Space(10);

            // Allow the user to select a type
            GUILayout.Label("Filter by asset type:");
            _currAssetType = EditorGUILayout.Popup(_currAssetType, _typesInBuild.Select(x => x.Name).ToArray());

            // Go over every packedAssetInfo and display it if it's on the selected type
            foreach (var info in _packedAssetInfos) {
                if (info.type != _typesInBuild[_currAssetType])
                    continue;

                var assetPath = info.sourceAssetPath;

                // Start a row for a single asset
                EditorGUILayout.BeginHorizontal();
                {
                    // Draw the icon
                    GUILayout.Label(AssetDatabase.GetCachedIcon(assetPath), GUILayout.MaxHeight(16), GUILayout.Width(20));

                    // Show the file name, which on clicking highlights the file in the Project window
                    var fileName = string.IsNullOrEmpty(assetPath) ? "Unknown" : Path.GetFileName(assetPath);
                    var buttonWidth = GUILayout.MaxWidth(EditorGUIUtility.currentViewWidth - 110);
                    if (GUILayout.Button(new GUIContent(fileName, assetPath), GUI.skin.label, buttonWidth))
                        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(assetPath));

                    // Write the asset size
                    GUILayout.Label(Utils.GetFormattedSizeString(info.packedSize));
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        // ================================================
        // LABELS
        // ================================================
        private void Label(string msg, int size = FONT_SIZE_H3) {
            var style = new GUIStyle() {
                normal = new GUIStyleState { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black },
                wordWrap = true,
                fontSize = size,
                padding = new RectOffset(2, 2, 2, 2)
            };
            GUILayout.Label(msg, style);
        }

        private void Label(string msg, Color color, int size = FONT_SIZE_H3) {
            var style = new GUIStyle() {
                normal = new GUIStyleState { textColor = color == null ? (EditorGUIUtility.isProSkin ? Color.white : Color.black) : color },
                wordWrap = true,
                fontSize = size,
                padding = new RectOffset(2, 2, 2, 2)
            };
            GUILayout.Label(msg, style);
        }
    }
}
