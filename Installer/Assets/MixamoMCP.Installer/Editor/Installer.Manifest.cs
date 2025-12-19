#if UNITY_EDITOR
using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace MixamoMCP.Installer
{
    /// <summary>
    /// Handles manifest.json manipulation for package installation.
    /// </summary>
    public static partial class Installer
    {
        /// <summary>
        /// Adds the Mixamo MCP Unity Helper package dependency to manifest.json
        /// </summary>
        public static bool AddPackageDependencyIfNeeded(string manifestPath)
        {
            if (!File.Exists(manifestPath))
            {
                Debug.LogWarning($"[{PackageName}] manifest.json not found at: {manifestPath}");
                return false;
            }

            try
            {
                string jsonText = File.ReadAllText(manifestPath);

                // Check if our package is already installed
                if (jsonText.Contains(PackageId) || jsonText.Contains("unity-mcp-mixamo"))
                {
                    Debug.Log($"[{PackageName}] Package already in manifest.json");
                    return false;
                }

                // Find the "dependencies" section and add our package
                string dependenciesPattern = @"(""dependencies""\s*:\s*\{)";
                if (!Regex.IsMatch(jsonText, dependenciesPattern))
                {
                    Debug.LogWarning($"[{PackageName}] Could not find dependencies section in manifest.json");
                    return false;
                }

                // Add our package as the first dependency
                string newDependency = $"\n    \"{PackageId}\": \"{GitUrl}\",";
                string modifiedJson = Regex.Replace(
                    jsonText,
                    dependenciesPattern,
                    $"$1{newDependency}"
                );

                // Write back
                File.WriteAllText(manifestPath, modifiedJson);
                Debug.Log($"[{PackageName}] Successfully added package to manifest.json");

                // Trigger Unity to refresh packages
                UnityEditor.PackageManager.Client.Resolve();

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{PackageName}] Failed to modify manifest.json: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Removes our package from manifest.json
        /// </summary>
        public static bool RemovePackageDependency(string manifestPath)
        {
            if (!File.Exists(manifestPath))
                return false;

            try
            {
                string jsonText = File.ReadAllText(manifestPath);

                // Remove our package line
                string pattern = $@"\s*""{PackageId}""\s*:\s*""[^""]*"",?";
                string modifiedJson = Regex.Replace(jsonText, pattern, "");

                // Clean up any double commas or trailing commas before }
                modifiedJson = Regex.Replace(modifiedJson, @",(\s*[}\]])", "$1");
                modifiedJson = Regex.Replace(modifiedJson, @",\s*,", ",");

                File.WriteAllText(manifestPath, modifiedJson);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{PackageName}] Failed to remove package: {ex.Message}");
                return false;
            }
        }
    }
}
#endif
