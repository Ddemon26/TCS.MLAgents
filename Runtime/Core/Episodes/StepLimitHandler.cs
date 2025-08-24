using TCS.MLAgents.Core;
using TCS.MLAgents.Interfaces;

namespace TCS.MLAgents.Episodes {
    /// <summary>
    /// Episode handler that monitors step count and ends episodes when limit is reached.
    /// </summary>
    [Serializable]
    public class StepLimitHandler : EpisodeHandlerBase {
        [Header("Step Limit Settings")]
        [SerializeField] private bool enableStepLimit = true;
        [SerializeField] [Range(100, 50000)] private int maxStepsPerEpisode = 5000;
        [SerializeField] private bool warnNearLimit = true;
        [SerializeField] [Range(0.1f, 0.9f)] private float warningThreshold = 0.9f; // Warning at 90% of limit
        
        private int currentStepCount = 0;
        private bool warningTriggered = false;
        
        protected override void OnInitialize(AgentContext context) {
            currentStepCount = 0;
            warningTriggered = false;
        }
        
        public override bool ShouldStartEpisode(AgentContext context) {
            // Step limit handler doesn't control episode start
            return false;
        }
        
        public override bool ShouldEndEpisode(AgentContext context) {
            if (!enableStepLimit || !isActive) return false;
            
            return currentStepCount >= maxStepsPerEpisode;
        }
        
        protected override void OnEpisodeStart(AgentContext context) {
            currentStepCount = 0;
            warningTriggered = false;
        }
        
        protected override void OnEpisodeComplete(AgentContext context, EpisodeEndReason reason) {
            if (reason == EpisodeEndReason.MaxStepsReached) {
                Debug.Log($"[{HandlerName}] Episode ended due to step limit: {currentStepCount}/{maxStepsPerEpisode}");
            }
        }
        
        protected override void OnUpdate(AgentContext context, float deltaTime) {
            if (!enableStepLimit || !isActive) return;
            
            currentStepCount++;
            
            // Check for warning threshold
            if (warnNearLimit && !warningTriggered) {
                float progress = (float)currentStepCount / maxStepsPerEpisode;
                if (progress >= warningThreshold) {
                    warningTriggered = true;
                    context.SetSharedData("StepLimitWarning", true);
                    Debug.LogWarning($"[{HandlerName}] Approaching step limit: {currentStepCount}/{maxStepsPerEpisode}");
                }
            }
            
            // Set shared data for other systems to use
            context.SetSharedData("CurrentStepCount", currentStepCount);
            context.SetSharedData("StepProgress", (float)currentStepCount / maxStepsPerEpisode);
            
            // Check if limit reached
            if (currentStepCount >= maxStepsPerEpisode) {
                context.SetSharedData("MaxStepsReached", true);
            }
        }
        
        protected override void OnReset() {
            currentStepCount = 0;
            warningTriggered = false;
        }
        
        public override string GetDebugInfo() {
            return base.GetDebugInfo() + $", Steps={currentStepCount}/{maxStepsPerEpisode} ({GetStepProgress():P1})";
        }
        
        // Public methods for runtime configuration
        public void SetStepLimit(int maxSteps) {
            maxStepsPerEpisode = Mathf.Max(1, maxSteps);
            enableStepLimit = maxSteps > 0;
        }
        
        public void EnableStepLimit(bool enable) {
            enableStepLimit = enable;
        }
        
        public void SetWarningThreshold(float threshold) {
            warningThreshold = Mathf.Clamp01(threshold);
        }
        
        // Utility methods
        public int GetCurrentStepCount() {
            return currentStepCount;
        }
        
        public int GetMaxSteps() {
            return maxStepsPerEpisode;
        }
        
        public float GetStepProgress() {
            return maxStepsPerEpisode > 0 ? (float)currentStepCount / maxStepsPerEpisode : 0f;
        }
        
        public int GetRemainingSteps() {
            return Mathf.Max(0, maxStepsPerEpisode - currentStepCount);
        }
        
        public bool IsNearLimit() {
            return GetStepProgress() >= warningThreshold;
        }
        
        public bool IsLimitReached() {
            return enableStepLimit && currentStepCount >= maxStepsPerEpisode;
        }
    }
}