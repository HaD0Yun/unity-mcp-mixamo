/*
 * AnimatorBuilder.cs
 * 
 * Automatic Animator Controller generator for Mixamo animations.
 * Creates state machines with proper transitions based on animation types.
 * 
 * Part of Unity-MCP Mixamo Animation Auto-Fetch System
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MCPForUnity.Editor.Mixamo
{
    /// <summary>
    /// Builds Animator Controllers automatically from downloaded animations.
    /// </summary>
    public static class AnimatorBuilder
    {
        #region Constants
        
        // Transition settings
        private const float DEFAULT_TRANSITION_DURATION = 0.25f;
        private const float EXIT_TIME_LOOPS = 0.9f;
        private const float EXIT_TIME_ACTIONS = 1.0f;
        
        // Parameter names
        private const string PARAM_SPEED = "Speed";
        private const string PARAM_IS_GROUNDED = "IsGrounded";
        private const string PARAM_IS_CROUCHING = "IsCrouching";
        private const string PARAM_IS_ATTACKING = "IsAttacking";
        private const string PARAM_IS_DEAD = "IsDead";
        private const string PARAM_TRIGGER_JUMP = "Jump";
        private const string PARAM_TRIGGER_ATTACK = "Attack";
        private const string PARAM_TRIGGER_HIT = "Hit";
        private const string PARAM_TRIGGER_DEATH = "Death";
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Create an Animator Controller from animations in a folder.
        /// </summary>
        /// <param name="animationFolder">Folder containing animation clips (e.g., "Assets/Animations/Character1")</param>
        /// <param name="defaultStateName">Name of the default state (typically "Idle")</param>
        /// <returns>Path to the created Animator Controller</returns>
        public static string CreateAnimatorController(
            string animationFolder,
            string defaultStateName = "Idle")
        {
            if (!Directory.Exists(animationFolder.Replace("Assets", Application.dataPath)))
            {
                throw new DirectoryNotFoundException($"Animation folder not found: {animationFolder}");
            }
            
            // Find all animation clips in folder
            var clipGuids = AssetDatabase.FindAssets("t:AnimationClip", new[] { animationFolder });
            var clips = clipGuids
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Select(path => AssetDatabase.LoadAssetAtPath<AnimationClip>(path))
                .Where(clip => clip != null)
                .ToList();
            
            if (clips.Count == 0)
            {
                throw new InvalidOperationException($"No animation clips found in: {animationFolder}");
            }
            
            // Create controller
            var folderName = Path.GetFileName(animationFolder);
            var controllerPath = $"{animationFolder}/{folderName}_Animator.controller";
            
            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var rootStateMachine = controller.layers[0].stateMachine;
            
            // Add parameters
            AddDefaultParameters(controller);
            
            // Create states for each clip
            var states = new Dictionary<string, AnimatorState>();
            AnimatorState defaultState = null;
            
            foreach (var clip in clips)
            {
                var stateName = GetStateNameFromClip(clip);
                var state = rootStateMachine.AddState(stateName);
                state.motion = clip;
                
                // Configure state based on clip type
                ConfigureState(state, clip);
                
                states[stateName.ToLower()] = state;
                
                // Check for default state
                if (stateName.Equals(defaultStateName, StringComparison.OrdinalIgnoreCase))
                {
                    defaultState = state;
                }
            }
            
            // Set default state
            if (defaultState != null)
            {
                rootStateMachine.defaultState = defaultState;
            }
            else if (states.ContainsKey("idle"))
            {
                rootStateMachine.defaultState = states["idle"];
            }
            else if (states.Count > 0)
            {
                rootStateMachine.defaultState = states.Values.First();
            }
            
            // Add common transitions
            AddCommonTransitions(rootStateMachine, states, controller);
            
            // Save and refresh
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"[AnimatorBuilder] Created Animator Controller at: {controllerPath}");
            return controllerPath;
        }
        
        /// <summary>
        /// Create an Animator Controller from a list of animations with specific configuration.
        /// </summary>
        public static string CreateAnimatorControllerFromConfig(
            AnimatorConfig config)
        {
            var controllerPath = $"{config.OutputFolder}/{config.ControllerName}.controller";
            
            // Ensure directory exists
            var fullPath = controllerPath.Replace("Assets", Application.dataPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            
            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var rootStateMachine = controller.layers[0].stateMachine;
            
            // Add parameters
            AddDefaultParameters(controller);
            foreach (var param in config.CustomParameters)
            {
                AddParameter(controller, param.Name, param.Type, param.DefaultValue);
            }
            
            // Create states
            var states = new Dictionary<string, AnimatorState>();
            
            foreach (var stateConfig in config.States)
            {
                var state = rootStateMachine.AddState(stateConfig.Name);
                
                if (stateConfig.Motion != null)
                {
                    state.motion = stateConfig.Motion;
                }
                else if (!string.IsNullOrEmpty(stateConfig.MotionPath))
                {
                    state.motion = AssetDatabase.LoadAssetAtPath<Motion>(stateConfig.MotionPath);
                }
                
                state.speed = stateConfig.Speed;
                state.speedParameterActive = stateConfig.UseSpeedParameter;
                
                if (stateConfig.UseSpeedParameter)
                {
                    state.speedParameter = stateConfig.SpeedParameterName ?? PARAM_SPEED;
                }
                
                states[stateConfig.Name.ToLower()] = state;
            }
            
            // Set default state
            if (!string.IsNullOrEmpty(config.DefaultStateName) && 
                states.TryGetValue(config.DefaultStateName.ToLower(), out var defaultState))
            {
                rootStateMachine.defaultState = defaultState;
            }
            
            // Add transitions
            foreach (var transConfig in config.Transitions)
            {
                if (states.TryGetValue(transConfig.FromState.ToLower(), out var fromState) &&
                    states.TryGetValue(transConfig.ToState.ToLower(), out var toState))
                {
                    var transition = fromState.AddTransition(toState);
                    ConfigureTransition(transition, transConfig, controller);
                }
            }
            
            // Add common transitions if requested
            if (config.AddCommonTransitions)
            {
                AddCommonTransitions(rootStateMachine, states, controller);
            }
            
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            return controllerPath;
        }
        
        /// <summary>
        /// Import FBX and configure for Humanoid animation.
        /// </summary>
        public static void ConfigureHumanoidImport(string fbxPath)
        {
            var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[AnimatorBuilder] Could not get ModelImporter for: {fbxPath}");
                return;
            }
            
            // Configure rig
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            
            // Configure animation
            importer.importAnimation = true;
            importer.animationCompression = ModelImporterAnimationCompression.Optimal;
            
            // Get clip settings
            var clipAnimations = importer.clipAnimations;
            if (clipAnimations.Length == 0)
            {
                clipAnimations = importer.defaultClipAnimations;
            }
            
            // Configure each clip
            for (int i = 0; i < clipAnimations.Length; i++)
            {
                var clip = clipAnimations[i];
                
                // Try to determine if this is a looping animation
                var clipName = clip.name.ToLower();
                var isLooping = IsLoopingAnimation(clipName);
                
                clip.loopTime = isLooping;
                clip.loopPose = isLooping;
                clip.lockRootRotation = true;
                clip.lockRootHeightY = false;
                clip.lockRootPositionXZ = false;
                
                // Root motion settings
                if (ShouldApplyRootMotion(clipName))
                {
                    clip.keepOriginalOrientation = true;
                    clip.keepOriginalPositionXZ = true;
                    clip.keepOriginalPositionY = true;
                }
                
                clipAnimations[i] = clip;
            }
            
            importer.clipAnimations = clipAnimations;
            
            // Apply import settings
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            
            Debug.Log($"[AnimatorBuilder] Configured Humanoid import for: {fbxPath}");
        }
        
        /// <summary>
        /// Apply Animator Controller to a GameObject.
        /// </summary>
        public static void ApplyAnimatorToGameObject(
            GameObject gameObject,
            string animatorControllerPath)
        {
            if (gameObject == null)
            {
                throw new ArgumentNullException(nameof(gameObject));
            }
            
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(animatorControllerPath);
            if (controller == null)
            {
                throw new FileNotFoundException($"Animator Controller not found: {animatorControllerPath}");
            }
            
            var animator = gameObject.GetComponent<Animator>();
            if (animator == null)
            {
                animator = gameObject.AddComponent<Animator>();
            }
            
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = true;
            
            EditorUtility.SetDirty(gameObject);
            
            Debug.Log($"[AnimatorBuilder] Applied Animator to: {gameObject.name}");
        }
        
        #endregion
        
        #region Private Methods
        
        private static void AddDefaultParameters(AnimatorController controller)
        {
            AddParameter(controller, PARAM_SPEED, AnimatorControllerParameterType.Float, 0f);
            AddParameter(controller, PARAM_IS_GROUNDED, AnimatorControllerParameterType.Bool, true);
            AddParameter(controller, PARAM_IS_CROUCHING, AnimatorControllerParameterType.Bool, false);
            AddParameter(controller, PARAM_IS_ATTACKING, AnimatorControllerParameterType.Bool, false);
            AddParameter(controller, PARAM_IS_DEAD, AnimatorControllerParameterType.Bool, false);
            AddParameter(controller, PARAM_TRIGGER_JUMP, AnimatorControllerParameterType.Trigger, null);
            AddParameter(controller, PARAM_TRIGGER_ATTACK, AnimatorControllerParameterType.Trigger, null);
            AddParameter(controller, PARAM_TRIGGER_HIT, AnimatorControllerParameterType.Trigger, null);
            AddParameter(controller, PARAM_TRIGGER_DEATH, AnimatorControllerParameterType.Trigger, null);
        }
        
        private static void AddParameter(
            AnimatorController controller,
            string name,
            AnimatorControllerParameterType type,
            object defaultValue)
        {
            // Check if parameter already exists
            if (controller.parameters.Any(p => p.name == name))
            {
                return;
            }
            
            controller.AddParameter(name, type);
            
            // Set default value
            var param = controller.parameters.FirstOrDefault(p => p.name == name);
            if (param != null && defaultValue != null)
            {
                switch (type)
                {
                    case AnimatorControllerParameterType.Float:
                        param.defaultFloat = Convert.ToSingle(defaultValue);
                        break;
                    case AnimatorControllerParameterType.Int:
                        param.defaultInt = Convert.ToInt32(defaultValue);
                        break;
                    case AnimatorControllerParameterType.Bool:
                        param.defaultBool = Convert.ToBoolean(defaultValue);
                        break;
                }
            }
        }
        
        private static string GetStateNameFromClip(AnimationClip clip)
        {
            var name = clip.name;
            
            // Remove common prefixes/suffixes
            var removeParts = new[] { "mixamo.com", "anim_", "_anim", "animation_", "_animation" };
            foreach (var part in removeParts)
            {
                name = name.Replace(part, "", StringComparison.OrdinalIgnoreCase);
            }
            
            // Clean up
            name = name.Trim('_', ' ', '-');
            
            // Capitalize first letter
            if (!string.IsNullOrEmpty(name))
            {
                name = char.ToUpperInvariant(name[0]) + name.Substring(1);
            }
            
            return name;
        }
        
        private static void ConfigureState(AnimatorState state, AnimationClip clip)
        {
            var clipName = clip.name.ToLower();
            
            // Configure looping
            if (IsLoopingAnimation(clipName))
            {
                // Loop time is set in the clip itself
            }
            
            // Configure speed for locomotion
            if (clipName.Contains("walk") || clipName.Contains("run") || clipName.Contains("sprint"))
            {
                state.speedParameterActive = true;
                state.speedParameter = PARAM_SPEED;
            }
        }
        
        private static void AddCommonTransitions(
            AnimatorStateMachine stateMachine,
            Dictionary<string, AnimatorState> states,
            AnimatorController controller)
        {
            // Idle <-> Walk
            TryAddTransition(states, "idle", "walk", controller, new TransitionConfig
            {
                HasExitTime = false,
                TransitionDuration = DEFAULT_TRANSITION_DURATION,
                Conditions = new[] { new ConditionConfig(PARAM_SPEED, AnimatorConditionMode.Greater, 0.1f) }
            });
            
            TryAddTransition(states, "walk", "idle", controller, new TransitionConfig
            {
                HasExitTime = false,
                TransitionDuration = DEFAULT_TRANSITION_DURATION,
                Conditions = new[] { new ConditionConfig(PARAM_SPEED, AnimatorConditionMode.Less, 0.1f) }
            });
            
            // Walk <-> Run
            TryAddTransition(states, "walk", "run", controller, new TransitionConfig
            {
                HasExitTime = false,
                TransitionDuration = DEFAULT_TRANSITION_DURATION,
                Conditions = new[] { new ConditionConfig(PARAM_SPEED, AnimatorConditionMode.Greater, 0.5f) }
            });
            
            TryAddTransition(states, "run", "walk", controller, new TransitionConfig
            {
                HasExitTime = false,
                TransitionDuration = DEFAULT_TRANSITION_DURATION,
                Conditions = new[] { new ConditionConfig(PARAM_SPEED, AnimatorConditionMode.Less, 0.5f) }
            });
            
            // Jump trigger
            foreach (var state in states.Values)
            {
                if (states.TryGetValue("jump", out var jumpState) && state != jumpState)
                {
                    TryAddTransition(state, jumpState, controller, new TransitionConfig
                    {
                        HasExitTime = false,
                        TransitionDuration = 0.1f,
                        Conditions = new[] { new ConditionConfig(PARAM_TRIGGER_JUMP, AnimatorConditionMode.If, 0) }
                    });
                }
            }
            
            // Jump -> Idle/Fall
            if (states.TryGetValue("jump", out var js))
            {
                AnimatorState landTarget = null;
                if (states.TryGetValue("land", out var landState))
                {
                    landTarget = landState;
                }
                else if (states.TryGetValue("idle", out var idleState))
                {
                    landTarget = idleState;
                }
                
                if (landTarget != null)
                {
                    var transition = js.AddTransition(landTarget);
                    transition.hasExitTime = true;
                    transition.exitTime = EXIT_TIME_ACTIONS;
                    transition.duration = DEFAULT_TRANSITION_DURATION;
                }
            }
            
            // Attack trigger
            foreach (var state in states.Values)
            {
                if (states.TryGetValue("attack", out var attackState) && state != attackState)
                {
                    var stateName = state.name.ToLower();
                    // Only allow attack from idle/walk/run states
                    if (stateName == "idle" || stateName == "walk" || stateName == "run")
                    {
                        TryAddTransition(state, attackState, controller, new TransitionConfig
                        {
                            HasExitTime = false,
                            TransitionDuration = 0.1f,
                            Conditions = new[] { new ConditionConfig(PARAM_TRIGGER_ATTACK, AnimatorConditionMode.If, 0) }
                        });
                    }
                }
            }
            
            // Attack -> Idle
            if (states.TryGetValue("attack", out var as_) && states.TryGetValue("idle", out var idle))
            {
                var transition = as_.AddTransition(idle);
                transition.hasExitTime = true;
                transition.exitTime = EXIT_TIME_ACTIONS;
                transition.duration = DEFAULT_TRANSITION_DURATION;
            }
            
            // Death trigger (from any state)
            if (states.TryGetValue("death", out var deathState))
            {
                foreach (var state in states.Values)
                {
                    if (state != deathState)
                    {
                        TryAddTransition(state, deathState, controller, new TransitionConfig
                        {
                            HasExitTime = false,
                            TransitionDuration = 0.1f,
                            Conditions = new[] { new ConditionConfig(PARAM_TRIGGER_DEATH, AnimatorConditionMode.If, 0) }
                        });
                    }
                }
            }
            
            // Hit reaction
            if (states.TryGetValue("hit", out var hitState))
            {
                foreach (var state in states.Values)
                {
                    var stateName = state.name.ToLower();
                    if (state != hitState && stateName != "death")
                    {
                        TryAddTransition(state, hitState, controller, new TransitionConfig
                        {
                            HasExitTime = false,
                            TransitionDuration = 0.05f,
                            Conditions = new[] { new ConditionConfig(PARAM_TRIGGER_HIT, AnimatorConditionMode.If, 0) }
                        });
                    }
                }
                
                // Hit -> Idle
                if (states.TryGetValue("idle", out var idleForHit))
                {
                    var transition = hitState.AddTransition(idleForHit);
                    transition.hasExitTime = true;
                    transition.exitTime = EXIT_TIME_ACTIONS;
                    transition.duration = DEFAULT_TRANSITION_DURATION;
                }
            }
        }
        
        private static void TryAddTransition(
            Dictionary<string, AnimatorState> states,
            string fromStateName,
            string toStateName,
            AnimatorController controller,
            TransitionConfig config)
        {
            if (states.TryGetValue(fromStateName.ToLower(), out var fromState) &&
                states.TryGetValue(toStateName.ToLower(), out var toState))
            {
                TryAddTransition(fromState, toState, controller, config);
            }
        }
        
        private static void TryAddTransition(
            AnimatorState fromState,
            AnimatorState toState,
            AnimatorController controller,
            TransitionConfig config)
        {
            var transition = fromState.AddTransition(toState);
            ConfigureTransition(transition, config, controller);
        }
        
        private static void ConfigureTransition(
            AnimatorStateTransition transition,
            TransitionConfig config,
            AnimatorController controller)
        {
            transition.hasExitTime = config.HasExitTime;
            transition.exitTime = config.ExitTime;
            transition.duration = config.TransitionDuration;
            transition.hasFixedDuration = config.HasFixedDuration;
            
            if (config.Conditions != null)
            {
                foreach (var condition in config.Conditions)
                {
                    // Ensure parameter exists
                    if (!controller.parameters.Any(p => p.name == condition.ParameterName))
                    {
                        var paramType = condition.Mode == AnimatorConditionMode.If || 
                                        condition.Mode == AnimatorConditionMode.IfNot
                            ? AnimatorControllerParameterType.Trigger
                            : AnimatorControllerParameterType.Float;
                        controller.AddParameter(condition.ParameterName, paramType);
                    }
                    
                    transition.AddCondition(condition.Mode, condition.Threshold, condition.ParameterName);
                }
            }
        }
        
        private static bool IsLoopingAnimation(string clipName)
        {
            var loopingKeywords = new[]
            {
                "idle", "walk", "run", "sprint", "jog", "strafe", "crouch",
                "swim", "fly", "hover", "breathing", "standing"
            };
            
            return loopingKeywords.Any(k => clipName.Contains(k));
        }
        
        private static bool ShouldApplyRootMotion(string clipName)
        {
            var rootMotionKeywords = new[]
            {
                "walk", "run", "sprint", "jog", "strafe", "jump", "roll", "dodge"
            };
            
            return rootMotionKeywords.Any(k => clipName.Contains(k));
        }
        
        #endregion
    }
    
    #region Configuration Classes
    
    /// <summary>
    /// Configuration for creating an Animator Controller.
    /// </summary>
    [Serializable]
    public class AnimatorConfig
    {
        public string ControllerName = "Character_Animator";
        public string OutputFolder = "Assets/Animations";
        public string DefaultStateName = "Idle";
        public bool AddCommonTransitions = true;
        public List<AnimatorStateConfig> States = new List<AnimatorStateConfig>();
        public List<TransitionConfig> Transitions = new List<TransitionConfig>();
        public List<ParameterConfig> CustomParameters = new List<ParameterConfig>();
    }
    
    [Serializable]
    public class AnimatorStateConfig
    {
        public string Name;
        public Motion Motion;
        public string MotionPath;
        public float Speed = 1f;
        public bool UseSpeedParameter = false;
        public string SpeedParameterName = "Speed";
    }
    
    [Serializable]
    public class TransitionConfig
    {
        public string FromState;
        public string ToState;
        public bool HasExitTime = false;
        public float ExitTime = 0.9f;
        public float TransitionDuration = 0.25f;
        public bool HasFixedDuration = true;
        public ConditionConfig[] Conditions;
    }
    
    [Serializable]
    public class ConditionConfig
    {
        public string ParameterName;
        public AnimatorConditionMode Mode;
        public float Threshold;
        
        public ConditionConfig() { }
        
        public ConditionConfig(string paramName, AnimatorConditionMode mode, float threshold)
        {
            ParameterName = paramName;
            Mode = mode;
            Threshold = threshold;
        }
    }
    
    [Serializable]
    public class ParameterConfig
    {
        public string Name;
        public AnimatorControllerParameterType Type;
        public object DefaultValue;
    }
    
    #endregion
}
