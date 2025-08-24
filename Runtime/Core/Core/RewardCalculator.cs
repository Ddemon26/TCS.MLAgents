using System.Linq;
using TCS.MLAgents.Interfaces;
using Unity.MLAgents.Sensors;

namespace TCS.MLAgents.Core {
    /// <summary>
    /// Calculates and manages rewards from multiple reward providers.
    /// Aggregates rewards and provides them to the ML agent.
    /// </summary>
    public class RewardCalculator : MonoBehaviour, IMLAgent {
        [Header("Reward Calculation")]
        [SerializeField] bool autoDiscoverProviders = true;
        [SerializeField] bool debugLogging = false;
        [SerializeField] bool validateRewards = true;
        [SerializeField] bool sortByPriority = true;
        
        [Header("Reward Limits")]
        [SerializeField] bool clampTotalReward = true;
        [SerializeField] float maxRewardPerStep = 10f;
        [SerializeField] float minRewardPerStep = -10f;
        
        [Header("Registered Providers")]
        [SerializeField] List<Component> explicitProviders = new List<Component>();
        
        private List<IRewardProvider> rewardProviders;
        private AgentContext context;
        private bool isInitialized = false;
        
        // Statistics
        private float currentStepReward = 0f;
        private float totalEpisodeReward = 0f;
        private float lastCalculationTime = 0f;
        private Dictionary<IRewardProvider, float> providerContributions;
        
        public AgentContext Context => context;
        public IReadOnlyList<IRewardProvider> RewardProviders => rewardProviders?.AsReadOnly();
        public float CurrentStepReward => currentStepReward;
        public float TotalEpisodeReward => totalEpisodeReward;
        public IReadOnlyDictionary<IRewardProvider, float> ProviderContributions => 
            providerContributions?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        
        public void Initialize() {
            if (isInitialized) return;
            
            rewardProviders = new List<IRewardProvider>();
            providerContributions = new Dictionary<IRewardProvider, float>();
            
            DiscoverAndRegisterProviders();
            SortProvidersByPriority();
            InitializeProviders();
            
            lastCalculationTime = Time.time;
            
            if (debugLogging) {
                Debug.Log($"[RewardCalculator] Initialized with {rewardProviders.Count} reward providers");
            }
            
            isInitialized = true;
        }
        
        public void OnEpisodeBegin() {
            if (!isInitialized) {
                Initialize();
            }
            
            ResetEpisodeStatistics();
            
            // Notify all providers of episode start
            foreach (var provider in rewardProviders) {
                if (provider.IsActive) {
                    try {
                        provider.OnEpisodeBegin(context);
                    } catch (Exception e) {
                        Debug.LogError($"[RewardCalculator] Error in OnEpisodeBegin for provider {provider.ProviderName}: {e.Message}");
                    }
                }
            }
            
            if (debugLogging) {
                Debug.Log($"[RewardCalculator] Episode began, reset {rewardProviders.Count} providers");
            }
        }
        
        public void CollectObservations(VectorSensor sensor) {
            // Reward calculator doesn't provide observations
        }
        
        public void OnActionReceived(Unity.MLAgents.Actuators.ActionBuffers actionBuffers) {
            if (!isInitialized) {
                Debug.LogWarning("[RewardCalculator] Not initialized, skipping reward calculation");
                return;
            }
            
            CalculateStepRewards();
        }
        
        public void Heuristic(in Unity.MLAgents.Actuators.ActionBuffers actionsOut) {
            // Reward calculator doesn't provide heuristic actions
        }
        
        public void FixedUpdate() {
            // Reward calculation happens in OnActionReceived
        }
        
        public void OnDestroy() {
            rewardProviders?.Clear();
            providerContributions?.Clear();
        }
        
        public void EndEpisode() {
            // Reward calculator doesn't control episode end
        }
        
        public void AddReward(float reward) {
            // Reward calculator doesn't directly add rewards
        }
        
        public void SetReward(float reward) {
            // Reward calculator doesn't directly set rewards
        }
        
        private void CalculateStepRewards() {
            currentStepReward = 0f;
            float deltaTime = Time.time - lastCalculationTime;
            lastCalculationTime = Time.time;
            
            providerContributions.Clear();
            
            // Calculate rewards from all active providers
            foreach (var provider in rewardProviders) {
                if (!provider.IsActive) continue;
                
                try {
                    float providerReward = provider.CalculateReward(context, deltaTime);
                    
                    providerContributions[provider] = providerReward;
                    currentStepReward += providerReward;
                    
                    if (debugLogging && Mathf.Abs(providerReward) > 0.001f) {
                        Debug.Log($"[RewardCalculator] {provider.ProviderName} contributed: {providerReward:F3}");
                    }
                } catch (Exception e) {
                    Debug.LogError($"[RewardCalculator] Error calculating reward from {provider.ProviderName}: {e.Message}");
                    providerContributions[provider] = 0f;
                }
            }
            
            // Apply reward limits if enabled
            if (clampTotalReward) {
                currentStepReward = Mathf.Clamp(currentStepReward, minRewardPerStep, maxRewardPerStep);
            }
            
            // Validate rewards
            if (validateRewards) {
                ValidateStepReward();
            }
            
            // Update episode total
            totalEpisodeReward += currentStepReward;
            
            // Apply reward to agent context
            if (context != null) {
                context.AddReward(currentStepReward);
            }
            
            if (debugLogging) {
                Debug.Log($"[RewardCalculator] Step reward: {currentStepReward:F3}, Episode total: {totalEpisodeReward:F3}");
            }
        }
        
