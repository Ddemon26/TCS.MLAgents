using TCS.MLAgents.Core;
using TCS.MLAgents.Interfaces;
using UnityEngine;
using System.Collections.Generic;

namespace TCS.MLAgents.Rewards {
    /// <summary>
    /// Provides rewards based on task completion and goal achievement.
    /// Handles various types of objectives and milestone-based rewards.
    /// </summary>
    [System.Serializable]
    public class TaskCompletionRewardProvider : RewardProviderBase {
        [Header("Task Settings")]
        [SerializeField] TaskType taskType = TaskType.ReachTarget;
        [SerializeField] float completionReward = 1f;
        [SerializeField] float failureReward = -1f;
        [SerializeField] bool endEpisodeOnCompletion = true;
        [SerializeField] bool endEpisodeOnFailure = true;
        
        [Header("Target Settings")]
        [SerializeField] Transform targetTransform;
        [SerializeField] string targetTag = "Goal";
        [SerializeField] float completionDistance = 1f;
        [SerializeField] bool requireLineOfSight = false;
        [SerializeField] LayerMask lineOfSightMask = -1;
        
        [Header("Collection Tasks")]
        [SerializeField] string collectibleTag = "Collectible";
        [SerializeField] int requiredCollections = 1;
        [SerializeField] float perItemReward = 0.1f;
        [SerializeField] bool allowPartialRewards = true;
        
        [Header("Time-Based Tasks")]
        [SerializeField] float taskTimeLimit = 30f;
        [SerializeField] bool enableTimeLimit = false;
        [SerializeField] float timeBonusMultiplier = 1f; // Bonus = (timeLimit - actualTime) * multiplier
        
        [Header("Progress Tracking")]
        [SerializeField] bool enableProgressRewards = true;
        [SerializeField] float progressRewardInterval = 0.25f; // Every 25% progress
        [SerializeField] float progressRewardValue = 0.1f;
        [SerializeField] AnimationCurve progressCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
        [Header("Milestone Rewards")]
        [SerializeField] List<MilestoneConfig> milestones = new List<MilestoneConfig>();
        
        public enum TaskType {
            ReachTarget,        // Reach a specific transform/position
            CollectItems,       // Collect a certain number of items
            SurviveTime,        // Survive for a certain duration
            DefeatEnemies,      // Defeat a number of enemies
            Custom             // Custom task logic
        }
        
        [System.Serializable]
        public class MilestoneConfig {
            public string milestoneName;
            public float progressThreshold; // 0-1 range
            public float rewardValue;
            public bool onlyOnce = true;
            [System.NonSerialized]
            public bool achieved = false;
        }
        
        private bool taskCompleted = false;
        private bool taskFailed = false;
        private float taskStartTime;
        private int itemsCollected = 0;
        private float lastProgressCheckValue = 0f;
        private List<string> achievedMilestones = new List<string>();
        private Transform currentTarget;
        
        protected override void OnInitialize(AgentContext context) {
            taskStartTime = Time.time;
            ResetTaskState();
            
            // Find target if using tag
            if (currentTarget == null && !string.IsNullOrEmpty(targetTag)) {
                try {
                    GameObject targetObject = GameObject.FindWithTag(targetTag);
                    if (targetObject != null) {
                        currentTarget = targetObject.transform;
                    }
                } catch (UnityEngine.UnityException) {
                    // Tag doesn't exist, ignore for now
                    Debug.LogWarning($"[{ProviderName}] Tag '{targetTag}' is not defined. Target will need to be set manually.");
                }
            } else if (targetTransform != null) {
                currentTarget = targetTransform;
            }
        }
        
        protected override bool OnValidate(AgentContext context) {
            if (taskType == TaskType.ReachTarget && currentTarget == null && string.IsNullOrEmpty(targetTag)) {
                Debug.LogError($"[{ProviderName}] Reach target task requires a target transform or tag");
                return false;
            }
            
            if (completionDistance <= 0f) {
                Debug.LogWarning($"[{ProviderName}] Completion distance should be positive");
                completionDistance = 1f;
            }
            
            if (enableTimeLimit && taskTimeLimit <= 0f) {
                Debug.LogWarning($"[{ProviderName}] Task time limit should be positive");
                taskTimeLimit = 30f;
            }
            
            return true;
        }
        
        protected override void OnEpisodeStart(AgentContext context) {
            taskStartTime = Time.time;
            ResetTaskState();
        }
        
