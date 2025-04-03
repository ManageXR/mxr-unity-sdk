using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEditor;

using UnityEngine;
using UnityEngine.SceneManagement;

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
        List<Violation> violations;
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
            // Start a scroll view, the entire windows contents are scrollable
            EditorGUILayout.BeginVertical();
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Width(WIDTH), GUILayout.Height(HEIGHT));

            // Early out if the current scene isn't saved
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene == null || string.IsNullOrEmpty(activeScene.path)) {
                Label("You're on an unsaved scene. Please save the scene to export.", Color.red, H1);
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
                return;
            }

            Label(
                "This tool allows you to export your Unity scene as .mxrus files." +
                "\n\nThese files can then be deployed via the ManageXR dashboard to customize your homescreen environment.",
                H1
            );

            GUILayout.Space(20);
            if (GUILayout.Button("Validate Scene"))
                violations = new SceneExportValidator().Validate();

            ShowViolationsSummary();

            if (violations != null && violations.Where(x => !x.IsWarning).Count() == 0) {
                GUILayout.Space(10);
                keepExportDir = GUILayout.Toggle(keepExportDir, "Keep intermediate export directory");
                GUILayout.Space(10);
                if (GUILayout.Button("Export Scene")) {
                    // Validate again before attempting export in case violating changes
                    // were made after successful validation
                    violations = new SceneExportValidator().Validate();
                    if (violations.Where(x => !x.IsWarning).Count() > 0)
                        return;

                    violations = null;
                    var directory = Application.dataPath.Replace("Assets", "");
                    var defaultName = Path.GetFileNameWithoutExtension(activeScene.path);
                    exportPath = EditorUtility.SaveFilePanel("Export scene", directory, defaultName, EXTENSION);
                    if (!string.IsNullOrEmpty(exportPath))
                        SceneExporter.ExportScene(activeScene.path, exportPath, BuildTarget.Android, !keepExportDir);
                    else
                        Debug.Log("Export path selection cancelled");
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        void ShowViolationsSummary() {
            if (violations == null)
                return;
            
            // Draw a horizontal divider with 20 padding
            EditorGUILayout.Space(20);
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, Color.gray);
            EditorGUILayout.Space(20);

            if (violations.Count == 0) {
                Label("Scene validated. You can export now!");
                return;
            }

            // Heading
            Label("Your scene has some issues.", H1);
            EditorGUILayout.Space(5);
            Label("The ones highlighted RED must be addressed for export.\n" +
            "Others may be ignored but it is recommended you fix them.", H2);
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
                        EditorGUILayout.ObjectField(violation.Object, violation.Object.GetType(), true);
                    EditorGUILayout.Space(10);
                }
                EditorGUILayout.Space(10);
            }
            return;
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
