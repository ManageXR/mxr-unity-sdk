using UnityEngine;

using UnityEngine.UI;

namespace MXR.SDK.Samples {
    public class RuntimeAppCell : MonoBehaviour {
        public RuntimeApp runtimeApp;
        public AppInstallStatus status;

        [SerializeField] Text title;
        [SerializeField] Image icon;
        [SerializeField] Image updateIndicator;
        [SerializeField] Image readyIndicator;
        [SerializeField] Text statusLabel;
        [SerializeField] Sprite defaultIcon;

        [ContextMenu("Refresh")]
        public void Refresh() {
            title.text = runtimeApp.title;

            // Instead of MXRStorage.GetFullPath(video.iconPath) you can also use
            // video.iconUrl to download the icon from a server.
            ImageDownloader.New().Download(MXRStorage.GetFullPath(runtimeApp.iconPath),
                x => {
                    if (isBeingDestroyed) return;

                    if (x == null) {
                        icon.sprite = defaultIcon;
                        return;
                    }

                    icon.sprite = Sprite.Create(x, new Rect(0, 0, x.width, x.height), Vector2.one / 2);
                    icon.preserveAspect = true;
                }
            );

            if (status != null) {
                if (status.IsNotUpdating()) {
                    SetStatus(null);
                    updateIndicator.enabled = false;
                    var isComplete = status.status == AppInstallStatus.Status.COMPLETE;
                    readyIndicator.enabled = false;
                } else if (status.UpdateIsQueued()) {
                    SetStatus("Queued...");
                    updateIndicator.enabled = false;
                    readyIndicator.enabled = false;
                } else if (status.IsUpdating()) {
                    switch (status.status) {
                        case AppInstallStatus.Status.DOWNLOADING:
                            SetStatus("Downloading..." + status.progress + "%");
                            break;
                        case AppInstallStatus.Status.PATCHING:
                            SetStatus("Patching..." + status.progress + "%");
                            break;
                        case AppInstallStatus.Status.INSTALLING:
                            SetStatus("Installing..." + status.progress + "%");
                            break;
                        case AppInstallStatus.Status.CLEANUP:
                            SetStatus("Cleaning up...");
                            break;
                        case AppInstallStatus.Status.SETUP:
                            SetStatus("Setup...");
                            break;
                        case AppInstallStatus.Status.READY_TO_INSTALL:
                            SetStatus("Ready to install...");
                            break;
                    }
                    updateIndicator.enabled = true;
                    updateIndicator.fillAmount = status.progress / 100f;
                    readyIndicator.enabled = false;
                }
            }
        }

        void SetStatus(string text) {
            if (string.IsNullOrEmpty(text)) {
                statusLabel.transform.parent.GetComponent<Image>().enabled = false;
                statusLabel.text = "";
            } else {
                statusLabel.transform.parent.GetComponent<Image>().enabled = true;
                statusLabel.text = text;
            }
        }

        public void OnClick() {
            Debug.Log("Open App " + runtimeApp.title);
            if (Application.platform == RuntimePlatform.Android)
                MXRAndroidUtils.LaunchRuntimeApp(runtimeApp);
        }

        bool isBeingDestroyed = false;
        void OnDestroy() {
            isBeingDestroyed = true;
        }
    }
}
