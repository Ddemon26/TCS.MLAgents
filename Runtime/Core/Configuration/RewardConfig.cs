namespace TCS.MLAgents.Configuration {
    /// <summary>
    /// ScriptableObject configuration for reward system settings.
    /// Provides centralized configuration for reward providers and calculation.
    /// </summary>
    [CreateAssetMenu(menuName = "ML Agents/Reward Config", fileName = "RewardConfig")]
    public class RewardConfig : ScriptableObject {
        [Header("General Settings")]
        [SerializeField] string configName = "Default Reward Config";
        [SerializeField, TextArea(3, 5)] string description = "Configuration for ML agent reward system";
        
        [Header("Reward Limits")]
        [SerializeField] bool enableRewardClamping = true;
        [SerializeField] float maxRewardPerStep = 10f;
        [SerializeField] float minRewardPerStep = -10f;
        [SerializeField] float maxTotalEpisodeReward = 100f;
        [SerializeField] float minTotalEpisodeReward = -100f;
        
        [Header("Performance Settings")]
        [SerializeField] bool enableRewardValidation = true;
        [SerializeField] bool enableDebugLogging = false;
        [SerializeField] float rewardCalculationFrequency = 0f; // 0 = every step
        
        [Header("Provider Settings")]
        [SerializeField] List<RewardProviderConfig> providerConfigs = new List<RewardProviderConfig>();
        [SerializeField] bool sortProvidersByPriority = true;
        [SerializeField] bool autoDiscoverProviders = true;
        
        [Header("Event System")]
        [SerializeField] List<string> registeredEventNames = new List<string>();
        [SerializeField] bool enableEventLogging = false;
        
        public string ConfigName => configName;
        public string Description => description;
        public bool EnableRewardClamping => enableRewardClamping;
        public float MaxRewardPerStep => maxRewardPerStep;
        public float MinRewardPerStep => minRewardPerStep;
        public float MaxTotalEpisodeReward => maxTotalEpisodeReward;
        public float MinTotalEpisodeReward => minTotalEpisodeReward;
        public bool EnableRewardValidation => enableRewardValidation;
        public bool EnableDebugLogging => enableDebugLogging;
        public float RewardCalculationFrequency => rewardCalculationFrequency;
        public IReadOnlyList<RewardProviderConfig> ProviderConfigs => providerConfigs.AsReadOnly();
        public bool SortProvidersByPriority => sortProvidersByPriority;
        public bool AutoDiscoverProviders => autoDiscoverProviders;
        public IReadOnlyList<string> RegisteredEventNames => registeredEventNames.AsReadOnly();
        public bool EnableEventLogging => enableEventLogging;
        
        [Serializable]
        public class RewardProviderConfig {
            [Header("Provider Identity")]
            public string providerName = "Reward Provider";
            public string providerType = "";
            public bool isActive = true;
            public int priority = 0;
            
            [Header("Reward Settings")]
            public float weight = 1f;
            public float minValue = -10f;
            public float maxValue = 10f;
            public bool clampToRange = true;
            
            [Header("Timing")]
            public float calculationInterval = 0f; // 0 = every step
            public bool calculateOnEvents = false;
            public List<string> triggerEvents = new List<string>();
            
            [Header("Configuration")]
            [SerializeField] List<ParameterConfig> parameters = new List<ParameterConfig>();
            
            public IReadOnlyList<ParameterConfig> Parameters => parameters.AsReadOnly();
            
            public void SetParameter(string name, object value) {
                var param = parameters.Find(p => p.name == name);
                if (param != null) {
                    param.SetValue(value);
                } else {
                    parameters.Add(new ParameterConfig { name = name });
                    parameters[parameters.Count - 1].SetValue(value);
                }
            }
            
            public T GetParameter<T>(string name, T defaultValue = default) {
                var param = parameters.Find(p => p.name == name);
                return param != null ? param.GetValue<T>() : defaultValue;
            }
            
            public bool HasParameter(string name) {
                return parameters.Find(p => p.name == name) != null;
            }
        }
        
        [Serializable]
        public class ParameterConfig {
            public string name;
            public ParameterType type;
            public string stringValue;
            public float floatValue;
            public int intValue;
            public bool boolValue;
            public Vector3 vector3Value;
            
            public enum ParameterType {
                String,
                Float,
                Int,
                Bool,
                Vector3
            }
            
            public void SetValue(object value) {
                switch (value) {
                    case string s:
                        type = ParameterType.String;
                        stringValue = s;
                        break;
                    case float f:
                        type = ParameterType.Float;
                        floatValue = f;
                        break;
                    case int i:
                        type = ParameterType.Int;
                        intValue = i;
                        break;
                    case bool b:
                        type = ParameterType.Bool;
                        boolValue = b;
                        break;
                    case Vector3 v:
                        type = ParameterType.Vector3;
                        vector3Value = v;
                        break;
                    default:
                        type = ParameterType.String;
                        stringValue = value?.ToString() ?? "";
                        break;
                }
            }
            
            public T GetValue<T>() {
                return type switch {
                    ParameterType.String when typeof(T) == typeof(string) => (T)(object)stringValue,
                    ParameterType.Float when typeof(T) == typeof(float) => (T)(object)floatValue,
                    ParameterType.Int when typeof(T) == typeof(int) => (T)(object)intValue,
                    ParameterType.Bool when typeof(T) == typeof(bool) => (T)(object)boolValue,
                    ParameterType.Vector3 when typeof(T) == typeof(Vector3) => (T)(object)vector3Value,
                    _ => default(T)
                };
            }
        }
        
        // Utility methods for configuration
        public RewardProviderConfig GetProviderConfig(string providerName) {
            return providerConfigs.Find(config => config.providerName == providerName);
        }
        
        public void AddProviderConfig(RewardProviderConfig config) {
            if (config != null && !providerConfigs.Contains(config)) {
                providerConfigs.Add(config);
            }
        }
        
        public void RemoveProviderConfig(string providerName) {
            providerConfigs.RemoveAll(config => config.providerName == providerName);
        }
        
        public void SetProviderWeight(string providerName, float weight) {
            var config = GetProviderConfig(providerName);
            if (config != null) {
                config.weight = weight;
            }
        }
        
        public void SetProviderActive(string providerName, bool active) {
            var config = GetProviderConfig(providerName);
            if (config != null) {
                config.isActive = active;
            }
        }
        
        public void SetProviderPriority(string providerName, int priority) {
            var config = GetProviderConfig(providerName);
            if (config != null) {
                config.priority = priority;
            }
        }
        
        public void AddRegisteredEvent(string eventName) {
            if (!string.IsNullOrEmpty(eventName) && !registeredEventNames.Contains(eventName)) {
                registeredEventNames.Add(eventName);
            }
        }
        
        public void RemoveRegisteredEvent(string eventName) {
            registeredEventNames.Remove(eventName);
        }
        
        public bool IsEventRegistered(string eventName) {
            return registeredEventNames.Contains(eventName);
        }
        
        // Validation methods
        public bool ValidateConfig() {
            bool isValid = true;
            
            // Validate reward limits
            if (maxRewardPerStep <= minRewardPerStep) {
                Debug.LogWarning($"[{configName}] Max reward per step ({maxRewardPerStep}) should be greater than min ({minRewardPerStep})");
                isValid = false;
            }
            
            if (maxTotalEpisodeReward <= minTotalEpisodeReward) {
                Debug.LogWarning($"[{configName}] Max total episode reward ({maxTotalEpisodeReward}) should be greater than min ({minTotalEpisodeReward})");
                isValid = false;
            }
            
            // Validate provider configs
            var providerNames = new HashSet<string>();
            foreach (var providerConfig in providerConfigs) {
                if (string.IsNullOrEmpty(providerConfig.providerName)) {
                    Debug.LogWarning($"[{configName}] Provider config has empty name");
                    isValid = false;
                    continue;
                }
                
                if (providerNames.Contains(providerConfig.providerName)) {
                    Debug.LogWarning($"[{configName}] Duplicate provider name: {providerConfig.providerName}");
                    isValid = false;
                }
                
                providerNames.Add(providerConfig.providerName);
                
                if (providerConfig.weight == 0f && providerConfig.isActive) {
                    Debug.LogWarning($"[{configName}] Active provider {providerConfig.providerName} has zero weight");
                }
                
                if (providerConfig.maxValue <= providerConfig.minValue) {
                    Debug.LogWarning($"[{configName}] Provider {providerConfig.providerName} max value should be greater than min value");
                    isValid = false;
                }
            }
            
            return isValid;
        }
        
        [ContextMenu("Validate Configuration")]
        private void ValidateFromEditor() {
            if (ValidateConfig()) {
                Debug.Log($"[{configName}] Configuration is valid");
            } else {
                Debug.LogError($"[{configName}] Configuration has issues");
            }
        }
        
        [ContextMenu("Create Default Providers")]
        private void CreateDefaultProviders() {
            // Clear existing configs
            providerConfigs.Clear();
            
            // Add common reward provider configs
            providerConfigs.Add(new RewardProviderConfig {
                providerName = "Proximity Reward",
                providerType = "ProximityRewardProvider",
                isActive = true,
                priority = 10,
                weight = 1f,
                minValue = -1f,
                maxValue = 1f
            });
            
            providerConfigs.Add(new RewardProviderConfig {
                providerName = "Time Penalty",
                providerType = "TimeRewardProvider",
                isActive = true,
                priority = 5,
                weight = -0.001f,
                minValue = -0.1f,
                maxValue = 0f
            });
            
            providerConfigs.Add(new RewardProviderConfig {
                providerName = "Boundary Penalty",
                providerType = "BoundaryRewardProvider",
                isActive = true,
                priority = 20,
                weight = -10f,
                minValue = -10f,
                maxValue = 0f,
                calculateOnEvents = true
            });
            
            providerConfigs.Add(new RewardProviderConfig {
                providerName = "Task Completion",
                providerType = "TaskCompletionRewardProvider",
                isActive = true,
                priority = 100,
                weight = 10f,
                minValue = 0f,
                maxValue = 10f,
                calculateOnEvents = true
            });
            
            // Add default events
            registeredEventNames.Clear();
            registeredEventNames.AddRange(new[] {
                "GoalReached",
                "BoundaryViolation",
                "CollisionOccurred",
                "TaskCompleted",
                "TaskFailed"
            });
            
            Debug.Log($"[{configName}] Created default provider configurations");
        }
        
        public override string ToString() {
            return $"RewardConfig[{configName}] - Providers: {providerConfigs.Count}, Events: {registeredEventNames.Count}";
        }
    }
}