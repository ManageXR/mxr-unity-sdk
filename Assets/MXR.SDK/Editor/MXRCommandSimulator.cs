using System.Linq;

using UnityEditor;

using UnityEngine;

namespace MXR.SDK.Editor {
    public class MXRCommandSimulator : EditorWindow {
        static EditorWindow window => GetWindow<MXRCommandSimulator>();

        [MenuItem("Tools/MXR/Launch MXR SDK Command Simulator")]
        public static void ShowWindow() {
            window.titleContent = new GUIContent("MXR SDK Command Simulator");
            window.minSize = new Vector2(610, 600);
            (window as MXRCommandSimulator).selectedCommandType = 0;
        }

        static IMXRSystem System;
        public static void SetSystem(IMXRSystem system) {
            System = system;
        }

        // COMMAND TYPE SELECTION
        int selectedCommandType;
        string[] commandTypes = new string[] {
            "None",
            "Play Video",
            "Pause Video"
        };

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
                    GUILayout.Label("This window allows you to simulate MXR Admin App commands in the editor. ");
                    GUILayout.Label("Currently it supports PLAY_VIDEO and PAUSE_VIDEO commands");
                    GUILayout.Label("Refer to CommandTypes.cs and CommandSubscriberExample.cs");
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
                    GUILayout.Label("IMXRSystem not set! Invoke MXRCommandSimulator.SetSystem for this window to work");
                }
                EditorGUILayout.EndVertical();
                return;
            }
            else {
                float width = Mathf.Min(300, window.position.width);

                if (System is MXREditorSystem) {
                    GUILayout.BeginVertical();
                    GUILayout.Label("Select Command Type");
                    selectedCommandType = GUILayout.SelectionGrid(selectedCommandType, commandTypes, 1, GUILayout.Width(200));

                    var system = System as MXREditorSystem;
                    GUILayout.Space(20);
                    EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), new Color(0f, 0f, 0f, 0.3f));
                    GUILayout.Space(20);
                    switch (selectedCommandType) {
                        case 0:
                            GUILayout.Label("Select a command type first.");
                            break;

                        case 1:
                            GUILayout.Label("Configure PLAY_VIDEO command data.");
                            GUILayout.Label("Refer to CommandTypes.cs for PlayVideoCommandData class.");
                            GUILayout.Label("PlayVideoCommandData requires a videoId and playFromBeginning bool.");
                            GUILayout.Label("This window allows you to select the video using a dropdown for convenience.");
                            GUILayout.Space(10);
                            GUILayout.BeginHorizontal(); 
                            {
                                GUILayout.Label("Video", GUILayout.Width(width));
                                selectedVideoIndex = EditorGUILayout.Popup(selectedVideoIndex, videoNames, GUILayout.Width(width));
                            }
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.Label("Video ID", GUILayout.Width(width));
                                GUILayout.TextField(videoIDs[selectedVideoIndex], GUILayout.Width(width));
                            }
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal(); 
                            {
                                GUILayout.Label("Play From Beginning?", GUILayout.Width(width));
                                playFromBeginning = GUILayout.Toggle(playFromBeginning, "", GUILayout.Width(width));
                            }
                            GUILayout.EndHorizontal();

                            GUILayout.Space(10);
                            if (GUILayout.Button("Invoke Play Video Command", GUILayout.Width(width *.6f)))
                                system.ExecuteCommand(new Command {
                                    action = CommandAction.PLAY_VIDEO,
                                    data = JsonUtility.ToJson(new PlayVideoCommandData {
                                        videoId = videoIDs[selectedVideoIndex],
                                        playFromBeginning = playFromBeginning
                                    })
                                });
                            break;

                        case 2:
                            GUILayout.Label("PAUSE_VIDEO command requires no additional data. " +
                                "Check out PauseVideoCommandData in CommandTypes.cs, it is an empty class.", 
                                EditorStyles.wordWrappedLabel
                            );

                            GUILayout.Label("Directly invoke using the button below.");
                            GUILayout.Space(20);
                            if (GUILayout.Button("Invoke Pause Video Command", GUILayout.Width(width * .6f)))
                                system.ExecuteCommand(new Command {
                                    action = CommandAction.PAUSE_VIDEO,
                                    data = JsonUtility.ToJson(new PauseVideoCommandData())
                                });
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
