using System;
using System.IO;
using System.Net;

using UnityEngine;

namespace MXR.SDK.Samples {
    /// <summary>
    /// Loads an image from a remote URL or the local disk as a Texture2D object
    /// </summary>
    public class ImageDownloader {
        [Obsolete("This method has been deprecated and may be removed soon. " +
        "Instead of this method, construct an instance using \"new ImageDownloader();\"")]
        public static ImageDownloader New() {
            return new ImageDownloader();
        }

        [Obsolete("This method has been deprecated and may be removed soon, use the Load method instead")]
        public void Download(string url, Action<Texture2D> callback) {
            Load(url, TextureFormat.ARGB32, true, callback, error => callback?.Invoke(null));
        }

        /// <summary>
        /// Load a texture from a location. If the location is on a remote server, 
        /// <see cref="LoadFromURL(string, TextureFormat, bool, Action{Texture2D}, Action{Exception})"/> is used
        /// otherwise <see cref="LoadFromDisk(string, TextureFormat, bool)"/> is used.
        /// </summary>
        /// <param name="location">The location (local path or remote URL) to load the image from.</param>
        /// <param name="format">The format used for the Texture2D instance returned in the success callback</param>
        /// <param name="mipMap">Whether mipmaps are enabled for the Texture2D object</param>
        /// <param name="onSuccess">Callback invoked when the load is successful.</param>
        /// <param name="onError">Callback invoked when the load fails.</param>
        public void Load(string location, TextureFormat format, bool mipMap, Action<Texture2D> onSuccess, Action<Exception> onError) {
            if (string.IsNullOrEmpty(location)) {
                onError?.Invoke(new Exception("Cannot load image from null or empty location"));
                return;
            }

            // If the location is a URL, we use the remote download method
            if (location.Contains("http://") || location.Contains("https://"))
                LoadFromURL(location, format, mipMap, onSuccess, onError);
            // Otherwise we load it from disk.
            else {
                try {
                    var tex = LoadFromDisk(location, format, mipMap);
                    onSuccess?.Invoke(tex);
                }
                catch (Exception e) {
                    onError?.Invoke(e);
                }
            }
        }

        /// <summary>
        /// Loads a texture from a remote URL.
        /// </summary>
        /// <param name="url">The URL to load the image from.</param>
        /// <param name="format">The format used fo rhte Texture2D instance returned in the success callback</param>
        /// <param name="mipMap">Whether mipmaps are enabled for the Texture2D object.</param>
        /// <param name="onSuccess">Callback innvoked when the load is successful</param>
        /// <param name="onError">Callback invoked when the load fails.</param>
        public void LoadFromURL(string url, TextureFormat format, bool mipMap, Action<Texture2D> onSuccess, Action<Exception> onError) {
            if (string.IsNullOrEmpty(url)) {
                onError?.Invoke(new Exception("Could not download image from " + url));
                return;
            }

            WebClient client = new WebClient();
            client.DownloadDataCompleted += (sender, args) => {
                if (args.Error != null) {
                    onError?.Invoke(new Exception("Could not download image from " + url + " . Error: " + args.Error));
                    return;
                }

                if (args.Result != null && args.Result.Length > 0) {
                    Texture2D tex = new Texture2D(2, 2, format, mipMap);
                    tex.LoadImage(args.Result);
                    if (tex.width == 8 && tex.height == 8)
                        onError?.Invoke(new Exception("Could not load texture data from image hosted at " + url));
                    else
                        onSuccess?.Invoke(tex);
                }
                else {
                    onError?.Invoke(new Exception("Could not download image from " + url + " . Error: No data was downloaded."));
                }
            };
            client.DownloadDataAsync(new Uri(url));
        }

        /// <summary>
        /// Loads an image from local disk.
        /// </summary>
        /// <param name="path">The path to load the image from</param>
        /// <param name="format">The format to load the Texture2D instance with</param>
        /// <param name="mipMap">Whether the loaded Texture2D object has mipmapping enabled.</param>
        /// <returns></returns>
        public static Texture2D LoadFromDisk(string path, TextureFormat format, bool mipMap) {
            if (string.IsNullOrEmpty(path))
                throw new Exception("Cannot load image from a null or empty subpath");

            if (File.Exists(path)) {
                var bytes = File.ReadAllBytes(path);
                var texture = new Texture2D(2, 2, format, mipMap);
                texture.LoadImage(bytes, true);
                if (texture.width != 8 && texture.height != 8)
                    return texture;
                else
                    throw new Exception("Loaded texture was not valid. " + path);
            }
            throw new Exception("No file exists at path " + path);
        }
    }
}
