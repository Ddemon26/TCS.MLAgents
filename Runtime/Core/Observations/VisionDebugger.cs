using System.Collections.Generic;
using UnityEngine;
using TCS.MLAgents.Core;
using Random = UnityEngine.Random;

namespace TCS.MLAgents.Observations {
    /// <summary>
    /// Advanced debugging and visualization tools for VisionObservationProvider.
    /// Provides real-time vision analysis, performance monitoring, and debug UI.
    /// </summary>
    [System.Serializable]
    public class VisionDebugger {
        [Header("Debug UI Settings")]
        [SerializeField] private bool showDebugUI = true;
        [SerializeField] private bool showPerformanceStats = true;
        [SerializeField] private bool showVisionStats = true;
        [SerializeField] private bool showDetectionList = true;
        [SerializeField] private Vector2 debugUIPosition = new Vector2(10, 10);
        [SerializeField] private Vector2 debugUISize = new Vector2(300, 400);
        
        [Header("Visualization Settings")]
        [SerializeField] private bool enableRayVisualization = true;
        [SerializeField] private bool enableHitPointMarkers = true;
        [SerializeField] private bool enableDistanceLabels = false;
        [SerializeField] private bool enableAngleIndicators = false;
        [SerializeField] private float markerSize = 0.1f;
        [SerializeField] private float labelOffset = 0.2f;
        
        [Header("Gizmo Settings")]
        [SerializeField] private bool showGizmosInScene = true;
        [SerializeField] private bool showVisionConeGizmo = true;
        [SerializeField] private bool showDetectionRangeGizmo = true;
        [SerializeField] private Color gizmoColor = Color.cyan;
        [SerializeField] private float gizmoAlpha = 0.3f;
        
        [Header("Performance Monitoring")]
        [SerializeField] private bool enablePerformanceLogging = false;
        [SerializeField] private float performanceLogInterval = 5f;
        [SerializeField] private bool trackFrameTime = true;
        [SerializeField] private bool trackMemoryUsage = true;
        
        // Internal state
        private VisionObservationProvider visionProvider;
        private Dictionary<string, DetectionData> detectionHistory = new Dictionary<string, DetectionData>();
        private List<PerformanceSnapshot> performanceHistory = new List<PerformanceSnapshot>();
        private float lastPerformanceLog = 0f;
        private GUIStyle debugStyle;
        private bool debugStyleInitialized = false;
        
        // Data structures
        public struct DetectionData {
            public string tagName;
            public float firstDetectedTime;
            public float lastDetectedTime;
            public float closestDistance;
            public float averageDistance;
            public int detectionCount;
            public Vector3 lastPosition;
            public Color debugColor;
        }
        
        public struct PerformanceSnapshot {
            public float timestamp;
            public float frameTime;
            public int raycastCount;
            public float averageRaycastTime;
            public float memoryUsage;
            public int hitCount;
            public float updateFrequency;
        }
        
        public void Initialize(VisionObservationProvider provider) {
            visionProvider = provider;
            detectionHistory.Clear();
            performanceHistory.Clear();
            InitializeDebugStyle();
        }
        
        public void Update(AgentContext context) {
            if (visionProvider == null) return;
            
            UpdateDetectionHistory();
            UpdatePerformanceTracking();
            
            if (enablePerformanceLogging && Time.time - lastPerformanceLog >= performanceLogInterval) {
                LogPerformanceData();
                lastPerformanceLog = Time.time;
            }
        }
        
        public void OnDrawGizmos(Transform visionOrigin) {
            if (!showGizmosInScene || visionProvider == null || visionOrigin == null) return;
            
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, gizmoAlpha);
            
            if (showVisionConeGizmo) {
                DrawVisionConeGizmo(visionOrigin);
            }
            
            if (showDetectionRangeGizmo) {
                DrawDetectionRangeGizmo(visionOrigin);
            }
            
            if (enableHitPointMarkers) {
                DrawHitPointMarkers();
            }
        }
        
        public void OnGUI() {
            if (!showDebugUI) return;
            
            if (!debugStyleInitialized) {
                InitializeDebugStyle();
            }
            
            Rect debugRect = new Rect(debugUIPosition.x, debugUIPosition.y, debugUISize.x, debugUISize.y);
            GUI.Box(debugRect, "Vision Debug Info", debugStyle);
            
            GUILayout.BeginArea(new Rect(debugRect.x + 10, debugRect.y + 25, debugRect.width - 20, debugRect.height - 35));
            
            if (showPerformanceStats) {
                DrawPerformanceStats();
            }
            
            if (showVisionStats) {
                DrawVisionStats();
            }
            
            if (showDetectionList) {
                DrawDetectionList();
            }
            
            GUILayout.EndArea();
        }
        
