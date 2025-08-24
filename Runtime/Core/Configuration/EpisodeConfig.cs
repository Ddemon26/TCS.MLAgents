namespace TCS.MLAgents.Configuration {
    /// <summary>
    /// ScriptableObject configuration for episode management settings.
    /// Defines episode behavior, handler configurations, and lifecycle parameters.
    /// </summary>
    [CreateAssetMenu(fileName = "EpisodeConfig", menuName = "TCS ML Agents/Episode Config")]
    public class EpisodeConfig : ScriptableObject {
        [Header("Episode Settings")]
        [SerializeField] public bool autoStartEpisodes = true;
        [SerializeField] [Range(0f, 5f)] public float episodeStartDelay = 0.1f;
        [SerializeField] public bool logEpisodeEvents = true;
        
        [Header("Episode Limits")]
        [SerializeField] public bool enableMaxSteps = false;
        [SerializeField] [Range(100, 50000)] public int maxStepsPerEpisode = 5000;
        [SerializeField] public bool enableTimeLimit = false;
        [SerializeField] [Range(1f, 300f)] public float maxEpisodeTime = 30f;
        
        [Header("Reset Conditions")]
        [SerializeField] public bool resetOnBoundaryViolation = true;
        [SerializeField] public bool resetOnTaskFailure = true;
        [SerializeField] public bool resetOnTaskSuccess = true;
        [SerializeField] [Range(0f, 10f)] public float resetDelay = 0.5f;
        
        [Header("Success Conditions")]
        [SerializeField] public List<SuccessCondition> successConditions = new List<SuccessCondition>();
        
        [Header("Failure Conditions")]
        [SerializeField] public List<FailureCondition> failureConditions = new List<FailureCondition>();
        
        [Header("Handler Configuration")]
        [SerializeField] public List<EpisodeHandlerConfig> handlerConfigs = new List<EpisodeHandlerConfig>();
        
        [Header("Statistics")]
        [SerializeField] public bool trackEpisodeStats = true;
        [SerializeField] public bool saveStatsToFile = false;
        [SerializeField] public string statsFilePath = "episode_stats.json";
        
        [Serializable]
        public class SuccessCondition {
            [SerializeField] public string conditionName;
            [SerializeField] public SuccessType successType = SuccessType.ReachTarget;
            [SerializeField] public string requiredTag = "Goal";
            [SerializeField] public float requiredDistance = 1f;
            [SerializeField] public string sharedDataKey = "TaskCompleted";
            [SerializeField] public bool boolValue = true;
            [SerializeField] public float floatValue = 1f;
            [SerializeField] public int intValue = 1;
        }
        
        [Serializable]
        public class FailureCondition {
            [SerializeField] public string conditionName;
            [SerializeField] public FailureType failureType = FailureType.BoundaryViolation;
            [SerializeField] public string requiredTag = "Obstacle";
            [SerializeField] public string sharedDataKey = "TaskFailed";
            [SerializeField] public bool boolValue = true;
            [SerializeField] public float floatValue = 0f;
            [SerializeField] public int intValue = 0;
        }
        
        [Serializable]
        public class EpisodeHandlerConfig {
            [SerializeField] public string handlerName;
            [SerializeField] public bool isActive = true;
            [SerializeField] public int priority = 0;
            [SerializeField] public Dictionary<string, object> parameters = new Dictionary<string, object>();
        }
        
        public enum SuccessType {
            ReachTarget,        // Agent reaches a specific target
            CollectItems,       // Agent collects required items
            SurviveTime,        // Agent survives for a certain duration
            SharedDataBool,     // Shared data boolean is true
            SharedDataFloat,    // Shared data float reaches threshold
            SharedDataInt,      // Shared data int reaches threshold
            Custom              // Custom success logic
        }
        
        public enum FailureType {
            BoundaryViolation,  // Agent goes out of bounds
            CollisionWithTag,   // Agent collides with tagged object
            TimeLimit,          // Time limit exceeded
            MaxStepsReached,    // Maximum steps reached
            SharedDataBool,     // Shared data boolean is true
            SharedDataFloat,    // Shared data float reaches threshold
            SharedDataInt,      // Shared data int reaches threshold
            Custom              // Custom failure logic
        }
        
        private void OnValidate() {
            ValidateConfiguration();
        }
        
        private void ValidateConfiguration() {
            // Validate episode limits
            if (enableMaxSteps && maxStepsPerEpisode <= 0) {
                Debug.LogWarning($"[{name}] Max steps per episode must be positive");
                maxStepsPerEpisode = 1000;
            }
            
            if (enableTimeLimit && maxEpisodeTime <= 0f) {
                Debug.LogWarning($"[{name}] Max episode time must be positive");
                maxEpisodeTime = 30f;
            }
            
            if (episodeStartDelay < 0f) {
                Debug.LogWarning($"[{name}] Episode start delay cannot be negative");
                episodeStartDelay = 0f;
            }
            
            if (resetDelay < 0f) {
                Debug.LogWarning($"[{name}] Reset delay cannot be negative");
                resetDelay = 0f;
            }
            
            // Validate success conditions
            foreach (var condition in successConditions) {
                if (string.IsNullOrEmpty(condition.conditionName)) {
                    condition.conditionName = $"SuccessCondition_{successConditions.IndexOf(condition)}";
                }
                
                if (condition.successType == SuccessType.ReachTarget && condition.requiredDistance <= 0f) {
                    Debug.LogWarning($"[{name}] Success condition '{condition.conditionName}' required distance must be positive");
                    condition.requiredDistance = 1f;
                }
            }
            
            // Validate failure conditions
            foreach (var condition in failureConditions) {
                if (string.IsNullOrEmpty(condition.conditionName)) {
                    condition.conditionName = $"FailureCondition_{failureConditions.IndexOf(condition)}";
                }
            }
            
            // Validate handler configs
            foreach (var config in handlerConfigs) {
                if (string.IsNullOrEmpty(config.handlerName)) {
                    Debug.LogWarning($"[{name}] Handler config has empty name");
                }
            }
        }
        
        // Helper methods for runtime configuration
        public void AddSuccessCondition(string name, SuccessType type) {
            successConditions.Add(new SuccessCondition {
                conditionName = name,
                successType = type
            });
        }
        
        public void AddFailureCondition(string name, FailureType type) {
            failureConditions.Add(new FailureCondition {
                conditionName = name,
                failureType = type
            });
        }
        
        public void AddHandlerConfig(string handlerName, bool isActive = true, int priority = 0) {
            var existing = handlerConfigs.Find(h => h.handlerName == handlerName);
            if (existing != null) {
                existing.isActive = isActive;
                existing.priority = priority;
            } else {
                handlerConfigs.Add(new EpisodeHandlerConfig {
                    handlerName = handlerName,
                    isActive = isActive,
                    priority = priority
                });
            }
        }
        
        public EpisodeHandlerConfig GetHandlerConfig(string handlerName) {
            return handlerConfigs.Find(h => h.handlerName == handlerName);
        }
        
        public void RemoveHandlerConfig(string handlerName) {
            handlerConfigs.RemoveAll(h => h.handlerName == handlerName);
        }
        
        public void SetHandlerActive(string handlerName, bool isActive) {
            var config = GetHandlerConfig(handlerName);
            if (config != null) {
                config.isActive = isActive;
            }
        }
        
        public void SetHandlerPriority(string handlerName, int priority) {
            var config = GetHandlerConfig(handlerName);
            if (config != null) {
                config.priority = priority;
            }
        }
        
        public SuccessCondition GetSuccessCondition(string conditionName) {
            return successConditions.Find(c => c.conditionName == conditionName);
        }
        
        public FailureCondition GetFailureCondition(string conditionName) {
            return failureConditions.Find(c => c.conditionName == conditionName);
        }
        
        public void RemoveSuccessCondition(string conditionName) {
            successConditions.RemoveAll(c => c.conditionName == conditionName);
        }
        
        public void RemoveFailureCondition(string conditionName) {
            failureConditions.RemoveAll(c => c.conditionName == conditionName);
        }
        
        // Validation method for external use
        public bool ValidateConfig() {
            bool isValid = true;
            
            // Check for conflicting settings
            if (!autoStartEpisodes && episodeStartDelay > 0f) {
                Debug.LogWarning($"[{name}] Episode start delay set but auto-start is disabled");
            }
            
            if (enableMaxSteps && enableTimeLimit) {
                Debug.Log($"[{name}] Both step limit and time limit are enabled");
            }
            
            if (successConditions.Count == 0) {
                Debug.LogWarning($"[{name}] No success conditions defined");
            }
            
            // Check for duplicate names
            var handlerNames = new HashSet<string>();
            foreach (var config in handlerConfigs) {
                if (!handlerNames.Add(config.handlerName)) {
                    Debug.LogError($"[{name}] Duplicate handler name: {config.handlerName}");
                    isValid = false;
                }
            }
            
            var successNames = new HashSet<string>();
            foreach (var condition in successConditions) {
                if (!successNames.Add(condition.conditionName)) {
                    Debug.LogError($"[{name}] Duplicate success condition name: {condition.conditionName}");
                    isValid = false;
                }
            }
            
            var failureNames = new HashSet<string>();
            foreach (var condition in failureConditions) {
                if (!failureNames.Add(condition.conditionName)) {
                    Debug.LogError($"[{name}] Duplicate failure condition name: {condition.conditionName}");
                    isValid = false;
                }
            }
            
            return isValid;
        }
        
        // Create default configuration
        [ContextMenu("Create Default Config")]
        public void CreateDefaultConfig() {
            // Clear existing
            successConditions.Clear();
            failureConditions.Clear();
            handlerConfigs.Clear();
            
            // Add default success conditions
            AddSuccessCondition("ReachGoal", SuccessType.ReachTarget);
            AddSuccessCondition("TaskComplete", SuccessType.SharedDataBool);
            
            // Add default failure conditions
            AddFailureCondition("OutOfBounds", FailureType.BoundaryViolation);
            AddFailureCondition("HitObstacle", FailureType.CollisionWithTag);
            AddFailureCondition("Timeout", FailureType.TimeLimit);
            
            // Add default handler configs
            AddHandlerConfig("StepLimitHandler", true, 100);
            AddHandlerConfig("TimeLimitHandler", true, 90);
            AddHandlerConfig("BoundaryHandler", true, 80);
            AddHandlerConfig("TaskHandler", true, 70);
            
            Debug.Log($"[{name}] Created default episode configuration");
        }
    }
}