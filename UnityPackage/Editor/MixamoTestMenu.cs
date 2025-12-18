/*
 * MixamoTestMenu.cs
 * 
 * Unity Editor menu for testing Mixamo MCP tools manually.
 * Provides GUI-based testing of all Mixamo functionality.
 * 
 * Menu Location: Tools/Mixamo/*
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace MCPForUnity.Editor.Mixamo
{
    /// <summary>
    /// Editor menu for testing Mixamo tools
    /// </summary>
    public static class MixamoTestMenu
    {
        // ===== 1. Check Token Status =====
        [MenuItem("Tools/Mixamo/1. Check Token")]
        public static async void CheckToken()
        {
            if (!MixamoClient.Instance.HasToken)
            {
                EditorUtility.DisplayDialog(
                    "No Token", 
                    "No Mixamo token stored.\n\n" +
                    "To authenticate:\n" +
                    "1. Go to mixamo.com and log in\n" +
                    "2. Press F12 -> Console\n" +
                    "3. Type: localStorage.access_token\n" +
                    "4. Copy the token and use 'Set Token' menu", 
                    "OK"
                );
                return;
            }
            
            EditorUtility.DisplayProgressBar("Mixamo", "Validating token...", 0.5f);
            
            try
            {
                var valid = await MixamoClient.Instance.ValidateTokenAsync();
                EditorUtility.ClearProgressBar();
                
                if (valid)
                {
                    EditorUtility.DisplayDialog("Token Valid", "Your stored token is valid!", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Token Expired", "Token is expired. Please set a new token.", "OK");
                }
            }
            catch (System.Exception ex)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Error", $"Token validation failed: {ex.Message}", "OK");
            }
        }
        
        // ===== 2. Set Token =====
        [MenuItem("Tools/Mixamo/2. Set Token")]
        public static async void SetToken()
        {
            var token = EditorInputDialog.Show("Set Mixamo Token", "Enter your access_token:", "");
            
            if (string.IsNullOrEmpty(token)) return;
            
            EditorUtility.DisplayProgressBar("Mixamo", "Validating token...", 0.5f);
            
            try
            {
                var valid = await MixamoClient.Instance.ValidateTokenAsync(token);
                EditorUtility.ClearProgressBar();
                
                if (valid)
                {
                    MixamoClient.Instance.StoreToken(token);
                    EditorUtility.DisplayDialog("Success", "Token saved successfully!", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Failed", "Invalid token provided.", "OK");
                }
            }
            catch (System.Exception ex)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Error", $"Token validation failed: {ex.Message}", "OK");
            }
        }
        
        // ===== 3. Search Animations =====
        [MenuItem("Tools/Mixamo/3. Search Animations")]
        public static async void SearchAnimations()
        {
            if (!MixamoClient.Instance.HasToken)
            {
                EditorUtility.DisplayDialog("No Token", "Please set a token first.", "OK");
                return;
            }
            
            var keyword = EditorInputDialog.Show("Search Animations", "Enter search keyword:", "idle");
            
            if (string.IsNullOrEmpty(keyword)) return;
            
            EditorUtility.DisplayProgressBar("Mixamo", $"Searching '{keyword}'...", 0.5f);
            
            try
            {
                var query = MixamoKeywords.GetPrimaryQuery(keyword);
                var results = await MixamoClient.Instance.SearchAnimationsAsync(query, 10);
                
                EditorUtility.ClearProgressBar();
                
                if (results.results == null || results.results.Length == 0)
                {
                    EditorUtility.DisplayDialog("No Results", $"No animations found for '{keyword}'.", "OK");
                    return;
                }
                
                var message = $"Results for '{keyword}': {results.pagination.num_results} found\n\n";
                for (int i = 0; i < System.Math.Min(5, results.results.Length); i++)
                {
                    var anim = results.results[i];
                    message += $"- {anim.name ?? anim.description}\n";
                }
                
                if (results.results.Length > 5)
                {
                    message += $"\n...and {results.results.Length - 5} more";
                }
                
                EditorUtility.DisplayDialog("Search Results", message, "OK");
                Debug.Log($"[Mixamo] Search Results:\n{message}");
            }
            catch (System.Exception ex)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Error", $"Search failed: {ex.Message}", "OK");
            }
        }
        
        // ===== 4. Batch Download =====
        [MenuItem("Tools/Mixamo/4. Batch Download")]
        public static async void BatchDownload()
        {
            if (!MixamoClient.Instance.HasToken)
            {
                EditorUtility.DisplayDialog("No Token", "Please set a token first.", "OK");
                return;
            }
            
            var charName = EditorInputDialog.Show("Character Name", "Enter character/folder name:", "MyCharacter");
            if (string.IsNullOrEmpty(charName)) return;
            
            var anims = EditorInputDialog.Show(
                "Animations", 
                "Enter animations (comma-separated):",
                "idle, walk, run"
            );
            if (string.IsNullOrEmpty(anims)) return;
            
            var keywords = MixamoKeywords.ParseKeywordString(anims);
            if (keywords.Count == 0)
            {
                EditorUtility.DisplayDialog("Error", "No valid animation keywords.", "OK");
                return;
            }
            
            Debug.Log($"[Mixamo] Starting download: {charName} - {string.Join(", ", keywords)}");
            
            int success = 0;
            int failed = 0;
            
            for (int i = 0; i < keywords.Count; i++)
            {
                var keyword = keywords[i];
                EditorUtility.DisplayProgressBar(
                    "Mixamo Download", 
                    $"({i + 1}/{keywords.Count}) Processing {keyword}...", 
                    (float)i / keywords.Count
                );
                
                try
                {
                    // Search
                    var query = MixamoKeywords.GetPrimaryQuery(keyword);
                    var results = await MixamoClient.Instance.SearchAnimationsAsync(query, 1);
                    
                    if (results.results == null || results.results.Length == 0)
                    {
                        Debug.LogWarning($"[Mixamo] '{keyword}' not found");
                        failed++;
                        continue;
                    }
                    
                    var anim = results.results[0];
                    Debug.Log($"[Mixamo] '{keyword}' -> '{anim.name ?? anim.description}'");
                    
                    // Get details
                    var product = await MixamoClient.Instance.GetProductAsync(anim.id);
                    
                    // Export request
                    var exportRequest = new MixamoExportRequest
                    {
                        character_id = MixamoConstants.DEFAULT_CHARACTER_ID,
                        product_name = MixamoKeywords.GetStateName(keyword),
                        gms_hash = new List<MixamoExportGmsHash>
                        {
                            MixamoExportGmsHash.FromGmsHash(product.details.gms_hash)
                        }
                    };
                    
                    var exportResponse = await MixamoClient.Instance.ExportAnimationAsync(exportRequest);
                    
                    // Monitor
                    var downloadUrl = await MixamoClient.Instance.MonitorExportAsync(
                        MixamoConstants.DEFAULT_CHARACTER_ID
                    );
                    
                    // Download
                    var savePath = $"Assets/Animations/{charName}";
                    var fileName = $"{charName}_{MixamoKeywords.GetStateName(keyword)}";
                    var assetPath = await MixamoClient.Instance.DownloadFbxAsync(downloadUrl, savePath, fileName);
                    
                    Debug.Log($"[Mixamo] '{keyword}' downloaded: {assetPath}");
                    success++;
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Mixamo] '{keyword}' failed: {ex.Message}");
                    failed++;
                }
            }
            
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
            
            // Configure imports
            var animFolder = $"Assets/Animations/{charName}";
            if (Directory.Exists(animFolder.Replace("Assets", Application.dataPath)))
            {
                var fbxFiles = AssetDatabase.FindAssets("t:Model", new[] { animFolder });
                foreach (var guid in fbxFiles)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    AnimatorBuilder.ConfigureHumanoidImport(path);
                }
                
                // Create Animator
                AnimatorBuilder.CreateAnimatorController(animFolder, "Idle");
            }
            
            EditorUtility.DisplayDialog(
                "Download Complete",
                $"Success: {success}\nFailed: {failed}\n\nSaved to: Assets/Animations/{charName}",
                "OK"
            );
        }
        
        // ===== 5. Clear Token =====
        [MenuItem("Tools/Mixamo/5. Clear Token")]
        public static void ClearToken()
        {
            if (EditorUtility.DisplayDialog("Confirm", "Clear stored token?", "Clear", "Cancel"))
            {
                MixamoClient.Instance.ClearToken();
                EditorUtility.DisplayDialog("Done", "Token cleared.", "OK");
            }
        }
        
        // ===== 6. Show Keywords =====
        [MenuItem("Tools/Mixamo/6. Show Keywords")]
        public static void ShowKeywords()
        {
            var keywords = MixamoKeywords.GetAllKeywords();
            var message = "Available Keywords:\n\n";
            
            foreach (var kw in keywords.OrderBy(k => k).Take(20))
            {
                var stateType = MixamoKeywords.GetStateType(kw);
                message += $"- {kw} [{stateType}]\n";
            }
            
            if (keywords.Length > 20)
            {
                message += $"\n...and {keywords.Length - 20} more\n";
            }
            
            message += "\nSee console for full list.";
            
            Debug.Log($"[Mixamo] All Keywords:\n{string.Join(", ", keywords)}");
            EditorUtility.DisplayDialog("Keywords", message, "OK");
        }
    }
    
    /// <summary>
    /// Simple input dialog for editor
    /// </summary>
    public class EditorInputDialog : EditorWindow
    {
        private string _value = "";
        private string _message = "";
        private bool _confirmed = false;
        private static EditorInputDialog _window;
        private static string _result;
        
        public static string Show(string title, string message, string defaultValue)
        {
            _result = null;
            _window = GetWindow<EditorInputDialog>(true, title, true);
            _window._message = message;
            _window._value = defaultValue;
            _window.minSize = new Vector2(350, 100);
            _window.maxSize = new Vector2(350, 100);
            _window.ShowModalUtility();
            return _result;
        }
        
        void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(_message);
            EditorGUILayout.Space(5);
            
            GUI.SetNextControlName("InputField");
            _value = EditorGUILayout.TextField(_value);
            EditorGUI.FocusTextInControl("InputField");
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Cancel"))
            {
                _result = null;
                Close();
            }
            if (GUILayout.Button("OK") || (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return))
            {
                _result = _value;
                Close();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
