using Unity.MLAgents.Sensors;
using TCS.MLAgents.Core;
using TCS.MLAgents.Interfaces;

namespace TCS.MLAgents.Sensors {
    /// <summary>
    /// Advanced raycast sensor provider that extends Unity ML-Agents RayPerceptionSensorComponent3D
    /// with additional features like multi-layer detection, tag filtering, and performance optimization.
    /// </summary>
    public class RaycastSensorProvider : SensorProviderBase {
        [Header("Raycast Configuration")]
        [SerializeField] private string[] detectionTags = { "Player", "Enemy", "Obstacle", "Collectible" };
        [SerializeField] private LayerMask rayLayerMask = -1;
        [SerializeField] private float rayLength = 20f;
        [SerializeField] private int rayCount = 16;
        [SerializeField] private float maxAngle = 180f;
        [SerializeField] private float startAngle = 0f;
        [SerializeField] private float endAngle = 360f;
        
        [Header("3D Raycast Settings")]
        [SerializeField] private bool use3DRays = false;
        [SerializeField] private float sphereCastRadius = 0.05f;
        [SerializeField] private float verticalAngleMin = -30f;
        [SerializeField] private float verticalAngleMax = 30f;
        [SerializeField] private int verticalRayLayers = 3;
        
        [Header("Detection Settings")]
        [SerializeField] private bool includeDistance = true;
        [SerializeField] private bool includeAngle = true;
        [SerializeField] private bool includeNormal = false;
        [SerializeField] private bool includeVelocity = false;
        [SerializeField] private bool normalizeValues = true;
        
        [Header("Performance Optimization")]
        [SerializeField] private bool enableLOD = true;
        [SerializeField] private float lodDistance1 = 10f; // Close range - full rays
        [SerializeField] private float lodDistance2 = 25f; // Medium range - reduced rays
        [SerializeField] private float lodDistance3 = 50f; // Far range - minimal rays
        [SerializeField] private bool useAsyncRaycasting = false;
        [SerializeField] private int raysPerFrame = 4;
        
        [Header("Debug Visualization")]
        [SerializeField] private bool showRayGizmos = false;
        [SerializeField] private Color hitRayColor = Color.red;
        [SerializeField] private Color missRayColor = Color.green;
        [SerializeField] private float gizmoDuration = 0.1f;
        
        // Sensor components
        private RayPerceptionSensorComponent3D rayPerceptionSensor;
        private CustomRaycastSensor customRaycastSensor;
        
        // Internal state
        private RaycastHit[] rayHits;
        private int currentLODLevel = 0;
        private int currentAsyncRayIndex = 0;
        private float lastRaycastUpdate = 0f;
        
        // Performance tracking
        private float totalRaycastTime = 0f;
        private int raycastCount = 0;
        
        public override ISensor Sensor => customRaycastSensor;
        
        public RaycastHit[] RayHits => rayHits;
        public int CurrentLODLevel => currentLODLevel;
        public float AverageRaycastTime => raycastCount > 0 ? (totalRaycastTime / raycastCount) * 1000f : 0f;
        
        protected override void OnInitialize(AgentContext context) {
            SetupRaycastSensor();
            InitializeRayHits();
            
            LogDebug($"Initialized with {rayCount} rays, length {rayLength}, tags: {string.Join(", ", detectionTags)}");
        }
        
        protected void OnValidate() {
            if (rayCount <= 0) {
                LogError("Ray count must be positive");
            }
            
            if (rayLength <= 0f) {
                LogWarning("Ray length should be positive");
                rayLength = 10f;
            }
            
            if (detectionTags.Length == 0) {
                LogWarning("No detection tags specified");
            }
            
            if (maxAngle <= 0f || maxAngle > 360f) {
                LogWarning("Max angle should be between 0 and 360 degrees");
                maxAngle = Mathf.Clamp(maxAngle, 1f, 360f);
            }
        }
        
        protected override void OnEpisodeStart(AgentContext context) {
            ResetRaycastData();
            UpdateLODLevel();
        }
        
        protected override void OnUpdateSensor(AgentContext context, float deltaTime) {
            UpdateLODLevel();
            
            if (useAsyncRaycasting) {
                UpdateRaycastsAsync(deltaTime);
            } else {
                UpdateRaycastsImmediate(deltaTime);
            }
        }
        
