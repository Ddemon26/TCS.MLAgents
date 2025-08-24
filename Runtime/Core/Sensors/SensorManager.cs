using System.Linq;
using Unity.MLAgents.Sensors;
using TCS.MLAgents.Core;
using TCS.MLAgents.Interfaces;

namespace TCS.MLAgents.Sensors {
    /// <summary>
    /// Manages and coordinates multiple sensor providers for ML agents.
    /// Handles sensor lifecycle, registration, and integration with Unity ML-Agents.
    /// </summary>
    [Serializable]
    public class SensorManager : MonoBehaviour, IMLAgent {
        [Header("Sensor Configuration")]
        [SerializeField] private bool enableSensorSystem = true;
        [SerializeField] private bool autoDiscoverSensors = true;
        [SerializeField] private float sensorUpdateInterval = 0.02f; // 50Hz default
        [SerializeField] private bool enableSensorValidation = true;
        
        [Header("Performance Settings")]
        [SerializeField] private int maxSensorsPerFrame = 10;
        [SerializeField] private bool enableAsyncUpdates = false;
        [SerializeField] private bool enableSensorCaching = true;
        [SerializeField] private float cacheValidityTime = 0.1f;
        
        [Header("Debug Settings")]
        [SerializeField] private bool showDebugInfo = false;
        [SerializeField] private bool logSensorEvents = false;
        [SerializeField] private bool validateSensorOutput = false;
        
        // Internal state
        private List<ISensorProvider> sensorProviders = new List<ISensorProvider>();
        private List<ISensor> unityMLSensors = new List<ISensor>();
        private AgentContext context;
        private float lastSensorUpdate = 0f;
        private int currentUpdateIndex = 0;
        
        // Performance tracking
        private Dictionary<ISensorProvider, float> sensorUpdateTimes = new Dictionary<ISensorProvider, float>();
        private Dictionary<ISensorProvider, int> sensorUpdateCounts = new Dictionary<ISensorProvider, int>();
        private float totalSensorUpdateTime = 0f;
        
        // Caching
        private Dictionary<string, object> sensorCache = new Dictionary<string, object>();
        private Dictionary<string, float> cacheTimestamps = new Dictionary<string, float>();
        
        public bool IsSensorSystemEnabled => enableSensorSystem;
        public int RegisteredSensorCount => sensorProviders.Count;
        public IReadOnlyList<ISensorProvider> SensorProviders => sensorProviders.AsReadOnly();
        public IReadOnlyList<ISensor> UnityMLSensors => unityMLSensors.AsReadOnly();
        public AgentContext Context => context;
        
        public void Initialize() {
            // This will be called by MLAgentComposer
            if (context == null) {
                var composer = GetComponent<MLAgentComposer>();
                if (composer != null) {
                    context = composer.Context;
                }
            }
            
            if (context != null) {
                Initialize(context);
            }
        }
        
        public void Initialize(AgentContext agentContext) {
            context = agentContext;
            
            if (autoDiscoverSensors) {
                DiscoverSensorProviders();
            }
            
            InitializeSensorProviders();
            RegisterUnityMLSensors();
            
            if (logSensorEvents) {
                Debug.Log($"[SensorManager] Initialized with {sensorProviders.Count} sensor providers");
            }
        }
        
        public void OnEpisodeBegin() {
            OnEpisodeBegin(context);
        }
        
        public void OnEpisodeBegin(AgentContext agentContext) {
            foreach (var provider in sensorProviders) {
                if (provider.IsActive) {
                    try {
                        provider.OnEpisodeBegin(agentContext);
                    } catch (Exception e) {
                        LogError($"Error in episode begin for sensor {provider.SensorName}: {e.Message}");
                    }
                }
            }
            
            ClearSensorCache();
        }
        
        public void CollectObservations(VectorSensor sensor) {
            // Sensor manager doesn't directly collect vector observations
            // Individual sensor providers handle their own observations
        }
        
        public void OnActionReceived(Unity.MLAgents.Actuators.ActionBuffers actionBuffers) {
            // Sensor manager doesn't handle actions
        }
        
        public void Heuristic(in Unity.MLAgents.Actuators.ActionBuffers actionsOut) {
            // Sensor manager doesn't provide heuristics
        }
        
        public void FixedUpdate() {
            if (!enableSensorSystem || context == null) return;
            
            UpdateSensorSystem();
        }
        
        public void OnDestroy() {
            CleanupSensors();
        }
        
        public void EndEpisode() {
            // Relay to context if available
            context?.AddReward(0f); // No-op, just to satisfy interface
        }
        
        public void AddReward(float reward) {
            context?.AddReward(reward);
        }
        
        public void SetReward(float reward) {
            context?.SetReward(reward);
        }
        
        private void UpdateSensorSystem() {
            float currentTime = Time.time;
            
            // Check if update is needed based on interval
            if (currentTime - lastSensorUpdate < sensorUpdateInterval) {
                return;
            }
            
            if (enableAsyncUpdates) {
                UpdateSensorsAsync(currentTime);
            } else {
                UpdateSensorsImmediate(currentTime);
            }
            
            lastSensorUpdate = currentTime;
        }
        
