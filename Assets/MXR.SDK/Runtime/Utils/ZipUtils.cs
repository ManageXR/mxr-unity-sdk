using System;
using System.IO;

using Unity.SharpZipLib.Zip;

using UnityEngine;

namespace MXR.SDK {
    public static class ZipUtils {
        /// <summary>
        /// Compresses a source directory to a zip file
        /// </summary>
        public static void CompressDirectory(string sourceDirectory, string outputZipPath) {
            // Ensure the output file doesn't already exist
            if (File.Exists(outputZipPath)) {
                File.Delete(outputZipPath);
            }

            try {
                // Create the zip file
                using (FileStream fsOut = File.Create(outputZipPath))
                using (ZipOutputStream zipStream = new ZipOutputStream(fsOut)) {
                    zipStream.SetLevel(9); // Compression level (0-9), 9 is maximum compression

                    // Add the directory to the zip
                    AddDirectoryToZip(zipStream, sourceDirectory, "");

                    // Close the zip stream
                    zipStream.IsStreamOwner = true; // Ensures the FileStream is closed
                    zipStream.Close();
                }
            }
            catch(Exception ex) {
                Debug.LogError($"Failed to ZIP file: {ex.Message}");
            }
        }

        /// <summary>
        /// Extracts a zip file into an output directory
        /// </summary>
        public static void ExtractZipFile(string zipFilePath, string outputDirectory) {
            if (!File.Exists(zipFilePath)) {
                Debug.LogError($"ZIP file does not exist: {zipFilePath}");
                return;
            }

            if (!Directory.Exists(outputDirectory)) {
                Directory.CreateDirectory(outputDirectory); // Ensure the output directory exists
            }

            try {
                // Create a new FastZip instance
                FastZip fastZip = new FastZip();
                fastZip.ExtractZip(zipFilePath, outputDirectory, null); // Extract the ZIP file
            }
            catch (Exception ex) {
                Debug.LogError($"Failed to extract ZIP file: {ex.Message}");
            }
        }

        static void AddDirectoryToZip(ZipOutputStream zipStream, string folderPath, string basePath) {
            // Add all files in the directory
            foreach (string filePath in Directory.GetFiles(folderPath)) {
                string entryName = Path.Combine(basePath, Path.GetFileName(filePath)).Replace("\\", "/");
                var entry = new ZipEntry(entryName) {
                    DateTime = File.GetLastWriteTime(filePath),
                    Size = new FileInfo(filePath).Length
                };

                zipStream.PutNextEntry(entry);

                // Write the file to the zip
                using (FileStream fileStream = File.OpenRead(filePath)) {
                    fileStream.CopyTo(zipStream);
                }

                zipStream.CloseEntry();
            }

            // Recursively add subdirectories
            foreach (string subFolderPath in Directory.GetDirectories(folderPath)) {
                string subFolderName = Path.Combine(basePath, Path.GetFileName(subFolderPath));
                AddDirectoryToZip(zipStream, subFolderPath, subFolderName);
            }
        }
    }
}
