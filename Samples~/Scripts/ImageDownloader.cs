using System;
using System.Collections;

using UnityEngine;

namespace MXR.SDK.Samples {
    public class ImageDownloader : MonoBehaviour {
        ImageDownloader() { }

        public static ImageDownloader New() {
            var go = new GameObject() {
                hideFlags = HideFlags.HideAndDontSave
            };
            DontDestroyOnLoad(go);
            return go.AddComponent<ImageDownloader>();
        }

        public void Download(string url, Action<Texture2D> callback) =>
            StartCoroutine(DownloadInternal(url, callback));

        #pragma warning disable 0618
        IEnumerator DownloadInternal(string url, Action<Texture2D> callback) {
            if (string.IsNullOrEmpty(url)) {
                callback?.Invoke(null);
                yield break;
            }

            var www = new WWW(url);
            yield return www;
            while (!www.isDone)
                yield return null;

            if (string.IsNullOrEmpty(www.error)) {
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                texture.LoadImage(www.bytes);
                texture.Apply();

                // Handle the infamous red question mark
                // texture that shows up in Unity sometimes
                // when the texture doesn't load. It's 8x8.
                if (texture.width != 8 && texture.height != 8)
                    callback?.Invoke(texture);
                else
                    callback?.Invoke(null);
            }
            else
                callback?.Invoke(null);

            Destroy(gameObject);
        }
        #pragma warning restore 0618
    }
}