        private void UpdateDetectionHistory() {
            if (visionProvider == null) return;
            
            var currentHits = visionProvider.GetCurrentVisionHits();
            var currentTime = Time.time;
            
            // Update detection data for each hit
            foreach (var hit in currentHits) {
                if (!hit.hasHit || string.IsNullOrEmpty(hit.detectedTag)) continue;
                
                if (!detectionHistory.ContainsKey(hit.detectedTag)) {
                    detectionHistory[hit.detectedTag] = new DetectionData {
                        tagName = hit.detectedTag,
                        firstDetectedTime = currentTime,
                        lastDetectedTime = currentTime,
                        closestDistance = hit.distance,
                        averageDistance = hit.distance,
                        detectionCount = 1,
                        lastPosition = hit.hitPoint,
                        debugColor = GetRandomDebugColor()
                    };
                } else {
                    var data = detectionHistory[hit.detectedTag];
                    data.lastDetectedTime = currentTime;
                    data.closestDistance = Mathf.Min(data.closestDistance, hit.distance);
                    data.averageDistance = (data.averageDistance * data.detectionCount + hit.distance) / (data.detectionCount + 1);
                    data.detectionCount++;
                    data.lastPosition = hit.hitPoint;
                    detectionHistory[hit.detectedTag] = data;
                }
            }
            
            // Clean up old detection data
            var keysToRemove = new List<string>();
            foreach (var kvp in detectionHistory) {
                if (currentTime - kvp.Value.lastDetectedTime > 5f) {
                    keysToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var key in keysToRemove) {
                detectionHistory.Remove(key);
            }
        }
        
        private void UpdatePerformanceTracking() {
            if (!trackFrameTime && !trackMemoryUsage) return;
            
            var snapshot = new PerformanceSnapshot {
                timestamp = Time.time,
                frameTime = Time.deltaTime,
                raycastCount = visionProvider?.GetTotalRaycastsPerformed() ?? 0,
                averageRaycastTime = visionProvider?.GetAverageRaycastTime() ?? 0f,
                memoryUsage = trackMemoryUsage ? System.GC.GetTotalMemory(false) / (1024f * 1024f) : 0f,
                hitCount = visionProvider?.GetHitCount() ?? 0,
                updateFrequency = 1f / (visionProvider?.GetDebugInfo().Contains("Frequency=") == true ? 
                    ParseFrequencyFromDebugInfo(visionProvider.GetDebugInfo()) : 0.1f)
            };
            
            performanceHistory.Add(snapshot);
            
            // Keep only recent history
            while (performanceHistory.Count > 1000) {
                performanceHistory.RemoveAt(0);
            }
        }
        
        private void DrawVisionConeGizmo(Transform visionOrigin) {
            // This would be called from the VisionObservationProvider's OnDrawGizmos
            Vector3 origin = visionOrigin.position;
            Vector3 forward = visionOrigin.forward;
            
            // Draw wireframe cone (simplified)
            Gizmos.DrawWireSphere(origin, 0.2f);
            Gizmos.DrawRay(origin, forward * 2f);
        }
        
        private void DrawDetectionRangeGizmo(Transform visionOrigin) {
            Vector3 origin = visionOrigin.position;
            Gizmos.DrawWireSphere(origin, 10f); // Default max vision distance
        }
        
        private void DrawHitPointMarkers() {
            if (visionProvider == null) return;
            
            var hits = visionProvider.GetCurrentVisionHits();
            foreach (var hit in hits) {
                if (!hit.hasHit) continue;
                
                Gizmos.color = detectionHistory.ContainsKey(hit.detectedTag) ? 
                    detectionHistory[hit.detectedTag].debugColor : Color.red;
                
                Gizmos.DrawWireSphere(hit.hitPoint, markerSize);
                
                if (enableDistanceLabels) {
                    // Note: Labels in gizmos require custom implementation or using Debug.DrawRay with custom text
                }
            }
        }
        
        private void DrawPerformanceStats() {
            GUILayout.Label("=== Performance Stats ===", debugStyle);
            
            if (performanceHistory.Count > 0) {
                var latest = performanceHistory[performanceHistory.Count - 1];
                GUILayout.Label($"Frame Time: {latest.frameTime * 1000:F2}ms");
                GUILayout.Label($"Raycast Count: {latest.raycastCount}");
                GUILayout.Label($"Avg Raycast Time: {latest.averageRaycastTime:F3}ms");
                GUILayout.Label($"Hit Count: {latest.hitCount}");
                GUILayout.Label($"Update Freq: {latest.updateFrequency:F1}Hz");
                
                if (trackMemoryUsage) {
                    GUILayout.Label($"Memory: {latest.memoryUsage:F1}MB");
                }
            }
            
            GUILayout.Space(10);
        }
        
        private void DrawVisionStats() {
            GUILayout.Label("=== Vision Stats ===", debugStyle);
            
            if (visionProvider != null) {
                string debugInfo = visionProvider.GetDebugInfo();
                var infoLines = debugInfo.Split(',');
                
                foreach (var line in infoLines) {
                    if (!string.IsNullOrEmpty(line.Trim())) {
                        GUILayout.Label(line.Trim());
                    }
                }
            }
            
            GUILayout.Space(10);
        }
        
        private void DrawDetectionList() {
            GUILayout.Label("=== Current Detections ===", debugStyle);
            
            if (detectionHistory.Count == 0) {
                GUILayout.Label("No detections");
                return;
            }
            
            foreach (var kvp in detectionHistory) {
                var data = kvp.Value;
                float timeSinceDetection = Time.time - data.lastDetectedTime;
                
                if (timeSinceDetection < 1f) {
                    GUI.color = Color.green;
                } else if (timeSinceDetection < 3f) {
                    GUI.color = Color.yellow;
                } else {
                    GUI.color = Color.red;
                }
                
                GUILayout.Label($"{data.tagName}: {data.closestDistance:F2}m");
                GUI.color = Color.white;
            }
        }
        
        private void LogPerformanceData() {
            if (performanceHistory.Count == 0) return;
            
            var latest = performanceHistory[performanceHistory.Count - 1];
            Debug.Log($"[VisionDebugger] Performance: FrameTime={latest.frameTime * 1000:F2}ms, " +
                     $"Raycasts={latest.raycastCount}, Hits={latest.hitCount}, " +
                     $"Memory={latest.memoryUsage:F1}MB");
        }
        
        private void InitializeDebugStyle() {
            debugStyle = new GUIStyle(GUI.skin.box) {
                normal = { textColor = Color.white },
                fontSize = 12,
                alignment = TextAnchor.UpperLeft
            };
            debugStyleInitialized = true;
        }
        
        private Color GetRandomDebugColor() {
            return new Color(
                Random.Range(0.3f, 1f),
                Random.Range(0.3f, 1f),
                Random.Range(0.3f, 1f),
                1f
            );
        }
        
        private float ParseFrequencyFromDebugInfo(string debugInfo) {
            // Simple parsing to extract frequency value
            try {
                var frequencyIndex = debugInfo.IndexOf("Frequency=");
                if (frequencyIndex >= 0) {
                    var startIndex = frequencyIndex + "Frequency=".Length;
                    var endIndex = debugInfo.IndexOf('s', startIndex);
                    if (endIndex > startIndex) {
                        var frequencyStr = debugInfo.Substring(startIndex, endIndex - startIndex);
                        if (float.TryParse(frequencyStr, out float frequency)) {
                            return frequency;
                        }
                    }
                }
            } catch {
                // Ignore parsing errors
            }
            
            return 0.1f; // Default fallback
        }
        
        // Public configuration methods
        public void SetDebugUIEnabled(bool enabled) {
            showDebugUI = enabled;
        }
        
        public void SetDebugUIPosition(Vector2 position) {
            debugUIPosition = position;
        }
        
        public void SetDebugUISize(Vector2 size) {
            debugUISize = size;
        }
        
        public void SetVisualizationEnabled(bool rays, bool hitPoints, bool labels) {
            enableRayVisualization = rays;
            enableHitPointMarkers = hitPoints;
            enableDistanceLabels = labels;
        }
        
        public void SetPerformanceLogging(bool enabled, float interval) {
            enablePerformanceLogging = enabled;
            performanceLogInterval = interval;
        }
        
        // Utility methods
        public DetectionData[] GetDetectionHistory() {
            var result = new DetectionData[detectionHistory.Count];
            int index = 0;
            foreach (var kvp in detectionHistory) {
                result[index++] = kvp.Value;
            }
            return result;
        }
        
        public PerformanceSnapshot[] GetPerformanceHistory() {
            return performanceHistory.ToArray();
        }
        
        public void ClearHistory() {
            detectionHistory.Clear();
            performanceHistory.Clear();
        }
        
        public bool IsDetectionActive(string tagName) {
            if (!detectionHistory.ContainsKey(tagName)) return false;
            return Time.time - detectionHistory[tagName].lastDetectedTime < 1f;
        }
        
        public float GetDetectionDistance(string tagName) {
            return detectionHistory.ContainsKey(tagName) ? detectionHistory[tagName].closestDistance : -1f;
        }
        
        public Vector3 GetLastDetectionPosition(string tagName) {
            return detectionHistory.ContainsKey(tagName) ? detectionHistory[tagName].lastPosition : Vector3.zero;
        }
    }
}