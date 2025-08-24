using TCS.MLAgents.Core;
using TCS.MLAgents.Interfaces;
using Unity.MLAgents.Sensors;

namespace TCS.MLAgents.Observations {
    /// <summary>
    /// Advanced vision system for ML agents using configurable raycast-based perception.
    /// Provides flexible vision configuration with multiple detection modes and optimizations.
    /// </summary>
    [Serializable]
    public class VisionObservationProvider : ObservationProviderBase {
        [Header("Vision Configuration")]
        [SerializeField] private VisionMode visionMode = VisionMode.Raycast;
        [SerializeField] private bool enableVision = true;
        [SerializeField] private Transform visionOrigin; // If null, uses agent transform
        [SerializeField] private Vector3 localVisionOffset = Vector3.zero;
        
        [Header("Raycast Settings")]
        [SerializeField] [Range(1, 360)] private int rayCount = 32;
        [SerializeField] [Range(1f, 100f)] private float maxVisionDistance = 20f;
        [SerializeField] [Range(0f, 360f)] private float visionAngle = 360f; // Field of view in degrees
        [SerializeField] [Range(-180f, 180f)] private float visionAngleOffset = 0f; // Angle offset for vision cone
        [SerializeField] private bool useVerticalRays = false;
        [SerializeField] [Range(-90f, 90f)] private float verticalAngleMin = -30f;
        [SerializeField] [Range(-90f, 90f)] private float verticalAngleMax = 30f;
        [SerializeField] [Range(1, 10)] private int verticalRayCount = 3;
        
        [Header("Detection Settings")]
        [SerializeField] private LayerMask visionLayerMask = -1;
        [SerializeField] private List<VisionTag> detectionTags = new List<VisionTag>();
        [SerializeField] private bool detectDistance = true;
        [SerializeField] private bool detectAngle = true;
        [SerializeField] private bool detectMaterial = false;
        [SerializeField] private bool normalizeDistances = true;
        
        [Header("Performance Optimization")]
        [SerializeField] private bool enableLOD = true;
        [SerializeField] [Range(0.01f, 1f)] private float updateFrequency = 0.1f; // Update interval in seconds
        [SerializeField] private bool useAsyncRaycasting = false;
        [SerializeField] [Range(1, 10)] private int raysPerFrame = 8; // For async mode
        [SerializeField] private bool enableSpatialCaching = true;
        [SerializeField] [Range(0.1f, 5f)] private float cacheRadius = 1f;
        
        [Header("Debug Visualization")]
        [SerializeField] private bool showDebugRays = false;
        [SerializeField] private Color hitRayColor = Color.red;
        [SerializeField] private Color missRayColor = Color.green;
        [SerializeField] private float debugRayDuration = 0.1f;
        [SerializeField] private bool showVisionCone = false;
        [SerializeField] private Color visionConeColor = Color.yellow;
        
        public enum VisionMode {
            Raycast,        // Traditional raycast-based vision
            Spherecast,     // Spherecast for wider detection
            Capsulecast,    // Capsulecast for tall object detection
            Boxcast,        // Boxcast for rectangular detection areas
            OverlapSphere,  // Physics overlap for nearby detection
            Custom          // Custom vision implementation
        }
        
        [Serializable]
        public class VisionTag {
            public string tagName;
            public float importance = 1f;
            public Color debugColor = Color.white;
            public bool trackDistance = true;
            public bool trackAngle = true;
            public bool trackVelocity = false;
            public float maxTrackingDistance = 50f;
        }
        
        // Vision data structures
        public struct VisionRay {
            public Vector3 direction;
            public float angle;
            public float verticalAngle;
            public bool isActive;
        }
        
        public struct VisionHit {
            public bool hasHit;
            public float distance;
            public float angle;
            public string detectedTag;
            public Vector3 hitPoint;
            public Vector3 hitNormal;
            public Collider hitCollider;
            public float importance;
        }
        
