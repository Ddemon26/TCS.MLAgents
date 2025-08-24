using TCS.MLAgents.Core;
using TCS.MLAgents.Interfaces;

namespace TCS.MLAgents.Rewards {
    /// <summary>
    /// Provides rewards and penalties based on boundary violations.
    /// Monitors agent position relative to defined boundaries.
    /// </summary>
    [Serializable]
    public class BoundaryRewardProvider : RewardProviderBase {
        [Header("Boundary Definition")]
        [SerializeField] BoundaryType boundaryType = BoundaryType.Box;
        [SerializeField] Vector3 boundaryCenter = Vector3.zero;
        [SerializeField] Vector3 boundarySize = Vector3.one * 10f;
        [SerializeField] float boundaryRadius = 5f;
        [SerializeField] Transform boundaryTransform;
        
        [Header("Violation Settings")]
        [SerializeField] float violationPenalty = -1f;
        [SerializeField] bool penaltyOnlyOnce = false;
        [SerializeField] bool endEpisodeOnViolation = true;
        [SerializeField] float violationGracePeriod = 0.1f; // Time before penalty applies
        
        [Header("Proximity Warnings")]
        [SerializeField] bool enableProximityWarning = true;
        [SerializeField] float warningDistance = 2f;
        [SerializeField] float warningPenalty = -0.01f;
        [SerializeField] AnimationCurve warningCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
        [Header("Safe Zone Rewards")]
        [SerializeField] bool enableSafeZoneReward = false;
        [SerializeField] float safeZoneRadius = 3f;
        [SerializeField] float safeZoneReward = 0.001f;
        
        [Header("Boundary Guidance")]
        [SerializeField] bool enableBoundaryGuidance = false;
        [SerializeField] float guidanceReward = 0.01f;
        [SerializeField] float guidanceDistance = 5f;
        
        public enum BoundaryType {
            Box,        // Rectangular boundary
            Sphere,     // Spherical boundary
            Cylinder,   // Cylindrical boundary (Y-axis)
            Custom      // Use colliders or custom logic
        }
        
        private bool hasViolated = false;
        private float violationStartTime = -1f;
        private Vector3 lastValidPosition;
        private bool wasInWarningZone = false;
        
        protected override void OnInitialize(AgentContext context) {
            if (boundaryTransform != null) {
                boundaryCenter = boundaryTransform.position;
            }
            
            lastValidPosition = GetAgentPosition(context);
            hasViolated = false;
            violationStartTime = -1f;
            wasInWarningZone = false;
        }
        
        protected override bool OnValidate(AgentContext context) {
            if (boundarySize.x <= 0f || boundarySize.y <= 0f || boundarySize.z <= 0f) {
                Debug.LogWarning($"[{ProviderName}] Boundary size components should be positive");
                boundarySize = Vector3.Max(boundarySize, Vector3.one);
            }
            
            if (boundaryRadius <= 0f) {
                Debug.LogWarning($"[{ProviderName}] Boundary radius should be positive");
                boundaryRadius = 1f;
            }
            
            if (warningDistance >= GetBoundaryExtent()) {
                Debug.LogWarning($"[{ProviderName}] Warning distance should be less than boundary extent");
                warningDistance = GetBoundaryExtent() * 0.5f;
            }
            
            return true;
        }
        
        protected override void OnEpisodeStart(AgentContext context) {
            hasViolated = false;
            violationStartTime = -1f;
            wasInWarningZone = false;
            lastValidPosition = GetAgentPosition(context);
        }
        