        protected override void OnReset() {
            ResetRaycastData();
        }
        
        protected override void OnEvent(string eventName, AgentContext context, object eventData = null) {
            switch (eventName) {
                case "UpdateLOD":
                    if (eventData is int lodLevel) {
                        SetLODLevel(lodLevel);
                    }
                    break;
                case "SetRayLength":
                    if (eventData is float length) {
                        SetRayLength(length);
                    }
                    break;
                case "ToggleDebugVisualization":
                    showRayGizmos = !showRayGizmos;
                    break;
            }
        }
        
        private void SetupRaycastSensor() {
            // Create custom raycast sensor
            customRaycastSensor = new CustomRaycastSensor(
                sensorName: SensorName,
                rayLength: rayLength,
                rayCount: GetCurrentRayCount(),
                detectionTags: detectionTags,
                maxAngle: maxAngle,
                startAngle: startAngle,
                endAngle: endAngle,
                includeDistance: includeDistance,
                includeAngle: includeAngle,
                includeNormal: includeNormal,
                transform: transform
            );
        }
        
        private void InitializeRayHits() {
            int totalRays = use3DRays ? GetCurrentRayCount() * verticalRayLayers : GetCurrentRayCount();
            rayHits = new RaycastHit[totalRays];
        }
        
        private void UpdateLODLevel() {
            if (!enableLOD) {
                currentLODLevel = 0;
                return;
            }
            
            // Calculate distance to main camera or other important reference
            float distance = CalculateImportanceDistance();
            
            int newLODLevel = 0;
            if (distance > lodDistance3) {
                newLODLevel = 3; // Minimal quality
            } else if (distance > lodDistance2) {
                newLODLevel = 2; // Medium quality
            } else if (distance > lodDistance1) {
                newLODLevel = 1; // Reduced quality
            } else {
                newLODLevel = 0; // Full quality
            }
            
            if (newLODLevel != currentLODLevel) {
                currentLODLevel = newLODLevel;
                UpdateRayCountForLOD();
                LogDebug($"LOD changed to level {currentLODLevel}");
            }
        }
        
        private void UpdateRaycastsImmediate(float deltaTime) {
            float startTime = Time.realtimeSinceStartup;
            
            Vector3 origin = transform.position;
            Quaternion rotation = transform.rotation;
            
            int totalRays = GetCurrentRayCount();
            
            for (int i = 0; i < totalRays; i++) {
                if (use3DRays) {
                    PerformRaycast3D(origin, rotation, i);
                } else {
                    PerformRaycast2D(origin, rotation, i);
                }
            }
            
            // Track performance
            float raycastTime = Time.realtimeSinceStartup - startTime;
            totalRaycastTime += raycastTime;
            raycastCount++;
            
            // Update custom sensor
            if (customRaycastSensor != null) {
                customRaycastSensor.UpdateRayHits(rayHits);
            }
            
            // Debug visualization
            if (showRayGizmos) {
                DrawRayGizmos();
            }
        }
        
        private void UpdateRaycastsAsync(float deltaTime) {
            int raysThisFrame = Mathf.Min(raysPerFrame, GetCurrentRayCount());
            Vector3 origin = transform.position;
            Quaternion rotation = transform.rotation;
            
            float startTime = Time.realtimeSinceStartup;
            
            for (int i = 0; i < raysThisFrame; i++) {
                int rayIndex = (currentAsyncRayIndex + i) % GetCurrentRayCount();
                
                if (use3DRays) {
                    PerformRaycast3D(origin, rotation, rayIndex);
                } else {
                    PerformRaycast2D(origin, rotation, rayIndex);
                }
            }
            
            currentAsyncRayIndex = (currentAsyncRayIndex + raysThisFrame) % GetCurrentRayCount();
            
            // Track performance
            float raycastTime = Time.realtimeSinceStartup - startTime;
            totalRaycastTime += raycastTime;
            raycastCount++;
            
            // Update custom sensor
            if (customRaycastSensor != null) {
                customRaycastSensor.UpdateRayHits(rayHits);
            }
        }
        