        // Internal state
        private VisionRay[] visionRays;
        private VisionHit[] visionHits;
        private float lastUpdateTime = 0f;
        private int currentAsyncRayIndex = 0;
        private Vector3 lastVisionPosition;
        private Dictionary<string, List<GameObject>> cachedDetections = new Dictionary<string, List<GameObject>>();
        private float lastCacheUpdate = 0f;
        
        // Performance tracking
        private int raycastsThisFrame = 0;
        private float averageRaycastTime = 0f;
        private int totalRaycasts = 0;
        
        protected override void OnInitialize(AgentContext context) {
            agentContext = context;
            InitializeVisionSystem();
            SetupDetectionTags();
            ResetVisionState();
            
            if (visionOrigin == null && context.AgentGameObject != null) {
                visionOrigin = context.AgentGameObject.transform;
            }
            
            // Register with VisionSystemManager if it exists
            var visionManager = VisionSystemManager.Instance;
            if (visionManager != null) {
                visionManager.RegisterVisionProvider(this);
            }
        }
        
        protected override bool OnValidate(AgentContext context) {
            if (rayCount <= 0) {
                Debug.LogError($"[{ProviderName}] Ray count must be positive");
                return false;
            }
            
            if (maxVisionDistance <= 0f) {
                Debug.LogWarning($"[{ProviderName}] Max vision distance should be positive");
                maxVisionDistance = 10f;
            }
            
            if (visionAngle < 0f || visionAngle > 360f) {
                Debug.LogWarning($"[{ProviderName}] Vision angle should be between 0 and 360 degrees");
                visionAngle = Mathf.Clamp(visionAngle, 0f, 360f);
            }
            
            if (verticalAngleMin > verticalAngleMax) {
                Debug.LogWarning($"[{ProviderName}] Vertical angle min should be less than max");
                (verticalAngleMin, verticalAngleMax) = (verticalAngleMax, verticalAngleMin);
            }
            
            return true;
        }
        
        public override int ObservationSize => GetObservationSize();
        
        public override void OnEpisodeBegin(AgentContext context) {
            ResetVisionState();
            ClearCache();
        }
        
        private int GetObservationSize() {
            int baseObservations = rayCount;
            
            if (detectDistance && detectAngle) {
                baseObservations *= 2; // Distance + angle per ray
            } else if (detectDistance || detectAngle) {
                baseObservations *= 1; // Either distance or angle
            }
            
            if (detectMaterial) {
                baseObservations += rayCount; // Material ID per ray
            }
            
            if (useVerticalRays) {
                baseObservations *= verticalRayCount;
            }
            
            // Add tag-specific observations
            foreach (var tag in detectionTags) {
                if (tag.trackDistance) baseObservations++;
                if (tag.trackAngle) baseObservations++;
                if (tag.trackVelocity) baseObservations += 3; // Velocity vector
            }
            
            return baseObservations;
        }
        
        public override void CollectObservations(VectorSensor sensor, AgentContext context) {
            if (!enableVision || !isActive) {
                AddZeroObservations(sensor);
                return;
            }
            
            UpdateVisionSystem(context);
            
            // Collect raycast-based observations
            CollectRaycastObservations(sensor);
            
            // Collect tag-specific observations
            CollectTagObservations(sensor, context);
            
            // Update debug visualization
            if (showDebugRays || showVisionCone) {
                UpdateDebugVisualization();
            }
        }
        
        private void InitializeVisionSystem() {
            int totalRays = useVerticalRays ? rayCount * verticalRayCount : rayCount;
            visionRays = new VisionRay[totalRays];
            visionHits = new VisionHit[totalRays];
            
            SetupVisionRays();
        }
        
