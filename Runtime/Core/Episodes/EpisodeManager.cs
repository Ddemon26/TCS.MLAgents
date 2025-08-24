using System.Linq;
using TCS.MLAgents.Core;
using TCS.MLAgents.Interfaces;
using TCS.MLAgents.Configuration;

namespace TCS.MLAgents.Episodes {
    /// <summary>
    /// Manages episode lifecycle and coordinates multiple episode handlers.
    /// Handles episode initialization, termination conditions, and state transitions.
    /// </summary>
    [Serializable]
    public class EpisodeManager : MonoBehaviour, IMLAgent {
        [Header("Episode Settings")]
        [SerializeField] private bool autoStartEpisodes = true;
        [SerializeField] private float episodeStartDelay = 0.1f;
        [SerializeField] private bool logEpisodeEvents = true;
        
        [Header("Configuration")]
        [SerializeField] private EpisodeConfig episodeConfig;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        
        private List<IEpisodeHandler> episodeHandlers = new List<IEpisodeHandler>();
        private AgentContext context;
        private bool isEpisodeActive = false;
        private bool pendingEpisodeStart = false;
        private bool pendingEpisodeEnd = false;
        private EpisodeEndReason pendingEndReason = EpisodeEndReason.Success;
        private float episodeStartTimer = 0f;
        
        // Episode statistics
        private int totalEpisodes = 0;
        private float currentEpisodeStartTime = 0f;
        private float totalEpisodeTime = 0f;
        private Dictionary<EpisodeEndReason, int> endReasonCounts = new Dictionary<EpisodeEndReason, int>();
        
        public bool IsEpisodeActive => isEpisodeActive;
        public int TotalEpisodes => totalEpisodes;
        public float CurrentEpisodeDuration => isEpisodeActive ? Time.time - currentEpisodeStartTime : 0f;
        public IReadOnlyList<IEpisodeHandler> EpisodeHandlers => episodeHandlers.AsReadOnly();
        
        // IMLAgent interface properties
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
            
            // Auto-discover episode handlers
            DiscoverEpisodeHandlers();
            
            // Apply configuration if available
            if (episodeConfig != null) {
                ApplyConfiguration(episodeConfig);
            }
            
            // Initialize all handlers
            InitializeHandlers();
            
            // Initialize end reason counts
            InitializeEndReasonCounts();
            
            if (logEpisodeEvents) {
                Debug.Log($"[EpisodeManager] Initialized with {episodeHandlers.Count} handlers");
            }
        }
        
        public void OnEpisodeBegin() {
            if (autoStartEpisodes) {
                RequestEpisodeStart();
            }
        }
        
        public void OnEpisodeBegin(AgentContext agentContext) {
            if (autoStartEpisodes) {
                RequestEpisodeStart();
            }
        }
        
        public void CollectObservations(Unity.MLAgents.Sensors.VectorSensor sensor) {
            // Episode managers don't typically add observations directly
            // but can be extended to do so if needed
        }
        
        public void OnActionReceived(Unity.MLAgents.Actuators.ActionBuffers actionBuffers) {
            // Episode managers don't typically handle actions directly
        }
        
        public void Heuristic(in Unity.MLAgents.Actuators.ActionBuffers actionsOut) {
            // Episode managers don't typically provide heuristics
        }
        
        public void FixedUpdate() {
            if (context == null) return;
            
            float deltaTime = Time.fixedDeltaTime;
            
            // Handle pending episode start
            if (pendingEpisodeStart) {
                episodeStartTimer += deltaTime;
                if (episodeStartTimer >= episodeStartDelay) {
                    ExecuteEpisodeStart();
                }
            }
            
            // Handle pending episode end
            if (pendingEpisodeEnd) {
                ExecuteEpisodeEnd();
            }
            
            // Update active episode
            if (isEpisodeActive) {
                UpdateEpisode(deltaTime);
                CheckEpisodeEndConditions();
            }
            
            // Check for episode start conditions
            if (!isEpisodeActive && !pendingEpisodeStart && autoStartEpisodes) {
                CheckEpisodeStartConditions();
            }
        }
        
        public void OnDestroy() {
            if (isEpisodeActive) {
                ForceEndEpisode(EpisodeEndReason.ManualReset);
            }
        }
        
        public void EndEpisode() {
            RequestEpisodeEnd(EpisodeEndReason.ManualReset);
        }
        
        public void AddReward(float reward) {
            // Episode managers don't directly handle rewards, but can relay to context
            context?.AddReward(reward);
        }
        
