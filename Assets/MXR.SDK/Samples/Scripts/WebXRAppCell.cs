using UnityEngine;
using UnityEngine.UI;

namespace MXR.SDK.Samples {
    public class WebXRAppCell : MonoBehaviour {
        public WebXRApp site;
        [SerializeField] Text title;
        [SerializeField] Image icon;
        [SerializeField] Sprite defaultIcon;

        [ContextMenu("Refresh")]
        public void Refresh() {
            title.text = site.title;

            // Instead of MXRStorage.GetFullPath(video.iconPath) you can also use
            // video.iconUrl to download the icon from a server.
            ImageDownloader.New().Download(MXRStorage.GetFullPath(site.iconPath),
                x => {
                    if (isBeingDestroyed) return;

                    if (x == null) {
                        icon.sprite = defaultIcon;
                        return;
                    }

                    icon.enabled = true;
                    icon.sprite = Sprite.Create(x, new Rect(0, 0, x.width, x.height), Vector2.one / 2);
                    icon.preserveAspect = true;
                }
            );
        }

        public void OnClick() {
            if (!string.IsNullOrEmpty(site.url)) {
                Debug.Log("Open URL " + site.url);
                Application.OpenURL(site.url);
            }
        }

        bool isBeingDestroyed = false;
        void OnDestroy() {
            isBeingDestroyed = true;
        }
    }
}
