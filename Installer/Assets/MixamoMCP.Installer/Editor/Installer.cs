#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace MixamoMCP.Installer
{
    /// <summary>
    /// Main installer entry point. Automatically runs when Unity imports the package.
    /// Uses [InitializeOnLoad] to execute on Unity Editor startup.
    /// </summary>
    [InitializeOnLoad]
    public static partial class Installer
    {
        public const string PackageId = "com.mixamomcp.unity";
        public const string PackageName = "Mixamo MCP";
        public const string Version = "4.0.0";
        public const string GitUrl = "https://github.com/HaD0Yun/unity-mcp-mixamo.git?path=unity-helper";
        
        private static readonly string ManifestPath = Path.Combine(
            Directory.GetParent(Application.dataPath).FullName,
            "Packages",
            "manifest.json"
        );

        static Installer()
        {
#if !MIXAMO_MCP_INSTALLER_PROJECT
            // Only run installation logic when imported into a user's project
            RunInstallation();
#endif
        }

        private static void RunInstallation()
        {
            Debug.Log($"[{PackageName}] Installer activated - v{Version}");

            // Step 1: Add package dependency to manifest.json
            if (AddPackageDependencyIfNeeded(ManifestPath))
            {
                Debug.Log($"[{PackageName}] Added package dependency to manifest.json");
            }

            // Step 2: Download MCP server binary
            ServerDownloader.DownloadIfNeeded();

            // Step 3: Show configuration window
            EditorApplication.delayCall += () =>
            {
                if (!SessionState.GetBool("MixamoMCP_InstallerShown", false))
                {
                    SessionState.SetBool("MixamoMCP_InstallerShown", true);
                    // Open Mixamo MCP window from unity-helper package
                    EditorApplication.ExecuteMenuItem("Window/Mixamo MCP");
                }
            };
        }

        [MenuItem("Window/Mixamo MCP/Reinstall", false, 100)]
        public static void Reinstall()
        {
            Debug.Log($"[{PackageName}] Running reinstallation...");
            SessionState.SetBool("MixamoMCP_InstallerShown", false);
            ServerDownloader.ForceRedownload();
            RunInstallation();
        }
    }
}
#endif
