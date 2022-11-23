using System.Linq;

using UnityEditor;

using UnityEngine;

namespace MXR.SDK.Editor {
    public class MXRCommandSimulator : EditorWindow {
        [MenuItem("Tools/MXR/Launch MXR SDK Command Simulator")]
        public static void ShowWindow() {
            EditorWindow window = GetWindow<MXRCommandSimulator>();
            window.titleContent = new GUIContent("MXR SDK Command Simulator");
            window.minSize = new Vector2(500, 500);
            window.maxSize = new Vector2(500, 500);
            (window as MXRCommandSimulator).selectedCommandType = 0;
        }

        static IMXRSystem System;
        public static void SetSystem(IMXRSystem system) {
            System = system;
        }

        // COMMAND TYPE SELECTION
        int selectedCommandType;
        string[] commandTypes = new string[] { "None", "Play Video", "Pause Video", "Get Home Screen State" };

        // PLAY VIDEO DATA CREATION
        string[] videoNames =>
            System.RuntimeSettingsSummary.videos.Select(x => x.Value.title.Replace('/', '\u2215')).ToArray();
        string[] videoIDs =>
            System.RuntimeSettingsSummary.videos.Select(x => x.Key).ToArray();
        int selectedVideoIndex = 0;
        bool playFromBeginning = false;

        private void OnGUI() {
            if (!Application.isPlaying) {
                EditorGUILayout.BeginVertical();
                {
                    GUILayout.Label("This window allows you to simulate " +
                        "MXR Admin App commands in the editor. Currently, this tool" +
                        "supports Video Play, Video Pause and HomeScreenState Requests", 
                        EditorStyles.wordWrappedLabel
                    );
                    GUILayout.Space(10);
                    GUILayout.Label("You must be in Play Mode to use this tool!");
                    if (GUILayout.Button("Enter Play Mode"))
                        EditorApplication.EnterPlaymode();
                }
                EditorGUILayout.EndVertical();
            }
            else if (System == null) {
                EditorGUILayout.BeginVertical();
                {
                    GUILayout.Label("No IMXRSystem object found!");
                    GUILayout.Label($"If you're not using MXR.SDK.MXRManager.Initialize() " +
                        $"Invoke {nameof(MXRCommandSimulator)}.SetSystem for this window to work");
                }
                EditorGUILayout.EndVertical();
                return;
            }
            else {
                if (System is MXREditorSystem) {
                    GUILayout.BeginVertical();
                    GUILayout.Label("Select Command Type");
                    selectedCommandType = GUILayout.SelectionGrid(selectedCommandType, commandTypes, 1);

                    var system = System as MXREditorSystem;
                    GUILayout.Space(20);
                    EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), new Color(0f, 0f, 0f, 0.3f));
                    GUILayout.Space(20);
                    switch(selectedCommandType) {
                        case 0:
                            GUILayout.Label("Select a command type first.");
                            break;
                        
                        case 1:
                            GUILayout.Label("Play Video command args");
                            GUILayout.Space(10);
                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.Label("Video");
                                selectedVideoIndex = EditorGUILayout.Popup(selectedVideoIndex, videoNames);
                            }
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal(); {
                                GUILayout.Label("Play From Beginning?");
                                playFromBeginning = GUILayout.Toggle(playFromBeginning, "");
                            }
                            GUILayout.EndHorizontal();

                            GUILayout.Space(10);
                            if (GUILayout.Button("Invoke Play Video Command"))
                                system.InvokeCommand(new Command {
                                    action = CommandAction.PLAY_VIDEO,
                                    data = JsonUtility.ToJson(new PlayVideoCommandData {
                                        videoId = videoIDs[selectedVideoIndex],
                                        playFromBeginning = playFromBeginning
                                    })
                                });
                            break;

                        case 2:
                            system = System as MXREditorSystem;
                            if (GUILayout.Button("Invoke Pause Video Command"))
                                system.InvokeCommand(new Command {
                                    action = CommandAction.PAUSE_VIDEO,
                                    data = JsonUtility.ToJson(new PauseVideoCommandData())
                                });
                            break;

                        case 3:
                            system = System as MXREditorSystem;
                            if (GUILayout.Button("Request Home Screen State"))
                                system.RequestHomeScreenState();
                            break;
                    }

                    GUILayout.EndVertical();
                }
                else {
                    EditorGUILayout.BeginVertical();
                    {
                        GUILayout.Label("System must be of type MXREditorSystem");
                    }
                }
            }
        }
    }
}
