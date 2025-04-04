using UnityEngine;

namespace MXR.SDK {
    /// <summary>
    /// Marks a 3D GameObject with a Renderer for showing a logo
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class MonoRendererLogoMarker : MonoBehaviour, ILogoMarker {
        [SerializeField] LogoMarkerType _logoMarkerType;

        public LogoMarkerType LogoMarkerType => _logoMarkerType;

        private Renderer _renderer;

        private void Awake () {
            _renderer = GetComponent<Renderer>();
        }

        public void SetLogo(Texture2D texture) {
            _renderer.material.mainTexture = texture;
        }
    }
}