        private void ResetTaskState() {
            taskCompleted = false;
            taskFailed = false;
            itemsCollected = 0;
            lastProgressCheckValue = 0f;
            achievedMilestones.Clear();
            
            // Reset milestone achievements
            foreach (var milestone in milestones) {
                milestone.achieved = false;
            }
        }
        
        public override float CalculateReward(AgentContext context, float deltaTime) {
            if (taskCompleted || taskFailed) return ProcessReward(0f);
            
            float reward = 0f;
            
            // Check for task completion based on type
            bool justCompleted = false;
            bool justFailed = false;
            
            switch (taskType) {
                case TaskType.ReachTarget:
                    justCompleted = CheckReachTargetCompletion(context);
                    break;
                case TaskType.CollectItems:
                    justCompleted = CheckCollectItemsCompletion(context);
                    break;
                case TaskType.SurviveTime:
                    justCompleted = CheckSurviveTimeCompletion();
                    break;
                case TaskType.DefeatEnemies:
                    justCompleted = CheckDefeatEnemiesCompletion(context);
                    break;
                case TaskType.Custom:
                    justCompleted = CheckCustomTaskCompletion(context);
                    break;
            }
            
            // Check for failure conditions
            if (enableTimeLimit && !taskCompleted && GetElapsedTime() >= taskTimeLimit) {
                justFailed = true;
            }
            
            // Handle completion
            if (justCompleted) {
                taskCompleted = true;
                reward += completionReward;
                
                // Add time bonus if applicable
                if (timeBonusMultiplier > 0f) {
                    float timeBonus = CalculateTimeBonus();
                    reward += timeBonus;
                }
                
                if (endEpisodeOnCompletion && context != null) {
                    context.SetSharedData("TaskCompleted", true);
                }
            }
            
            // Handle failure
            if (justFailed) {
                taskFailed = true;
                reward += failureReward;
                
                if (endEpisodeOnFailure && context != null) {
                    context.SetSharedData("TaskFailed", true);
                }
            }
            
            // Check progress rewards
            if (enableProgressRewards && !taskCompleted && !taskFailed) {
                reward += CheckProgressRewards(context);
            }
            
            // Check milestone rewards
            reward += CheckMilestoneRewards(context);
            
            return ProcessReward(reward);
        }
        
        private bool CheckReachTargetCompletion(AgentContext context) {
            if (currentTarget == null || context?.AgentGameObject == null) return false;
            
            float distance = GetDistance(context.AgentGameObject.transform, currentTarget);
            if (distance > completionDistance) return false;
            
            // Check line of sight if required
            if (requireLineOfSight) {
                Vector3 agentPos = context.AgentGameObject.transform.position;
                Vector3 targetPos = currentTarget.position;
                
                if (Physics.Linecast(agentPos, targetPos, lineOfSightMask)) {
                    return false; // Line of sight blocked
                }
            }
            
            return true;
        }
        
        private bool CheckCollectItemsCompletion(AgentContext context) {
            return itemsCollected >= requiredCollections;
        }
        
        private bool CheckSurviveTimeCompletion() {
            return GetElapsedTime() >= taskTimeLimit;
        }
        
        private bool CheckDefeatEnemiesCompletion(AgentContext context) {
            // This would typically be tracked via events
            int enemiesDefeated = context?.GetSharedData<int>("EnemiesDefeated", 0) ?? 0;
            return enemiesDefeated >= requiredCollections; // Reusing requiredCollections for enemy count
        }
        
        private bool CheckCustomTaskCompletion(AgentContext context) {
            // Override this method or handle via events for custom task logic
            return context?.GetSharedData<bool>("CustomTaskCompleted", false) ?? false;
        }
        
        private float CheckProgressRewards(AgentContext context) {
            float progress = CalculateTaskProgress(context);
            float reward = 0f;
            
            // Check if we've crossed a progress threshold
            float progressThresholds = Mathf.Floor(progress / progressRewardInterval);
            float lastThresholds = Mathf.Floor(lastProgressCheckValue / progressRewardInterval);
            
            if (progressThresholds > lastThresholds) {
                float progressDiff = progressThresholds - lastThresholds;
                reward = progressDiff * progressRewardValue * progressCurve.Evaluate(progress);
            }
            
            lastProgressCheckValue = progress;
            return reward;
        }
        
