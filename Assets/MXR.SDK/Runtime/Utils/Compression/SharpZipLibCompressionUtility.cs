using System.IO;

using Unity.SharpZipLib.Zip;

using UnityEngine;

namespace MXR.SDK {
    /// <summary>
    /// <see cref="ICompressionUtility"/> based on SharpZipLib 
    /// </summary>
    public class SharpZipLibCompressionUtility : ICompressionUtility {
        public void CompressDirectory(string sourceDirectory, string outputFilePath) {
            // Ensure the output file doesn't already exist
            if (File.Exists(outputFilePath)) {
                File.Delete(outputFilePath);
            }

            // Create the zip file
            using (FileStream fsOut = File.Create(outputFilePath))
            using (ZipOutputStream zipStream = new ZipOutputStream(fsOut)) {
                zipStream.SetLevel(9); // Compression level (0-9), 9 is maximum compression

                // Add the directory to the zip
                AddDirectoryToZip(zipStream, sourceDirectory, "");

                // Close the zip stream
                zipStream.IsStreamOwner = true; // Ensures the FileStream is closed
                zipStream.Close();
            }
        }

        public void ExtractToDirectory(string sourceFilePath, string outputDirectory) {
            if (!File.Exists(sourceFilePath)) {
                Debug.LogError($"ZIP file does not exist: {sourceFilePath}");
                return;
            }

            if (!Directory.Exists(outputDirectory)) {
                Directory.CreateDirectory(outputDirectory); // Ensure the output directory exists
            }

            // Create a new FastZip instance
            FastZip fastZip = new FastZip();
            fastZip.ExtractZip(sourceFilePath, outputDirectory, null); // Extract the ZIP file
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
