using System;

using UnityEngine;

namespace MXR.SDK {
    /// <summary>
    /// A hidden player object created at runtime so plain classes can hear Unity destroy.
    /// </summary>
    public sealed class UnityLifecycleEvents : MonoBehaviour {
        /// <summary>
        /// The player object for this run, or null before <see cref="Ensure"/>.
        /// </summary>
        public static UnityLifecycleEvents Current { get; private set; }

        /// <summary>
        /// Raised from <see cref="OnDestroy"/>.
        /// </summary>
        public event Action Destroyed;

        /// <summary>
        /// Creates the hidden object on first use.
        /// </summary>
        public static UnityLifecycleEvents Ensure() {
            if (Current != null) {
                return Current;
            }

            var go = new GameObject(nameof(UnityLifecycleEvents));
            go.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(go);
            Current = go.AddComponent<UnityLifecycleEvents>();
            return Current;
        }

        private void OnDestroy() {
            var handler = Destroyed;
            Destroyed = null;
            if (handler != null) {
                handler();
            }

            if (Current == this) {
                Current = null;
            }
        }
    }
}