        private void SetupVisionRays() {
            int rayIndex = 0;
            
            if (useVerticalRays) {
                // Create rays in 3D cone
                for (int v = 0; v < verticalRayCount; v++) {
                    float verticalAngle = Mathf.Lerp(verticalAngleMin, verticalAngleMax, 
                        verticalRayCount > 1 ? (float)v / (verticalRayCount - 1) : 0f);
                    
                    for (int h = 0; h < rayCount; h++) {
                        float horizontalAngle = (visionAngle / rayCount) * h - (visionAngle / 2f) + visionAngleOffset;
                        
                        visionRays[rayIndex] = new VisionRay {
                            direction = CalculateRayDirection(horizontalAngle, verticalAngle),
                            angle = horizontalAngle,
                            verticalAngle = verticalAngle,
                            isActive = true
                        };
                        
                        rayIndex++;
                    }
                }
            } else {
                // Create rays in 2D plane
                for (int i = 0; i < rayCount; i++) {
                    float angle = (visionAngle / rayCount) * i - (visionAngle / 2f) + visionAngleOffset;
                    
                    visionRays[i] = new VisionRay {
                        direction = CalculateRayDirection(angle, 0f),
                        angle = angle,
                        verticalAngle = 0f,
                        isActive = true
                    };
                }
            }
        }
        
        private Vector3 CalculateRayDirection(float horizontalAngle, float verticalAngle) {
            // Convert angles to radians
            float hRad = horizontalAngle * Mathf.Deg2Rad;
            float vRad = verticalAngle * Mathf.Deg2Rad;
            
            // Calculate direction vector
            Vector3 direction = new Vector3(
                Mathf.Sin(hRad) * Mathf.Cos(vRad),
                Mathf.Sin(vRad),
                Mathf.Cos(hRad) * Mathf.Cos(vRad)
            );
            
            return direction.normalized;
        }
        
        private void UpdateVisionSystem(AgentContext context) {
            // Check if update is needed based on frequency
            if (Time.time - lastUpdateTime < updateFrequency) {
                return;
            }
            
            // Check if agent has moved significantly (for spatial caching)
            Vector3 currentPosition = GetVisionOriginPosition();
            if (enableSpatialCaching && Vector3.Distance(currentPosition, lastVisionPosition) < cacheRadius) {
                return;
            }
            
            lastUpdateTime = Time.time;
            lastVisionPosition = currentPosition;
            raycastsThisFrame = 0;
            
            if (useAsyncRaycasting) {
                UpdateVisionAsync();
            } else {
                UpdateVisionImmediate();
            }
        }
        
        private void UpdateVisionImmediate() {
            Vector3 origin = GetVisionOriginPosition();
            
            for (int i = 0; i < visionRays.Length; i++) {
                if (!visionRays[i].isActive) continue;
                
                Vector3 worldDirection = GetVisionOriginRotation() * visionRays[i].direction;
                visionHits[i] = PerformVisionRaycast(origin, worldDirection, maxVisionDistance);
                raycastsThisFrame++;
            }
            
            UpdatePerformanceStats();
        }
        
        private void UpdateVisionAsync() {
            Vector3 origin = GetVisionOriginPosition();
            int raysThisFrame = Mathf.Min(raysPerFrame, visionRays.Length);
            
            for (int i = 0; i < raysThisFrame; i++) {
                int rayIndex = (currentAsyncRayIndex + i) % visionRays.Length;
                
                if (!visionRays[rayIndex].isActive) continue;
                
                Vector3 worldDirection = GetVisionOriginRotation() * visionRays[rayIndex].direction;
                visionHits[rayIndex] = PerformVisionRaycast(origin, worldDirection, maxVisionDistance);
                raycastsThisFrame++;
            }
            
            currentAsyncRayIndex = (currentAsyncRayIndex + raysThisFrame) % visionRays.Length;
            UpdatePerformanceStats();
        }
        
