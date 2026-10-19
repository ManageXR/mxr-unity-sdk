using System;
using System.IO;

namespace MXR.SDK {
    /// <summary>
    /// Checks for the SDK's on-disk JSON cache files.
    /// </summary>
    internal static class CacheFile {
        /// <summary>
        /// Returns true when the file at <paramref name="path"/> does not hold exactly
        /// <paramref name="contents"/>. A missing or unreadable file counts as different.
        /// </summary>
        internal static bool Differs(string path, string contents) {
            try {
                return !File.Exists(path) || File.ReadAllText(path) != contents;
            } catch (Exception) {
                return true;
            }
        }
    }
}