        private void PerformRaycast2D(Vector3 origin, Quaternion rotation, int rayIndex) {
            float angle = CalculateRayAngle(rayIndex, GetCurrentRayCount());
            Vector3 direction = rotation * Quaternion.Euler(0, angle, 0) * Vector3.forward;
            
            bool hit = Physics.Raycast(origin, direction, out RaycastHit hitInfo, rayLength, rayLayerMask);
            
            if (hit && IsValidHit(hitInfo)) {
                rayHits[rayIndex] = hitInfo;
            } else {
                // Create empty hit for missed rays
                rayHits[rayIndex] = new RaycastHit();
            }
        }
        
        private void PerformRaycast3D(Vector3 origin, Quaternion rotation, int rayIndex) {
            float horizontalAngle = CalculateRayAngle(rayIndex, GetCurrentRayCount());
            
            for (int v = 0; v < verticalRayLayers; v++) {
                float verticalAngle = Mathf.Lerp(verticalAngleMin, verticalAngleMax,
                    verticalRayLayers > 1 ? (float)v / (verticalRayLayers - 1) : 0f);
                
                Vector3 direction = rotation * 
                    Quaternion.Euler(verticalAngle, horizontalAngle, 0) * Vector3.forward;
                
                int hitIndex = rayIndex * verticalRayLayers + v;
                bool hit;
                
                if (sphereCastRadius > 0f) {
                    hit = Physics.SphereCast(origin, sphereCastRadius, direction, out RaycastHit hitInfo, rayLength, rayLayerMask);
                    rayHits[hitIndex] = hit ? hitInfo : new RaycastHit();
                } else {
                    hit = Physics.Raycast(origin, direction, out RaycastHit hitInfo, rayLength, rayLayerMask);
                    rayHits[hitIndex] = hit && IsValidHit(hitInfo) ? hitInfo : new RaycastHit();
                }
            }
        }
        
        private bool IsValidHit(RaycastHit hit) {
            if (detectionTags.Length == 0) return true;
            
            foreach (string tag in detectionTags) {
                if (hit.collider.CompareTag(tag)) {
                    return true;
                }
            }
            
            return false;
        }
        
        private float CalculateRayAngle(int rayIndex, int totalRays) {
            if (totalRays == 1) return startAngle;
            
            float angleRange = endAngle - startAngle;
            float angleStep = angleRange / (totalRays - 1);
            return startAngle + rayIndex * angleStep;
        }
        
        private int GetCurrentRayCount() {
            if (!enableLOD) return rayCount;
            
            return currentLODLevel switch {
                0 => rayCount,              // Full quality
                1 => rayCount * 3 / 4,      // 75% rays
                2 => rayCount / 2,          // 50% rays
                3 => rayCount / 4,          // 25% rays
                _ => rayCount
            };
        }
        
        private void UpdateRayCountForLOD() {
            InitializeRayHits();
            
            if (customRaycastSensor != null) {
                customRaycastSensor.UpdateRayCount(GetCurrentRayCount());
            }
        }
        
        private float CalculateImportanceDistance() {
            // Calculate distance to camera or other important reference point
            if (Camera.main != null) {
                return Vector3.Distance(transform.position, Camera.main.transform.position);
            }
            
            return 10f; // Default distance
        }
        
        private void ResetRaycastData() {
            for (int i = 0; i < rayHits.Length; i++) {
                rayHits[i] = new RaycastHit();
            }
            
            currentAsyncRayIndex = 0;
            totalRaycastTime = 0f;
            raycastCount = 0;
        }
        