        private VisionHit PerformVisionRaycast(Vector3 origin, Vector3 direction, float maxDistance) {
            var hit = new VisionHit();
            float startTime = Time.realtimeSinceStartup;
            
            RaycastHit raycastHit;
            bool hasHit = false;
            
            switch (visionMode) {
                case VisionMode.Raycast:
                    hasHit = Physics.Raycast(origin, direction, out raycastHit, maxDistance, visionLayerMask);
                    break;
                    
                case VisionMode.Spherecast:
                    hasHit = Physics.SphereCast(origin, 0.1f, direction, out raycastHit, maxDistance, visionLayerMask);
                    break;
                    
                case VisionMode.Capsulecast:
                    hasHit = Physics.CapsuleCast(origin, origin + Vector3.up * 0.5f, 0.1f, direction, out raycastHit, maxDistance, visionLayerMask);
                    break;
                    
                case VisionMode.Boxcast:
                    hasHit = Physics.BoxCast(origin, Vector3.one * 0.1f, direction, out raycastHit, Quaternion.identity, maxDistance, visionLayerMask);
                    break;
                    
                default:
                    hasHit = Physics.Raycast(origin, direction, out raycastHit, maxDistance, visionLayerMask);
                    break;
            }
            
            if (hasHit) {
                hit.hasHit = true;
                hit.distance = normalizeDistances ? raycastHit.distance / maxDistance : raycastHit.distance;
                hit.angle = Vector3.SignedAngle(Vector3.forward, direction, Vector3.up);
                hit.hitPoint = raycastHit.point;
                hit.hitNormal = raycastHit.normal;
                hit.hitCollider = raycastHit.collider;
                hit.detectedTag = GetBestMatchingTag(raycastHit.collider);
                hit.importance = GetTagImportance(hit.detectedTag);
            } else {
                hit.hasHit = false;
                hit.distance = normalizeDistances ? 1f : maxDistance;
                hit.angle = Vector3.SignedAngle(Vector3.forward, direction, Vector3.up);
                hit.importance = 0f;
            }
            
            // Update performance tracking
            float raycastTime = Time.realtimeSinceStartup - startTime;
            averageRaycastTime = (averageRaycastTime * totalRaycasts + raycastTime) / (totalRaycasts + 1);
            totalRaycasts++;
            
            return hit;
        }
        
        private void CollectRaycastObservations(VectorSensor sensor) {
            foreach (var hit in visionHits) {
                if (detectDistance) {
                    sensor.AddObservation(hit.distance);
                }
                
                if (detectAngle) {
                    sensor.AddObservation(hit.angle / 180f); // Normalize to [-1, 1]
                }
                
                if (detectMaterial && hit.hasHit && hit.hitCollider != null) {
                    // Simple material detection based on tag or layer
                    float materialID = GetMaterialID(hit.hitCollider);
                    sensor.AddObservation(materialID);
                }
            }
        }
        
        private void CollectTagObservations(VectorSensor sensor, AgentContext context) {
            foreach (var tag in detectionTags) {
                var closestHit = FindClosestHitWithTag(tag.tagName);
                
                if (tag.trackDistance) {
                    float distance = closestHit.hasHit ? closestHit.distance : (normalizeDistances ? 1f : maxVisionDistance);
                    sensor.AddObservation(distance);
                }
                
                if (tag.trackAngle) {
                    float angle = closestHit.hasHit ? closestHit.angle / 180f : 0f;
                    sensor.AddObservation(angle);
                }
                
                if (tag.trackVelocity && closestHit.hasHit && closestHit.hitCollider != null) {
                    var rb = closestHit.hitCollider.GetComponent<Rigidbody>();
                    Vector3 velocity = rb != null ? rb.linearVelocity : Vector3.zero;
                    sensor.AddObservation(velocity.x);
                    sensor.AddObservation(velocity.y);
                    sensor.AddObservation(velocity.z);
                }
            }
        }
        
        private VisionHit FindClosestHitWithTag(string tagName) {
            VisionHit closestHit = new VisionHit();
            float closestDistance = float.MaxValue;
            
            foreach (var hit in visionHits) {
                if (hit.hasHit && hit.detectedTag == tagName && hit.distance < closestDistance) {
                    closestHit = hit;
                    closestDistance = hit.distance;
                }
            }
            
            return closestHit;
        }
        
        private string GetBestMatchingTag(Collider collider) {
            if (collider == null) return string.Empty;
            
            // Check for specific detection tags first
            foreach (var tag in detectionTags) {
                if (collider.CompareTag(tag.tagName)) {
                    return tag.tagName;
                }
            }
            
            // Return the actual tag as fallback
            return collider.tag;
        }
        
