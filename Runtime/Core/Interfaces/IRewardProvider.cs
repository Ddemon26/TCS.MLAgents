using TCS.MLAgents.Core;

namespace TCS.MLAgents.Interfaces {
    /// <summary>
    /// Interface for components that calculate and provide rewards to the ML agent.
    /// Reward providers can contribute positive or negative rewards based on agent behavior.
    /// </summary>
    public interface IRewardProvider {
        /// <summary>
        /// A descriptive name for this reward provider for debugging and logging.
        /// </summary>
        string ProviderName { get; }
        
        /// <summary>
        /// Whether this provider is currently active and should calculate rewards.
        /// </summary>
        bool IsActive { get; }
        
        /// <summary>
        /// The weight/multiplier for rewards from this provider.
        /// </summary>
        float RewardWeight { get; }
        
        /// <summary>
        /// The priority of this provider when multiple providers calculate rewards.
        /// Higher priority providers are processed first.
        /// </summary>
        int Priority { get; }
        
        /// <summary>
        /// Called to calculate the current reward from this provider.
        /// Should return the raw reward value (before weight is applied).
        /// </summary>
        /// <param name="context">Agent context for accessing shared data</param>
        /// <param name="deltaTime">Time since last reward calculation</param>
        /// <returns>The reward value to add to the agent's total reward</returns>
        float CalculateReward(AgentContext context, float deltaTime);
        
        /// <summary>
        /// Called to validate that the provider can function correctly.
        /// Should check dependencies and configuration.
        /// </summary>
        /// <param name="context">Agent context for validation</param>
        /// <returns>True if the provider is valid and ready to use</returns>
        bool ValidateProvider(AgentContext context);
        
        /// <summary>
        /// Called when the provider is first initialized.
        /// Use this to set up dependencies and cache references.
        /// </summary>
        /// <param name="context">Agent context for initialization</param>
        void Initialize(AgentContext context);
        
        /// <summary>
        /// Called at the start of each episode.
        /// Use this to reset any episode-specific state.
        /// </summary>
        /// <param name="context">Agent context for episode reset</param>
        void OnEpisodeBegin(AgentContext context);
        
        /// <summary>
        /// Called when a significant event occurs that might affect rewards.
        /// Examples: goal reached, collision occurred, boundary crossed, etc.
        /// </summary>
        /// <param name="eventName">Name of the event</param>
        /// <param name="context">Agent context</param>
        /// <param name="eventData">Optional event-specific data</param>
        void OnRewardEvent(string eventName, AgentContext context, object eventData = null);
        
        /// <summary>
        /// Gets debug information about the current state of this provider.
        /// </summary>
        /// <returns>Debug string with current provider state</returns>
        string GetDebugInfo();
    }
    
    /// <summary>
    /// Base implementation of IRewardProvider with common functionality.
    /// Provides standard implementations for name, active state, and validation.
    /// </summary>
    public abstract class RewardProviderBase : IRewardProvider {
        [SerializeField] protected string providerName;
        [SerializeField] protected bool isActive = true;
        [SerializeField] protected float rewardWeight = 1f;
        [SerializeField] protected int priority = 0;
        
        public virtual string ProviderName => string.IsNullOrEmpty(providerName) ? GetType().Name : providerName;
        public virtual bool IsActive => isActive;
        public virtual float RewardWeight => rewardWeight;
        public virtual int Priority => priority;
        
        protected bool isInitialized = false;
        protected float lastRewardValue = 0f;
        protected int rewardCalculationCount = 0;
        protected float totalRewardGiven = 0f;
        
        public virtual void Initialize(AgentContext context) {
            if (isInitialized) return;
            
            OnInitialize(context);
            isInitialized = true;
            
            ResetStatistics();
        }
        
        public virtual bool ValidateProvider(AgentContext context) {
            if (context?.AgentGameObject == null) {
                Debug.LogWarning($"[{ProviderName}] AgentContext or GameObject is null");
                return false;
            }
            
            if (rewardWeight == 0f) {
                Debug.LogWarning($"[{ProviderName}] Reward weight is zero, provider will have no effect");
            }
            
            return OnValidate(context);
        }
        
        public abstract float CalculateReward(AgentContext context, float deltaTime);
        
        public virtual void OnEpisodeBegin(AgentContext context) {
            ResetStatistics();
            OnEpisodeStart(context);
        }
        
