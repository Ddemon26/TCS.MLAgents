using TCS.MLAgents.Core;
using TCS.MLAgents.Interfaces;

namespace TCS.MLAgents.Rewards {
    /// <summary>
    /// Provides time-based rewards and penalties.
    /// Can provide constant time penalties or time-based bonuses.
    /// </summary>
    [Serializable]
    public class TimeRewardProvider : RewardProviderBase {
        [Header("Time Penalty Settings")]
        [SerializeField] bool enableTimePenalty = true;
        [SerializeField] float timePenaltyPerSecond = -0.001f;
        [SerializeField] float maxTimePenaltyPerStep = -0.01f;
        
        [Header("Time Bonus Settings")]
        [SerializeField] bool enableTimeBonus = false;
        [SerializeField] float timeBonusThreshold = 10f; // Seconds
        [SerializeField] float timeBonusPerSecond = 0.01f;
        [SerializeField] bool bonusOnlyAfterThreshold = true;
        
        [Header("Episode Time Limits")]
        [SerializeField] bool enableTimeLimit = false;
        [SerializeField] float episodeTimeLimit = 30f;
        [SerializeField] float timeLimitPenalty = -1f;
        [SerializeField] bool endEpisodeOnTimeLimit = false;
        
        [Header("Efficiency Rewards")]
        [SerializeField] bool enableEfficiencyReward = false;
        [SerializeField] float targetCompletionTime = 15f;
        [SerializeField] float maxEfficiencyBonus = 1f;
        [SerializeField] AnimationCurve efficiencyCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        
        private float episodeStartTime;
        private float lastRewardTime;
        private bool timeoutTriggered = false;
        private bool efficiencyBonusGiven = false;
        
        protected override void OnInitialize(AgentContext context) {
            episodeStartTime = Time.time;
            lastRewardTime = Time.time;
            timeoutTriggered = false;
            efficiencyBonusGiven = false;
        }
        
        protected override bool OnValidate(AgentContext context) {
            if (enableTimeLimit && episodeTimeLimit <= 0f) {
                Debug.LogWarning($"[{ProviderName}] Episode time limit should be positive");
                episodeTimeLimit = 30f;
            }
            
            if (enableTimeBonus && timeBonusThreshold < 0f) {
                Debug.LogWarning($"[{ProviderName}] Time bonus threshold should be non-negative");
                timeBonusThreshold = 0f;
            }
            
            return true;
        }
        
        protected override void OnEpisodeStart(AgentContext context) {
            episodeStartTime = Time.time;
            lastRewardTime = Time.time;
            timeoutTriggered = false;
            efficiencyBonusGiven = false;
        }
        
        public override float CalculateReward(AgentContext context, float deltaTime) {
            float currentTime = Time.time;
            float episodeTime = currentTime - episodeStartTime;
            float reward = 0f;
            
            // Apply time penalty
            if (enableTimePenalty) {
                float timePenalty = timePenaltyPerSecond * deltaTime;
                timePenalty = Mathf.Max(timePenalty, maxTimePenaltyPerStep);
                reward += timePenalty;
            }
            
            // Apply time bonus
            if (enableTimeBonus) {
                bool shouldGiveBonus = !bonusOnlyAfterThreshold || episodeTime >= timeBonusThreshold;
                if (shouldGiveBonus) {
                    reward += timeBonusPerSecond * deltaTime;
                }
            }
            
            // Check time limit
            if (enableTimeLimit && !timeoutTriggered && episodeTime >= episodeTimeLimit) {
                timeoutTriggered = true;
                reward += timeLimitPenalty;
                
                if (endEpisodeOnTimeLimit && context != null) {
                    // Trigger episode end event
                    context.SetSharedData("TimeoutTriggered", true);
                }
            }
            
            lastRewardTime = currentTime;
            
            return ProcessReward(reward);
        }
        
