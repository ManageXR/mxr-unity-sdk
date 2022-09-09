using UnityEngine;
using UnityEngine.UI;

namespace MXR.SDK.Samples {
    public class VideoCell : MonoBehaviour {
        public Video video;
        public FileInstallStatus status;
        [SerializeField] Text title;
        [SerializeField] Image icon;
        [SerializeField] Image updateIndicator;
        [SerializeField] Image readyIndicator;
        [SerializeField] Text statusLabel;
        [SerializeField] Sprite defaultIcon;

        [ContextMenu("Refresh")]
        public void Refresh() {
            title.text = video.title;

            // Instead of MXRStorage.GetFullPath(video.iconPath) you can also use
            // video.iconUrl to download the icon from a server.
            ImageDownloader.New().Download(MXRStorage.GetFullPath(video.iconPath),
                x => {
                    icon.enabled = true;
                    if (x == null) {
                        icon.sprite = defaultIcon;
                        return;
                    }

                    icon.sprite = Sprite.Create(x, new Rect(0, 0, x.width, x.height), Vector2.one / 2);
                    icon.preserveAspect = true;
                }
            );

            if (status != null) {
                if (status.status == FileInstallStatus.Status.COMPLETE) {
                    SetStatus(null);
                    updateIndicator.enabled = false;
                    readyIndicator.enabled = false; // Luke 6/11 - disabling all readyIndicators this for now  (value was "true")
                } else if (status.status == FileInstallStatus.Status.QUEUED) {
                    SetStatus("Queued...");
                    updateIndicator.enabled = false;
                    readyIndicator.enabled = false;
                } else if (status.status == FileInstallStatus.Status.DOWNLOADING) {
                    SetStatus("Downloading..." + status.progress + "%");
                    updateIndicator.enabled = true;
                    updateIndicator.fillAmount = status.progress / 100f;
                    readyIndicator.enabled = false;
                }
            } else {
                readyIndicator.enabled = false;
                updateIndicator.enabled = false;
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
            Debug.Log($"Play Video titled {video.title} from {MXRStorage.GetFullPath(video.videoPath)}");
        }
    }
}
