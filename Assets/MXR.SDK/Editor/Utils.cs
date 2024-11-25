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
    }
}
