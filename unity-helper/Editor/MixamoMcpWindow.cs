#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace MixamoMcp.Editor
{
    public class MixamoMcpWindow : EditorWindow
    {
        private const string GITHUB_RELEASE_URL = "https://github.com/HaD0Yun/unity-mcp-mixamo/releases/latest/download/mixamo-mcp.exe";
        private const string EXE_NAME = "mixamo-mcp.exe";
        
        private string _token = "";
        private bool _isDownloading = false;
        private float _downloadProgress = 0f;
        private string _statusMessage = "";
        private MessageType _statusType = MessageType.None;
        private UnityWebRequest _downloadRequest;
        
        private static string TokenFilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".mixamo_mcp_token");
        
        private static string ExeInstallPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MixamoMCP", EXE_NAME);

        [MenuItem("Window/Mixamo MCP/Settings", priority = 1)]
        public static void ShowWindow()
        {
            var window = GetWindow<MixamoMcpWindow>("Mixamo MCP");
            window.minSize = new Vector2(450, 400);
            window.Focus();
        }

        private void OnEnable()
        {
            LoadToken();
        }

        private void OnDisable()
        {
            if (_downloadRequest != null)
            {
                _downloadRequest.Dispose();
                _downloadRequest = null;
            }
            EditorApplication.update -= UpdateDownload;
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };
            GUILayout.Label("Mixamo MCP", titleStyle);
            GUILayout.Label("AI-powered Mixamo animation downloader", EditorStyles.centeredGreyMiniLabel);
            
            GUILayout.Space(20);

            if (!string.IsNullOrEmpty(_statusMessage))
            {
                EditorGUILayout.HelpBox(_statusMessage, _statusType);
                GUILayout.Space(10);
            }

            DrawExeSection();
            GUILayout.Space(15);
            DrawConfigurationSection();
            GUILayout.Space(15);
            DrawMcpClientsSection();
            GUILayout.Space(15);
            DrawTokenSection();
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Documentation", GUILayout.Height(25)))
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
            GUILayout.Label(exeExists ? "Installed" : "Not Installed", 
                exeExists ? EditorStyles.label : EditorStyles.boldLabel);
            
            GUI.enabled = !_isDownloading;
            if (GUILayout.Button(exeExists ? "Reinstall" : "Download & Install", GUILayout.Width(120)))
            {
                StartDownload();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            
            if (exeExists)
            {
                EditorGUILayout.SelectableLabel(ExeInstallPath, EditorStyles.miniLabel, GUILayout.Height(16));
            }
            
            if (_isDownloading)
            {
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(GUILayout.Height(20)), _downloadProgress, "Downloading...");
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawConfigurationSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Configuration:", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Copy", GUILayout.Width(50)))
            {
                string config = GetConfigJson();
                EditorGUIUtility.systemCopyBuffer = config;
                SetStatus("Configuration copied to clipboard!", MessageType.Info);
            }
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            string configJson = GetConfigJson();
            var textStyle = new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = true,
                fontSize = 11,
                padding = new RectOffset(8, 8, 8, 8)
            };
            
            float height = textStyle.CalcHeight(new GUIContent(configJson), EditorGUIUtility.currentViewWidth - 40);
            EditorGUILayout.SelectableLabel(configJson, textStyle, GUILayout.Height(Mathf.Max(80, height + 10)));
            
            EditorGUILayout.EndVertical();
        }
        
        private string GetConfigJson()
        {
            string exePath = ExeInstallPath.Replace("\\", "\\\\");
            return "{\n" +
                   "  \"mcpServers\": {\n" +
                   "    \"mixamo\": {\n" +
                   "      \"command\": \"" + exePath + "\"\n" +
                   "    }\n" +
                   "  }\n" +
                   "}";
        }

        private void DrawMcpClientsSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("MCP Clients", EditorStyles.boldLabel);
            GUILayout.Space(5);
            
            EditorGUILayout.BeginHorizontal();
            
            bool isServerRunning = IsMcpServerRunning();
            var statusColor = isServerRunning ? Color.green : Color.yellow;
            string statusText = isServerRunning ? "CONNECTED" : "DISCONNECTED";
            
            var prevColor = GUI.color;
            GUI.color = statusColor;
            GUILayout.Label("●", GUILayout.Width(15));
            GUI.color = prevColor;
            
            var statusStyle = new GUIStyle(EditorStyles.label);
            statusStyle.normal.textColor = statusColor;
            statusStyle.fontStyle = FontStyle.Bold;
            GUILayout.Label(statusText, statusStyle);
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        private bool IsMcpServerRunning()
        {
            try
            {
                var processes = System.Diagnostics.Process.GetProcessesByName("mixamo-mcp");
                return processes.Length > 0;
            }
            catch
            {
                return false;
            }
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

        private void StartDownload()
        {
            var dir = Path.GetDirectoryName(ExeInstallPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            _isDownloading = true;
            _downloadProgress = 0f;
            SetStatus("Starting download...", MessageType.Info);
            
            _downloadRequest = UnityWebRequest.Get(GITHUB_RELEASE_URL);
            _downloadRequest.SendWebRequest();
            
            EditorApplication.update += UpdateDownload;
        }

        private void UpdateDownload()
        {
            if (_downloadRequest == null)
            {
                EditorApplication.update -= UpdateDownload;
                return;
            }

            _downloadProgress = _downloadRequest.downloadProgress;
            Repaint();

            if (!_downloadRequest.isDone)
            {
                return;
            }

            EditorApplication.update -= UpdateDownload;
            _isDownloading = false;

#if UNITY_2020_1_OR_NEWER
            if (_downloadRequest.result == UnityWebRequest.Result.Success)
#else
            if (!_downloadRequest.isNetworkError && !_downloadRequest.isHttpError)
#endif
            {
                try
                {
                    File.WriteAllBytes(ExeInstallPath, _downloadRequest.downloadHandler.data);
                    SetStatus("Downloaded and installed successfully!", MessageType.Info);
                    Debug.Log("[Mixamo MCP] Installed to: " + ExeInstallPath);
                }
                catch (Exception e)
                {
                    SetStatus("Failed to save file: " + e.Message, MessageType.Error);
                }
            }
            else
            {
                SetStatus("Download failed: " + _downloadRequest.error, MessageType.Error);
            }

            _downloadRequest.Dispose();
            _downloadRequest = null;
            Repaint();
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
                SetStatus("Token saved!", MessageType.Info);
            }
            catch (Exception e)
            {
                SetStatus("Failed to save token: " + e.Message, MessageType.Error);
            }
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
#endif
