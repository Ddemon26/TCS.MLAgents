namespace TCS.MLAgents.Core {
    /// <summary>
    /// Shared context and state container for agent components.
    /// Provides a central place for components to share data and communicate.
    /// </summary>
    [Serializable]
    public class AgentContext {
        [Header("Agent Identity")]
        [SerializeField] string agentId;
        [SerializeField] GameObject agentGameObject;
        
        [Header("Episode Information")]
        [SerializeField] int episodeCount;
        [SerializeField] float episodeStartTime;
        [SerializeField] float currentEpisodeTime;
        [SerializeField] bool isEpisodeActive;
        
        [Header("Performance Metrics")]
        [SerializeField] float cumulativeReward;
        [SerializeField] float stepCount;
        [SerializeField] float averageRewardPerStep;
        
        // Component registry for fast lookups
        private Dictionary<Type, Component> componentCache;
        private Dictionary<string, object> sharedData;
        private List<Action> episodeStartCallbacks;
        private List<Action> episodeEndCallbacks;
        
        public string AgentId => agentId;
        public GameObject AgentGameObject => agentGameObject;
        public int EpisodeCount => episodeCount;
        public float EpisodeStartTime => episodeStartTime;
        public float CurrentEpisodeTime => currentEpisodeTime;
        public bool IsEpisodeActive => isEpisodeActive;
        public float CumulativeReward => cumulativeReward;
        public float StepCount => stepCount;
        public float AverageRewardPerStep => averageRewardPerStep;
        
        public AgentContext(GameObject agentGameObject) {
            this.agentGameObject = agentGameObject;
            agentId = agentGameObject.name + "_" + agentGameObject.GetInstanceID();
            
            componentCache = new Dictionary<Type, Component>();
            sharedData = new Dictionary<string, object>();
            episodeStartCallbacks = new List<Action>();
            episodeEndCallbacks = new List<Action>();
            
            Reset();
        }
        
        public void Reset() {
            episodeCount = 0;
            episodeStartTime = 0f;
            currentEpisodeTime = 0f;
            isEpisodeActive = false;
            cumulativeReward = 0f;
            stepCount = 0f;
            averageRewardPerStep = 0f;
            
            sharedData.Clear();
        }
        
        public void StartEpisode() {
            episodeCount++;
            episodeStartTime = Time.time;
            currentEpisodeTime = 0f;
            isEpisodeActive = true;
            cumulativeReward = 0f;
            stepCount = 0f;
            averageRewardPerStep = 0f;
            
            // Invoke episode start callbacks
            foreach (var callback in episodeStartCallbacks) {
                try {
                    callback?.Invoke();
                } catch (Exception e) {
                    Debug.LogError($"Error in episode start callback: {e.Message}");
                }
            }
        }
        
        public void EndEpisode() {
            isEpisodeActive = false;
            
            // Invoke episode end callbacks
            foreach (var callback in episodeEndCallbacks) {
                try {
                    callback?.Invoke();
                } catch (Exception e) {
                    Debug.LogError($"Error in episode end callback: {e.Message}");
                }
            }
        }
        
        public void UpdateStep() {
            if (!isEpisodeActive) return;
            
            currentEpisodeTime = Time.time - episodeStartTime;
            stepCount++;
            
            if (stepCount > 0) {
                averageRewardPerStep = cumulativeReward / stepCount;
            }
        }
        
        public void AddReward(float reward) {
            cumulativeReward += reward;
        }
        
        public void SetReward(float reward) {
            cumulativeReward = reward;
        }
        
        // Component caching for performance
        public T GetComponent<T>() where T : Component {
            Type componentType = typeof(T);
            
            if (componentCache.TryGetValue(componentType, out Component cachedComponent)) {
                return cachedComponent as T;
            }
            
            T component = agentGameObject.GetComponent<T>();
            if (component != null) {
                componentCache[componentType] = component;
            }
            
            return component;
        }
        
        public bool TryGetComponent<T>(out T component) where T : Component {
            component = GetComponent<T>();
            return component != null;
        }
        
        // Shared data system for component communication
        public void SetSharedData<T>(string key, T data) {
            sharedData[key] = data;
        }
        
        public T GetSharedData<T>(string key, T defaultValue = default) {
            if (sharedData.TryGetValue(key, out object data) && data is T) {
                return (T)data;
            }
            return defaultValue;
        }
        
        public bool HasSharedData(string key) {
            return sharedData.ContainsKey(key);
        }
        
        public void RemoveSharedData(string key) {
            sharedData.Remove(key);
        }
        
        // Episode lifecycle callbacks
        public void RegisterEpisodeStartCallback(Action callback) {
            if (callback != null && !episodeStartCallbacks.Contains(callback)) {
                episodeStartCallbacks.Add(callback);
            }
        }
        
        public void UnregisterEpisodeStartCallback(Action callback) {
            episodeStartCallbacks.Remove(callback);
        }
        
        public void RegisterEpisodeEndCallback(Action callback) {
            if (callback != null && !episodeEndCallbacks.Contains(callback)) {
                episodeEndCallbacks.Add(callback);
            }
        }
        
        public void UnregisterEpisodeEndCallback(Action callback) {
            episodeEndCallbacks.Remove(callback);
        }
        
        // Debug information
        public override string ToString() {
            return $"AgentContext[{agentId}] - Episode: {episodeCount}, Time: {currentEpisodeTime:F2}s, " +
                   $"Reward: {cumulativeReward:F3}, Steps: {stepCount}, Avg: {averageRewardPerStep:F3}";
        }
    }
}