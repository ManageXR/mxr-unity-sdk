namespace MXR.SDK {
    public static class ZipUtils {
        readonly static ICompressionUtility _compressionUtility = new SharpZipLibCompressionUtility();

        /// <summary>
        /// Compresses a source directory to a zip file
        /// </summary>
        public static void CompressDirectory(string sourceDirectory, string outputZipPath) {
            _compressionUtility.CompressDirectory(sourceDirectory, outputZipPath);
        }

        /// <summary>
        /// Extracts a zip file into an output directory
        /// </summary>
        public static void ExtractZipFile(string zipFilePath, string outputDirectory) {
            _compressionUtility.ExtractToDirectory(zipFilePath, outputDirectory);
        }
    }
}
