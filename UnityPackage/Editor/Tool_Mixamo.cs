/*
 * Tool_Mixamo.cs
 * 
 * MCP Tools for Mixamo animation auto-fetch system.
 * Enables AI assistants to search, download, and configure Mixamo animations.
 * 
 * Tools:
 * - mixamo-auth: Store/validate Adobe authentication token
 * - mixamo-search: Search animations by keyword
 * - mixamo-download: Download single animation
 * - mixamo-batch: Batch download multiple animations
 * - mixamo-upload: Upload character for auto-rigging
 * - mixamo-configure: Create Animator Controller
 * - mixamo-apply: Apply Animator to GameObject
 * 
 * Part of Unity-MCP Mixamo Animation Auto-Fetch System
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;

namespace MCPForUnity.Editor.Mixamo
{
    /// <summary>
    /// MCP Tools for Mixamo animation integration.
    /// </summary>
    [McpPluginToolType]
    public class Tool_Mixamo
    {
        #region Authentication
        
        /// <summary>
        /// Store or validate Adobe/Mixamo authentication token.
        /// </summary>
        [McpPluginTool("mixamo-auth", Title = "Mixamo / Authenticate")]
        [Description(@"Store or validate Adobe/Mixamo authentication token.

To get your token:
1. Go to https://www.mixamo.com and log in
2. Open browser DevTools (F12)
3. In Console, run: localStorage.access_token
4. Copy the token value (without quotes)

The token is stored with encryption and persists across Unity sessions.")]
        public async Task<string> Authenticate(
            [Description("Adobe access_token from Mixamo localStorage. Leave empty to check current token status.")]
            string accessToken = null,
            
            [Description("Set to true to clear stored token")]
            bool clear = false)
        {
            try
            {
                if (clear)
                {
                    MixamoClient.Instance.ClearToken();
                    return "[Success] Mixamo authentication cleared.";
                }
                
                if (string.IsNullOrEmpty(accessToken))
                {
                    // Check current token status
                    if (!MixamoClient.Instance.HasToken)
                    {
                        return "[Info] No Mixamo token stored.\n\n" +
                               "To authenticate:\n" +
                               "1. Go to https://www.mixamo.com and log in\n" +
                               "2. Open browser DevTools (F12)\n" +
                               "3. In Console, run: localStorage.access_token\n" +
                               "4. Use mixamo-auth with the token value";
                    }
                    
                    var isValid = await MixamoClient.Instance.ValidateTokenAsync();
                    if (isValid)
                    {
                        return "[Success] Stored token is valid and ready to use.";
                    }
                    else
                    {
                        return "[Warning] Stored token is invalid or expired. Please provide a new token.";
                    }
                }
                
                // Validate and store new token
                var valid = await MixamoClient.Instance.ValidateTokenAsync(accessToken);
                if (!valid)
                {
                    return "[Error] Invalid token. Please check your token and try again.\n\n" +
                           "Make sure you're copying the full token from: localStorage.access_token";
                }
                
                MixamoClient.Instance.StoreToken(accessToken);
                return "[Success] Mixamo authentication saved and validated.\n" +
                       "You can now use mixamo-search, mixamo-download, and mixamo-batch commands.";
            }
            catch (Exception ex)
            {
                return $"[Error] Authentication failed: {ex.Message}";
            }
        }
        
        #endregion
        
        #region Search
        
        /// <summary>
        /// Search for animations on Mixamo by keyword.
        /// </summary>
        [McpPluginTool("mixamo-search", Title = "Mixamo / Search Animations")]
        [Description(@"Search for animations on Mixamo by keyword.

Supports natural language keywords like 'run', 'jump', 'attack', 'idle', etc.
Keywords are automatically mapped to Mixamo search queries for best results.

Example: 'run' -> searches for 'running', 'run', 'jog', 'sprint'")]
        public async Task<string> SearchAnimations(
            [Description("Search keyword (e.g., 'run', 'jump', 'idle', 'attack', 'dance')")]
            string keyword,
            
            [Description("Maximum number of results to return (default: 10, max: 50)")]
            int limit = 10,
            
            [Description("Show all alternative search queries for the keyword")]
            bool showQueries = false)
        {
            try
            {
                if (!MixamoClient.Instance.HasToken)
                {
                    return "[Error] Not authenticated. Use 'mixamo-auth' first.";
                }
                
                limit = Math.Clamp(limit, 1, 50);
                
                // Get search queries for keyword
                var queries = MixamoKeywords.GetSearchQueries(keyword);
                var primaryQuery = queries[0];
                
                var results = new List<string>();
                
                if (showQueries)
                {
                    results.Add($"Search queries for '{keyword}':");
                    results.Add($"  Primary: {primaryQuery}");
                    if (queries.Length > 1)
                    {
                        results.Add($"  Alternatives: {string.Join(", ", queries.Skip(1))}");
                    }
                    results.Add("");
                }
                
                // Perform search
                var response = await MixamoClient.Instance.SearchAnimationsAsync(primaryQuery, limit);
                
                if (response.results == null || response.results.Length == 0)
                {
                    // Try alternative queries
                    if (queries.Length > 1)
                    {
                        foreach (var altQuery in queries.Skip(1))
                        {
                            response = await MixamoClient.Instance.SearchAnimationsAsync(altQuery, limit);
                            if (response.results != null && response.results.Length > 0)
                            {
                                results.Add($"(Found results using alternative query: '{altQuery}')");
                                break;
                            }
                        }
                    }
                    
                    if (response.results == null || response.results.Length == 0)
                    {
                        var suggestions = MixamoKeywords.SuggestSimilar(keyword);
                        var suggestionText = suggestions.Length > 0
                            ? $"\n\nDid you mean: {string.Join(", ", suggestions)}?"
                            : "";
                        return $"[Info] No animations found for '{keyword}'.{suggestionText}";
                    }
                }
                
                results.Add($"Found {response.pagination.num_results} animations for '{keyword}':");
                results.Add("");
                
                foreach (var anim in response.results)
                {
                    var looping = anim.details?.looping == true ? " [Loop]" : "";
                    var duration = anim.details?.duration > 0 ? $" ({anim.details.duration:F1}s)" : "";
                    results.Add($"  ID: {anim.id}");
                    results.Add($"  Name: {anim.DisplayName}{looping}{duration}");
                    results.Add("");
                }
                
                results.Add("Use 'mixamo-download' with an ID to download an animation.");
                
                return string.Join("\n", results);
            }
            catch (Exception ex)
            {
                return $"[Error] Search failed: {ex.Message}";
            }
        }
        
        #endregion
        
        #region Download
        
        /// <summary>
        /// Download a single animation from Mixamo.
        /// </summary>
        [McpPluginTool("mixamo-download", Title = "Mixamo / Download Animation")]
        [Description(@"Download a single animation from Mixamo and import to Unity.

The animation is:
1. Downloaded as FBX
2. Imported with Humanoid rig settings
3. Configured for proper looping (if applicable)

Use 'mixamo-search' first to find animation IDs.")]
        public async Task<string> DownloadAnimation(
            [Description("Animation ID from search results (or animation name to search and download first result)")]
            string animationIdOrName,
            
            [Description("Character name for folder organization (default: 'Character')")]
            string characterName = "Character",
            
            [Description("Custom file name (default: uses animation name)")]
            string fileName = null,
            
            [Description("Character ID for rigging (default: Mixamo Y-Bot)")]
            string characterId = null)
        {
            try
            {
                if (!MixamoClient.Instance.HasToken)
                {
                    return "[Error] Not authenticated. Use 'mixamo-auth' first.";
                }
                
                string animationId = animationIdOrName;
                string animationName = fileName;
                
                // Check if input is a name (search) or ID
                if (!animationIdOrName.Contains("-") && animationIdOrName.Length < 30)
                {
                    // Treat as search keyword
                    var searchQuery = MixamoKeywords.GetPrimaryQuery(animationIdOrName);
                    var searchResults = await MixamoClient.Instance.SearchAnimationsAsync(searchQuery, 1);
                    
                    if (searchResults.results == null || searchResults.results.Length == 0)
                    {
                        return $"[Error] No animations found for '{animationIdOrName}'";
                    }
                    
                    var firstResult = searchResults.results[0];
                    animationId = firstResult.id;
                    animationName = fileName ?? firstResult.DisplayName;
                }
                
                // Get animation details
                var charId = characterId ?? MixamoConstants.DEFAULT_CHARACTER_ID;
                var product = await MixamoClient.Instance.GetProductAsync(animationId, charId);
                
                if (product == null)
                {
                    return $"[Error] Animation not found: {animationId}";
                }
                
                animationName = animationName ?? product.name ?? product.description ?? animationId;
                
                // Create export request
                var exportRequest = new MixamoExportRequest
                {
                    character_id = charId,
                    product_name = animationName,
                    gms_hash = new List<MixamoExportGmsHash>
                    {
                        MixamoExportGmsHash.FromGmsHash(product.details.gms_hash)
                    }
                };
                
                // Request export
                var exportResponse = await MixamoClient.Instance.ExportAnimationAsync(exportRequest);
                
                // Monitor until complete
                var progress = new Progress<string>(msg => Debug.Log($"[Mixamo] {msg}"));
                var downloadUrl = await MixamoClient.Instance.MonitorExportAsync(charId, progress);
                
                // Download FBX
                var savePath = $"Assets/Animations/{SanitizeName(characterName)}";
                string assetPath = null;
                
                try
                {
                    // Progress callback needs to run on main thread
                    var downloadProgress = new Progress<float>(p => 
                    {
                        MainThread.Instance.Run(() => 
                            EditorUtility.DisplayProgressBar("Downloading Animation", animationName, p));
                    });
                    
                    assetPath = await MixamoClient.Instance.DownloadFbxAsync(
                        downloadUrl, 
                        savePath, 
                        animationName,
                        downloadProgress);
                    
                    // Configure import settings on main thread
                    await MainThread.Instance.RunAsync(() =>
                    {
                        EditorUtility.ClearProgressBar();
                        AssetDatabase.Refresh();
                        AnimatorBuilder.ConfigureHumanoidImport(assetPath);
                    });
                    
                    return $"[Success] Animation downloaded!\n\n" +
                           $"  Name: {animationName}\n" +
                           $"  Path: {assetPath}\n" +
                           $"  Rig: Humanoid (configured)\n\n" +
                           $"Use 'mixamo-configure' to create an Animator Controller.";
                }
                catch
                {
                    await MainThread.Instance.RunAsync(() => EditorUtility.ClearProgressBar());
                    throw;
                }
            }
            catch (Exception ex)
            {
                await MainThread.Instance.RunAsync(() => EditorUtility.ClearProgressBar());
                return $"[Error] Download failed: {ex.Message}";
            }
        }
        
        #endregion
        
        #region Batch Download
        
        /// <summary>
        /// Batch download multiple animations from Mixamo.
        /// </summary>
        [McpPluginTool("mixamo-batch", Title = "Mixamo / Batch Download")]
        [Description(@"Download multiple animations from Mixamo at once.

Provide a comma-separated list of animation keywords.
Each keyword is searched and the first result is downloaded.

Example: 'idle, walk, run, jump, attack'

Optionally creates an Animator Controller with all downloaded animations.")]
        public async Task<string> BatchDownload(
            [Description("Character name for folder organization")]
            string characterName,
            
            [Description("Animation keywords (comma-separated: 'idle,walk,run,jump,attack')")]
            string animations,
            
            [Description("Create Animator Controller automatically (default: true)")]
            bool createAnimator = true,
            
            [Description("Default state name in Animator (default: 'Idle')")]
            string defaultState = "Idle",
            
            [Description("Character ID for rigging (default: Mixamo Y-Bot)")]
            string characterId = null)
        {
            try
            {
                if (!MixamoClient.Instance.HasToken)
                {
                    return "[Error] Not authenticated. Use 'mixamo-auth' first.";
                }
                
                var keywords = MixamoKeywords.ParseKeywordString(animations);
                if (keywords.Count == 0)
                {
                    return "[Error] No valid animation keywords provided.";
                }
                
                var charId = characterId ?? MixamoConstants.DEFAULT_CHARACTER_ID;
                var savePath = $"Assets/Animations/{SanitizeName(characterName)}";
                var results = new MixamoBatchResult { totalRequested = keywords.Count };
                
                // Ensure directory exists (needs main thread for Application.dataPath)
                await MainThread.Instance.RunAsync(() => 
                    Directory.CreateDirectory(savePath.Replace("Assets", Application.dataPath)));
                
                for (int i = 0; i < keywords.Count; i++)
                {
                    var keyword = keywords[i];
                    var progressTitle = $"Downloading Animations ({i + 1}/{keywords.Count})";
                    var currentIndex = i;
                    var totalCount = keywords.Count;
                    
                    try
                    {
                        await MainThread.Instance.RunAsync(() => 
                            EditorUtility.DisplayProgressBar(progressTitle, $"Searching: {keyword}", (float)currentIndex / totalCount));
                        
                        // Search for animation
                        var searchQuery = MixamoKeywords.GetPrimaryQuery(keyword);
                        var searchResults = await MixamoClient.Instance.SearchAnimationsAsync(searchQuery, 1);
                        
                        if (searchResults.results == null || searchResults.results.Length == 0)
                        {
                            // Try alternative queries
                            var altQueries = MixamoKeywords.GetSearchQueries(keyword).Skip(1);
                            var found = false;
                            
                            foreach (var altQuery in altQueries)
                            {
                                searchResults = await MixamoClient.Instance.SearchAnimationsAsync(altQuery, 1);
                                if (searchResults.results?.Length > 0)
                                {
                                    found = true;
                                    break;
                                }
                            }
                            
                            if (!found)
                            {
                                results.results.Add(MixamoDownloadResult.Failure(keyword, "Not found"));
                                results.failureCount++;
                                continue;
                            }
                        }
                        
                        var anim = searchResults.results[0];
                        var animName = MixamoKeywords.GetStateName(keyword);
                        
                        await MainThread.Instance.RunAsync(() => 
                            EditorUtility.DisplayProgressBar(progressTitle, $"Downloading: {animName}", (float)currentIndex / totalCount + 0.3f / totalCount));
                        
                        // Get product details
                        var product = await MixamoClient.Instance.GetProductAsync(anim.id, charId);
                        
                        // Create export request
                        var exportRequest = new MixamoExportRequest
                        {
                            character_id = charId,
                            product_name = animName,
                            gms_hash = new List<MixamoExportGmsHash>
                            {
                                MixamoExportGmsHash.FromGmsHash(product.details.gms_hash)
                            }
                        };
                        
                        // Export
                        var exportResponse = await MixamoClient.Instance.ExportAnimationAsync(exportRequest);
                        
                        await MainThread.Instance.RunAsync(() => 
                            EditorUtility.DisplayProgressBar(progressTitle, $"Processing: {animName}", (float)currentIndex / totalCount + 0.5f / totalCount));
                        
                        // Monitor
                        var downloadUrl = await MixamoClient.Instance.MonitorExportAsync(charId);
                        
                        await MainThread.Instance.RunAsync(() => 
                            EditorUtility.DisplayProgressBar(progressTitle, $"Saving: {animName}", (float)currentIndex / totalCount + 0.8f / totalCount));
                        
                        // Download
                        var assetPath = await MixamoClient.Instance.DownloadFbxAsync(
                            downloadUrl, 
                            savePath, 
                            $"{characterName}_{animName}");
                        
                        results.results.Add(MixamoDownloadResult.Success(animName, assetPath));
                        results.successCount++;
                    }
                    catch (Exception ex)
                    {
                        results.results.Add(MixamoDownloadResult.Failure(keyword, ex.Message));
                        results.failureCount++;
                    }
                }
                
                // Finalize on main thread
                await MainThread.Instance.RunAsync(() =>
                {
                    EditorUtility.ClearProgressBar();
                    
                    // Refresh and configure imports
                    AssetDatabase.Refresh();
                    
                    foreach (var result in results.results.Where(r => r.success))
                    {
                        AnimatorBuilder.ConfigureHumanoidImport(result.localPath);
                    }
                    
                    // Create Animator Controller
                    if (createAnimator && results.successCount > 0)
                    {
                        try
                        {
                            results.animatorControllerPath = AnimatorBuilder.CreateAnimatorController(savePath, defaultState);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[Mixamo] Failed to create Animator: {ex.Message}");
                        }
                    }
                });
                
                return $"[{(results.failureCount == 0 ? "Success" : "Partial")}] Batch download complete!\n\n" +
                       results.GetSummary();
            }
            catch (Exception ex)
            {
                await MainThread.Instance.RunAsync(() => EditorUtility.ClearProgressBar());
                return $"[Error] Batch download failed: {ex.Message}";
            }
        }
        
        #endregion
        
        #region Character Upload
        
        /// <summary>
        /// Upload a character FBX for auto-rigging.
        /// </summary>
        [McpPluginTool("mixamo-upload", Title = "Mixamo / Upload Character")]
        [Description(@"Upload a character FBX file to Mixamo for auto-rigging.

The uploaded character can then be used with mixamo-download to get
animations specifically rigged for that character.

Returns a character ID to use with other commands.")]
        public async Task<string> UploadCharacter(
            [Description("Path to FBX file (e.g., 'Assets/Characters/MyCharacter.fbx')")]
            string fbxPath)
        {
            try
            {
                if (!MixamoClient.Instance.HasToken)
                {
                    return "[Error] Not authenticated. Use 'mixamo-auth' first.";
                }
                
                // Check file exists - needs main thread for Application.dataPath
                var fileExists = await MainThread.Instance.RunAsync(() => 
                    File.Exists(fbxPath.Replace("Assets", Application.dataPath)));
                
                if (!fileExists)
                {
                    return $"[Error] FBX file not found: {fbxPath}";
                }
                
                var fileName = Path.GetFileName(fbxPath);
                var fullPath = await MainThread.Instance.RunAsync(() => 
                    fbxPath.Replace("Assets", Application.dataPath));
                var fbxData = await Task.Run(() => File.ReadAllBytes(fullPath));
                
                await MainThread.Instance.RunAsync(() => 
                    EditorUtility.DisplayProgressBar("Uploading Character", fileName, 0.3f));
                
                try
                {
                    var progress = new Progress<string>(msg => Debug.Log($"[Mixamo] {msg}"));
                    var response = await MixamoClient.Instance.UploadCharacterAsync(fbxData, fileName, progress);
                    
                    await MainThread.Instance.RunAsync(() => 
                        EditorUtility.DisplayProgressBar("Processing Character", "Auto-rigging...", 0.6f));
                    
                    // Monitor rigging
                    await MixamoClient.Instance.MonitorExportAsync(response.uuid, progress);
                    
                    await MainThread.Instance.RunAsync(() => EditorUtility.ClearProgressBar());
                    
                    return $"[Success] Character uploaded and rigged!\n\n" +
                           $"  Character ID: {response.uuid}\n\n" +
                           $"Use this ID with 'mixamo-download' or 'mixamo-batch' using the characterId parameter.";
                }
                catch
                {
                    await MainThread.Instance.RunAsync(() => EditorUtility.ClearProgressBar());
                    throw;
                }
            }
            catch (Exception ex)
            {
                await MainThread.Instance.RunAsync(() => EditorUtility.ClearProgressBar());
                return $"[Error] Upload failed: {ex.Message}";
            }
        }
        
        #endregion
        
        #region Animator Configuration
        
        /// <summary>
        /// Create an Animator Controller from downloaded animations.
        /// </summary>
        [McpPluginTool("mixamo-configure", Title = "Mixamo / Configure Animator")]
        [Description(@"Create an Animator Controller from downloaded Mixamo animations.

Automatically:
- Creates states for each animation clip
- Configures looping for locomotion animations
- Adds common transitions (Idle <-> Walk <-> Run)
- Sets up trigger parameters (Jump, Attack, Death, Hit)")]
        public string ConfigureAnimator(
            [Description("Path to animations folder (e.g., 'Assets/Animations/Character1')")]
            string animationFolder,
            
            [Description("Default state name (typically 'Idle')")]
            string defaultState = "Idle")
        => MainThread.Instance.Run(() =>
        {
            try
            {
                if (!Directory.Exists(animationFolder.Replace("Assets", Application.dataPath)))
                {
                    return $"[Error] Folder not found: {animationFolder}";
                }
                
                var controllerPath = AnimatorBuilder.CreateAnimatorController(animationFolder, defaultState);
                
                return $"[Success] Animator Controller created!\n\n" +
                       $"  Path: {controllerPath}\n\n" +
                       $"Parameters added:\n" +
                       $"  - Speed (float): For locomotion blend\n" +
                       $"  - IsGrounded (bool): Ground detection\n" +
                       $"  - Jump (trigger): Jump animation\n" +
                       $"  - Attack (trigger): Attack animation\n" +
                       $"  - Hit (trigger): Hit reaction\n" +
                       $"  - Death (trigger): Death animation\n\n" +
                       $"Use 'mixamo-apply' to assign to a GameObject.";
            }
            catch (Exception ex)
            {
                return $"[Error] Failed to create Animator: {ex.Message}";
            }
        });
        
        #endregion
        
        #region Apply to GameObject
        
        /// <summary>
        /// Apply an Animator Controller to a GameObject.
        /// </summary>
        [McpPluginTool("mixamo-apply", Title = "Mixamo / Apply Animator")]
        [Description(@"Apply an Animator Controller to a selected or specified GameObject.

The GameObject must have a Humanoid rig (Avatar) for Mixamo animations to work correctly.")]
        public string ApplyAnimator(
            [Description("Path to Animator Controller (e.g., 'Assets/Animations/Character1/Character1_Animator.controller')")]
            string animatorPath,
            
            [Description("GameObject name in scene (leave empty to use selected object)")]
            string gameObjectName = null)
        => MainThread.Instance.Run(() =>
        {
            try
            {
                if (!File.Exists(animatorPath.Replace("Assets", Application.dataPath)))
                {
                    return $"[Error] Animator Controller not found: {animatorPath}";
                }
                
                GameObject targetObject = null;
                
                if (string.IsNullOrEmpty(gameObjectName))
                {
                    // Use selected object
                    targetObject = Selection.activeGameObject;
                    if (targetObject == null)
                    {
                        return "[Error] No GameObject selected. Select a GameObject or provide a name.";
                    }
                }
                else
                {
                    targetObject = GameObject.Find(gameObjectName);
                    if (targetObject == null)
                    {
                        return $"[Error] GameObject not found: {gameObjectName}";
                    }
                }
                
                AnimatorBuilder.ApplyAnimatorToGameObject(targetObject, animatorPath);
                
                return $"[Success] Animator applied to '{targetObject.name}'!\n\n" +
                       $"The character is now ready for animation.\n\n" +
                       $"To test in Play mode, use these parameter names:\n" +
                       $"  animator.SetFloat(\"Speed\", moveSpeed);\n" +
                       $"  animator.SetTrigger(\"Jump\");\n" +
                       $"  animator.SetTrigger(\"Attack\");";
            }
            catch (Exception ex)
            {
                return $"[Error] Failed to apply Animator: {ex.Message}";
            }
        });
        
        #endregion
        
        #region Keywords Info
        
        /// <summary>
        /// List all available animation keywords and their mappings.
        /// </summary>
        [McpPluginTool("mixamo-keywords", Title = "Mixamo / List Keywords")]
        [Description(@"List all available animation keywords and their Mixamo search mappings.

Use these keywords with mixamo-search and mixamo-batch for best results.")]
        public string ListKeywords(
            [Description("Filter by keyword category (e.g., 'combat', 'locomotion', 'social')")]
            string filter = null)
        {
            var keywords = MixamoKeywords.GetAllKeywords();
            
            if (!string.IsNullOrEmpty(filter))
            {
                filter = filter.ToLower();
                keywords = keywords.Where(k => k.Contains(filter) || 
                    MixamoKeywords.GetSearchQueries(k).Any(q => q.Contains(filter))).ToArray();
            }
            
            var results = new List<string>
            {
                "Available Animation Keywords:",
                ""
            };
            
            foreach (var keyword in keywords.OrderBy(k => k))
            {
                var queries = MixamoKeywords.GetSearchQueries(keyword);
                var stateType = MixamoKeywords.GetStateType(keyword);
                results.Add($"  {keyword} [{stateType}]");
                results.Add($"    -> {string.Join(", ", queries)}");
            }
            
            results.Add("");
            results.Add($"Total: {keywords.Length} keywords");
            results.Add("");
            results.Add("Usage: mixamo-batch characterName \"idle,walk,run,jump,attack\"");
            
            return string.Join("\n", results);
        }
        
        #endregion
        
        #region Helpers
        
        private static string SanitizeName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            foreach (var c in invalid)
            {
                name = name.Replace(c, '_');
            }
            return name.Replace(' ', '_');
        }
        
        #endregion
    }
    
}