        private void DrawRayGizmos() {
            if (!Application.isPlaying) return;
            
            Vector3 origin = transform.position;
            Quaternion rotation = transform.rotation;
            
            for (int i = 0; i < GetCurrentRayCount(); i++) {
                float angle = CalculateRayAngle(i, GetCurrentRayCount());
                Vector3 direction = rotation * Quaternion.Euler(0, angle, 0) * Vector3.forward;
                
                bool hasHit = i < rayHits.Length && rayHits[i].collider != null;
                Color rayColor = hasHit ? hitRayColor : missRayColor;
                float rayDistance = hasHit ? rayHits[i].distance : rayLength;
                
                Debug.DrawRay(origin, direction * rayDistance, rayColor, gizmoDuration);
                
                if (hasHit && rayHits[i].collider != null) {
                    // Draw a simple cross at the hit point to mark hits
                    Vector3 hitPoint = rayHits[i].point;
                    Debug.DrawLine(hitPoint + Vector3.up * 0.1f, hitPoint - Vector3.up * 0.1f, hitRayColor, gizmoDuration);
                    Debug.DrawLine(hitPoint + Vector3.right * 0.1f, hitPoint - Vector3.right * 0.1f, hitRayColor, gizmoDuration);
                    Debug.DrawLine(hitPoint + Vector3.forward * 0.1f, hitPoint - Vector3.forward * 0.1f, hitRayColor, gizmoDuration);
                }
            }
        }
        
        public override string GetDebugInfo() {
            return base.GetDebugInfo() + 
                   $", Rays={GetCurrentRayCount()}/{rayCount}, Length={rayLength:F1}, " +
                   $"LOD={currentLODLevel}, AvgTime={AverageRaycastTime:F2}ms";
        }
        
        // Public configuration methods
        public void SetRayLength(float length) {
            rayLength = Mathf.Max(0.1f, length);
            if (customRaycastSensor != null) {
                customRaycastSensor.SetRayLength(rayLength);
            }
        }
        
        public void SetRayCount(int count) {
            rayCount = Mathf.Max(1, count);
            InitializeRayHits();
            UpdateRayCountForLOD();
        }
        
        public void SetDetectionTags(string[] tags) {
            detectionTags = tags ?? new string[0];
            if (customRaycastSensor != null) {
                customRaycastSensor.SetDetectionTags(detectionTags);
            }
        }
        
        public void SetLODLevel(int level) {
            currentLODLevel = Mathf.Clamp(level, 0, 3);
            UpdateRayCountForLOD();
        }
        
        public void EnableDebugVisualization(bool enabled) {
            showRayGizmos = enabled;
        }
        
        // Utility methods
        public RaycastHit GetClosestHit() {
            RaycastHit closestHit = new RaycastHit();
            float closestDistance = float.MaxValue;
            
            foreach (var hit in rayHits) {
                if (hit.collider != null && hit.distance < closestDistance) {
                    closestHit = hit;
                    closestDistance = hit.distance;
                }
            }
            
            return closestHit;
        }
        
        public RaycastHit[] GetHitsWithTag(string tag) {
            var hits = new List<RaycastHit>();
            
            foreach (var hit in rayHits) {
                if (hit.collider != null && hit.collider.CompareTag(tag)) {
                    hits.Add(hit);
                }
            }
            
            return hits.ToArray();
        }
        
        public bool CanSeeTag(string tag, float maxDistance = -1f) {
            if (maxDistance < 0f) maxDistance = rayLength;
            
            foreach (var hit in rayHits) {
                if (hit.collider != null && hit.collider.CompareTag(tag) && hit.distance <= maxDistance) {
                    return true;
                }
            }
            
            return false;
        }
        
        public float GetDistanceToTag(string tag) {
            float closestDistance = float.MaxValue;
            
            foreach (var hit in rayHits) {
                if (hit.collider != null && hit.collider.CompareTag(tag) && hit.distance < closestDistance) {
                    closestDistance = hit.distance;
                }
            }
            
            return closestDistance != float.MaxValue ? closestDistance : -1f;
        }
    }
    
    /// <summary>
    /// Custom raycast sensor that integrates with Unity ML-Agents sensor system
    /// </summary>
    public class CustomRaycastSensor : ISensor {
        private string sensorName;
        private float[] observations;
        private RaycastHit[] rayHits;
        private string[] detectionTags;
        private float rayLength;
        private int rayCount;
        private float maxAngle;
        private float startAngle;
        private float endAngle;
        private bool includeDistance;
        private bool includeAngle;
        private bool includeNormal;
        private Transform sensorTransform;
        
        private int observationSize;
        
