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
                    return "file://" + Path.Combine(Application.dataPath, "MXR.SDK", "Runtime", "Editor", "Files");
                else {
                    var path = new AndroidJavaClass("android.os.Environment")
                        .CallStatic<AndroidJavaObject>("getExternalStorageDirectory")
                        .Call<string>("getPath");
                    EnsureDirectory(path);
                    return path;
                }
            }
        }

        /// <summary>
        /// Returns the full path to a sub path inside <see cref="ExternalStorageDirectory"/>
        /// </summary>
        public static string GetFullPath(string path) {
            path = CleanPath(path);
            var newPath = Path.Combine(ExternalStorageDirectory, path);
            EnsureDirectory(newPath);
            return newPath;
        }

        // Checks for the path starting with "/" and removes it if true
        static string CleanPath(string path) {
            if (path.StartsWith("/"))
                return path.Substring(1, path.Length - 1);
            return path;
        }

        // Creates a directory if not present.
        // Caution: You can pass a file path here and it'll create
        // a folder using the file name
        static void EnsureDirectory(string directoryPath) {
            if (Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);
        }
    }
}
