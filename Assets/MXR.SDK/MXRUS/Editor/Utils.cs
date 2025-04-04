using System.IO;

using UnityEditor;
using UnityEditor.Build.Reporting;

namespace MXR.SDK.Editor {
    public class Utils {
        public static BuildReport GetLatestBuildReport() {
            try {
                // Get the build report from the Library directory by importing Library/LastBuild.buildReport
                // into the asset database, loading it, then deleting it.
                // We use this because BuildReport.GetLatestReport is not supported on
                // several Unity editors that the MXR SDK may be used in.
                var source = Path.Combine("Library", "LastBuild.buildreport");
                var dest = Path.Combine("Assets", "LastBuild.buildreport");
                File.Copy(source, dest, true);
                AssetDatabase.ImportAsset(dest);
                var report = AssetDatabase.LoadAssetAtPath<BuildReport>(dest);
                File.Delete(dest);
                File.Delete(dest + ".meta");
                return report;
            }
            catch {
                return null;
            }
        }

        public static string GetFormattedSizeString(ulong bytes) {
            ulong oneKB = 1024;
            ulong oneMB = oneKB * 1024;
            ulong oneGB = oneMB * 1024;

            if ((decimal)bytes > oneGB)
                return ((decimal)bytes / oneGB).ToString("F3") + " GB";
            else if ((decimal)bytes > oneMB)
                return ((decimal)bytes / oneMB).ToString("F3") + " MB";
            else if ((decimal)bytes > oneKB)
                return ((decimal)bytes / oneKB).ToString("F3") + " KB";
            else
                return bytes + " B";
        }
    }
}
