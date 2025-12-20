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

                string exePathEscaped = ExeInstallPath.Replace("\\", "\\\\");
                
                // Parse existing config or create new one
                ClaudeConfig config;
                if (File.Exists(ClaudeConfigPath))
                {
                    string existingJson = File.ReadAllText(ClaudeConfigPath);
                    config = ParseClaudeConfig(existingJson);
                }
                else
                {
                    config = new ClaudeConfig();
                }
                
                // Add or update mixamo server entry
                config.mcpServers["mixamo"] = new McpServerConfig { command = exePathEscaped };
                
                // Serialize back to JSON with proper formatting
                string newJson = SerializeClaudeConfig(config);
                File.WriteAllText(ClaudeConfigPath, newJson);
                
                Debug.Log("[Mixamo MCP] Claude Desktop configured: " + ClaudeConfigPath);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Mixamo MCP] Could not configure Claude Desktop: " + e.Message);
            }
        }
        
        /// <summary>
        /// Simple JSON config classes for Claude Desktop configuration.
        /// </summary>
        [Serializable]
        private class McpServerConfig
        {
            public string command;
            public string[] args;
        }
        
        private class ClaudeConfig
        {
            public System.Collections.Generic.Dictionary<string, McpServerConfig> mcpServers = 
                new System.Collections.Generic.Dictionary<string, McpServerConfig>();
        }
        
        /// <summary>
        /// Parse Claude Desktop config JSON into ClaudeConfig object.
        /// Uses simple regex-based parsing since Unity's JsonUtility doesn't support Dictionary.
        /// </summary>
        private static ClaudeConfig ParseClaudeConfig(string json)
        {
            var config = new ClaudeConfig();
            
            try
            {
                // Find mcpServers block
                var serversMatch = System.Text.RegularExpressions.Regex.Match(
                    json, 
                    @"""mcpServers""\s*:\s*\{([^{}]*(?:\{[^{}]*\}[^{}]*)*)\}",
                    System.Text.RegularExpressions.RegexOptions.Singleline);
                
                if (serversMatch.Success)
                {
                    string serversContent = serversMatch.Groups[1].Value;
                    
                    // Parse each server entry: "name": {"command": "...", "args": [...]}
                    var serverPattern = new System.Text.RegularExpressions.Regex(
                        @"""([^""]+)""\s*:\s*\{([^{}]*)\}",
                        System.Text.RegularExpressions.RegexOptions.Singleline);
                    
                    foreach (System.Text.RegularExpressions.Match serverMatch in serverPattern.Matches(serversContent))
                    {
                        string serverName = serverMatch.Groups[1].Value;
                        string serverBody = serverMatch.Groups[2].Value;
                        
                        var serverConfig = new McpServerConfig();
                        
                        // Extract command
                        var cmdMatch = System.Text.RegularExpressions.Regex.Match(
                            serverBody, @"""command""\s*:\s*""([^""\\]*(?:\\.[^""\\]*)*)""");
                        if (cmdMatch.Success)
                            serverConfig.command = cmdMatch.Groups[1].Value;
                        
                        // Extract args array
                        var argsMatch = System.Text.RegularExpressions.Regex.Match(
                            serverBody, @"""args""\s*:\s*\[(.*?)\]",
                            System.Text.RegularExpressions.RegexOptions.Singleline);
                        if (argsMatch.Success)
                        {
                            var argStrings = new System.Collections.Generic.List<string>();
                            var argPattern = new System.Text.RegularExpressions.Regex(@"""([^""\\]*(?:\\.[^""\\]*)*)""");
                            foreach (System.Text.RegularExpressions.Match argMatch in argPattern.Matches(argsMatch.Groups[1].Value))
                            {
                                argStrings.Add(argMatch.Groups[1].Value);
                            }
                            serverConfig.args = argStrings.ToArray();
                        }
                        
                        config.mcpServers[serverName] = serverConfig;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Mixamo MCP] Error parsing existing config, creating new: " + e.Message);
            }
            
            return config;
        }
        
        /// <summary>
        /// Serialize ClaudeConfig to JSON string with proper formatting.
        /// </summary>
        private static string SerializeClaudeConfig(ClaudeConfig config)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("  \"mcpServers\": {");
            
            var serverEntries = new System.Collections.Generic.List<string>();
            foreach (var kvp in config.mcpServers)
            {
                var entry = new System.Text.StringBuilder();
                entry.Append($"    \"{kvp.Key}\": {{");
                entry.Append($"\"command\": \"{kvp.Value.command}\"");
                
                if (kvp.Value.args != null && kvp.Value.args.Length > 0)
                {
                    entry.Append(", \"args\": [");
                    for (int i = 0; i < kvp.Value.args.Length; i++)
                    {
                        if (i > 0) entry.Append(", ");
                        entry.Append($"\"{kvp.Value.args[i]}\"");
                    }
                    entry.Append("]");
                }
                
                entry.Append("}");
                serverEntries.Add(entry.ToString());
            }
            
            sb.AppendLine(string.Join(",\n", serverEntries));
            sb.AppendLine("  }");
            sb.Append("}");
            
            return sb.ToString();
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
