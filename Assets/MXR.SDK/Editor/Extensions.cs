using System.Text;

using UnityEditor.Build.Reporting;

namespace MXR.SDK.Editor {
    public static class Extensions {
        public static string ToPrettyString(this BuildReport buildReport, int indentLength = 4) {
            StringBuilder sb = new StringBuilder();
            int indentLevel = 0;
            void Append(string text, bool newLine = true) {
                sb.Append(' ', indentLevel * indentLength);
                sb.Append(text);
                if (newLine) {
                    sb.Append("\n");
                }
            }

            Append("Build Report:");

            // Show summary
            indentLevel = 1;
            Append("summary:");
            indentLevel = 2;
            Append($"buildStartedAt: {buildReport.summary.buildStartedAt}");
            Append($"guid: {buildReport.summary.guid}");
            Append($"platform: {buildReport.summary.platform}");
            Append($"platformGroup: {buildReport.summary.platformGroup}");
            Append($"options: {buildReport.summary.options}");
            Append($"outputPath: {buildReport.summary.outputPath}");
            Append($"totalSize: {buildReport.summary.totalSize}");
            Append($"totalTime: {buildReport.summary.totalTime}");
            Append($"buildEndedAt: {buildReport.summary.buildEndedAt}");
            Append($"totalErrors: {buildReport.summary.totalErrors}");
            Append($"totalWarnings: {buildReport.summary.totalWarnings}");
            Append($"result: {buildReport.summary.result}");
            Append(string.Empty);

            // Show files, if any
            if (buildReport.files != null) {
                indentLevel = 1;
                Append("files:");
                foreach (var file in buildReport.files) {
                    indentLevel = 2;
                    Append($"id: {file.id}");
                    Append($"path: {file.path}");
                    Append($"role: {file.role}");
                    Append($"size: {file.size}");
                    Append(string.Empty);
                }
            }

            // Show steps
            indentLevel = 1;
            Append("steps:");
            foreach (var step in buildReport.steps) {
                indentLevel = 2;
                Append($"name: {step.name}");
                Append($"duration: {step.duration}");
                Append($"depth: {step.depth}");

                if (step.messages.Length > 0) {
                    Append("messages:");
                    foreach (var message in step.messages) {
                        indentLevel = 3;
                        Append($"type: {message.type}");
                        Append($"content: {message.content}");
                        Append(string.Empty);
                    }
                    Append(string.Empty);
                }
                Append(string.Empty);
            }

            // Show stripping info, if any
            if (buildReport.strippingInfo != null) {
                indentLevel = 1;
                Append("strippingInfo:");
                indentLevel = 2;
                Append("Included Modules:");
                foreach (var module in buildReport.strippingInfo.includedModules) {
                    indentLevel = 3;
                    Append($"{module}");
                }
            }

            // Show packed assets, if any
            if (buildReport.packedAssets != null) {
                indentLevel = 1;
                Append("packedAssets:");
                foreach (var packedAsset in buildReport.packedAssets) {
                    indentLevel = 2;
                    Append($"Short Path: {packedAsset.shortPath}");
                    Append($"Overhead: {packedAsset.overhead}");
                    Append($"Contents:");
                    foreach (var content in packedAsset.contents) {
                        indentLevel = 3;
                        Append($"id: {content.id}");
                        Append($"type: {content.type.FullName}");
                        Append($"packedSize: {content.packedSize}");
                        Append($"offset: {content.offset}");
                        Append($"sourceAssetGUID: {content.sourceAssetGUID}");
                        Append($"sourceAssetPath: {content.sourceAssetPath}\n");
                    }
                }
            }

            return sb.ToString();
        }
    }
}
