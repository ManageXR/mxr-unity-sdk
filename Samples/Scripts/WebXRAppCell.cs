using UnityEngine;
using UnityEngine.UI;

namespace MXR.SDK.Samples {
    public class WebXRAppCell : MonoBehaviour {
        public WebXRApp webXRApp;
        [SerializeField] Text title;
        [SerializeField] Image icon;
        [SerializeField] Sprite defaultIcon;
        [SerializeField] Image internetRequirement;
        [SerializeField] Image controllerRequirement;

        [ContextMenu("Refresh")]
        public void Refresh() {
            title.text = webXRApp.title;

            SetRequirementIcon(controllerRequirement, webXRApp.controllersRequired);
            SetRequirementIcon(internetRequirement, webXRApp.internetRequired);

            // Instead of MXRStorage.GetFullPath(video.iconPath) you can also use
            // video.iconUrl to download the icon from a server.
            ImageDownloader.New().Download(MXRStorage.GetFullPath(webXRApp.iconPath),
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

        public void OnClick() {
            if (!string.IsNullOrEmpty(webXRApp.url)) {
                Debug.Log("Open URL " + webXRApp.url);
                Application.OpenURL(webXRApp.url);
            }
        }

        bool isBeingDestroyed = false;
        void OnDestroy() {
            isBeingDestroyed = true;
        }
    }
}
