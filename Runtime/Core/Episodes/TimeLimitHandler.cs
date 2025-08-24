using TCS.MLAgents.Core;
using TCS.MLAgents.Interfaces;
using UnityEngine;

namespace TCS.MLAgents.Episodes {
    /// <summary>
    /// Episode handler that monitors elapsed time and ends episodes when limit is reached.
    /// </summary>
    [System.Serializable]
    public class TimeLimitHandler : EpisodeHandlerBase {
        [Header("Time Limit Settings")]
        [SerializeField] private bool enableTimeLimit = true;
        [SerializeField] [Range(1f, 300f)] private float maxEpisodeTime = 30f;
        [SerializeField] private bool warnNearLimit = true;
        [SerializeField] [Range(0.1f, 0.9f)] private float warningThreshold = 0.8f; // Warning at 80% of limit
        
        [Header("Time Scale")]
        [SerializeField] private bool useRealTime = false; // Use real time instead of scaled time
        
        private float episodeStartTime = 0f;
        private bool warningTriggered = false;
        
        protected override void OnInitialize(AgentContext context) {
            episodeStartTime = GetCurrentTime();
            warningTriggered = false;
        }
        
        public override bool ShouldStartEpisode(AgentContext context) {
            // Time limit handler doesn't control episode start
            return false;
        }
        
        public override bool ShouldEndEpisode(AgentContext context) {
            if (!enableTimeLimit || !isActive) return false;
            
            return GetElapsedTime() >= maxEpisodeTime;
        }
        
        protected override void OnEpisodeStart(AgentContext context) {
            episodeStartTime = GetCurrentTime();
            warningTriggered = false;
        }
        
        protected override void OnEpisodeComplete(AgentContext context, EpisodeEndReason reason) {
            float elapsedTime = GetElapsedTime();
            
            if (reason == EpisodeEndReason.TimeLimit) {
                Debug.Log($"[{HandlerName}] Episode ended due to time limit: {elapsedTime:F1}s/{maxEpisodeTime:F1}s");
            } else {
                Debug.Log($"[{HandlerName}] Episode completed in {elapsedTime:F1}s");
            }
        }
        
        protected override void OnUpdate(AgentContext context, float deltaTime) {
            if (!enableTimeLimit || !isActive) return;
            
            float elapsedTime = GetElapsedTime();
            float progress = elapsedTime / maxEpisodeTime;
            
            // Check for warning threshold
            if (warnNearLimit && !warningTriggered && progress >= warningThreshold) {
                warningTriggered = true;
                context.SetSharedData("TimeLimitWarning", true);
                Debug.LogWarning($"[{HandlerName}] Approaching time limit: {elapsedTime:F1}s/{maxEpisodeTime:F1}s");
            }
            
            // Set shared data for other systems to use
            context.SetSharedData("CurrentEpisodeTime", elapsedTime);
            context.SetSharedData("TimeProgress", progress);
            context.SetSharedData("RemainingTime", GetRemainingTime());
            
            // Check if limit reached
            if (elapsedTime >= maxEpisodeTime) {
                context.SetSharedData("TimeoutTriggered", true);
            }
        }
        
        protected override void OnReset() {
            episodeStartTime = GetCurrentTime();
            warningTriggered = false;
        }
        
        private float GetCurrentTime() {
            return useRealTime ? Time.realtimeSinceStartup : Time.time;
        }
        
        private float GetElapsedTime() {
            return GetCurrentTime() - episodeStartTime;
        }
        
        public override string GetDebugInfo() {
            float elapsedTime = GetElapsedTime();
            return base.GetDebugInfo() + $", Time={elapsedTime:F1}s/{maxEpisodeTime:F1}s ({GetTimeProgress():P1})";
        }
        
        // Public methods for runtime configuration
        public void SetTimeLimit(float maxTime) {
            maxEpisodeTime = Mathf.Max(0.1f, maxTime);
            enableTimeLimit = maxTime > 0f;
        }
        
        public void EnableTimeLimit(bool enable) {
            enableTimeLimit = enable;
        }
        
        public void SetWarningThreshold(float threshold) {
            warningThreshold = Mathf.Clamp01(threshold);
        }
        
        public void UseRealTime(bool realTime) {
            useRealTime = realTime;
        }
        
        // Utility methods
        public float GetCurrentEpisodeTime() {
            return GetElapsedTime();
        }
        
        public float GetMaxTime() {
            return maxEpisodeTime;
        }
        
        public float GetTimeProgress() {
            return maxEpisodeTime > 0f ? GetElapsedTime() / maxEpisodeTime : 0f;
        }
        
        public float GetRemainingTime() {
            return Mathf.Max(0f, maxEpisodeTime - GetElapsedTime());
        }
        
        public bool IsNearLimit() {
            return GetTimeProgress() >= warningThreshold;
        }
        
        public bool IsLimitReached() {
            return enableTimeLimit && GetElapsedTime() >= maxEpisodeTime;
        }
        
        public bool IsTimeUp() {
            return IsLimitReached();
        }
    }
}