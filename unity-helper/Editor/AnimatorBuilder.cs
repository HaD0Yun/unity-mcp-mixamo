#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MixamoMcp.Editor
{
    public static class MixamoAnimatorBuilder
    {
        public static string CreateFromFolder(string folderPath, string defaultStateName = "Idle")
        {
            if (!Directory.Exists(folderPath))
            {
                Debug.LogError("[MixamoAnimatorBuilder] Folder not found: " + folderPath);
                return null;
            }

            var clips = new List<AnimationClip>();
            var fbxFiles = Directory.GetFiles(folderPath, "*.fbx", SearchOption.TopDirectoryOnly);
            
            foreach (var fbxPath in fbxFiles)
            {
                string assetPath = fbxPath.Replace("\\", "/");
                if (!assetPath.StartsWith("Assets/"))
                {
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
                Debug.LogError("[MixamoAnimatorBuilder] No animation clips found in: " + folderPath);
                return null;
            }

            string folderName = Path.GetFileName(folderPath);
            string controllerPath = folderPath + "/" + folderName + "_Animator.controller";
            
            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var rootStateMachine = controller.layers[0].stateMachine;

            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Death", AnimatorControllerParameterType.Trigger);

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
                
                if (clip.name.ToLower().Contains(defaultStateName.ToLower()))
                {
                    defaultState = state;
                }
                
                col++;
                xPos += 250;
                if (col >= 4)
                {
                    col = 0;
                    xPos = 0;
                    yPos += 80;
                }
            }

            if (defaultState != null)
            {
                rootStateMachine.defaultState = defaultState;
            }

            AddBasicTransitions(rootStateMachine, states);

            AssetDatabase.SaveAssets();
            Debug.Log("[MixamoAnimatorBuilder] Created Animator Controller: " + controllerPath);
            
            return controllerPath;
        }

        private static void AddBasicTransitions(AnimatorStateMachine stateMachine, Dictionary<string, AnimatorState> states)
        {
            AnimatorState idle = FindState(states, "idle");
            AnimatorState walk = FindState(states, "walk");
            AnimatorState run = FindState(states, "run");
            AnimatorState jump = FindState(states, "jump");
            AnimatorState attack = FindState(states, "attack");

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

            if (jump != null)
            {
                var anyToJump = stateMachine.AddAnyStateTransition(jump);
                anyToJump.AddCondition(AnimatorConditionMode.If, 0, "Jump");
                anyToJump.hasExitTime = false;
                anyToJump.duration = 0.1f;

                if (idle != null)
                {
                    var jumpToIdle = jump.AddTransition(idle);
                    jumpToIdle.hasExitTime = true;
                    jumpToIdle.exitTime = 0.9f;
                    jumpToIdle.duration = 0.15f;
                }
            }

            if (attack != null)
            {
                var anyToAttack = stateMachine.AddAnyStateTransition(attack);
                anyToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
                anyToAttack.hasExitTime = false;
                anyToAttack.duration = 0.1f;

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
            if (states.TryGetValue(keyword, out var exactState))
                return exactState;
            
            foreach (var kvp in states)
            {
                if (kvp.Key.Contains(keyword))
                    return kvp.Value;
            }
            
            return null;
        }
    }

    public static class MixamoHelperMenu
    {
        [MenuItem("Tools/Mixamo Helper/Create Animator from Selected Folder", priority = 1600)]
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

            string result = MixamoAnimatorBuilder.CreateFromFolder(path);
            if (result != null)
            {
                EditorUtility.DisplayDialog("Success", "Created Animator Controller:\n" + result, "OK");
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
#endif
