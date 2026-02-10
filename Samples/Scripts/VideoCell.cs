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
        [SerializeField] Image internetRequirement;
        [SerializeField] Image controllerRequirement;

        [ContextMenu("Refresh")]
        public void Refresh() {
            title.text = video.title;

            // Try local path first, fall back to remote URL if not available
            string iconLocation = string.IsNullOrEmpty(video.iconPath)
                ? video.iconUrl
                : MXRStorage.GetFullPath(video.iconPath);

            new ImageDownloader().Load(iconLocation, TextureFormat.ARGB32, true,
                result => {
                    if (isBeingDestroyed) return;

                    if (result == null) {
                        icon.sprite = defaultIcon;
                        return;
                    }

                    icon.sprite = Sprite.Create(result, new Rect(0, 0, result.width, result.height), Vector2.one / 2);
                    icon.preserveAspect = true;
                },
                error => icon.sprite = defaultIcon
            );

            SetRequirementIcon(controllerRequirement, video.controllersRequired);
            SetRequirementIcon(internetRequirement, video.internetRequired);

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

        void SetRequirementIcon(Image icon, Content.Requirement requirement) {
            switch (requirement) {
                case Content.Requirement.UNDEFINED:
                    icon.transform.parent.gameObject.SetActive(false);
                    break;
                case Content.Requirement.OPTIONAL:
                    icon.transform.parent.gameObject.SetActive(true);
                    icon.color = Color.yellow;
                    break;
                case Content.Requirement.MANDATORY:
                    icon.transform.parent.gameObject.SetActive(true);
                    icon.color = Color.green;
                    break;
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

        bool isBeingDestroyed = false;
        void OnDestroy() {
            isBeingDestroyed = true;
        }
    }
}
