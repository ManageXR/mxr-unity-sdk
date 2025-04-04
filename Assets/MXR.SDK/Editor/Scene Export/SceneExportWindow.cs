using System.IO;

using UnityEditor;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace MXR.SDK.Editor {
    public class SceneExportWindow : EditorWindow {
        private const string EXTENSION = "mxrus";

        private string exportPath;
        private bool keepExportDir;

        [MenuItem("Tools/MXR/Scene Exporter")]
        public static void OpenWindow() {
            var window = (SceneExportWindow)GetWindow(typeof(SceneExportWindow));
            window.Show();
            window.titleContent = new GUIContent("MXR Scene Exporter");
            window.minSize = new Vector2(400, 200);
            window.maxSize = new Vector2(400, 200);
        }

        private void OnGUI() {
            var activeScene = SceneManager.GetActiveScene();

            if (activeScene == null || string.IsNullOrEmpty(activeScene.path)) {
                GUILayout.Space(20);
                Label("You're on an unsaved scene. Please save to export.", Color.red);
                return;
            }

            GUILayout.Space(20);
            Label(
                "This tool allows you to export your Unity scene as .mxrus files." +
                "\n\nThese files can then be deployed via the ManageXR dashboard to customize your homescreen environment."
            );
            GUILayout.Space(40);
            keepExportDir = GUILayout.Toggle(keepExportDir, "Keep intermediate export directory");
            GUILayout.Space(10);
            if (GUILayout.Button("Export this scene")) {
                var directory = Application.dataPath.Replace("Assets", "");
                var defaultName = Path.GetFileNameWithoutExtension(activeScene.path);
                exportPath = EditorUtility.SaveFilePanel("Export scene", directory, defaultName, EXTENSION);
                if (!string.IsNullOrEmpty(exportPath)) {
                    SceneExporter.ExportScene(activeScene.path, exportPath, BuildTarget.Android, !keepExportDir);
                }
                else {
                    Debug.Log("Export path selection cancelled");
                }
            }
        }

        private void Label(string msg) => Label(msg, Color.white);

        private void Label(string msg, Color color) {
            var style = new GUIStyle() {
                normal = new GUIStyleState { textColor = color },
                wordWrap = true
            };
            GUILayout.Label(msg, style);
        }
    }
}
