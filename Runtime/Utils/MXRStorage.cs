using System.IO;

using UnityEngine;

namespace MXR.SDK {
    /// <summary>
    /// API for ManageXR disk directory
    /// </summary>
    public static class MXRStorage {
        /// <summary>
        /// Returns root system storage directory
        /// </summary>
        public static string ExternalStorageDirectory {
            get {
                if (Application.isEditor) 
                    return Application.dataPath.Replace("Assets", "Files");
                else {
                    var path = new AndroidJavaClass("android.os.Environment")
                        .SafeCallStatic<AndroidJavaObject>("getExternalStorageDirectory")
                        .SafeCall<string>("getPath");
                    EnsurePath(path);
                    return path;
                }
            }
        }

        /// <summary>
        /// Returns the root directory where ManageXR files are stored. 
        /// Basically <see cref="ExternalStorageDirectory"/>/MightyImmersion
        /// </summary>
        public static string MXRRootDirectory => Path.Combine(ExternalStorageDirectory, "MightyImmersion");

        /// <summary>
        /// Returns the full path to a sub path inside <see cref="ExternalStorageDirectory"/>.
        /// Returns null if the provided path is null or empty.
        /// </summary>
        /// <param name="path">The relative path to convert to a full path. May be null or empty.</param>
        /// <returns>The full path, or null if the input path is null or empty.</returns>
        public static string GetFullPath(string path) {
            if (string.IsNullOrEmpty(path))
                return null;

            path = TryRemoveLeadingSpash(path);
            return Path.Combine(ExternalStorageDirectory, path);
        }

        /// <summary>
        /// Ensures the path to a directory or file is valid by 
        /// creating parent directories to it
        /// </summary>
        /// <param name="path"></param>
        /// <param name="isFile"></param>
        public static void EnsurePath(string path, bool isFile = true) {
            string directory = path;
            if (isFile)
                directory = new FileInfo(path).Directory.FullName;
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
        }

        // Checks for the path starting with "/" and removes it if true
        static string TryRemoveLeadingSpash(string path) {
            if (path.StartsWith("/"))
                return path.Substring(1, path.Length - 1);
            return path;
        }
    }
}