        public virtual void OnRewardEvent(string eventName, AgentContext context, object eventData = null) {
            // Override in derived classes to handle specific events
        }
        
        public virtual string GetDebugInfo() {
            return $"{ProviderName}: Active={IsActive}, Weight={RewardWeight:F2}, " +
                   $"LastReward={lastRewardValue:F3}, TotalReward={totalRewardGiven:F3}, " +
                   $"Calculations={rewardCalculationCount}";
        }
        
        /// <summary>
        /// Override this method to implement provider-specific initialization.
        /// </summary>
        protected virtual void OnInitialize(AgentContext context) {
            // Override in derived classes
        }
        
        /// <summary>
        /// Override this method to implement provider-specific validation.
        /// </summary>
        /// <param name="context">Agent context for validation</param>
        /// <returns>True if provider is valid</returns>
        protected virtual bool OnValidate(AgentContext context) {
            return true;
        }
        
        /// <summary>
        /// Override this method to implement provider-specific episode start logic.
        /// </summary>
        protected virtual void OnEpisodeStart(AgentContext context) {
            // Override in derived classes
        }
        
        /// <summary>
        /// Helper method to calculate reward and update statistics.
        /// Call this from CalculateReward implementations.
        /// </summary>
        /// <param name="rawReward">Raw reward value before weight is applied</param>
        /// <returns>Final weighted reward value</returns>
        protected float ProcessReward(float rawReward) {
            // Apply weight
            float weightedReward = rawReward * rewardWeight;
            
            // Update statistics
            lastRewardValue = weightedReward;
            rewardCalculationCount++;
            totalRewardGiven += weightedReward;
            
            // Clamp to reasonable bounds to prevent extreme rewards
            weightedReward = Mathf.Clamp(weightedReward, -100f, 100f);
            
            // Check for NaN or Infinity
            if (float.IsNaN(weightedReward) || float.IsInfinity(weightedReward)) {
                Debug.LogWarning($"[{ProviderName}] Invalid reward calculated: {weightedReward}, returning 0");
                return 0f;
            }
            
            return weightedReward;
        }
        
        /// <summary>
        /// Helper method to safely get a component with caching.
        /// </summary>
        protected T SafeGetComponent<T>(AgentContext context) where T : Component {
            return context.GetComponent<T>();
        }
        
        /// <summary>
        /// Helper method to calculate distance between two transforms.
        /// </summary>
        protected float GetDistance(Transform a, Transform b) {
            if (a == null || b == null) return float.MaxValue;
            return Vector3.Distance(a.position, b.position);
        }
        
        /// <summary>
        /// Helper method to calculate normalized distance (0 = at target, 1 = at max distance).
        /// </summary>
        protected float GetNormalizedDistance(Transform a, Transform b, float maxDistance) {
            if (maxDistance <= 0f) return 0f;
            float distance = GetDistance(a, b);
            return Mathf.Clamp01(distance / maxDistance);
        }
        
        /// <summary>
        /// Helper method to calculate velocity magnitude.
        /// </summary>
        protected float GetVelocityMagnitude(Rigidbody rb) {
            return rb != null ? rb.linearVelocity.magnitude : 0f;
        }
        
        /// <summary>
        /// Helper method to check if agent is within bounds.
        /// </summary>
        protected bool IsWithinBounds(Vector3 position, Vector3 minBounds, Vector3 maxBounds) {
            return position.x >= minBounds.x && position.x <= maxBounds.x &&
                   position.y >= minBounds.y && position.y <= maxBounds.y &&
                   position.z >= minBounds.z && position.z <= maxBounds.z;
        }
        
        /// <summary>
        /// Resets provider statistics.
        /// </summary>
        protected void ResetStatistics() {
            lastRewardValue = 0f;
            rewardCalculationCount = 0;
            totalRewardGiven = 0f;
        }
        
        /// <summary>
        /// Sets the reward weight at runtime.
        /// </summary>
        public virtual void SetRewardWeight(float weight) {
            rewardWeight = weight;
        }
        
        /// <summary>
        /// Sets the active state at runtime.
        /// </summary>
        public virtual void SetActive(bool active) {
            isActive = active;
        }
        
        /// <summary>
        /// Sets the priority at runtime.
        /// </summary>
        public virtual void SetPriority(int newPriority) {
            priority = newPriority;
        }
    }
}