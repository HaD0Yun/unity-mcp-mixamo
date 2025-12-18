/*
 * MixamoModels.cs
 * 
 * Data models for Mixamo API communication.
 * Based on reverse-engineered Mixamo API (unofficial).
 * 
 * Part of Unity-MCP Mixamo Animation Auto-Fetch System
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace MCPForUnity.Editor.Mixamo
{
    #region Search API Models
    
    /// <summary>
    /// Response from /api/v1/products search endpoint
    /// </summary>
    [Serializable]
    public class MixamoSearchResponse
    {
        public MixamoPagination pagination;
        public MixamoAnimation[] results;  // Must be array for JsonUtility
    }
    
    [Serializable]
    public class MixamoPagination
    {
        public int num_results;
        public int page;
        public int limit;
        public int num_pages;
    }
    
    [Serializable]
    public class MixamoAnimation
    {
        public string id;
        public string type;          // "Motion" or "MotionPack"
        public string description;
        public string name;
        public string category;
        public string character_type;
        public string thumbnail;      // API returns "thumbnail", not "thumbnail_url"
        public string thumbnail_animated;
        public string motion_id;
        public string source;
        
        // These fields are only in product details response, not search
        public MixamoMotionDetails details;
        public string thumbnail_url;  // Legacy field name
        public string character_id;
        
        // Computed properties
        public string DisplayName => string.IsNullOrEmpty(name) ? description : name;
        public string ThumbnailUrl => thumbnail ?? thumbnail_url;
    }
    
    [Serializable]
    public class MixamoMotionDetails
    {
        public string gms_hash_json;
        public MixamoGmsHash gms_hash;
        public string[] supports;
        public string default_frame_length;
        public float duration;
        public bool looping;
    }
    
    #endregion
    
    #region Product API Models
    
    /// <summary>
    /// Response from /api/v1/products/{id} endpoint
    /// </summary>
    [Serializable]
    public class MixamoProductResponse
    {
        public string id;
        public string type;
        public string description;
        public string name;
        public MixamoProductDetails details;
    }
    
    [Serializable]
    public class MixamoProductDetails
    {
        public MixamoGmsHash gms_hash;
        public float duration;
        public bool looping;
        public string default_frame_length;
    }
    
    [Serializable]
    public class MixamoGmsHash
    {
        [SerializeField]
        private long model_id;      // API returns as number, not string
        
        public string ModelId
        {
            get => model_id.ToString();
            set => long.TryParse(value, out model_id);
        }
        
        public bool mirror;
        public float[] trim;        // [start, end] as percentages (0.0-100.0)
        public int overdrive;
        public string @params;      // Comma-separated param values (not parsed, API returns array)
        public int arm_space;
        public bool inplace;
        
        // Original params for modification
        public List<MixamoParam> paramList;
    }
    
    [Serializable]
    public class MixamoParam
    {
        public string name;
        public float value;
        public float min;
        public float max;
        public string type;
    }
    
    #endregion
    
    #region Export API Models
    
    /// <summary>
    /// Request body for /api/v1/animations/export
    /// </summary>
    [Serializable]
    public class MixamoExportRequest
    {
        public string character_id;
        public List<MixamoExportGmsHash> gms_hash;
        public MixamoExportPreferences preferences;
        public string product_name;
        public string type;
        
        public MixamoExportRequest()
        {
            type = "Motion";
            preferences = new MixamoExportPreferences();
            gms_hash = new List<MixamoExportGmsHash>();
        }
    }
    
    [Serializable]
    public class MixamoExportGmsHash
    {
        [SerializeField]
        private string model_id;
        
        public string ModelId
        {
            get => model_id;
            set => model_id = value;
        }
        
        public bool mirror;
        public int[] trim;
        public int overdrive;
        public string @params;
        public int arm_space;
        public bool inplace;
        
        public static MixamoExportGmsHash FromGmsHash(MixamoGmsHash source)
        {
            // Convert float[] trim to int[]
            int[] trimInt = null;
            if (source.trim != null && source.trim.Length >= 2)
            {
                trimInt = new[] { (int)source.trim[0], (int)source.trim[1] };
            }
            else
            {
                trimInt = new[] { 0, 100 };
            }
            
            return new MixamoExportGmsHash
            {
                ModelId = source.ModelId,
                mirror = source.mirror,
                trim = trimInt,
                overdrive = source.overdrive,
                @params = source.@params ?? "0,0,0",
                arm_space = source.arm_space,
                inplace = source.inplace
            };
        }
    }
    
    [Serializable]
    public class MixamoExportPreferences
    {
        public string format;
        public string skin;
        public string fps;
        public string reducekf;
        
        public MixamoExportPreferences()
        {
            format = "fbx7_2019";   // Unity-compatible FBX
            skin = "false";         // No skin for animation-only export
            fps = "30";
            reducekf = "0";         // No keyframe reduction
        }
        
        public static MixamoExportPreferences WithSkin()
        {
            return new MixamoExportPreferences { skin = "true" };
        }
    }
    
    /// <summary>
    /// Response from /api/v1/animations/export
    /// </summary>
    [Serializable]
    public class MixamoExportResponse
    {
        public string status;
        public string uuid;
        public string message;
    }
    
    #endregion
    
    #region Monitor API Models
    
    /// <summary>
    /// Response from /api/v1/characters/{id}/monitor
    /// </summary>
    [Serializable]
    public class MixamoMonitorResponse
    {
        public string status;       // "processing", "completed", "failed"
        public string job_result;   // Download URL when completed
        public string uuid;
        public MixamoJobError error;
    }
    
    [Serializable]
    public class MixamoJobError
    {
        public string message;
        public string code;
    }
    
    #endregion
    
    #region Character Upload Models
    
    /// <summary>
    /// Response from /api/v1/characters (upload)
    /// </summary>
    [Serializable]
    public class MixamoUploadResponse
    {
        public string uuid;
        public string status;
        public string message;
    }
    
    /// <summary>
    /// Character data model
    /// </summary>
    [Serializable]
    public class MixamoCharacter
    {
        public string id;
        public string uuid;
        public string name;
        public bool is_rigged;
        public string thumbnail_url;
    }
    
    #endregion
    
    #region Internal Models
    
    /// <summary>
    /// Result of a download operation
    /// </summary>
    [Serializable]
    public class MixamoDownloadResult
    {
        public bool success;
        public string animationName;
        public string localPath;
        public string errorMessage;
        
        public static MixamoDownloadResult Success(string name, string path)
        {
            return new MixamoDownloadResult
            {
                success = true,
                animationName = name,
                localPath = path
            };
        }
        
        public static MixamoDownloadResult Failure(string name, string error)
        {
            return new MixamoDownloadResult
            {
                success = false,
                animationName = name,
                errorMessage = error
            };
        }
    }
    
    /// <summary>
    /// Configuration for batch download operations
    /// </summary>
    [Serializable]
    public class MixamoBatchConfig
    {
        public string characterName;
        public string characterId;
        public List<string> animationKeywords;
        public bool createAnimator;
        public string defaultAnimatorState;
        public string outputFolder;
        
        public MixamoBatchConfig()
        {
            characterId = MixamoConstants.DEFAULT_CHARACTER_ID;
            createAnimator = true;
            defaultAnimatorState = "Idle";
            outputFolder = "Assets/Animations";
            animationKeywords = new List<string>();
        }
    }
    
    /// <summary>
    /// Result of batch download operation
    /// </summary>
    [Serializable]
    public class MixamoBatchResult
    {
        public int totalRequested;
        public int successCount;
        public int failureCount;
        public List<MixamoDownloadResult> results;
        public string animatorControllerPath;
        
        public MixamoBatchResult()
        {
            results = new List<MixamoDownloadResult>();
        }
        
        public string GetSummary()
        {
            var lines = new List<string>
            {
                $"Batch Download Complete: {successCount}/{totalRequested} succeeded"
            };
            
            foreach (var result in results)
            {
                var icon = result.success ? "+" : "x";
                var detail = result.success ? result.localPath : result.errorMessage;
                lines.Add($"  [{icon}] {result.animationName}: {detail}");
            }
            
            if (!string.IsNullOrEmpty(animatorControllerPath))
            {
                lines.Add($"  Animator Controller: {animatorControllerPath}");
            }
            
            return string.Join("\n", lines);
        }
    }
    
    #endregion
    
    #region Constants
    
    public static class MixamoConstants
    {
        public const string BASE_URL = "https://www.mixamo.com/api/v1";
        public const string API_KEY = "mixamo2";
        public const string DEFAULT_CHARACTER_ID = "4f5d21e1-4ccc-41f1-b35b-fb2547bd8493"; // Y-Bot (updated 2024)
        
        // Rate limiting
        public const int MIN_REQUEST_DELAY_MS = 2000;  // 2 seconds
        public const int MAX_REQUEST_DELAY_MS = 5000;  // 5 seconds
        
        // Retry settings
        public const int MAX_RETRY_ATTEMPTS = 3;
        public const int BASE_RETRY_DELAY_MS = 1000;
        
        // Export monitoring
        public const int MONITOR_POLL_INTERVAL_MS = 2000;
        public const int MONITOR_MAX_ATTEMPTS = 60;    // 2 minutes max wait
        
        // FBX Import settings
        public const float DEFAULT_ANIMATION_SCALE = 1.0f;
    }
    
    #endregion
    
    #region Enums
    
    public enum MixamoExportFormat
    {
        Fbx7_2019,      // Recommended for Unity
        Fbx7_2014,
        Fbx6,
        Collada
    }
    
    public enum MixamoExportStatus
    {
        Unknown,
        Processing,
        Completed,
        Failed
    }
    
    public enum AnimatorStateType
    {
        Idle,
        Locomotion,
        Action,
        Reaction
    }
    
    #endregion
}