        private void UpdateSensorsImmediate(float currentTime) {
            float deltaTime = currentTime - lastSensorUpdate;
            
            foreach (var provider in sensorProviders) {
                if (!provider.IsActive) continue;
                
                float startTime = Time.realtimeSinceStartup;
                
                try {
                    provider.UpdateSensor(context, deltaTime);
                    
                    // Track performance
                    float updateTime = Time.realtimeSinceStartup - startTime;
                    TrackSensorPerformance(provider, updateTime);
                    
                } catch (Exception e) {
                    LogError($"Error updating sensor {provider.SensorName}: {e.Message}");
                }
            }
        }
        
        private void UpdateSensorsAsync(float currentTime) {
            float deltaTime = currentTime - lastSensorUpdate;
            int sensorsUpdated = 0;
            int maxUpdates = Mathf.Min(maxSensorsPerFrame, sensorProviders.Count);
            
            while (sensorsUpdated < maxUpdates && sensorProviders.Count > 0) {
                var provider = sensorProviders[currentUpdateIndex % sensorProviders.Count];
                
                if (provider.IsActive) {
                    float startTime = Time.realtimeSinceStartup;
                    
                    try {
                        provider.UpdateSensor(context, deltaTime);
                        
                        float updateTime = Time.realtimeSinceStartup - startTime;
                        TrackSensorPerformance(provider, updateTime);
                        
                        sensorsUpdated++;
                    } catch (Exception e) {
                        LogError($"Error updating sensor {provider.SensorName}: {e.Message}");
                    }
                }
                
                currentUpdateIndex = (currentUpdateIndex + 1) % sensorProviders.Count;
            }
        }
        
        private void DiscoverSensorProviders() {
            sensorProviders.Clear();
            
            // Find all ISensorProvider components on this GameObject and children
            var providers = GetComponentsInChildren<ISensorProvider>();
            sensorProviders.AddRange(providers);
            
            // Sort by priority (higher priority first)
            sensorProviders.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            
            LogDebug($"Discovered {sensorProviders.Count} sensor providers");
        }
        
        private void InitializeSensorProviders() {
            foreach (var provider in sensorProviders) {
                try {
                    provider.Initialize(context);
                    
                    if (enableSensorValidation && !provider.ValidateSensor(context)) {
                        LogWarning($"Sensor validation failed for {provider.SensorName}");
                    }
                    
                    // Initialize performance tracking
                    sensorUpdateTimes[provider] = 0f;
                    sensorUpdateCounts[provider] = 0;
                    
                } catch (Exception e) {
                    LogError($"Failed to initialize sensor provider {provider.SensorName}: {e.Message}");
                }
            }
        }
        
        private void RegisterUnityMLSensors() {
            unityMLSensors.Clear();
            
            foreach (var provider in sensorProviders) {
                if (provider.IsActive && provider.Sensor != null) {
                    unityMLSensors.Add(provider.Sensor);
                    LogDebug($"Registered Unity ML sensor: {provider.SensorName}");
                }
            }
        }
        
        private void TrackSensorPerformance(ISensorProvider provider, float updateTime) {
            if (sensorUpdateTimes.ContainsKey(provider)) {
                sensorUpdateTimes[provider] += updateTime;
                sensorUpdateCounts[provider]++;
                totalSensorUpdateTime += updateTime;
            }
        }
        
        private void ClearSensorCache() {
            sensorCache.Clear();
            cacheTimestamps.Clear();
        }
        
        private void CleanupSensors() {
            foreach (var provider in sensorProviders) {
                try {
                    provider.Reset();
                } catch (Exception e) {
                    LogError($"Error cleaning up sensor {provider.SensorName}: {e.Message}");
                }
            }
            
            sensorProviders.Clear();
            unityMLSensors.Clear();
            ClearSensorCache();
        }
        
        // Public sensor management methods
        public void RegisterSensorProvider(ISensorProvider provider) {
            if (!sensorProviders.Contains(provider)) {
                sensorProviders.Add(provider);
                sensorProviders.Sort((a, b) => b.Priority.CompareTo(a.Priority));
                
                if (context != null) {
                    provider.Initialize(context);
                    
                    if (provider.Sensor != null) {
                        unityMLSensors.Add(provider.Sensor);
                    }
                }
                
                LogDebug($"Registered sensor provider: {provider.SensorName}");
            }
        }
        
        public void UnregisterSensorProvider(ISensorProvider provider) {
            sensorProviders.Remove(provider);
            
            if (provider.Sensor != null) {
                unityMLSensors.Remove(provider.Sensor);
            }
            
            sensorUpdateTimes.Remove(provider);
            sensorUpdateCounts.Remove(provider);
            
            LogDebug($"Unregistered sensor provider: {provider.SensorName}");
        }
        
