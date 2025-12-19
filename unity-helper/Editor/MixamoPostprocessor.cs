using UnityEditor;
using UnityEngine;

public class MixamoPostprocessor : AssetPostprocessor
{
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
        if (!IsMixamoAnimation(assetPath))
            return;
    }

    void OnPostprocessAnimation(GameObject root, AnimationClip clip)
    {
        if (!IsMixamoAnimation(assetPath))
            return;

        string clipNameLower = clip.name.ToLower();
        bool shouldLoop = IsLoopingAnimation(clipNameLower);
        
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = shouldLoop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        
        Debug.Log("[MixamoHelper] Animation '" + clip.name + "' - Loop: " + shouldLoop);
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
