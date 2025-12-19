#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace MixamoMCP.Installer
{
    /// <summary>
    /// Exports the installer as a .unitypackage file.
    /// Used by GitHub Actions for automated releases.
    /// </summary>
    public static class PackageExporter
    {
        private const string PackagePath = "Assets/MixamoMCP.Installer";
        private const string OutputPath = "build/MixamoMCP-Installer.unitypackage";

        [MenuItem("Window/Mixamo MCP/Export Installer Package", false, 200)]
        public static void ExportPackage()
        {
            Debug.Log("[Mixamo MCP] Exporting installer package...");

            string outputDir = Path.GetDirectoryName(OutputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            AssetDatabase.ExportPackage(
                PackagePath,
                OutputPath,
                ExportPackageOptions.Recurse
            );

            Debug.Log($"[Mixamo MCP] Package exported to: {Path.GetFullPath(OutputPath)}");
            EditorUtility.RevealInFinder(Path.GetFullPath(OutputPath));
        }

        /// <summary>
        /// Called by CI/CD pipeline to export the package.
        /// </summary>
        public static void ExportForCI()
        {
            Debug.Log("[Mixamo MCP] CI Export started...");
            
            if (!Directory.Exists("build"))
                Directory.CreateDirectory("build");

            AssetDatabase.ExportPackage(
                PackagePath,
                OutputPath,
                ExportPackageOptions.Recurse
            );

            Debug.Log($"[Mixamo MCP] CI Export completed: {OutputPath}");
        }
    }
}
#endif
