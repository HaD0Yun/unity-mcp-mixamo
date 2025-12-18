/*
 * MixamoKeywords.cs
 * 
 * Animation keyword mappings for natural language to Mixamo search queries.
 * Maps user input like "run" to Mixamo queries like "running", "run", "sprint".
 * 
 * Part of Unity-MCP Mixamo Animation Auto-Fetch System
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace MCPForUnity.Editor.Mixamo
{
    /// <summary>
    /// Maps natural language animation keywords to Mixamo search queries.
    /// </summary>
    public static class MixamoKeywords
    {
        #region Keyword Mappings
        
        /// <summary>
        /// Primary keyword mappings from user input to Mixamo search terms.
        /// Key: user input (lowercase), Value: array of Mixamo search terms
        /// </summary>
        public static readonly Dictionary<string, string[]> Mappings = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            // Locomotion - Basic Movement
            ["idle"] = new[] { "idle", "breathing idle", "standing idle", "happy idle" },
            ["walk"] = new[] { "walking", "walk", "strut", "walk forward" },
            ["run"] = new[] { "running", "run", "jog", "sprint", "fast run" },
            ["sprint"] = new[] { "sprint", "fast run", "running fast" },
            ["jog"] = new[] { "jog", "jogging", "slow run" },
            
            // Locomotion - Directional
            ["strafe"] = new[] { "strafe", "strafe left", "strafe right", "side step" },
            ["backward"] = new[] { "walk backward", "walking backward", "back up" },
            ["turn"] = new[] { "turn", "turn left", "turn right", "turning" },
            
            // Jumping & Falling
            ["jump"] = new[] { "jump", "jumping", "hop", "leap", "jump up" },
            ["fall"] = new[] { "falling", "fall", "falling idle" },
            ["land"] = new[] { "landing", "land", "hard landing" },
            ["flip"] = new[] { "flip", "backflip", "front flip", "somersault" },
            
            // Crouching & Sneaking
            ["crouch"] = new[] { "crouch", "crouching", "crouch idle", "crouch walk" },
            ["sneak"] = new[] { "sneak", "sneaking", "stealth", "crouch walk" },
            ["prone"] = new[] { "prone", "lying down", "crawl" },
            
            // Combat - Melee
            ["attack"] = new[] { "attack", "melee attack", "swing", "slash" },
            ["punch"] = new[] { "punch", "punching", "jab", "hook", "uppercut" },
            ["kick"] = new[] { "kick", "kicking", "roundhouse", "front kick" },
            ["slash"] = new[] { "slash", "sword slash", "sword swing", "melee" },
            ["stab"] = new[] { "stab", "stabbing", "thrust", "sword thrust" },
            ["block"] = new[] { "block", "blocking", "parry", "shield block" },
            ["dodge"] = new[] { "dodge", "evade", "roll", "side step" },
            
            // Combat - Ranged
            ["shoot"] = new[] { "shooting", "shoot", "rifle shoot", "pistol shoot" },
            ["aim"] = new[] { "aim", "aiming", "rifle aim", "pistol aim" },
            ["reload"] = new[] { "reload", "reloading", "rifle reload" },
            ["throw"] = new[] { "throw", "throwing", "grenade throw", "toss" },
            ["bow"] = new[] { "bow", "archery", "draw bow", "bow shoot" },
            
            // Combat - Magic/Special
            ["cast"] = new[] { "cast", "casting", "magic cast", "spell" },
            ["magic"] = new[] { "magic", "spell", "casting", "magic attack" },
            
            // Death & Reactions
            ["death"] = new[] { "death", "dying", "die", "death forward", "death backward" },
            ["hit"] = new[] { "hit reaction", "get hit", "impact", "take damage" },
            ["knockback"] = new[] { "knockback", "knocked back", "pushed" },
            ["knockdown"] = new[] { "knockdown", "knocked down", "fall down" },
            ["getup"] = new[] { "get up", "stand up", "recover" },
            
            // Social & Emotes
            ["wave"] = new[] { "wave", "waving", "greeting", "hello" },
            ["talk"] = new[] { "talking", "talk", "conversation", "argue" },
            ["nod"] = new[] { "nod", "nodding", "yes", "agree" },
            ["shake"] = new[] { "shake head", "no", "disagree" },
            ["point"] = new[] { "point", "pointing", "gesture" },
            ["salute"] = new[] { "salute", "saluting", "military salute" },
            ["clap"] = new[] { "clap", "clapping", "applause" },
            ["cheer"] = new[] { "cheer", "cheering", "celebrate", "victory" },
            ["taunt"] = new[] { "taunt", "taunting", "provoke" },
            ["laugh"] = new[] { "laugh", "laughing", "giggle" },
            ["cry"] = new[] { "cry", "crying", "sad", "sobbing" },
            
            // Dance
            ["dance"] = new[] { "dance", "dancing", "groove", "hip hop" },
            ["breakdance"] = new[] { "breakdance", "break dance", "b-boy" },
            ["ballet"] = new[] { "ballet", "pirouette", "ballet dance" },
            
            // Sports
            ["kick_ball"] = new[] { "soccer kick", "kick ball", "football kick" },
            ["throw_ball"] = new[] { "throw ball", "baseball throw", "pitch" },
            ["catch"] = new[] { "catch", "catching", "catch ball" },
            ["swing_bat"] = new[] { "bat swing", "baseball swing", "batting" },
            ["golf"] = new[] { "golf swing", "golf", "putting" },
            ["swim"] = new[] { "swim", "swimming", "treading water" },
            ["climb"] = new[] { "climb", "climbing", "wall climb", "ladder" },
            
            // Sitting & Resting
            ["sit"] = new[] { "sitting", "sit", "sit down", "seated" },
            ["stand"] = new[] { "stand", "standing", "stand up" },
            ["lean"] = new[] { "lean", "leaning", "lean against" },
            ["sleep"] = new[] { "sleep", "sleeping", "rest", "lying" },
            
            // Work & Activities
            ["pick_up"] = new[] { "pick up", "picking up", "grab", "lift" },
            ["carry"] = new[] { "carry", "carrying", "hold", "holding" },
            ["push"] = new[] { "push", "pushing", "shove" },
            ["pull"] = new[] { "pull", "pulling", "drag" },
            ["drink"] = new[] { "drink", "drinking", "sip" },
            ["eat"] = new[] { "eat", "eating", "chew" },
            
            // Vehicle Related
            ["drive"] = new[] { "driving", "drive", "steering" },
            ["ride"] = new[] { "ride", "riding", "horseback" },
            
            // Special
            ["zombie"] = new[] { "zombie", "zombie walk", "zombie attack", "undead" },
            ["injured"] = new[] { "injured", "limping", "wounded walk" },
            ["drunk"] = new[] { "drunk", "drunk walk", "stagger" },
        };
        
        #endregion
        
        #region Animator State Mappings
        
        /// <summary>
        /// Maps animation keywords to Animator state types for proper state machine setup.
        /// </summary>
        public static readonly Dictionary<string, AnimatorStateType> StateTypeMappings = new Dictionary<string, AnimatorStateType>(StringComparer.OrdinalIgnoreCase)
        {
            // Idle states
            ["idle"] = AnimatorStateType.Idle,
            ["standing"] = AnimatorStateType.Idle,
            ["breathing"] = AnimatorStateType.Idle,
            
            // Locomotion states
            ["walk"] = AnimatorStateType.Locomotion,
            ["run"] = AnimatorStateType.Locomotion,
            ["sprint"] = AnimatorStateType.Locomotion,
            ["jog"] = AnimatorStateType.Locomotion,
            ["crouch"] = AnimatorStateType.Locomotion,
            ["sneak"] = AnimatorStateType.Locomotion,
            
            // Action states
            ["jump"] = AnimatorStateType.Action,
            ["attack"] = AnimatorStateType.Action,
            ["punch"] = AnimatorStateType.Action,
            ["kick"] = AnimatorStateType.Action,
            ["shoot"] = AnimatorStateType.Action,
            ["cast"] = AnimatorStateType.Action,
            ["dance"] = AnimatorStateType.Action,
            ["wave"] = AnimatorStateType.Action,
            
            // Reaction states
            ["death"] = AnimatorStateType.Reaction,
            ["hit"] = AnimatorStateType.Reaction,
            ["fall"] = AnimatorStateType.Reaction,
            ["knockback"] = AnimatorStateType.Reaction,
        };
        
        #endregion
        
        #region State Name Mappings
        
        /// <summary>
        /// Maps keywords to clean Animator state names.
        /// </summary>
        public static readonly Dictionary<string, string> StateNameMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["idle"] = "Idle",
            ["walk"] = "Walk",
            ["run"] = "Run",
            ["sprint"] = "Sprint",
            ["jog"] = "Jog",
            ["jump"] = "Jump",
            ["fall"] = "Fall",
            ["land"] = "Land",
            ["crouch"] = "Crouch",
            ["sneak"] = "Sneak",
            ["attack"] = "Attack",
            ["punch"] = "Punch",
            ["kick"] = "Kick",
            ["shoot"] = "Shoot",
            ["death"] = "Death",
            ["hit"] = "Hit",
            ["dance"] = "Dance",
            ["wave"] = "Wave",
        };
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Get Mixamo search queries for a user keyword.
        /// </summary>
        /// <param name="keyword">User input keyword (e.g., "run", "attack")</param>
        /// <returns>Array of Mixamo search terms, or the original keyword if no mapping exists</returns>
        public static string[] GetSearchQueries(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return Array.Empty<string>();
            }
            
            keyword = keyword.Trim().ToLowerInvariant();
            
            if (Mappings.TryGetValue(keyword, out var queries))
            {
                return queries;
            }
            
            // No mapping found - return original keyword
            return new[] { keyword };
        }
        
        /// <summary>
        /// Get the primary search query for a keyword.
        /// </summary>
        public static string GetPrimaryQuery(string keyword)
        {
            var queries = GetSearchQueries(keyword);
            return queries.Length > 0 ? queries[0] : keyword;
        }
        
        /// <summary>
        /// Get all search queries for multiple keywords.
        /// </summary>
        public static Dictionary<string, string[]> GetSearchQueriesForKeywords(IEnumerable<string> keywords)
        {
            var result = new Dictionary<string, string[]>();
            
            foreach (var keyword in keywords)
            {
                var cleanKeyword = keyword.Trim().ToLowerInvariant();
                if (!string.IsNullOrEmpty(cleanKeyword))
                {
                    result[cleanKeyword] = GetSearchQueries(cleanKeyword);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Get the animator state type for a keyword.
        /// </summary>
        public static AnimatorStateType GetStateType(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return AnimatorStateType.Action;
            }
            
            keyword = keyword.Trim().ToLowerInvariant();
            
            if (StateTypeMappings.TryGetValue(keyword, out var stateType))
            {
                return stateType;
            }
            
            // Default to Action for unknown keywords
            return AnimatorStateType.Action;
        }
        
        /// <summary>
        /// Get a clean state name for the Animator.
        /// </summary>
        public static string GetStateName(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return "Unknown";
            }
            
            keyword = keyword.Trim().ToLowerInvariant();
            
            if (StateNameMappings.TryGetValue(keyword, out var stateName))
            {
                return stateName;
            }
            
            // Capitalize first letter
            return char.ToUpperInvariant(keyword[0]) + keyword.Substring(1);
        }
        
        /// <summary>
        /// Parse a comma-separated string of animation keywords.
        /// </summary>
        public static List<string> ParseKeywordString(string keywordString)
        {
            if (string.IsNullOrWhiteSpace(keywordString))
            {
                return new List<string>();
            }
            
            return keywordString
                .Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(k => k.Trim().ToLowerInvariant())
                .Where(k => !string.IsNullOrEmpty(k))
                .Distinct()
                .ToList();
        }
        
        /// <summary>
        /// Get all available keyword categories.
        /// </summary>
        public static string[] GetAllKeywords()
        {
            return Mappings.Keys.ToArray();
        }
        
        /// <summary>
        /// Check if a keyword has a mapping.
        /// </summary>
        public static bool HasMapping(string keyword)
        {
            return Mappings.ContainsKey(keyword.Trim().ToLowerInvariant());
        }
        
        /// <summary>
        /// Suggest similar keywords for a misspelled or unknown keyword.
        /// </summary>
        public static string[] SuggestSimilar(string keyword, int maxSuggestions = 3)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return Array.Empty<string>();
            }
            
            keyword = keyword.Trim().ToLowerInvariant();
            
            return Mappings.Keys
                .Select(k => new { Key = k, Distance = LevenshteinDistance(keyword, k) })
                .Where(x => x.Distance <= 3) // Max 3 character difference
                .OrderBy(x => x.Distance)
                .Take(maxSuggestions)
                .Select(x => x.Key)
                .ToArray();
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Calculate Levenshtein distance between two strings.
        /// </summary>
        private static int LevenshteinDistance(string s, string t)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.IsNullOrEmpty(t) ? 0 : t.Length;
            }
            
            if (string.IsNullOrEmpty(t))
            {
                return s.Length;
            }
            
            var n = s.Length;
            var m = t.Length;
            var d = new int[n + 1, m + 1];
            
            for (var i = 0; i <= n; i++)
            {
                d[i, 0] = i;
            }
            
            for (var j = 0; j <= m; j++)
            {
                d[0, j] = j;
            }
            
            for (var i = 1; i <= n; i++)
            {
                for (var j = 1; j <= m; j++)
                {
                    var cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost
                    );
                }
            }
            
            return d[n, m];
        }
        
        #endregion
    }
}