        public void SetReward(float reward) {
            // Episode managers don't directly handle rewards, but can relay to context
            context?.SetReward(reward);
        }
        
        private void DiscoverEpisodeHandlers() {
            episodeHandlers.Clear();
            
            // Find all IEpisodeHandler components on this GameObject and children
            var handlers = GetComponentsInChildren<IEpisodeHandler>();
            episodeHandlers.AddRange(handlers);
            
            // Sort by priority (higher priority first)
            episodeHandlers.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }
        
        private void InitializeHandlers() {
            foreach (var handler in episodeHandlers) {
                try {
                    handler.Initialize(context);
                } catch (Exception e) {
                    Debug.LogError($"[EpisodeManager] Failed to initialize handler {handler.HandlerName}: {e.Message}");
                }
            }
        }
        
        private void InitializeEndReasonCounts() {
            endReasonCounts.Clear();
            foreach (EpisodeEndReason reason in Enum.GetValues(typeof(EpisodeEndReason))) {
                endReasonCounts[reason] = 0;
            }
        }
        
        private void ApplyConfiguration(EpisodeConfig config) {
            // Apply configuration settings
            autoStartEpisodes = config.autoStartEpisodes;
            episodeStartDelay = config.episodeStartDelay;
            logEpisodeEvents = config.logEpisodeEvents;
            
            // Configure handlers based on config
            foreach (var handlerConfig in config.handlerConfigs) {
                var handler = episodeHandlers.FirstOrDefault(h => h.HandlerName == handlerConfig.handlerName);
                if (handler != null && handler is EpisodeHandlerBase baseHandler) {
                    baseHandler.SetActive(handlerConfig.isActive);
                    baseHandler.SetPriority(handlerConfig.priority);
                }
            }
        }
        
        private void CheckEpisodeStartConditions() {
            foreach (var handler in episodeHandlers) {
                if (!handler.IsActive) continue;
                
                try {
                    if (handler.ShouldStartEpisode(context)) {
                        RequestEpisodeStart();
                        break;
                    }
                } catch (Exception e) {
                    Debug.LogError($"[EpisodeManager] Error checking start condition for {handler.HandlerName}: {e.Message}");
                }
            }
        }
        
        private void CheckEpisodeEndConditions() {
            foreach (var handler in episodeHandlers) {
                if (!handler.IsActive) continue;
                
                try {
                    if (handler.ShouldEndEpisode(context)) {
                        RequestEpisodeEnd(EpisodeEndReason.Success);
                        break;
                    }
                } catch (Exception e) {
                    Debug.LogError($"[EpisodeManager] Error checking end condition for {handler.HandlerName}: {e.Message}");
                }
            }
            
            // Check shared data for episode end signals
            CheckSharedDataEndConditions();
        }
        
        private void CheckSharedDataEndConditions() {
            if (context.GetSharedData<bool>("TaskCompleted", false)) {
                RequestEpisodeEnd(EpisodeEndReason.Success);
            } else if (context.GetSharedData<bool>("TaskFailed", false)) {
                RequestEpisodeEnd(EpisodeEndReason.Failure);
            } else if (context.GetSharedData<bool>("BoundaryViolation", false)) {
                RequestEpisodeEnd(EpisodeEndReason.BoundaryViolation);
            } else if (context.GetSharedData<bool>("TimeoutTriggered", false)) {
                RequestEpisodeEnd(EpisodeEndReason.TimeLimit);
            }
        }
        
        private void UpdateEpisode(float deltaTime) {
            foreach (var handler in episodeHandlers) {
                if (!handler.IsActive) continue;
                
                try {
                    handler.OnEpisodeUpdate(context, deltaTime);
                } catch (Exception e) {
                    Debug.LogError($"[EpisodeManager] Error updating handler {handler.HandlerName}: {e.Message}");
                }
            }
        }
        
        private void ExecuteEpisodeStart() {
            pendingEpisodeStart = false;
            episodeStartTimer = 0f;
            
            if (isEpisodeActive) return; // Already active
            
            isEpisodeActive = true;
            totalEpisodes++;
            currentEpisodeStartTime = Time.time;
            
            // Clear shared data episode end flags
            context.SetSharedData("TaskCompleted", false);
            context.SetSharedData("TaskFailed", false);
            context.SetSharedData("BoundaryViolation", false);
            context.SetSharedData("TimeoutTriggered", false);
            
            // Notify all handlers
            foreach (var handler in episodeHandlers) {
                if (!handler.IsActive) continue;
                
                try {
                    handler.OnEpisodeBegin(context);
                } catch (Exception e) {
                    Debug.LogError($"[EpisodeManager] Error starting episode for handler {handler.HandlerName}: {e.Message}");
                }
            }
            
            // Start the episode in the context
            context.StartEpisode();
            
            if (logEpisodeEvents) {
                Debug.Log($"[EpisodeManager] Episode {totalEpisodes} started");
            }
        }
        
