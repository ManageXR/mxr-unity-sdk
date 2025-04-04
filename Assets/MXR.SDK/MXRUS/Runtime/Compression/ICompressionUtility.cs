namespace MXR.SDK {
    /// <summary>
    /// Provides directory compression and extraction capabilities
    /// </summary>
    public interface ICompressionUtility {
        /// <summary>
        /// Compresses a directory to a file
        /// </summary>
        /// <param name="sourceDirectory">The directory to be compressed</param>
        /// <param name="outputFilePath">The output file path</param>
        void CompressDirectory(string sourceDirectory, string outputFilePath);

        /// <summary>
        /// Extracts a file to a directory
        /// </summary>
        /// <param name="sourceFilePath">The file to be extracted from</param>
        /// <param name="outputDirectory">The directory to which the file contents are extracted</param>
        void ExtractToDirectory(string sourceFilePath, string outputDirectory);
    }
}
