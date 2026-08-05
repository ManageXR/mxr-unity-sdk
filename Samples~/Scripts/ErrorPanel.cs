using UnityEngine;
using UnityEngine.UI;

namespace MXR.SDK.Samples {
    public class ErrorPanel : MonoBehaviour {
        [SerializeField] CanvasGroup cg;
        [SerializeField] Text message;

        void Update() {
            string error = string.Empty;

            if (GetError(MXRManager.System.RuntimeSettingsSummary, "runtimeSettingsSummary.json", out string e1))
                error += e1;
            if (GetError(MXRManager.System.DeviceStatus, "deviceStatus.json", out string e2))
                error += e2;
            if (GetError(MXRManager.System.WifiNetworks, "wifiNetworks.json", out string e3))
                error += e3;
            if (GetError(MXRManager.System.WifiConnectionStatus, "wifiConnectionStatus.json", out string e4))
                error += e4;

            if(!string.IsNullOrEmpty(error)) {
                cg.alpha = 1;
                cg.blocksRaycasts = true;
            }
            else {
                cg.alpha = 0;
                cg.blocksRaycasts = false;
            }
            message.text = error;
        }

        bool GetError(object obj, string fileName, out string error) {
            if (obj == null){
                error = $"\n\n{fileName} not found under Files/MightyImmersion";
                return true;
            }
            error = string.Empty;
            return false;
        }
    }
}