        public override void OnRewardEvent(string eventName, AgentContext context, object eventData = null) {
            // Handle task completion for efficiency bonus
            if (eventName == "TaskCompleted" && enableEfficiencyReward && !efficiencyBonusGiven) {
                float completionTime = Time.time - episodeStartTime;
                float efficiencyBonus = CalculateEfficiencyBonus(completionTime);
                
                if (efficiencyBonus > 0f) {
                    // Add bonus to context for immediate application
                    context?.AddReward(efficiencyBonus * rewardWeight);
                    efficiencyBonusGiven = true;
                }
            }
            // Handle episode reset events
            else if (eventName == "EpisodeReset" || eventName == "EpisodeBegin") {
                OnEpisodeStart(context);
            }
            // Handle time limit changes
            else if (eventName == "SetTimeLimit" && eventData is float newLimit) {
                episodeTimeLimit = newLimit;
                timeoutTriggered = false;
            }
        }
        
        private float CalculateEfficiencyBonus(float completionTime) {
            if (completionTime >= targetCompletionTime) {
                return 0f; // No bonus for slow completion
            }
            
            float efficiency = 1f - (completionTime / targetCompletionTime);
            efficiency = Mathf.Clamp01(efficiency);
            
            return efficiencyCurve.Evaluate(efficiency) * maxEfficiencyBonus;
        }
        
        public override string GetDebugInfo() {
            float currentEpisodeTime = Time.time - episodeStartTime;
            
            return base.GetDebugInfo() + 
                   $", EpisodeTime={currentEpisodeTime:F1}s, " +
                   $"TimeoutTriggered={timeoutTriggered}, " +
                   $"EfficiencyBonusGiven={efficiencyBonusGiven}";
        }
        
        // Public methods for runtime configuration
        public void SetTimePenalty(float penaltyPerSecond) {
            timePenaltyPerSecond = penaltyPerSecond;
            enableTimePenalty = penaltyPerSecond != 0f;
        }
        
        public void SetTimeBonus(float bonusPerSecond, float threshold = 0f) {
            timeBonusPerSecond = bonusPerSecond;
            timeBonusThreshold = threshold;
            enableTimeBonus = bonusPerSecond != 0f;
        }
        
        public void SetTimeLimit(float limitSeconds, float penalty = -1f, bool endOnLimit = false) {
            episodeTimeLimit = limitSeconds;
            timeLimitPenalty = penalty;
            endEpisodeOnTimeLimit = endOnLimit;
            enableTimeLimit = limitSeconds > 0f;
            timeoutTriggered = false;
        }
        
        public void SetEfficiencyReward(float targetTime, float maxBonus) {
            targetCompletionTime = targetTime;
            maxEfficiencyBonus = maxBonus;
            enableEfficiencyReward = maxBonus > 0f;
        }
        
        public void ResetEpisodeTime() {
            episodeStartTime = Time.time;
            lastRewardTime = Time.time;
            timeoutTriggered = false;
            efficiencyBonusGiven = false;
        }
        
        // Utility methods
        public float GetCurrentEpisodeTime() {
            return Time.time - episodeStartTime;
        }
        
        public float GetRemainingTime() {
            if (!enableTimeLimit) return float.MaxValue;
            return Mathf.Max(0f, episodeTimeLimit - GetCurrentEpisodeTime());
        }
        
        public bool IsTimeoutTriggered() {
            return timeoutTriggered;
        }
        
        public bool IsEfficiencyBonusAvailable() {
            return enableEfficiencyReward && !efficiencyBonusGiven;
        }
        
        public float GetPotentialEfficiencyBonus() {
            if (!IsEfficiencyBonusAvailable()) return 0f;
            return CalculateEfficiencyBonus(GetCurrentEpisodeTime());
        }
        
        public float GetTimeToEfficiencyTarget() {
            return Mathf.Max(0f, targetCompletionTime - GetCurrentEpisodeTime());
        }
    }
}