        private float GetTagImportance(string tagName) {
            var tag = detectionTags.Find(t => t.tagName == tagName);
            return tag?.importance ?? 1f;
        }
        
        private float GetMaterialID(Collider collider) {
            // Simple material ID based on layer
            return (float)collider.gameObject.layer / 32f; // Normalize layer to [0, 1]
        }
        
        private Vector3 GetVisionOriginPosition() {
            if (visionOrigin == null) return Vector3.zero;
            return visionOrigin.position + visionOrigin.TransformDirection(localVisionOffset);
        }
        
        private Quaternion GetVisionOriginRotation() {
            return visionOrigin != null ? visionOrigin.rotation : Quaternion.identity;
        }
        
        private void UpdateDebugVisualization() {
            if (!showDebugRays && !showVisionCone) return;
            
            Vector3 origin = GetVisionOriginPosition();
            
            if (showDebugRays) {
                for (int i = 0; i < visionRays.Length && i < visionHits.Length; i++) {
                    if (!visionRays[i].isActive) continue;
                    
                    Vector3 direction = GetVisionOriginRotation() * visionRays[i].direction;
                    Color rayColor = visionHits[i].hasHit ? hitRayColor : missRayColor;
                    float distance = visionHits[i].hasHit ? visionHits[i].distance * maxVisionDistance : maxVisionDistance;
                    
                    Debug.DrawRay(origin, direction * distance, rayColor, debugRayDuration);
                }
            }
            
            if (showVisionCone) {
                DrawVisionCone(origin);
            }
        }
        
        private void DrawVisionCone(Vector3 origin) {
            // Draw vision cone edges
            float halfAngle = visionAngle / 2f;
            Vector3 forward = GetVisionOriginRotation() * Vector3.forward;
            
            Vector3 leftEdge = Quaternion.AngleAxis(-halfAngle + visionAngleOffset, Vector3.up) * forward;
            Vector3 rightEdge = Quaternion.AngleAxis(halfAngle + visionAngleOffset, Vector3.up) * forward;
            
            Debug.DrawRay(origin, leftEdge * maxVisionDistance, visionConeColor, debugRayDuration);
            Debug.DrawRay(origin, rightEdge * maxVisionDistance, visionConeColor, debugRayDuration);
            
            // Draw arc (simplified with line segments)
            int arcSegments = Mathf.Max(8, rayCount / 4);
            for (int i = 0; i < arcSegments; i++) {
                float angle1 = Mathf.Lerp(-halfAngle, halfAngle, (float)i / arcSegments) + visionAngleOffset;
                float angle2 = Mathf.Lerp(-halfAngle, halfAngle, (float)(i + 1) / arcSegments) + visionAngleOffset;
                
                Vector3 direction1 = Quaternion.AngleAxis(angle1, Vector3.up) * forward;
                Vector3 direction2 = Quaternion.AngleAxis(angle2, Vector3.up) * forward;
                
                Vector3 point1 = origin + direction1 * maxVisionDistance;
                Vector3 point2 = origin + direction2 * maxVisionDistance;
                
                Debug.DrawLine(point1, point2, visionConeColor, debugRayDuration);
            }
        }
        
        private void SetupDetectionTags() {
            // Initialize tag-specific caching
            foreach (var tag in detectionTags) {
                if (!cachedDetections.ContainsKey(tag.tagName)) {
                    cachedDetections[tag.tagName] = new List<GameObject>();
                }
            }
        }
        
        private void ResetVisionState() {
            lastUpdateTime = 0f;
            currentAsyncRayIndex = 0;
            raycastsThisFrame = 0;
            lastVisionPosition = Vector3.zero;
        }
        
        private void ClearCache() {
            foreach (var cache in cachedDetections.Values) {
                cache.Clear();
            }
            lastCacheUpdate = 0f;
        }
        
