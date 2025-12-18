using UnityEditor;
using UnityEngine;

namespace MixamoHelper
{
    /// <summary>
    /// Automatically configures imported FBX files from Mixamo with Humanoid rig.
    /// </summary>
    public class MixamoPostprocessor : AssetPostprocessor
    {
        // Folder pattern to detect Mixamo animations
        private static readonly string[] MixamoFolderPatterns = new[]
        {
            "Mixamo",
            "Animations",
            "Animation"
        };

        void OnPreprocessModel()
        {
            if (!IsMixamoAnimation(assetPath))
                return;

            ModelImporter importer = assetImporter as ModelImporter;
            if (importer == null)
                return;

            // Configure as Humanoid
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            
            // Import settings
            importer.importAnimation = true;
            importer.importBlendShapes = false;
            importer.importVisibility = false;
            importer.importCameras = false;
            importer.importLights = false;
            
            // Material settings
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            
            Debug.Log($"[MixamoHelper] Configured Humanoid rig for: {assetPath}");
        }

        void OnPostprocessModel(GameObject go)
        {
            if (!IsMixamoAnimation(assetPath))
                return;

            // Additional post-processing if needed
        }

        void OnPostprocessAnimation(GameObject root, AnimationClip clip)
        {
            if (!IsMixamoAnimation(assetPath))
                return;

            // Determine if this should loop based on animation name
            string clipNameLower = clip.name.ToLower();
            bool shouldLoop = IsLoopingAnimation(clipNameLower);
            
            // Set loop time
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = shouldLoop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            
            Debug.Log($"[MixamoHelper] Animation '{clip.name}' - Loop: {shouldLoop}");
        }

        private bool IsMixamoAnimation(string path)
        {
            if (!path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                return false;

            string pathLower = path.ToLower();
            
            foreach (var pattern in MixamoFolderPatterns)
            {
                if (pathLower.Contains(pattern.ToLower()))
                    return true;
            }
            
            return false;
        }

        private bool IsLoopingAnimation(string animName)
        {
            // Animations that should loop
            string[] loopingPatterns = new[]
            {
                "idle", "walk", "run", "jog", "sprint",
                "crouch", "crawl", "swim", "fly",
                "strafe", "dance", "breathing"
            };

            // Animations that should NOT loop
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

            // Default to non-looping for unknown animations
            return false;
        }
    }
}
