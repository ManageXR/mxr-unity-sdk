using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace MXR.SDK {
    /// <summary>
    /// Implements command operations that can be executed on the device through the MXR Admin App.
    /// Provides functionality for device control, app management, and system operations.
    /// </summary>
    public partial class MXRAndroidSystem {
        // When receiving commands through the Android intent, we get
        // the values in a flattened manner. Schema:
        // {intentID=VALUE,action=VALUE, videoId=VALUE, playFromBeginning=VALUE}
        // 
        // This is different from how the messenger manager sends it, the root has action:string and 
        // data:object where the latter has the string:videoId and 
        // bool:playFromBeginning. Schema:
        // {action=VALUE, data={videoId=VALUE, fromFromBeginning=VALUE}}
        //
        // We currently only handle the PLAY_VIDEO action through intents
        // so we first check if the "action" and "videoId" keys are present
        // in the intent bundle and then create a command object as a dictionary,
        // serialize it to json, and send it for processing. We attempt to read the
        // "playFromBeginning" boolean, but if not found, we default to true.
        private readonly HashSet<string> executedIntentIds = new();

        private void TryExecuteIntentCommands() {
            LogIfEnabled(LogType.Log, "Checking for intent commands");

            if (!MXRAndroidUtils.HasIntentExtra("intentId")) {
                LogIfEnabled(LogType.Log, "No 'intentId' key found in intent extras.");
                return;
            }

            if (!MXRAndroidUtils.HasIntentExtra("action")) {
                LogIfEnabled(LogType.Log, "No 'action' key found in intent extras.");
                return;
            }

            var action = MXRAndroidUtils.GetIntentStringExtra("action");
            if (!action.Equals("PLAY_VIDEO")) {
                LogIfEnabled(LogType.Log, "Only PLAY_VIDEO command action is supported via intents.");
                return;
            }

            LogIfEnabled(LogType.Log, "PLAY_VIDEO command action found.");

            if (!MXRAndroidUtils.HasIntentExtra("videoId")) {
                LogIfEnabled(LogType.Error, "No 'videoId' key found in intent extras.");
                return;
            }

            var intentId = MXRAndroidUtils.GetIntentStringExtra("intentId");
            var videoId = MXRAndroidUtils.GetIntentStringExtra("videoId");
            var playFromBeginning = MXRAndroidUtils.GetIntentBooleanExtra("playFromBeginning", true);

            var commandObj = new Dictionary<string, object> {
                { "action", "PLAY_VIDEO" }, {
                    "data", new Dictionary<string, object> {
                        { "videoId", videoId },
                        { "playFromBeginning", playFromBeginning }
                    }
                }
            };

            executedIntentIds.Add(intentId);
            ProcessCommandJson(JsonConvert.SerializeObject(commandObj));
        }

        private void ProcessCommandJson(string json) {
            try {
                LogIfEnabled(LogType.Log, $"Command received: {json}");

                var command = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                if (command == null) {
                    LogIfEnabled(LogType.Error, $"Could not deserialize command json string: {json}");
                    return;
                }

                var action = command["action"].ToString();
                var data = command["data"].ToString();

                switch (action) {
                    case Command.LAUNCH_ACTION:
                        var launchCommandData = JsonUtility.FromJson<LaunchMXRHomeScreenCommandData>(data);
                        if (launchCommandData != null) {
                            OnLaunchMXRHomeScreenCommand?.Invoke(launchCommandData);
                        } else {
                            LogIfEnabled(LogType.Error, "Could not deserialize command data string.");
                        }

                        break;
                    case Command.PLAY_VIDEO_ACTION:
                        var playVideoCommandData = JsonUtility.FromJson<PlayVideoCommandData>(data);
                        if (playVideoCommandData != null) {
                            OnPlayVideoCommand?.Invoke(playVideoCommandData);
                        } else {
                            LogIfEnabled(LogType.Error, "Could not deserialize command data string.");
                        }

                        break;
                    case Command.PAUSE_VIDEO_ACTION:
                        var pauseVideoCommandData = JsonUtility.FromJson<PauseVideoCommandData>(data);
                        if (pauseVideoCommandData != null) {
                            OnPauseVideoCommand?.Invoke(pauseVideoCommandData);
                        } else {
                            LogIfEnabled(LogType.Error, "Could not deserialize command data string.");
                        }

                        break;
                    case Command.RESUME_VIDEO_ACTION:
                        var resumeVideoCommandData = JsonUtility.FromJson<ResumeVideoCommandData>(data);
                        if (resumeVideoCommandData != null) {
                            OnResumeVideoCommand?.Invoke(resumeVideoCommandData);
                        } else {
                            LogIfEnabled(LogType.Error, "Could not deserialize command data string.");
                        }

                        break;
                }
            } catch (Exception e) {
                LogIfEnabled(new Exception("Could not process command json", e));
            }
        }
    }
}
