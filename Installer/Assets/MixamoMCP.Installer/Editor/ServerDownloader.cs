#if UNITY_EDITOR
using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

namespace MixamoMCP.Installer
{
    /// <summary>
    /// Downloads and manages the Mixamo MCP server executable.
    /// Auto-downloads platform-specific binary from GitHub Releases.
    /// </summary>
    public static class ServerDownloader
    {
        private const string GitHubRepo = "HaD0Yun/unity-mcp-mixamo";
        
        private static string DownloadUrl => 
            $"https://github.com/{GitHubRepo}/releases/latest/download/{GetPlatformFileName()}";

        private static string ServerFolderPath => 
            Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Library", "mixamo-mcp-server");

        private static string ServerExecutablePath => 
            Path.Combine(ServerFolderPath, GetPlatformFileName());

        private static string VersionFilePath => 
            Path.Combine(ServerFolderPath, "version");

        public static string GetServerPath() => ServerExecutablePath;

        private static string GetPlatformFileName()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "mixamo-mcp.exe";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return "mixamo-mcp-macos";
            else
                return "mixamo-mcp-linux";
        }

        public static bool IsServerInstalled()
        {
            return File.Exists(ServerExecutablePath);
        }

        public static bool IsVersionMatch()
        {
            if (!File.Exists(VersionFilePath))
                return false;

            string installedVersion = File.ReadAllText(VersionFilePath).Trim();
            return installedVersion == Installer.Version;
        }

        public static void DownloadIfNeeded()
        {
            if (IsServerInstalled() && IsVersionMatch())
            {
                Debug.Log($"[{Installer.PackageName}] Server already installed (v{Installer.Version})");
                return;
            }

            EditorApplication.delayCall += DownloadServerAsync;
        }

        public static void ForceRedownload()
        {
            if (Directory.Exists(ServerFolderPath))
            {
                try
                {
                    Directory.Delete(ServerFolderPath, true);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[{Installer.PackageName}] Could not delete server folder: {ex.Message}");
                }
            }
        }

        private static async void DownloadServerAsync()
        {
            Debug.Log($"[{Installer.PackageName}] Downloading MCP server from GitHub...");
            Debug.Log($"[{Installer.PackageName}] URL: {DownloadUrl}");

            try
            {
                // Create directory
                if (!Directory.Exists(ServerFolderPath))
                    Directory.CreateDirectory(ServerFolderPath);

                // Download file
                using (var client = new WebClient())
                {
                    client.Headers.Add("User-Agent", "Unity-MixamoMCP-Installer");
                    
                    var progressId = Progress.Start("Downloading Mixamo MCP Server", null, Progress.Options.Managed);
                    
                    client.DownloadProgressChanged += (sender, e) =>
                    {
                        Progress.Report(progressId, e.ProgressPercentage / 100f, 
                            $"Downloading... {e.BytesReceived / 1024}KB / {e.TotalBytesToReceive / 1024}KB");
                    };

                    try
                    {
                        await client.DownloadFileTaskAsync(new Uri(DownloadUrl), ServerExecutablePath);
                        Progress.Finish(progressId, Progress.Status.Succeeded);
                    }
                    catch (Exception ex)
                    {
                        Progress.Finish(progressId, Progress.Status.Failed);
                        throw ex;
                    }
                }

                // Set executable permissions on Unix
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    SetExecutablePermission(ServerExecutablePath);
                }

                // Write version file
                File.WriteAllText(VersionFilePath, Installer.Version);

                Debug.Log($"[{Installer.PackageName}] Server downloaded successfully!");
                Debug.Log($"[{Installer.PackageName}] Location: {ServerExecutablePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{Installer.PackageName}] Failed to download server: {ex.Message}");
                Debug.LogError($"[{Installer.PackageName}] Please download manually from: https://github.com/{GitHubRepo}/releases");
            }
        }

        private static void SetExecutablePermission(string path)
        {
            try
            {
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "chmod",
                        Arguments = $"+x \"{path}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                process.WaitForExit();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{Installer.PackageName}] Could not set executable permission: {ex.Message}");
            }
        }
    }
}
#endif
