using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace MXR.SDK.Samples {
    public class CommandSubscriber : MonoBehaviour {
        void Start() {
            Debug.Log("Open the command simulator window using Tools/MXR");
            MXRManager.Init();
            MXRManager.System.OnPlayVideoCommand += System_OnPlayVideoCommandReceived;
            MXRManager.System.OnPauseVideoCommand += System_OnPauseVideoCommandReceived;
        }

        private void System_OnPauseVideoCommandReceived(PauseVideoCommandData obj) {
            Debug.Log("Pause Video");
        }

        private void System_OnPlayVideoCommandReceived(PlayVideoCommandData obj) {
            Debug.Log("Play Video ID " + obj.videoId + (obj.playFromBeginning ? " from beginning" : ""));
        }
    }
}