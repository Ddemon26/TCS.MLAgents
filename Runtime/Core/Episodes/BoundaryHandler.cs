using TCS.MLAgents.Core;
using TCS.MLAgents.Interfaces;
using UnityEngine;

namespace TCS.MLAgents.Episodes {
    /// <summary>
    /// Episode handler that monitors boundary violations and manages episode termination.
    /// Works in conjunction with BoundaryRewardProvider.
    /// </summary>
    [System.Serializable]
    public class BoundaryHandler : EpisodeHandlerBase {
        [Header("Boundary Settings")]
        [SerializeField] private BoundaryType boundaryType = BoundaryType.Box;
        [SerializeField] private Vector3 boundaryCenter = Vector3.zero;
        [SerializeField] private Vector3 boundarySize = Vector3.one * 10f;
        [SerializeField] private float boundaryRadius = 5f;
        [SerializeField] private Transform boundaryTransform;
        
        [Header("Episode Control")]
        [SerializeField] private bool endEpisodeOnViolation = true;
        [SerializeField] private float violationGracePeriod = 0.1f;
        [SerializeField] private bool allowReturnToBounds = true;
        [SerializeField] private float returnGracePeriod = 2f;
        
        [Header("Monitoring")]
        [SerializeField] private bool trackViolationHistory = true;
        [SerializeField] private bool warnNearBoundary = true;
        [SerializeField] private float warningDistance = 2f;
        
        public enum BoundaryType {
            Box,        // Rectangular boundary
            Sphere,     // Spherical boundary
            Cylinder,   // Cylindrical boundary (Y-axis)
            Custom      // Use colliders or custom logic
        }
        
        private bool isViolating = false;
        private bool wasViolating = false;
        private float violationStartTime = -1f;
        private float returnStartTime = -1f;
        private bool warningTriggered = false;
        private int violationCount = 0;
        private float totalViolationTime = 0f;
        
        protected override void OnInitialize(AgentContext context) {
            ResetBoundaryState();
            
            if (boundaryTransform != null) {
                boundaryCenter = boundaryTransform.position;
            }
        }
        
        public override bool ShouldStartEpisode(AgentContext context) {
            // Boundary handler doesn't control episode start
            return false;
        }
        
        public override bool ShouldEndEpisode(AgentContext context) {
            if (!isActive || !endEpisodeOnViolation) return false;
            
            UpdateBoundaryState(context);
            
            // Check if violation grace period has elapsed
            if (isViolating && violationStartTime >= 0f) {
                float violationDuration = Time.time - violationStartTime;
                if (violationDuration >= violationGracePeriod) {
                    return true;
                }
            }
            
            return false;
        }
        
        protected override void OnEpisodeStart(AgentContext context) {
            ResetBoundaryState();
            
            // Clear boundary violation flags
            context.SetSharedData("BoundaryViolation", false);
            context.SetSharedData("BoundaryWarning", false);
        }
        
        protected override void OnEpisodeComplete(AgentContext context, EpisodeEndReason reason) {
            if (reason == EpisodeEndReason.BoundaryViolation) {
                Debug.Log($"[{HandlerName}] Episode ended due to boundary violation. " +
                         $"Violations: {violationCount}, Total violation time: {totalViolationTime:F2}s");
            }
        }
        
        protected override void OnUpdate(AgentContext context, float deltaTime) {
            if (!isActive) return;
            
            UpdateBoundaryState(context);
            UpdateBoundaryWarnings(context);
            TrackViolationStats(deltaTime);
            
            // Set shared data for other systems
            context.SetSharedData("IsInsideBoundary", !isViolating);
            context.SetSharedData("BoundaryDistance", GetDistanceToBoundary(context));
            context.SetSharedData("BoundaryViolationCount", violationCount);
            context.SetSharedData("TotalViolationTime", totalViolationTime);
        }
        
        private void UpdateBoundaryState(AgentContext context) {
            Vector3 agentPosition = GetAgentPosition(context);
            if (agentPosition == Vector3.zero) return; // Invalid position
            
            // Update boundary center if using transform
            if (boundaryTransform != null) {
                boundaryCenter = boundaryTransform.position;
            }
            
            wasViolating = isViolating;
            isViolating = !IsInsideBoundary(agentPosition);
            
            // Handle violation start
            if (isViolating && !wasViolating) {
                violationStartTime = Time.time;
                violationCount++;
                context.SetSharedData("BoundaryViolation", true);
                
                Debug.LogWarning($"[{HandlerName}] Boundary violation detected at {agentPosition}");
            }
            
            // Handle violation end (return to bounds)
            if (!isViolating && wasViolating) {
                violationStartTime = -1f;
                returnStartTime = Time.time;
                context.SetSharedData("BoundaryViolation", false);
                
                Debug.Log($"[{HandlerName}] Agent returned to bounds");
            }
        }
        
        private void UpdateBoundaryWarnings(AgentContext context) {
            if (!warnNearBoundary) return;
            
            float distanceToBoundary = GetDistanceToBoundary(context);
            bool nearBoundary = distanceToBoundary <= warningDistance;
            
            if (nearBoundary && !warningTriggered && !isViolating) {
                warningTriggered = true;
                context.SetSharedData("BoundaryWarning", true);
                Debug.LogWarning($"[{HandlerName}] Approaching boundary: {distanceToBoundary:F2}m");
            } else if (!nearBoundary) {
                warningTriggered = false;
                context.SetSharedData("BoundaryWarning", false);
            }
        }
        
        private void TrackViolationStats(float deltaTime) {
            if (!trackViolationHistory) return;
            
            if (isViolating) {
                totalViolationTime += deltaTime;
            }
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
        
        private float GetDistanceToBoundary(AgentContext context) {
            Vector3 position = GetAgentPosition(context);
            
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
        
        private Vector3 GetAgentPosition(AgentContext context) {
            return context?.AgentGameObject?.transform.position ?? Vector3.zero;
        }
        
        private void ResetBoundaryState() {
            isViolating = false;
            wasViolating = false;
            violationStartTime = -1f;
            returnStartTime = -1f;
            warningTriggered = false;
            violationCount = 0;
            totalViolationTime = 0f;
        }
        
        protected override void OnReset() {
            ResetBoundaryState();
        }
        
        public override string GetDebugInfo() {
            return base.GetDebugInfo() + 
                   $", Violating={isViolating}, Violations={violationCount}, ViolationTime={totalViolationTime:F1}s";
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
        
        public void SetEndOnViolation(bool endOnViolation) {
            endEpisodeOnViolation = endOnViolation;
        }
        
        public void SetGracePeriod(float gracePeriod) {
            violationGracePeriod = Mathf.Max(0f, gracePeriod);
        }
        
        public void SetWarningDistance(float distance) {
            warningDistance = Mathf.Max(0f, distance);
        }
        
        // Utility methods
        public bool IsCurrentlyViolating() {
            return isViolating;
        }
        
        public float GetCurrentViolationDuration() {
            return isViolating && violationStartTime >= 0f ? Time.time - violationStartTime : 0f;
        }
        
        public int GetViolationCount() {
            return violationCount;
        }
        
        public float GetTotalViolationTime() {
            return totalViolationTime;
        }
        
        public bool IsNearBoundary() {
            return warningTriggered;
        }
        
        public Vector3 GetBoundaryCenter() {
            return boundaryCenter;
        }
        
        public Vector3 GetBoundarySize() {
            return boundarySize;
        }
        
        public float GetBoundaryRadius() {
            return boundaryRadius;
        }
    }
}