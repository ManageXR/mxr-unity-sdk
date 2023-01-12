using UnityEngine;
#if UNITY_EDITOR
using MXR.SDK.Editor;
#endif

namespace MXR.SDK.Samples {
    public class CommandSubscriberExample : MonoBehaviour {
        void Start() {
            Debug.Log("<color=\"yellow\">Open the command simulator window using Tools/MXR</color>");
            MXRManager.Init();
#if UNITY_EDITOR
            MXRCommandSimulator.SetSystem(MXRManager.System);
#endif
            MXRManager.System.OnPlayVideoCommand += System_OnPlayVideoCommandReceived;
            MXRManager.System.OnPauseVideoCommand += System_OnPauseVideoCommandReceived;
        }

        private void System_OnPauseVideoCommandReceived(PauseVideoCommandData obj) {
            Debug.Log("<color=\"red\">Pause Video command invoked!</color>");
            Debug.Log("Pause the video playback when this command is received");
        }

        private void System_OnPlayVideoCommandReceived(PlayVideoCommandData data) {
            Debug.Log("<color=\"#00ff00\">Play Video Command Received with video ID " + data.videoId + "" +
                " and playFromBeginning set to " + data.playFromBeginning + "</color>");

            Debug.Log("With this information, use the video player of your preference to play/resume the video.");
            Debug.Log("You can use MXRManager.System.RuntimeSettingsSummary.videos[videoId] to get the ManageXR Video object");
            var video = MXRManager.System.RuntimeSettingsSummary.videos[data.videoId];
            Debug.Log("In this case, the video to be played is " + video.title + " from subpath " + video.videoPath);
        }
    }
}