        private void ValidateStepReward() {
            if (float.IsNaN(currentStepReward) || float.IsInfinity(currentStepReward)) {
                Debug.LogWarning($"[RewardCalculator] Invalid step reward: {currentStepReward}, setting to 0");
                currentStepReward = 0f;
            }
            
            if (Mathf.Abs(currentStepReward) > 50f) {
                Debug.LogWarning($"[RewardCalculator] Unusually large step reward: {currentStepReward:F3}");
            }
        }
        
        private void DiscoverAndRegisterProviders() {
            if (autoDiscoverProviders) {
                // Find all reward providers on this GameObject and children
                IRewardProvider[] foundProviders = GetComponentsInChildren<IRewardProvider>();
                
                foreach (var provider in foundProviders) {
                    if (!rewardProviders.Contains(provider)) {
                        rewardProviders.Add(provider);
                        
                        if (debugLogging) {
                            Debug.Log($"[RewardCalculator] Discovered provider: {provider.ProviderName}");
                        }
                    }
                }
            }
            
            // Register explicitly assigned providers
            foreach (var component in explicitProviders) {
                if (component is IRewardProvider provider && !rewardProviders.Contains(provider)) {
                    rewardProviders.Add(provider);
                    
                    if (debugLogging) {
                        Debug.Log($"[RewardCalculator] Registered explicit provider: {provider.ProviderName}");
                    }
                }
            }
        }
        
        private void SortProvidersByPriority() {
            if (sortByPriority) {
                rewardProviders = rewardProviders.OrderByDescending(p => p.Priority).ThenBy(p => p.ProviderName).ToList();
            }
        }
        
        private void InitializeProviders() {
            foreach (var provider in rewardProviders) {
                try {
                    provider.Initialize(context);
                    
                    if (!provider.ValidateProvider(context)) {
                        Debug.LogWarning($"[RewardCalculator] Provider {provider.ProviderName} failed validation");
                    }
                } catch (Exception e) {
                    Debug.LogError($"[RewardCalculator] Error initializing provider {provider.ProviderName}: {e.Message}");
                }
            }
        }
        
        private void ResetEpisodeStatistics() {
            currentStepReward = 0f;
            totalEpisodeReward = 0f;
            lastCalculationTime = Time.time;
            providerContributions.Clear();
        }
        
        public void RegisterProvider(IRewardProvider provider) {
            if (provider != null && !rewardProviders.Contains(provider)) {
                rewardProviders.Add(provider);
                
                if (isInitialized) {
                    provider.Initialize(context);
                    SortProvidersByPriority();
                }
                
                if (debugLogging) {
                    Debug.Log($"[RewardCalculator] Dynamically registered provider: {provider.ProviderName}");
                }
            }
        }
        
        public void UnregisterProvider(IRewardProvider provider) {
            if (rewardProviders.Remove(provider)) {
                providerContributions.Remove(provider);
                
                if (debugLogging) {
                    Debug.Log($"[RewardCalculator] Unregistered provider: {provider.ProviderName}");
                }
            }
        }
        
        public T GetProvider<T>() where T : class, IRewardProvider {
            foreach (var provider in rewardProviders) {
                if (provider is T typedProvider) {
                    return typedProvider;
                }
            }
            return null;
        }
        
        public List<T> GetProviders<T>() where T : class, IRewardProvider {
            List<T> results = new List<T>();
            
            foreach (var provider in rewardProviders) {
                if (provider is T typedProvider) {
                    results.Add(typedProvider);
                }
            }
            
            return results;
        }
        
        public void TriggerRewardEvent(string eventName, object eventData = null) {
            foreach (var provider in rewardProviders) {
                if (provider.IsActive) {
                    try {
                        provider.OnRewardEvent(eventName, context, eventData);
                    } catch (Exception e) {
                        Debug.LogError($"[RewardCalculator] Error in reward event for {provider.ProviderName}: {e.Message}");
                    }
                }
            }
        }
        
        public float GetProviderContribution(IRewardProvider provider) {
            return providerContributions.TryGetValue(provider, out float contribution) ? contribution : 0f;
        }
        
        public Dictionary<string, float> GetProviderContributionsByName() {
            var result = new Dictionary<string, float>();
            foreach (var kvp in providerContributions) {
                result[kvp.Key.ProviderName] = kvp.Value;
            }
            return result;
        }
        
        [ContextMenu("Refresh Providers")]
        private void RefreshProviders() {
            if (Application.isPlaying) {
                rewardProviders.Clear();
                DiscoverAndRegisterProviders();
                SortProvidersByPriority();
                InitializeProviders();
                Debug.Log($"[RewardCalculator] Refreshed - found {rewardProviders.Count} providers");
            }
        }
        
        [ContextMenu("Debug Reward Info")]
        private void DebugRewardInfo() {
            Debug.Log($"[RewardCalculator] Reward System Status:");
            Debug.Log($"  Current Step Reward: {currentStepReward:F3}");
            Debug.Log($"  Total Episode Reward: {totalEpisodeReward:F3}");
            Debug.Log($"[RewardCalculator] Providers ({rewardProviders.Count}):");
            
            foreach (var provider in rewardProviders) {
                float contribution = GetProviderContribution(provider);
                Debug.Log($"  - {provider.ProviderName} (Priority: {provider.Priority}, Active: {provider.IsActive})");
                Debug.Log($"    Weight: {provider.RewardWeight:F2}, Contribution: {contribution:F3}");
                Debug.Log($"    Debug: {provider.GetDebugInfo()}");
            }
        }
        
        [ContextMenu("Reset Episode Statistics")]
        private void ResetStatistics() {
            ResetEpisodeStatistics();
            Debug.Log("[RewardCalculator] Episode statistics reset");
        }
    }
}