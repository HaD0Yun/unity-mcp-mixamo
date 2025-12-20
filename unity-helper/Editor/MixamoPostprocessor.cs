#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace MixamoMcp.Editor
{
    /// <summary>
    /// Auto-configures FBX files from Mixamo with Humanoid rig and loop settings.
    /// Only triggers for FBX files in folders explicitly named "Mixamo" to avoid
    /// accidentally modifying other animation files.
    /// </summary>
    public class MixamoPostprocessor : AssetPostprocessor
    {
        // EditorPrefs key to enable/disable auto-processing
        private const string ENABLED_KEY = "MixamoMcp_PostprocessorEnabled";
        
        // Only match folders explicitly named "Mixamo" (case-insensitive)
        // This prevents accidentally processing unrelated FBX files
        private static readonly string[] ExactFolderNames = new[]
        {
            "Mixamo",
            "MixamoAnimations",
            "Mixamo_Animations"
        };

        /// <summary>
        /// Check if Mixamo postprocessor is enabled (default: true)
        /// </summary>
        public static bool IsEnabled
        {
            get => EditorPrefs.GetBool(ENABLED_KEY, true);
            set => EditorPrefs.SetBool(ENABLED_KEY, value);
        }

        [MenuItem("Window/Mixamo MCP/Enable FBX Auto-Config", true)]
        private static bool ValidateEnableAutoConfig()
        {
            Menu.SetChecked("Window/Mixamo MCP/Enable FBX Auto-Config", IsEnabled);
            return true;
        }

        [MenuItem("Window/Mixamo MCP/Enable FBX Auto-Config", false, 100)]
        private static void ToggleAutoConfig()
        {
            IsEnabled = !IsEnabled;
            Debug.Log("[MixamoHelper] FBX Auto-Config " + (IsEnabled ? "enabled" : "disabled"));
        }

        void OnPreprocessModel()
        {
            if (!IsEnabled || !IsMixamoAnimation(assetPath))
                return;

            ModelImporter importer = assetImporter as ModelImporter;
            if (importer == null)
                return;

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            
            importer.importAnimation = true;
            importer.importBlendShapes = false;
            importer.importVisibility = false;
            importer.importCameras = false;
            importer.importLights = false;
            
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            
            Debug.Log("[MixamoHelper] Configured Humanoid rig for: " + assetPath);
        }

        void OnPostprocessModel(GameObject go)
        {
            if (!IsEnabled || !IsMixamoAnimation(assetPath))
                return;
        }

        void OnPostprocessAnimation(GameObject root, AnimationClip clip)
        {
            if (!IsEnabled || !IsMixamoAnimation(assetPath))
                return;

            string clipNameLower = clip.name.ToLower();
            bool shouldLoop = IsLoopingAnimation(clipNameLower);
            
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = shouldLoop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            
            Debug.Log("[MixamoHelper] Animation '" + clip.name + "' - Loop: " + shouldLoop);
        }

        /// <summary>
        /// Check if the asset path is a Mixamo animation.
        /// Uses EXACT folder name matching to avoid false positives.
        /// Only matches if a parent folder is exactly named "Mixamo" (or similar).
        /// </summary>
        private bool IsMixamoAnimation(string path)
        {
            if (!path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                return false;

            // Get all parent folder names
            string directory = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(directory))
                return false;

            // Check each parent folder for exact match
            string[] pathParts = directory.Replace("\\", "/").Split('/');
            foreach (string folderName in pathParts)
            {
                foreach (string pattern in ExactFolderNames)
                {
                    if (string.Equals(folderName, pattern, System.StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            
            return false;
        }

        private bool IsLoopingAnimation(string animName)
        {
            string[] loopingPatterns = new[]
            {
                "idle", "walk", "run", "jog", "sprint",
                "crouch", "crawl", "swim", "fly",
                "strafe", "dance", "breathing"
            };

            string[] nonLoopingPatterns = new[]
            {
                "jump", "attack", "hit", "death", "die",
                "shoot", "reload", "throw", "dodge",
                "roll", "land", "fall", "pickup", "use",
                "wave", "bow", "clap", "cheer", "salute"
            };

            foreach (var pattern in nonLoopingPatterns)
            {
                if (animName.Contains(pattern))
                    return false;
            }

            foreach (var pattern in loopingPatterns)
            {
                if (animName.Contains(pattern))
                    return true;
            }

            return false;
        }
    }
}
#endif
