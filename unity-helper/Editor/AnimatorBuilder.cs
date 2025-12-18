using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MixamoHelper
{
    /// <summary>
    /// Utility to build Animator Controllers from animation clips.
    /// </summary>
    public static class AnimatorBuilder
    {
        /// <summary>
        /// Create an Animator Controller from all animation clips in a folder.
        /// </summary>
        /// <param name="folderPath">Path to folder containing FBX files (e.g., "Assets/Animations/Player")</param>
        /// <param name="defaultStateName">Name of the default state (e.g., "Idle")</param>
        /// <returns>Created AnimatorController asset path</returns>
        public static string CreateFromFolder(string folderPath, string defaultStateName = "Idle")
        {
            if (!Directory.Exists(folderPath))
            {
                Debug.LogError($"[AnimatorBuilder] Folder not found: {folderPath}");
                return null;
            }

            // Find all animation clips
            var clips = new List<AnimationClip>();
            var fbxFiles = Directory.GetFiles(folderPath, "*.fbx", SearchOption.TopDirectoryOnly);
            
            foreach (var fbxPath in fbxFiles)
            {
                string assetPath = fbxPath.Replace("\\", "/");
                if (!assetPath.StartsWith("Assets/"))
                {
                    // Convert absolute path to relative
                    int assetsIndex = assetPath.IndexOf("Assets/");
                    if (assetsIndex >= 0)
                        assetPath = assetPath.Substring(assetsIndex);
                }
                
                var objects = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                foreach (var obj in objects)
                {
                    if (obj is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                    {
                        clips.Add(clip);
                    }
                }
            }

            if (clips.Count == 0)
            {
                Debug.LogError($"[AnimatorBuilder] No animation clips found in: {folderPath}");
                return null;
            }

            // Create animator controller
            string folderName = Path.GetFileName(folderPath);
            string controllerPath = $"{folderPath}/{folderName}_Animator.controller";
            
            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var rootStateMachine = controller.layers[0].stateMachine;

            // Add parameters
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Death", AnimatorControllerParameterType.Trigger);

            // Create states
            AnimatorState defaultState = null;
            var states = new Dictionary<string, AnimatorState>();
            
            float xPos = 0;
            float yPos = 0;
            int col = 0;
            
            foreach (var clip in clips)
            {
                var state = rootStateMachine.AddState(clip.name, new Vector3(xPos, yPos, 0));
                state.motion = clip;
                states[clip.name.ToLower()] = state;
                
                // Check if this is the default state
                if (clip.name.ToLower().Contains(defaultStateName.ToLower()))
                {
                    defaultState = state;
                }
                
                // Grid layout
                col++;
                xPos += 250;
                if (col >= 4)
                {
                    col = 0;
                    xPos = 0;
                    yPos += 80;
                }
            }

            // Set default state
            if (defaultState != null)
            {
                rootStateMachine.defaultState = defaultState;
            }

            // Add basic transitions
            AddBasicTransitions(rootStateMachine, states, controller);

            AssetDatabase.SaveAssets();
            Debug.Log($"[AnimatorBuilder] Created Animator Controller: {controllerPath}");
            
            return controllerPath;
        }

        private static void AddBasicTransitions(
            AnimatorStateMachine stateMachine,
            Dictionary<string, AnimatorState> states,
            AnimatorController controller)
        {
            AnimatorState idle = FindState(states, "idle");
            AnimatorState walk = FindState(states, "walk");
            AnimatorState run = FindState(states, "run");
            AnimatorState jump = FindState(states, "jump");
            AnimatorState attack = FindState(states, "attack");

            // Idle <-> Walk transitions based on Speed
            if (idle != null && walk != null)
            {
                var toWalk = idle.AddTransition(walk);
                toWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
                toWalk.hasExitTime = false;
                toWalk.duration = 0.15f;

                var toIdle = walk.AddTransition(idle);
                toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
                toIdle.hasExitTime = false;
                toIdle.duration = 0.15f;
            }

            // Walk <-> Run transitions based on Speed
            if (walk != null && run != null)
            {
                var toRun = walk.AddTransition(run);
                toRun.AddCondition(AnimatorConditionMode.Greater, 0.5f, "Speed");
                toRun.hasExitTime = false;
                toRun.duration = 0.15f;

                var toWalkFromRun = run.AddTransition(walk);
                toWalkFromRun.AddCondition(AnimatorConditionMode.Less, 0.5f, "Speed");
                toWalkFromRun.hasExitTime = false;
                toWalkFromRun.duration = 0.15f;
            }

            // Jump transition (Any State -> Jump)
            if (jump != null)
            {
                var anyToJump = stateMachine.AddAnyStateTransition(jump);
                anyToJump.AddCondition(AnimatorConditionMode.If, 0, "Jump");
                anyToJump.hasExitTime = false;
                anyToJump.duration = 0.1f;

                // Jump -> Idle (exit time)
                if (idle != null)
                {
                    var jumpToIdle = jump.AddTransition(idle);
                    jumpToIdle.hasExitTime = true;
                    jumpToIdle.exitTime = 0.9f;
                    jumpToIdle.duration = 0.15f;
                }
            }

            // Attack transition (Any State -> Attack)
            if (attack != null)
            {
                var anyToAttack = stateMachine.AddAnyStateTransition(attack);
                anyToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
                anyToAttack.hasExitTime = false;
                anyToAttack.duration = 0.1f;

                // Attack -> Idle (exit time)
                if (idle != null)
                {
                    var attackToIdle = attack.AddTransition(idle);
                    attackToIdle.hasExitTime = true;
                    attackToIdle.exitTime = 0.9f;
                    attackToIdle.duration = 0.15f;
                }
            }
        }

        private static AnimatorState FindState(Dictionary<string, AnimatorState> states, string keyword)
        {
            // Exact match first
            if (states.TryGetValue(keyword, out var exactState))
                return exactState;
            
            // Partial match
            foreach (var kvp in states)
            {
                if (kvp.Key.Contains(keyword))
                    return kvp.Value;
            }
            
            return null;
        }
    }

    /// <summary>
    /// Editor menu items for Mixamo Helper.
    /// </summary>
    public static class MixamoHelperMenu
    {
        [MenuItem("Tools/Mixamo Helper/Create Animator from Selected Folder")]
        public static void CreateAnimatorFromSelectedFolder()
        {
            var selected = Selection.activeObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a folder containing animation FBX files.", "OK");
                return;
            }

            string path = AssetDatabase.GetAssetPath(selected);
            if (!AssetDatabase.IsValidFolder(path))
            {
                EditorUtility.DisplayDialog("Error", "Please select a folder, not a file.", "OK");
                return;
            }

            string result = AnimatorBuilder.CreateFromFolder(path);
            if (result != null)
            {
                EditorUtility.DisplayDialog("Success", $"Created Animator Controller:\n{result}", "OK");
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<AnimatorController>(result);
            }
        }

        [MenuItem("Tools/Mixamo Helper/Create Animator from Selected Folder", true)]
        public static bool CreateAnimatorFromSelectedFolderValidate()
        {
            var selected = Selection.activeObject;
            if (selected == null)
                return false;
            
            string path = AssetDatabase.GetAssetPath(selected);
            return AssetDatabase.IsValidFolder(path);
        }
    }
}