        public ISensorProvider GetSensorProvider(string sensorName) {
            return sensorProviders.FirstOrDefault(p => p.SensorName == sensorName);
        }
        
        public T GetSensorProvider<T>() where T : class, ISensorProvider {
            return sensorProviders.OfType<T>().FirstOrDefault();
        }
        
        public List<ISensorProvider> GetSensorProviders<T>() where T : class, ISensorProvider {
            return sensorProviders.OfType<T>().Cast<ISensorProvider>().ToList();
        }
        
        public void TriggerSensorEvent(string eventName, object eventData = null) {
            foreach (var provider in sensorProviders) {
                if (provider.IsActive) {
                    try {
                        provider.OnSensorEvent(eventName, context, eventData);
                    } catch (Exception e) {
                        LogError($"Error in sensor event {eventName} for {provider.SensorName}: {e.Message}");
                    }
                }
            }
            
            if (logSensorEvents) {
                LogDebug($"Triggered sensor event: {eventName}");
            }
        }
        
        // Caching methods
        public void CacheSensorData(string key, object data) {
            if (enableSensorCaching) {
                sensorCache[key] = data;
                cacheTimestamps[key] = Time.time;
            }
        }
        
        public T GetCachedSensorData<T>(string key, T defaultValue = default(T)) {
            if (!enableSensorCaching || !sensorCache.ContainsKey(key)) {
                return defaultValue;
            }
            
            // Check cache validity
            if (Time.time - cacheTimestamps[key] > cacheValidityTime) {
                sensorCache.Remove(key);
                cacheTimestamps.Remove(key);
                return defaultValue;
            }
            
            if (sensorCache[key] is T cachedValue) {
                return cachedValue;
            }
            
            return defaultValue;
        }
        
        public bool HasCachedData(string key) {
            if (!enableSensorCaching || !sensorCache.ContainsKey(key)) {
                return false;
            }
            
            return Time.time - cacheTimestamps[key] <= cacheValidityTime;
        }
        
        // Performance and debug methods
        public float GetAverageSensorUpdateTime(ISensorProvider provider) {
            if (sensorUpdateTimes.ContainsKey(provider) && sensorUpdateCounts[provider] > 0) {
                return sensorUpdateTimes[provider] / sensorUpdateCounts[provider];
            }
            return 0f;
        }
        
        public Dictionary<string, float> GetSensorPerformanceStats() {
            var stats = new Dictionary<string, float>();
            
            foreach (var provider in sensorProviders) {
                stats[provider.SensorName] = GetAverageSensorUpdateTime(provider) * 1000f; // Convert to milliseconds
            }
            
            return stats;
        }
        
        public string GetSystemDebugInfo() {
            var info = $"SensorManager: Sensors={sensorProviders.Count}, Active={sensorProviders.Count(p => p.IsActive)}, ";
            info += $"UpdateInterval={sensorUpdateInterval:F3}s, TotalUpdateTime={totalSensorUpdateTime * 1000f:F2}ms";
            
            return info;
        }
        
        private void LogDebug(string message) {
            if (showDebugInfo) {
                Debug.Log($"[SensorManager] {message}");
            }
        }
        
        private void LogWarning(string message) {
            Debug.LogWarning($"[SensorManager] {message}");
        }
        
        private void LogError(string message) {
            Debug.LogError($"[SensorManager] {message}");
        }
        
        // Configuration methods
        public void SetSensorUpdateInterval(float interval) {
            sensorUpdateInterval = Mathf.Max(0.001f, interval);
        }
        
        public void EnableAsyncUpdates(bool enabled) {
            enableAsyncUpdates = enabled;
        }
        
        public void SetMaxSensorsPerFrame(int maxSensors) {
            maxSensorsPerFrame = Mathf.Max(1, maxSensors);
        }
        
        public void EnableSensorCaching(bool enabled) {
            enableSensorCaching = enabled;
            if (!enabled) {
                ClearSensorCache();
            }
        }
        
        public void SetCacheValidityTime(float time) {
            cacheValidityTime = Mathf.Max(0.01f, time);
        }
        
        // Unity GUI debug display
        void OnGUI() {
            if (!showDebugInfo) return;
            
            GUILayout.BeginArea(new Rect(400, 10, 300, 400));
            GUILayout.Label("Sensor Manager Debug");
            GUILayout.Label($"Sensors: {sensorProviders.Count}");
            GUILayout.Label($"Active: {sensorProviders.Count(p => p.IsActive)}");
            GUILayout.Label($"Update Rate: {1f / sensorUpdateInterval:F1}Hz");
            GUILayout.Label($"Cache Entries: {sensorCache.Count}");
            
            GUILayout.Label("Sensor Performance:");
            foreach (var provider in sensorProviders) {
                if (provider.IsActive) {
                    float avgTime = GetAverageSensorUpdateTime(provider) * 1000f;
                    GUILayout.Label($"  {provider.SensorName}: {avgTime:F2}ms");
                }
            }
            
            GUILayout.EndArea();
        }
    }
}