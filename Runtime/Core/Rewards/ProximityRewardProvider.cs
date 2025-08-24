using TCS.MLAgents.Core;
using TCS.MLAgents.Interfaces;

namespace TCS.MLAgents.Rewards {
    /// <summary>
    /// Provides rewards based on proximity to target objects.
    /// Gives positive rewards for getting closer and negative for moving away.
    /// </summary>
    [Serializable]
    public class ProximityRewardProvider : RewardProviderBase {
        [Header("Proximity Settings")]
        [SerializeField] Transform targetTransform;
        [SerializeField] bool useClosestTarget = false;
        [SerializeField] string targetTag = "Target";
        
        [Header("Distance Settings")]
        [SerializeField] float maxDistance = 20f;
        [SerializeField] float minDistance = 1f;
        [SerializeField] bool normalizeByMaxDistance = true;
        
        [Header("Reward Calculation")]
        [SerializeField] RewardMode rewardMode = RewardMode.InverseDistance;
        [SerializeField] AnimationCurve rewardCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        [SerializeField] bool rewardDistanceChange = true;
        [SerializeField] float distanceChangeMultiplier = 1f;
        
        [Header("Bonus Rewards")]
        [SerializeField] bool enableProximityBonus = true;
        [SerializeField] float bonusDistance = 2f;
        [SerializeField] float bonusReward = 0.1f;
        
        public enum RewardMode {
            InverseDistance,    // Higher reward when closer
            Distance,           // Higher reward when farther
            DistanceChange,     // Reward based on distance change
            Curve              // Use animation curve
        }
        
        private float previousDistance;
        private Transform closestTarget;
        private Transform[] availableTargets;
        
        protected override void OnInitialize(AgentContext context) {
            if (targetTransform == null && useClosestTarget) {
                RefreshAvailableTargets();
            }
            
            previousDistance = GetCurrentDistance(context);
        }
        
        protected override bool OnValidate(AgentContext context) {
            if (targetTransform == null && !useClosestTarget) {
                Debug.LogError($"[{ProviderName}] No target transform specified and useClosestTarget is false");
                return false;
            }
            
            if (maxDistance <= minDistance) {
                Debug.LogWarning($"[{ProviderName}] Max distance should be greater than min distance");
                maxDistance = minDistance + 1f;
            }
            
            return true;
        }
        
        protected override void OnEpisodeStart(AgentContext context) {
            if (useClosestTarget) {
                RefreshAvailableTargets();
            }
            
            previousDistance = GetCurrentDistance(context);
        }
        
        public override float CalculateReward(AgentContext context, float deltaTime) {
            if (context?.AgentGameObject == null) return ProcessReward(0f);
            
            // Update target if using closest target mode
            if (useClosestTarget) {
                UpdateClosestTarget(context);
            }
            
            Transform currentTarget = GetCurrentTarget();
            if (currentTarget == null) return ProcessReward(0f);
            
            float currentDistance = GetDistance(context.AgentGameObject.transform, currentTarget);
            float reward = 0f;
            
            // Calculate base reward
            switch (rewardMode) {
                case RewardMode.InverseDistance:
                    reward = CalculateInverseDistanceReward(currentDistance);
                    break;
                case RewardMode.Distance:
                    reward = CalculateDistanceReward(currentDistance);
                    break;
                case RewardMode.DistanceChange:
                    reward = CalculateDistanceChangeReward(currentDistance);
                    break;
                case RewardMode.Curve:
                    reward = CalculateCurveReward(currentDistance);
                    break;
            }
            
            // Add distance change component if enabled
            if (rewardDistanceChange && rewardMode != RewardMode.DistanceChange) {
                float distanceChange = previousDistance - currentDistance; // Positive = getting closer
                reward += distanceChange * distanceChangeMultiplier;
            }
            
            // Add proximity bonus if enabled
            if (enableProximityBonus && currentDistance <= bonusDistance) {
                reward += bonusReward;
            }
            
            previousDistance = currentDistance;
            
            return ProcessReward(reward);
        }
        