        public CustomRaycastSensor(string sensorName, float rayLength, int rayCount, string[] detectionTags,
            float maxAngle, float startAngle, float endAngle, bool includeDistance, bool includeAngle,
            bool includeNormal, Transform transform) {
            this.sensorName = sensorName;
            this.rayLength = rayLength;
            this.rayCount = rayCount;
            this.detectionTags = detectionTags ?? new string[0];
            this.maxAngle = maxAngle;
            this.startAngle = startAngle;
            this.endAngle = endAngle;
            this.includeDistance = includeDistance;
            this.includeAngle = includeAngle;
            this.includeNormal = includeNormal;
            sensorTransform = transform;
            
            CalculateObservationSize();
            observations = new float[observationSize];
        }
        
        public string GetName() => sensorName;
        
        public int[] GetObservationShape() => new[] { observationSize };
        
        public SensorCompressionType GetCompressionType() => SensorCompressionType.None;
        
        public byte[] GetCompressedObservation() => null;
        
        public int Write(ObservationWriter writer) {
            UpdateObservations();
            
            for (int i = 0; i < observations.Length; i++) {
                writer[i] = observations[i];
            }
            
            return observations.Length;
        }
        
        public void Update() {
            // Sensor updates are handled by the provider
        }
        
        public void Reset() {
            for (int i = 0; i < observations.Length; i++) {
                observations[i] = 0f;
            }
        }
        
        public CompressionSpec GetCompressionSpec() {
            return CompressionSpec.Default();
        }
        
        public ObservationSpec GetObservationSpec() {
            return ObservationSpec.Vector(observationSize);
        }
        
        public void UpdateRayHits(RaycastHit[] hits) {
            rayHits = hits;
        }
        
        public void UpdateRayCount(int count) {
            rayCount = count;
            CalculateObservationSize();
            observations = new float[observationSize];
        }
        
        public void SetRayLength(float length) {
            rayLength = length;
        }
        
        public void SetDetectionTags(string[] tags) {
            detectionTags = tags ?? new string[0];
            CalculateObservationSize();
            observations = new float[observationSize];
        }
        
        private void CalculateObservationSize() {
            int baseObservations = rayCount * detectionTags.Length; // One per tag per ray
            
            if (includeDistance) baseObservations += rayCount;
            if (includeAngle) baseObservations += rayCount;
            if (includeNormal) baseObservations += rayCount * 3; // X, Y, Z components
            
            observationSize = baseObservations;
        }
        
        private void UpdateObservations() {
            int index = 0;
            
            // Add tag detections
            for (int ray = 0; ray < rayCount; ray++) {
                foreach (string tag in detectionTags) {
                    bool hasTag = ray < rayHits.Length && rayHits[ray].collider != null && 
                                  rayHits[ray].collider.CompareTag(tag);
                    observations[index++] = hasTag ? 1f : 0f;
                }
            }
            
            // Add distance observations
            if (includeDistance) {
                for (int ray = 0; ray < rayCount; ray++) {
                    if (ray < rayHits.Length && rayHits[ray].collider != null) {
                        observations[index++] = rayHits[ray].distance / rayLength; // Normalized
                    } else {
                        observations[index++] = 1f; // Max distance for no hit
                    }
                }
            }
            
            // Add angle observations
            if (includeAngle) {
                for (int ray = 0; ray < rayCount; ray++) {
                    float angle = CalculateRayAngle(ray, rayCount);
                    observations[index++] = angle / 180f; // Normalized to [-1, 1]
                }
            }
            
            // Add normal observations
            if (includeNormal) {
                for (int ray = 0; ray < rayCount; ray++) {
                    if (ray < rayHits.Length && rayHits[ray].collider != null) {
                        Vector3 normal = rayHits[ray].normal;
                        observations[index++] = normal.x;
                        observations[index++] = normal.y;
                        observations[index++] = normal.z;
                    } else {
                        observations[index++] = 0f;
                        observations[index++] = 0f;
                        observations[index++] = 0f;
                    }
                }
            }
        }
        
        private float CalculateRayAngle(int rayIndex, int totalRays) {
            if (totalRays == 1) return startAngle;
            
            float angleRange = endAngle - startAngle;
            float angleStep = angleRange / (totalRays - 1);
            return startAngle + rayIndex * angleStep;
        }
    }
}