        private void ExecuteEpisodeEnd() {
            pendingEpisodeEnd = false;
            
            if (!isEpisodeActive) return; // Not active
            
            float episodeDuration = Time.time - currentEpisodeStartTime;
            totalEpisodeTime += episodeDuration;
            endReasonCounts[pendingEndReason]++;
            
            // Notify all handlers
            foreach (var handler in episodeHandlers) {
                if (!handler.IsActive) continue;
                
                try {
                    handler.OnEpisodeEnd(context, pendingEndReason);
                } catch (Exception e) {
                    Debug.LogError($"[EpisodeManager] Error ending episode for handler {handler.HandlerName}: {e.Message}");
                }
            }
            
            isEpisodeActive = false;
            
            if (logEpisodeEvents) {
                Debug.Log($"[EpisodeManager] Episode {totalEpisodes} ended: {pendingEndReason}, Duration: {episodeDuration:F2}s");
            }
        }
        
        // Public methods for manual episode control
        public void RequestEpisodeStart() {
            if (isEpisodeActive || pendingEpisodeStart) return;
            
            pendingEpisodeStart = true;
            episodeStartTimer = 0f;
        }
        
        public void RequestEpisodeEnd(EpisodeEndReason reason) {
            if (!isEpisodeActive || pendingEpisodeEnd) return;
            
            pendingEpisodeEnd = true;
            pendingEndReason = reason;
        }
        
        public void ForceEndEpisode(EpisodeEndReason reason) {
            if (!isEpisodeActive) return;
            
            pendingEpisodeEnd = true;
            pendingEndReason = reason;
            ExecuteEpisodeEnd();
        }
        
        public void ResetAllHandlers() {
            foreach (var handler in episodeHandlers) {
                try {
                    handler.Reset();
                } catch (Exception e) {
                    Debug.LogError($"[EpisodeManager] Error resetting handler {handler.HandlerName}: {e.Message}");
                }
            }
        }
        
        // Handler management
        public void RegisterHandler(IEpisodeHandler handler) {
            if (!episodeHandlers.Contains(handler)) {
                episodeHandlers.Add(handler);
                episodeHandlers.Sort((a, b) => b.Priority.CompareTo(a.Priority));
                
                if (context != null) {
                    handler.Initialize(context);
                }
            }
        }
        
        public void UnregisterHandler(IEpisodeHandler handler) {
            episodeHandlers.Remove(handler);
        }
        
        // Configuration
        public void SetConfiguration(EpisodeConfig config) {
            episodeConfig = config;
            if (context != null) {
                ApplyConfiguration(config);
            }
        }
        
        // Statistics
        public float GetAverageEpisodeDuration() {
            return totalEpisodes > 0 ? totalEpisodeTime / totalEpisodes : 0f;
        }
        
        public int GetEndReasonCount(EpisodeEndReason reason) {
            return endReasonCounts.GetValueOrDefault(reason, 0);
        }
        
        public Dictionary<EpisodeEndReason, int> GetEndReasonStats() {
            return new Dictionary<EpisodeEndReason, int>(endReasonCounts);
        }
        
        // Debug
        public string GetDebugInfo() {
            var info = $"EpisodeManager: Episodes={totalEpisodes}, Active={isEpisodeActive}, Handlers={episodeHandlers.Count}";
            
            if (isEpisodeActive) {
                info += $", Duration={CurrentEpisodeDuration:F1}s";
            }
            
            return info;
        }
        
        void OnGUI() {
            if (!showDebugInfo) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 400, 300));
            GUILayout.Label($"Episode Manager Debug");
            GUILayout.Label($"Episodes: {totalEpisodes}");
            GUILayout.Label($"Active: {isEpisodeActive}");
            GUILayout.Label($"Handlers: {episodeHandlers.Count}");
            
            if (isEpisodeActive) {
                GUILayout.Label($"Current Duration: {CurrentEpisodeDuration:F1}s");
            }
            
            GUILayout.Label("End Reason Stats:");
            foreach (var kvp in endReasonCounts) {
                if (kvp.Value > 0) {
                    GUILayout.Label($"  {kvp.Key}: {kvp.Value}");
                }
            }
            
            GUILayout.EndArea();
        }
    }
}