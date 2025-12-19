#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace MixamoMcp.Editor
{
    /// <summary>
    /// Auto-installer that runs on first package import via [InitializeOnLoad].
    /// Downloads MCP server and configures AI tools automatically.
    /// </summary>
    [InitializeOnLoad]
    public static class MixamoMcpInstaller
    {
        private const string INSTALLED_KEY = "MixamoMcp_Installed_v4";
        private const string GITHUB_RELEASE_URL = "https://github.com/HaD0Yun/unity-mcp-mixamo/releases/latest/download/mixamo-mcp.exe";
        
        private static string ExeInstallPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MixamoMCP", "mixamo-mcp.exe");
        
        private static string ClaudeConfigPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Claude", "claude_desktop_config.json");

        static MixamoMcpInstaller()
        {
            // Check if already installed this version
            if (EditorPrefs.GetBool(INSTALLED_KEY, false))
                return;

            // Delay to ensure Unity is fully loaded
            EditorApplication.delayCall += RunAutoInstall;
        }

        private static void RunAutoInstall()
        {
            Debug.Log("[Mixamo MCP] Auto-installer activated");
            
            // Mark as installed to prevent running again
            EditorPrefs.SetBool(INSTALLED_KEY, true);
            
            // Check if exe already exists
            if (File.Exists(ExeInstallPath))
            {
                Debug.Log("[Mixamo MCP] Server already installed: " + ExeInstallPath);
                ShowWelcomeDialog(alreadyInstalled: true);
                return;
            }

            // Show install dialog
            bool install = EditorUtility.DisplayDialog(
                "Mixamo MCP Setup",
                "Welcome to Mixamo MCP!\n\n" +
                "This will:\n" +
                "• Download MCP server (~5MB)\n" +
                "• Configure Claude Desktop automatically\n\n" +
                "Install now?",
                "Install", "Later");

            if (install)
            {
                DownloadAndInstall();
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Manual Setup",
                    "You can install later via:\nWindow > Mixamo MCP",
                    "OK");
            }
        }

        private static async void DownloadAndInstall()
        {
            try
            {
                // Create directory
                var dir = Path.GetDirectoryName(ExeInstallPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                // Download with progress
                var progressId = Progress.Start("Downloading Mixamo MCP", null, Progress.Options.Managed);
                
                using (var request = UnityWebRequest.Get(GITHUB_RELEASE_URL))
                {
                    var operation = request.SendWebRequest();
                    
                    while (!operation.isDone)
                    {
                        Progress.Report(progressId, request.downloadProgress, "Downloading server...");
                        await System.Threading.Tasks.Task.Delay(100);
                    }

#if UNITY_2020_1_OR_NEWER
                    if (request.result == UnityWebRequest.Result.Success)
#else
                    if (!request.isNetworkError && !request.isHttpError)
#endif
                    {
                        File.WriteAllBytes(ExeInstallPath, request.downloadHandler.data);
                        Progress.Finish(progressId, Progress.Status.Succeeded);
                        
                        Debug.Log("[Mixamo MCP] Server installed: " + ExeInstallPath);
                        
                        // Auto-configure Claude Desktop
                        ConfigureClaudeDesktop();
                        
                        ShowWelcomeDialog(alreadyInstalled: false);
                    }
                    else
                    {
                        Progress.Finish(progressId, Progress.Status.Failed);
                        Debug.LogError("[Mixamo MCP] Download failed: " + request.error);
                        
                        EditorUtility.DisplayDialog(
                            "Download Failed",
                            "Could not download MCP server.\n\n" +
                            "Please try manually via:\nWindow > Mixamo MCP",
                            "OK");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[Mixamo MCP] Installation error: " + e.Message);
            }
        }

        private static void ConfigureClaudeDesktop()
        {
            try
            {
                var dir = Path.GetDirectoryName(ClaudeConfigPath);
                if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
                {
                    Debug.Log("[Mixamo MCP] Claude Desktop not detected, skipping auto-config");
                    return;
                }

                string configJson = "{}";
                if (File.Exists(ClaudeConfigPath))
                    configJson = File.ReadAllText(ClaudeConfigPath);

                string exePathEscaped = ExeInstallPath.Replace("\\", "\\\\");
                string mcpEntry = $"\"mixamo\": {{\"command\": \"{exePathEscaped}\"}}";

                if (configJson.Contains("\"mcpServers\""))
                {
                    if (!configJson.Contains("\"mixamo\""))
                    {
                        int idx = configJson.IndexOf("\"mcpServers\"");
                        int braceIdx = configJson.IndexOf("{", idx);
                        if (braceIdx >= 0)
                        {
                            string before = configJson.Substring(0, braceIdx + 1);
                            string after = configJson.Substring(braceIdx + 1).TrimStart();
                            string sep = after.StartsWith("}") ? "" : ", ";
                            configJson = before + mcpEntry + sep + after;
                        }
                    }
                }
                else
                {
                    configJson = "{\n  \"mcpServers\": {\n    " + mcpEntry + "\n  }\n}";
                }

                File.WriteAllText(ClaudeConfigPath, configJson);
                Debug.Log("[Mixamo MCP] Claude Desktop configured: " + ClaudeConfigPath);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Mixamo MCP] Could not configure Claude Desktop: " + e.Message);
            }
        }

        private static void ShowWelcomeDialog(bool alreadyInstalled)
        {
            string message = alreadyInstalled
                ? "Mixamo MCP is ready!\n\n" +
                  "Restart Claude Desktop to use MCP tools.\n\n" +
                  "Open settings window?"
                : "Installation complete!\n\n" +
                  "• Server: Installed\n" +
                  "• Claude Desktop: Configured\n\n" +
                  "Please restart Claude Desktop.\n\n" +
                  "Open settings window?";

            if (EditorUtility.DisplayDialog("Mixamo MCP", message, "Open Settings", "Close"))
            {
                MixamoMcpWindow.ShowWindow();
            }
        }

        [MenuItem("Window/Mixamo MCP/Reset Installation", false, 200)]
        public static void ResetInstallation()
        {
            EditorPrefs.DeleteKey(INSTALLED_KEY);
            Debug.Log("[Mixamo MCP] Installation reset. Reimport package to trigger installer.");
        }
    }
}
#endif
