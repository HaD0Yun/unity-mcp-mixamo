using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace MixamoHelper
{
    /// <summary>
    /// Unity Editor Window for Mixamo MCP configuration.
    /// Provides one-click setup for MCP clients (Claude Desktop, Cursor, Windsurf).
    /// </summary>
    public class MixamoMcpWindow : EditorWindow
    {
        private const string GITHUB_RELEASE_URL = "https://github.com/HaD0Yun/unity-mcp-mixamo/releases/latest/download/mixamo-mcp.exe";
        private const string EXE_NAME = "mixamo-mcp.exe";
        
        private string _token = "";
        private string _exePath = "";
        private bool _isDownloading = false;
        private string _statusMessage = "";
        private MessageType _statusType = MessageType.None;
        
        // MCP Client paths
        private static string ClaudeConfigPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Claude", "claude_desktop_config.json");
        
        private static string CursorConfigPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".cursor", "mcp.json");
        
        private static string WindsurfConfigPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".codeium", "windsurf", "mcp_config.json");
        
        private static string TokenFilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".mixamo_mcp_token");
        
        private static string ExeInstallPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MixamoMCP", EXE_NAME);

        [MenuItem("Window/Mixamo MCP")]
        public static void ShowWindow()
        {
            var window = GetWindow<MixamoMcpWindow>("Mixamo MCP");
            window.minSize = new Vector2(450, 400);
        }

        private void OnEnable()
        {
            LoadToken();
            CheckExeInstalled();
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            
            // Title
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };
            GUILayout.Label("Mixamo MCP", titleStyle);
            GUILayout.Label("AI-powered Mixamo animation downloader", EditorStyles.centeredGreyMiniLabel);
            
            GUILayout.Space(20);

            // Status
            if (!string.IsNullOrEmpty(_statusMessage))
            {
                EditorGUILayout.HelpBox(_statusMessage, _statusType);
                GUILayout.Space(10);
            }

            // Exe Status
            DrawExeSection();
            
            GUILayout.Space(15);
            
            // MCP Clients
            DrawMcpClientsSection();
            
            GUILayout.Space(15);
            
            // Token
            DrawTokenSection();
            
            GUILayout.FlexibleSpace();
            
            // Help link
            if (GUILayout.Button("📖 Documentation", GUILayout.Height(25)))
            {
                Application.OpenURL("https://github.com/HaD0Yun/unity-mcp-mixamo");
            }
        }

        private void DrawExeSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("MCP Server", EditorStyles.boldLabel);
            
            bool exeExists = File.Exists(ExeInstallPath);
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(exeExists ? "✅ Installed" : "❌ Not Installed", 
                exeExists ? EditorStyles.label : EditorStyles.boldLabel);
            
            GUI.enabled = !_isDownloading;
            if (GUILayout.Button(exeExists ? "Reinstall" : "Download & Install", GUILayout.Width(120)))
            {
                DownloadAndInstallExe();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            
            if (exeExists)
            {
                EditorGUILayout.SelectableLabel(ExeInstallPath, EditorStyles.miniLabel, GUILayout.Height(16));
            }
            
            if (_isDownloading)
            {
                EditorGUILayout.HelpBox("Downloading...", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawMcpClientsSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("MCP Clients", EditorStyles.boldLabel);
            GUILayout.Space(5);
            
            DrawClientRow("Claude Desktop", ClaudeConfigPath, IsClaudeInstalled());
            DrawClientRow("Cursor", CursorConfigPath, IsCursorInstalled());
            DrawClientRow("Windsurf", WindsurfConfigPath, IsWindsurfInstalled());
            
            EditorGUILayout.EndVertical();
        }

        private void DrawClientRow(string clientName, string configPath, bool isInstalled)
        {
            EditorGUILayout.BeginHorizontal();
            
            // Status icon
            string status = isInstalled ? "✅" : "⚪";
            string label = isInstalled ? $"{status} {clientName}" : $"{status} {clientName} (not detected)";
            GUILayout.Label(label, GUILayout.Width(200));
            
            GUI.enabled = isInstalled && File.Exists(ExeInstallPath);
            if (GUILayout.Button("Configure", GUILayout.Width(80)))
            {
                ConfigureClient(clientName, configPath);
            }
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawTokenSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Mixamo Token", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            _token = EditorGUILayout.PasswordField(_token);
            if (GUILayout.Button("Save", GUILayout.Width(50)))
            {
                SaveToken();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("How to get token?", EditorStyles.linkLabel))
            {
                ShowTokenHelp();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }

        private bool IsClaudeInstalled()
        {
            return Directory.Exists(Path.GetDirectoryName(ClaudeConfigPath));
        }

        private bool IsCursorInstalled()
        {
            return Directory.Exists(Path.GetDirectoryName(CursorConfigPath));
        }

        private bool IsWindsurfInstalled()
        {
            var parentDir = Path.GetDirectoryName(WindsurfConfigPath);
            return parentDir != null && Directory.Exists(Path.GetDirectoryName(parentDir));
        }

        private void ConfigureClient(string clientName, string configPath)
        {
            try
            {
                // Ensure directory exists
                var dir = Path.GetDirectoryName(configPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                // Read existing config or create new
                string configJson = "{}";
                if (File.Exists(configPath))
                {
                    configJson = File.ReadAllText(configPath);
                }

                // Parse and modify JSON (simple approach without external JSON library)
                string exePathEscaped = ExeInstallPath.Replace("\\", "\\\\");
                string mcpServerEntry = $"\"mixamo\": {{\"command\": \"{exePathEscaped}\"}}";

                if (configJson.Contains("\"mcpServers\""))
                {
                    // Add to existing mcpServers
                    if (configJson.Contains("\"mixamo\""))
                    {
                        // Already configured - update it
                        int mixamoStart = configJson.IndexOf("\"mixamo\"");
                        int braceStart = configJson.IndexOf("{", mixamoStart);
                        int braceEnd = FindMatchingBrace(configJson, braceStart);
                        if (braceEnd > braceStart)
                        {
                            configJson = configJson.Substring(0, mixamoStart) + 
                                        mcpServerEntry + 
                                        configJson.Substring(braceEnd + 1);
                        }
                    }
                    else
                    {
                        // Add new entry
                        int serversStart = configJson.IndexOf("\"mcpServers\"");
                        int braceStart = configJson.IndexOf("{", serversStart);
                        if (braceStart >= 0)
                        {
                            string before = configJson.Substring(0, braceStart + 1);
                            string after = configJson.Substring(braceStart + 1).TrimStart();
                            string separator = after.StartsWith("}") ? "" : ", ";
                            configJson = before + mcpServerEntry + separator + after;
                        }
                    }
                }
                else
                {
                    // Create new mcpServers section
                    if (configJson.Trim() == "{}" || string.IsNullOrWhiteSpace(configJson))
                    {
                        configJson = $"{{\n  \"mcpServers\": {{\n    {mcpServerEntry}\n  }}\n}}";
                    }
                    else
                    {
                        // Add to existing config
                        int lastBrace = configJson.LastIndexOf("}");
                        if (lastBrace > 0)
                        {
                            string before = configJson.Substring(0, lastBrace).TrimEnd();
                            if (!before.EndsWith(",") && !before.EndsWith("{"))
                            {
                                before += ",";
                            }
                            configJson = before + $"\n  \"mcpServers\": {{\n    {mcpServerEntry}\n  }}\n}}";
                        }
                    }
                }

                File.WriteAllText(configPath, configJson);
                
                SetStatus($"✅ {clientName} configured successfully!\nPlease restart {clientName}.", MessageType.Info);
                Debug.Log($"[Mixamo MCP] {clientName} configured: {configPath}");
            }
            catch (Exception e)
            {
                SetStatus($"❌ Failed to configure {clientName}: {e.Message}", MessageType.Error);
                Debug.LogError($"[Mixamo MCP] Failed to configure {clientName}: {e}");
            }
        }

        private int FindMatchingBrace(string json, int start)
        {
            int depth = 0;
            for (int i = start; i < json.Length; i++)
            {
                if (json[i] == '{') depth++;
                else if (json[i] == '}') depth--;
                
                if (depth == 0) return i;
            }
            return -1;
        }

        private async void DownloadAndInstallExe()
        {
            _isDownloading = true;
            SetStatus("Downloading mixamo-mcp.exe...", MessageType.Info);
            Repaint();

            try
            {
                // Ensure directory exists
                var dir = Path.GetDirectoryName(ExeInstallPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMinutes(5);
                    var response = await client.GetAsync(GITHUB_RELEASE_URL);
                    response.EnsureSuccessStatusCode();
                    
                    var bytes = await response.Content.ReadAsByteArrayAsync();
                    File.WriteAllBytes(ExeInstallPath, bytes);
                }

                SetStatus("✅ Downloaded and installed successfully!", MessageType.Info);
                Debug.Log($"[Mixamo MCP] Installed to: {ExeInstallPath}");
            }
            catch (Exception e)
            {
                SetStatus($"❌ Download failed: {e.Message}", MessageType.Error);
                Debug.LogError($"[Mixamo MCP] Download failed: {e}");
            }
            finally
            {
                _isDownloading = false;
                Repaint();
            }
        }

        private void LoadToken()
        {
            if (File.Exists(TokenFilePath))
            {
                try
                {
                    _token = File.ReadAllText(TokenFilePath).Trim();
                }
                catch { }
            }
        }

        private void SaveToken()
        {
            try
            {
                File.WriteAllText(TokenFilePath, _token.Trim());
                SetStatus("✅ Token saved!", MessageType.Info);
            }
            catch (Exception e)
            {
                SetStatus($"❌ Failed to save token: {e.Message}", MessageType.Error);
            }
        }

        private void CheckExeInstalled()
        {
            _exePath = File.Exists(ExeInstallPath) ? ExeInstallPath : "";
        }

        private void ShowTokenHelp()
        {
            bool openBrowser = EditorUtility.DisplayDialog(
                "How to get Mixamo Token",
                "1. Go to mixamo.com and log in\n" +
                "2. Press F12 to open Developer Tools\n" +
                "3. Go to Console tab\n" +
                "4. Type: copy(localStorage.access_token)\n" +
                "5. Press Enter\n" +
                "6. Token is now in your clipboard!\n\n" +
                "Open Mixamo website now?",
                "Open Mixamo", "Cancel");

            if (openBrowser)
            {
                Application.OpenURL("https://www.mixamo.com");
            }
        }

        private void SetStatus(string message, MessageType type)
        {
            _statusMessage = message;
            _statusType = type;
        }
    }
}