        private float CalculateTaskProgress(AgentContext context) {
            return taskType switch {
                TaskType.ReachTarget => CalculateReachTargetProgress(context),
                TaskType.CollectItems => (float)itemsCollected / requiredCollections,
                TaskType.SurviveTime => GetElapsedTime() / taskTimeLimit,
                TaskType.DefeatEnemies => (context?.GetSharedData<int>("EnemiesDefeated", 0) ?? 0) / (float)requiredCollections,
                TaskType.Custom => context?.GetSharedData<float>("CustomTaskProgress", 0f) ?? 0f,
                _ => 0f
            };
        }
        
        private float CalculateReachTargetProgress(AgentContext context) {
            if (currentTarget == null || context?.AgentGameObject == null) return 0f;
            
            float distance = GetDistance(context.AgentGameObject.transform, currentTarget);
            float maxDistance = Vector3.Distance(Vector3.zero, currentTarget.position); // Simple max distance estimation
            
            return Mathf.Clamp01(1f - (distance / maxDistance));
        }
        
        private float CheckMilestoneRewards(AgentContext context) {
            float reward = 0f;
            float progress = CalculateTaskProgress(context);
            
            foreach (var milestone in milestones) {
                if (!milestone.achieved && progress >= milestone.progressThreshold) {
                    milestone.achieved = true;
                    reward += milestone.rewardValue;
                    achievedMilestones.Add(milestone.milestoneName);
                }
            }
            
            return reward;
        }
        
        private float CalculateTimeBonus() {
            if (!enableTimeLimit) return 0f;
            
            float elapsedTime = GetElapsedTime();
            float timeRemaining = Mathf.Max(0f, taskTimeLimit - elapsedTime);
            return timeRemaining * timeBonusMultiplier;
        }
        
        private float GetElapsedTime() {
            return Time.time - taskStartTime;
        }
        
        public override void OnRewardEvent(string eventName, AgentContext context, object eventData = null) {
            if (eventName == "ItemCollected" && taskType == TaskType.CollectItems) {
                itemsCollected++;
                
                if (allowPartialRewards) {
                    context?.AddReward(perItemReward * rewardWeight);
                }
            }
            else if (eventName == "EnemyDefeated" && taskType == TaskType.DefeatEnemies) {
                int currentCount = context?.GetSharedData<int>("EnemiesDefeated", 0) ?? 0;
                context?.SetSharedData("EnemiesDefeated", currentCount + 1);
                
                if (allowPartialRewards) {
                    context?.AddReward(perItemReward * rewardWeight);
                }
            }
            else if (eventName == "SetTarget" && eventData is Transform newTarget) {
                currentTarget = newTarget;
            }
            else if (eventName == "TaskReset") {
                ResetTaskState();
                taskStartTime = Time.time;
            }
        }
        
        public override string GetDebugInfo() {
            return base.GetDebugInfo() + 
                   $", TaskType={taskType}, " +
                   $"Completed={taskCompleted}, Failed={taskFailed}, " +
                   $"ElapsedTime={GetElapsedTime():F1}s";
        }
        
        public string GetDebugInfo(AgentContext context) {
            float progress = CalculateTaskProgress(context);
            return GetDebugInfo() + $", Progress={progress:P1}";
        }
        
        // Public methods for runtime configuration
        public void SetTarget(Transform target) {
            currentTarget = target;
            targetTransform = target;
        }
        
        public void SetTaskType(TaskType type) {
            taskType = type;
            
            // Clear target tag for task types that don't need targets
            if (type != TaskType.ReachTarget) {
                targetTag = "";
            }
            
            ResetTaskState();
        }
        
        public void SetCompletionReward(float reward) {
            completionReward = reward;
        }
        
        public void SetTimeLimit(float timeLimit) {
            taskTimeLimit = timeLimit;
            enableTimeLimit = timeLimit > 0f;
        }
        
        public void ResetTask() {
            ResetTaskState();
            taskStartTime = Time.time;
        }
        
        // Utility methods
        public bool IsTaskCompleted() {
            return taskCompleted;
        }
        
        public bool IsTaskFailed() {
            return taskFailed;
        }
        
        public float GetTaskProgress(AgentContext context) {
            return CalculateTaskProgress(context);
        }
        
        public float GetRemainingTime() {
            if (!enableTimeLimit) return float.MaxValue;
            return Mathf.Max(0f, taskTimeLimit - GetElapsedTime());
        }
        
        public int GetItemsCollected() {
            return itemsCollected;
        }
        
        public List<string> GetAchievedMilestones() {
            return new List<string>(achievedMilestones);
        }
    }
}