        private float CalculateInverseDistanceReward(float distance) {
            if (normalizeByMaxDistance) {
                float normalizedDistance = Mathf.Clamp01(distance / maxDistance);
                return 1f - normalizedDistance;
            } else {
                return Mathf.Max(0f, maxDistance - distance) / maxDistance;
            }
        }
        
        private float CalculateDistanceReward(float distance) {
            if (normalizeByMaxDistance) {
                return Mathf.Clamp01(distance / maxDistance);
            } else {
                return Mathf.Min(distance / maxDistance, 1f);
            }
        }
        
        private float CalculateDistanceChangeReward(float currentDistance) {
            float distanceChange = previousDistance - currentDistance; // Positive = getting closer
            return distanceChange * distanceChangeMultiplier;
        }
        
        private float CalculateCurveReward(float distance) {
            float normalizedDistance = Mathf.Clamp01(distance / maxDistance);
            return rewardCurve.Evaluate(normalizedDistance);
        }
        
        private float GetCurrentDistance(AgentContext context) {
            Transform currentTarget = GetCurrentTarget();
            if (currentTarget == null || context?.AgentGameObject == null) {
                return maxDistance;
            }
            
            return GetDistance(context.AgentGameObject.transform, currentTarget);
        }
        
        private Transform GetCurrentTarget() {
            return useClosestTarget ? closestTarget : targetTransform;
        }
        
        private void RefreshAvailableTargets() {
            if (string.IsNullOrEmpty(targetTag)) return;
            
            GameObject[] targetObjects = GameObject.FindGameObjectsWithTag(targetTag);
            availableTargets = new Transform[targetObjects.Length];
            
            for (int i = 0; i < targetObjects.Length; i++) {
                availableTargets[i] = targetObjects[i].transform;
            }
        }
        
        private void UpdateClosestTarget(AgentContext context) {
            if (availableTargets == null || availableTargets.Length == 0) {
                RefreshAvailableTargets();
                return;
            }
            
            Transform agentTransform = context.AgentGameObject.transform;
            float closestDistance = float.MaxValue;
            Transform newClosestTarget = null;
            
            foreach (Transform target in availableTargets) {
                if (target == null) continue;
                
                float distance = GetDistance(agentTransform, target);
                if (distance < closestDistance) {
                    closestDistance = distance;
                    newClosestTarget = target;
                }
            }
            
            closestTarget = newClosestTarget;
        }
        
        public override void OnRewardEvent(string eventName, AgentContext context, object eventData = null) {
            // Handle target-related events
            if (eventName == "TargetChanged" && eventData is Transform newTarget) {
                targetTransform = newTarget;
                previousDistance = GetCurrentDistance(context);
            } else if (eventName == "RefreshTargets") {
                RefreshAvailableTargets();
            }
        }
        
        public override string GetDebugInfo() {
            Transform currentTarget = GetCurrentTarget();
            string targetInfo = currentTarget != null ? currentTarget.name : "None";
            
            return base.GetDebugInfo() + 
                   $", Target={targetInfo}, Distance={previousDistance:F2}, " +
                   $"Mode={rewardMode}";
        }
        
        // Public methods for runtime configuration
        public void SetTarget(Transform target) {
            targetTransform = target;
            useClosestTarget = false;
        }
        
        public void SetUseClosestTarget(bool useClosest, string tag = "") {
            useClosestTarget = useClosest;
            if (!string.IsNullOrEmpty(tag)) {
                targetTag = tag;
            }
            if (useClosest) {
                RefreshAvailableTargets();
            }
        }
        
        public void SetDistanceRange(float min, float max) {
            minDistance = min;
            maxDistance = max;
        }
        
        public void SetRewardMode(RewardMode mode) {
            rewardMode = mode;
        }
        
        // Utility methods
        public float GetCurrentDistanceToTarget(AgentContext context) {
            return GetCurrentDistance(context);
        }
        
        public Transform GetCurrentTargetTransform() {
            return GetCurrentTarget();
        }
        
        public bool HasValidTarget() {
            return GetCurrentTarget() != null;
        }
    }
}