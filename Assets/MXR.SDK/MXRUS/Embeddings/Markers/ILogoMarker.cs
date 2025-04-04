using UnityEngine;

namespace MXR.SDK {
    /// <summary>
    /// Marks an object to show a logo on
    /// </summary>
    public interface ILogoMarker {
        /// <summary>
        /// The logo type to be shown
        /// </summary>
        LogoMarkerType LogoMarkerType { get; }

        /// <summary>
        /// Sets the logo Texture2D on the object
        /// </summary>
        /// <param name="texture"></param>
        void SetLogo(Texture2D texture);
    }
}
