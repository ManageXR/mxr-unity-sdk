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

            // Try local path first, fall back to remote URL if not available
            string iconLocation = string.IsNullOrEmpty(webXRApp.iconPath)
                ? webXRApp.iconUrl
                : MXRStorage.GetFullPath(webXRApp.iconPath);

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

            SetRequirementIcon(controllerRequirement, webXRApp.controllersRequired);
            SetRequirementIcon(internetRequirement, webXRApp.internetRequired);
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