        public override float CalculateReward(AgentContext context, float deltaTime) {
            Vector3 agentPosition = GetAgentPosition(context);
            if (agentPosition == Vector3.zero) return ProcessReward(0f); // Invalid position
            
            // Update boundary center if using transform
            if (boundaryTransform != null) {
                boundaryCenter = boundaryTransform.position;
            }
            
            bool isInsideBoundary = IsInsideBoundary(agentPosition);
            float distanceToBoundary = GetDistanceToBoundary(agentPosition);
            float reward = 0f;
            
            // Handle boundary violation
            if (!isInsideBoundary) {
                if (!hasViolated || !penaltyOnlyOnce) {
                    // Check grace period
                    if (violationStartTime < 0f) {
                        violationStartTime = Time.time;
                    }
                    
                    if (Time.time - violationStartTime >= violationGracePeriod) {
                        reward += violationPenalty;
                        hasViolated = true;
                        
                        if (endEpisodeOnViolation && context != null) {
                            context.SetSharedData("BoundaryViolation", true);
                        }
                    }
                }
            } else {
                // Reset violation state when back inside
                violationStartTime = -1f;
                lastValidPosition = agentPosition;
            }
            
            // Handle proximity warning
            if (enableProximityWarning && isInsideBoundary && distanceToBoundary <= warningDistance) {
                float warningIntensity = 1f - (distanceToBoundary / warningDistance);
                warningIntensity = warningCurve.Evaluate(warningIntensity);
                reward += warningPenalty * warningIntensity;
                
                if (!wasInWarningZone) {
                    wasInWarningZone = true;
                }
            } else {
                wasInWarningZone = false;
            }
            
            // Handle safe zone reward
            if (enableSafeZoneReward && isInsideBoundary) {
                float distanceFromCenter = Vector3.Distance(agentPosition, boundaryCenter);
                if (distanceFromCenter <= safeZoneRadius) {
                    reward += safeZoneReward;
                }
            }
            
            // Handle boundary guidance (reward for moving toward center when near boundary)
            if (enableBoundaryGuidance && isInsideBoundary && distanceToBoundary <= guidanceDistance) {
                Vector3 toCenter = (boundaryCenter - agentPosition).normalized;
                Vector3 agentVelocity = GetAgentVelocity(context);
                
                if (agentVelocity.magnitude > 0.1f) {
                    float alignment = Vector3.Dot(agentVelocity.normalized, toCenter);
                    if (alignment > 0f) {
                        reward += guidanceReward * alignment;
                    }
                }
            }
            
            return ProcessReward(reward);
        }
        
        private bool IsInsideBoundary(Vector3 position) {
            return boundaryType switch {
                BoundaryType.Box => IsInsideBox(position),
                BoundaryType.Sphere => IsInsideSphere(position),
                BoundaryType.Cylinder => IsInsideCylinder(position),
                BoundaryType.Custom => IsInsideCustomBoundary(position),
                _ => true
            };
        }
        
        private bool IsInsideBox(Vector3 position) {
            Vector3 localPos = position - boundaryCenter;
            Vector3 halfSize = boundarySize * 0.5f;
            
            return Mathf.Abs(localPos.x) <= halfSize.x &&
                   Mathf.Abs(localPos.y) <= halfSize.y &&
                   Mathf.Abs(localPos.z) <= halfSize.z;
        }
        
        private bool IsInsideSphere(Vector3 position) {
            return Vector3.Distance(position, boundaryCenter) <= boundaryRadius;
        }
        
        private bool IsInsideCylinder(Vector3 position) {
            Vector3 localPos = position - boundaryCenter;
            float horizontalDistance = Mathf.Sqrt(localPos.x * localPos.x + localPos.z * localPos.z);
            float halfHeight = boundarySize.y * 0.5f;
            
            return horizontalDistance <= boundaryRadius && Mathf.Abs(localPos.y) <= halfHeight;
        }
        
        private bool IsInsideCustomBoundary(Vector3 position) {
            // Override this method or use colliders for custom boundary logic
            return true;
        }
        
        private float GetDistanceToBoundary(Vector3 position) {
            return boundaryType switch {
                BoundaryType.Box => GetDistanceToBox(position),
                BoundaryType.Sphere => GetDistanceToSphere(position),
                BoundaryType.Cylinder => GetDistanceToCylinder(position),
                BoundaryType.Custom => GetDistanceToCustomBoundary(position),
                _ => 0f
            };
        }
        
        private float GetDistanceToBox(Vector3 position) {
            Vector3 localPos = position - boundaryCenter;
            Vector3 halfSize = boundarySize * 0.5f;
            
            Vector3 distance = Vector3.Max(Vector3.zero, 
                new Vector3(Mathf.Abs(localPos.x), Mathf.Abs(localPos.y), Mathf.Abs(localPos.z)) - halfSize);
            
            return distance.magnitude;
        }
        