        private void UpdatePerformanceStats() {
            // Performance optimization based on raycast count
            if (enableLOD) {
                float performanceRatio = raycastsThisFrame / (float)visionRays.Length;
                if (performanceRatio > 0.8f && updateFrequency < 0.5f) {
                    updateFrequency = Mathf.Min(updateFrequency * 1.1f, 0.5f);
                } else if (performanceRatio < 0.3f && updateFrequency > 0.05f) {
                    updateFrequency = Mathf.Max(updateFrequency * 0.9f, 0.05f);
                }
            }
        }
        
        private void AddZeroObservations(VectorSensor sensor) {
            int observationCount = GetObservationSize();
            for (int i = 0; i < observationCount; i++) {
                sensor.AddObservation(0f);
            }
        }
        
        public virtual string GetDebugInfo() {
            return $"{ProviderName}: Active={IsActive}, " + 
                   $"Rays={rayCount}, Distance={maxVisionDistance:F1}, Angle={visionAngle:F0}°, " +
                   $"Mode={visionMode}, Frequency={updateFrequency:F3}s, AvgRaycastTime={averageRaycastTime * 1000:F2}ms";
        }
        
        // Public methods for runtime configuration
        public void SetVisionParameters(int rays, float distance, float angle) {
            rayCount = Mathf.Max(1, rays);
            maxVisionDistance = Mathf.Max(0.1f, distance);
            visionAngle = Mathf.Clamp(angle, 0f, 360f);
            
            InitializeVisionSystem();
        }
        
        public void SetVisionMode(VisionMode mode) {
            visionMode = mode;
        }
        
        public void SetUpdateFrequency(float frequency) {
            updateFrequency = Mathf.Clamp(frequency, 0.01f, 1f);
        }
        
        public void EnableDebugVisualization(bool showRays, bool showCone) {
            showDebugRays = showRays;
            showVisionCone = showCone;
        }
        
        public void AddDetectionTag(string tagName, float importance = 1f) {
            if (detectionTags.Find(t => t.tagName == tagName) == null) {
                detectionTags.Add(new VisionTag {
                    tagName = tagName,
                    importance = importance,
                    debugColor = Color.white,
                    trackDistance = true,
                    trackAngle = true
                });
                
                SetupDetectionTags();
            }
        }
        
        public void RemoveDetectionTag(string tagName) {
            detectionTags.RemoveAll(t => t.tagName == tagName);
            cachedDetections.Remove(tagName);
        }
        
        // Utility methods
        public VisionHit[] GetCurrentVisionHits() {
            return (VisionHit[])visionHits.Clone();
        }
        
        public bool CanSeeTag(string tagName, float maxDistance = -1f) {
            if (maxDistance < 0f) maxDistance = maxVisionDistance;
            
            var hit = FindClosestHitWithTag(tagName);
            return hit.hasHit && hit.distance * maxVisionDistance <= maxDistance;
        }
        
        public float GetDistanceToTag(string tagName) {
            var hit = FindClosestHitWithTag(tagName);
            return hit.hasHit ? hit.distance * maxVisionDistance : -1f;
        }
        
        public Vector3 GetDirectionToTag(string tagName) {
            var hit = FindClosestHitWithTag(tagName);
            if (hit.hasHit) {
                return (hit.hitPoint - GetVisionOriginPosition()).normalized;
            }
            return Vector3.zero;
        }
        
        public int GetHitCount() {
            int count = 0;
            foreach (var hit in visionHits) {
                if (hit.hasHit) count++;
            }
            return count;
        }
        
        public float GetAverageRaycastTime() {
            return averageRaycastTime * 1000f; // Return in milliseconds
        }
        
        public int GetTotalRaycastsPerformed() {
            return totalRaycasts;
        }
        
        // Properties needed by VisionSystemManager
        public AgentContext Context => agentContext;
        private AgentContext agentContext;
        
        // Method to set context (called by VisionSystemManager)
        public void SetContext(AgentContext context) {
            agentContext = context;
        }
    }
}