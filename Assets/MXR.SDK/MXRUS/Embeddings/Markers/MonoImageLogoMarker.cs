using UnityEngine;
using UnityEngine.UI;

namespace MXR.SDK {
    /// <summary>
    /// Marks a UI GameObject with Image component for showing a logo
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class MonoImageLogoMarker : MonoBehaviour, ILogoMarker {
        [SerializeField] LogoMarkerType _logoMarkerType;

        public LogoMarkerType LogoMarkerType => _logoMarkerType;

        private Image _image;

        private void Awake() {
            _image = GetComponent<Image>();
        }

        public void SetLogo(Texture2D texture) {
            _image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one / 2);
        }
    }
}