        private float GetDistanceToSphere(Vector3 position) {
            return Mathf.Max(0f, Vector3.Distance(position, boundaryCenter) - boundaryRadius);
        }
        
        private float GetDistanceToCylinder(Vector3 position) {
            Vector3 localPos = position - boundaryCenter;
            float horizontalDistance = Mathf.Sqrt(localPos.x * localPos.x + localPos.z * localPos.z);
            float halfHeight = boundarySize.y * 0.5f;
            
            float horizontalDist = Mathf.Max(0f, horizontalDistance - boundaryRadius);
            float verticalDist = Mathf.Max(0f, Mathf.Abs(localPos.y) - halfHeight);
            
            return Mathf.Sqrt(horizontalDist * horizontalDist + verticalDist * verticalDist);
        }
        
        private float GetDistanceToCustomBoundary(Vector3 position) {
            return 0f; // Override for custom logic
        }
        
        private float GetBoundaryExtent() {
            return boundaryType switch {
                BoundaryType.Box => Mathf.Max(boundarySize.x, boundarySize.y, boundarySize.z) * 0.5f,
                BoundaryType.Sphere => boundaryRadius,
                BoundaryType.Cylinder => Mathf.Max(boundaryRadius, boundarySize.y * 0.5f),
                _ => 5f
            };
        }
        
        private Vector3 GetAgentPosition(AgentContext context) {
            return context?.AgentGameObject?.transform.position ?? Vector3.zero;
        }
        
        private Vector3 GetAgentVelocity(AgentContext context) {
            Rigidbody rb = context?.GetComponent<Rigidbody>();
            return rb != null ? rb.linearVelocity : Vector3.zero;
        }
        
        public override void OnRewardEvent(string eventName, AgentContext context, object eventData = null) {
            if (eventName == "SetBoundaryCenter" && eventData is Vector3 newCenter) {
                boundaryCenter = newCenter;
            } else if (eventName == "SetBoundarySize" && eventData is Vector3 newSize) {
                boundarySize = newSize;
            } else if (eventName == "SetBoundaryRadius" && eventData is float newRadius) {
                boundaryRadius = newRadius;
            } else if (eventName == "ResetViolation") {
                hasViolated = false;
                violationStartTime = -1f;
            }
        }
        
        public override string GetDebugInfo() {
            return base.GetDebugInfo() + 
                   $", HasViolated={hasViolated}, InWarningZone={wasInWarningZone}, " +
                   $"BoundaryType={boundaryType}";
        }
        
        // Public methods for runtime configuration
        public void SetBoundary(BoundaryType type, Vector3 center, Vector3 size) {
            boundaryType = type;
            boundaryCenter = center;
            boundarySize = size;
        }
        
        public void SetSphereBoundary(Vector3 center, float radius) {
            boundaryType = BoundaryType.Sphere;
            boundaryCenter = center;
            boundaryRadius = radius;
        }
        
        public void SetViolationPenalty(float penalty, bool onlyOnce = false, bool endEpisode = true) {
            violationPenalty = penalty;
            penaltyOnlyOnce = onlyOnce;
            endEpisodeOnViolation = endEpisode;
        }
        
        public void SetGracePeriod(float gracePeriod) {
            violationGracePeriod = gracePeriod;
        }
        
        public void ResetViolationState() {
            hasViolated = false;
            violationStartTime = -1f;
            wasInWarningZone = false;
        }
        
        // Utility methods
        public bool IsAgentInsideBoundary(AgentContext context) {
            return IsInsideBoundary(GetAgentPosition(context));
        }
        
        public float GetDistanceFromBoundary(AgentContext context) {
            return GetDistanceToBoundary(GetAgentPosition(context));
        }
        
        public Vector3 GetBoundaryCenter() {
            return boundaryCenter;
        }
        
        public Vector3 GetBoundarySize() {
            return boundarySize;
        }
        
        public bool HasViolatedBoundary() {
            return hasViolated;
        }
    }
}