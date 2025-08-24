using Unity.MLAgents.Sensors;
using TCS.MLAgents.Core;
using TCS.MLAgents.Interfaces;

namespace TCS.MLAgents.Sensors {
    /// <summary>
    /// Camera sensor provider that creates visual observations for ML agents.
    /// Supports multiple camera types, image processing, and performance optimization.
    /// </summary>
    public class CameraSensorProvider : SensorProviderBase {
        [Header("Camera Configuration")]
        [SerializeField] private Camera attachedCamera;
        [SerializeField] private bool createCameraIfMissing = true;
        [SerializeField] private string cameraName = "AgentCamera";
        [SerializeField] private int width = 84;
        [SerializeField] private int height = 84;
        [SerializeField] private bool grayscale = false;
        
        [Header("Camera Settings")]
        [SerializeField] private float fieldOfView = 60f;
        [SerializeField] private float nearClipPlane = 0.1f;
        [SerializeField] private float farClipPlane = 100f;
        [SerializeField] private CameraClearFlags clearFlags = CameraClearFlags.Skybox;
        [SerializeField] private Color backgroundColor = Color.black;
        [SerializeField] private LayerMask cullingMask = -1;
        
        [Header("Observation Settings")]
        [SerializeField] private SensorCompressionType compression = SensorCompressionType.PNG;
        [SerializeField] private int observationStacks = 1;
        [SerializeField] private CameraTargetType targetType = CameraTargetType.Agent;
        [SerializeField] private Transform customTarget;
        [SerializeField] private Vector3 cameraOffset = new Vector3(0, 2, -5);
        [SerializeField] private Vector3 cameraRotationOffset = new Vector3(15, 0, 0);
        
        [Header("Performance Settings")]
        [SerializeField] private bool enableLOD = true;
        [SerializeField] private float lodDistance1 = 15f; // Close range - full resolution
        [SerializeField] private float lodDistance2 = 30f; // Medium range - reduced resolution
        [SerializeField] private float lodDistance3 = 50f; // Far range - minimal resolution
        [SerializeField] private float updateFrequency = 30f; // FPS for camera updates
        [SerializeField] private bool enableFrustumCulling = true;
        
        [Header("Image Processing")]
        [SerializeField] private bool enableImageFilters = false;
        [SerializeField] private FilterMode filterMode = FilterMode.Bilinear;
        [SerializeField] private bool normalizePixels = true;
        [SerializeField] private float brightness = 1f;
        [SerializeField] private float contrast = 1f;
        [SerializeField] private float saturation = 1f;
        
        [Header("Debug Settings")]
        [SerializeField] private bool showCameraPreview = false;
        [SerializeField] private bool showCameraGizmos = false;
        [SerializeField] private bool logCameraStats = false;
        
        public enum CameraTargetType {
            Agent,          // Follow agent transform
            Custom,         // Follow custom target
            Fixed,          // Fixed position
            FirstPerson,    // Attached to agent (first person view)
            ThirdPerson     // Follow agent with offset (third person view)
        }
        
        // Sensor components
        private CameraSensorComponent cameraSensorComponent;
        private RenderTexture renderTexture;
        private GameObject cameraGameObject;
        
        // Internal state
        private int currentLODLevel = 0;
        private float lastCameraUpdate = 0f;
        private Vector3 lastTargetPosition;
        private int currentWidth;
        private int currentHeight;
        
        // Performance tracking
        private float totalRenderTime = 0f;
        private int renderCount = 0;
        
        public override ISensor Sensor {
            get {
                if (cameraSensorComponent == null) return null;
                
                // Get the sensor directly from the component
                var sensorField = typeof(CameraSensorComponent).GetField("m_Sensor", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                return sensorField?.GetValue(cameraSensorComponent) as ISensor;
            }
        }
        
        public Camera Camera => attachedCamera;
        public RenderTexture RenderTexture => renderTexture;
        public int CurrentLODLevel => currentLODLevel;
        public Vector2 CurrentResolution => new Vector2(currentWidth, currentHeight);
        public float AverageRenderTime => renderCount > 0 ? (totalRenderTime / renderCount) * 1000f : 0f;
        
        protected override void OnInitialize(AgentContext context) {
            SetupCamera();
            SetupCameraSensor();
            UpdateLODSettings();
            
            LogDebug($"Initialized camera sensor: {currentWidth}x{currentHeight}, " +
                    $"Compression={compression}, Target={targetType}");
        }
        
        protected override bool OnValidate(AgentContext context) {
            if (width <= 0 || height <= 0) {
                LogError("Camera width and height must be positive");
                return false;
            }
            
            if (width > 512 || height > 512) {
                LogWarning("High resolution camera may impact performance");
            }
            
            if (updateFrequency <= 0f) {
                LogWarning("Update frequency should be positive");
                updateFrequency = 10f;
            }
            
            if (targetType == CameraTargetType.Custom && customTarget == null) {
                LogWarning("Custom target is null, falling back to agent target");
                targetType = CameraTargetType.Agent;
            }
            
            return true;
        }
        
        protected override void OnEpisodeStart(AgentContext context) {
            ResetCameraState();
            UpdateCameraTarget();
        }
        
        protected override void OnUpdateSensor(AgentContext context, float deltaTime) {
            UpdateLODLevel();
            UpdateCameraPosition(deltaTime);
            
            // Control camera update frequency
            if (Time.time - lastCameraUpdate >= 1f / updateFrequency) {
                UpdateCameraSettings();
                lastCameraUpdate = Time.time;
                
                // Track performance
                float startTime = Time.realtimeSinceStartup;
                
                if (attachedCamera != null) {
                    attachedCamera.Render();
                    
                    float renderTime = Time.realtimeSinceStartup - startTime;
                    totalRenderTime += renderTime;
                    renderCount++;
                    
                    if (logCameraStats && renderCount % 60 == 0) {
                        LogDebug($"Camera render stats: Avg time {AverageRenderTime:F2}ms, Resolution {currentWidth}x{currentHeight}");
                    }
                }
            }
        }
        
        protected override void OnReset() {
            ResetCameraState();
        }
        
        protected override void OnEvent(string eventName, AgentContext context, object eventData = null) {
            switch (eventName) {
                case "UpdateLOD":
                    if (eventData is int lodLevel) {
                        SetLODLevel(lodLevel);
                    }
                    break;
                case "SetResolution":
                    if (eventData is Vector2Int resolution) {
                        SetResolution(resolution.x, resolution.y);
                    }
                    break;
                case "SetTarget":
                    if (eventData is Transform target) {
                        SetCustomTarget(target);
                    }
                    break;
                case "TogglePreview":
                    showCameraPreview = !showCameraPreview;
                    break;
            }
        }
        
        private void SetupCamera() {
            if (attachedCamera == null && createCameraIfMissing) {
                CreateCamera();
            }
            
            if (attachedCamera != null) {
                ConfigureCamera();
                SetupRenderTexture();
            }
        }
        
        private void CreateCamera() {
            cameraGameObject = new GameObject(cameraName);
            cameraGameObject.transform.SetParent(transform);
            
            attachedCamera = cameraGameObject.AddComponent<Camera>();
            
            LogDebug($"Created camera: {cameraName}");
        }
        
        private void ConfigureCamera() {
            attachedCamera.fieldOfView = fieldOfView;
            attachedCamera.nearClipPlane = nearClipPlane;
            attachedCamera.farClipPlane = farClipPlane;
            attachedCamera.clearFlags = clearFlags;
            attachedCamera.backgroundColor = backgroundColor;
            attachedCamera.cullingMask = cullingMask;
            
            // Set camera position based on target type
            UpdateCameraTarget();
        }
        
        private void SetupRenderTexture() {
            currentWidth = width;
            currentHeight = height;
            
            if (renderTexture != null) {
                renderTexture.Release();
            }
            
            renderTexture = new RenderTexture(currentWidth, currentHeight, 24);
            renderTexture.filterMode = filterMode;
            attachedCamera.targetTexture = renderTexture;
        }
        
        private void SetupCameraSensor() {
            if (attachedCamera == null) {
                LogError("No camera available for sensor setup");
                return;
            }
            
            // Create camera sensor component
            cameraSensorComponent = gameObject.AddComponent<CameraSensorComponent>();
            
            // Configure the camera sensor
            var sensorNameField = typeof(CameraSensorComponent).GetField("m_SensorName", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            sensorNameField?.SetValue(cameraSensorComponent, SensorName);
            
            var cameraField = typeof(CameraSensorComponent).GetField("m_Camera",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            cameraField?.SetValue(cameraSensorComponent, attachedCamera);
            
            var widthField = typeof(CameraSensorComponent).GetField("m_Width",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            widthField?.SetValue(cameraSensorComponent, currentWidth);
            
            var heightField = typeof(CameraSensorComponent).GetField("m_Height",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            heightField?.SetValue(cameraSensorComponent, currentHeight);
            
            var grayscaleField = typeof(CameraSensorComponent).GetField("m_Grayscale",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            grayscaleField?.SetValue(cameraSensorComponent, grayscale);
            
            var compressionField = typeof(CameraSensorComponent).GetField("m_Compression",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            compressionField?.SetValue(cameraSensorComponent, compression);
        }
        
        private void UpdateLODLevel() {
            if (!enableLOD) {
                currentLODLevel = 0;
                return;
            }
            
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
                UpdateLODSettings();
                LogDebug($"Camera LOD changed to level {currentLODLevel}");
            }
        }
        
        private void UpdateLODSettings() {
            // Adjust resolution based on LOD level
            float resolutionMultiplier = currentLODLevel switch {
                0 => 1.0f,      // Full resolution
                1 => 0.75f,     // 75% resolution
                2 => 0.5f,      // 50% resolution
                3 => 0.25f,     // 25% resolution
                _ => 1.0f
            };
            
            int newWidth = Mathf.RoundToInt(width * resolutionMultiplier);
            int newHeight = Mathf.RoundToInt(height * resolutionMultiplier);
            
            if (newWidth != currentWidth || newHeight != currentHeight) {
                SetResolution(newWidth, newHeight);
            }
            
            // Adjust update frequency based on LOD
            float frequencyMultiplier = currentLODLevel switch {
                0 => 1.0f,      // Full frequency
                1 => 0.8f,      // 80% frequency
                2 => 0.6f,      // 60% frequency
                3 => 0.4f,      // 40% frequency
                _ => 1.0f
            };
            
            updateFrequency = Mathf.Max(1f, updateFrequency * frequencyMultiplier);
        }
        
        private void UpdateCameraPosition(float deltaTime) {
            if (attachedCamera == null) return;
            
            Transform target = GetCameraTarget();
            if (target == null) return;
            
            Vector3 targetPosition = target.position;
            
            switch (targetType) {
                case CameraTargetType.Agent:
                case CameraTargetType.Custom:
                case CameraTargetType.ThirdPerson:
                    Vector3 desiredPosition = targetPosition + target.TransformDirection(cameraOffset);
                    attachedCamera.transform.position = desiredPosition;
                    attachedCamera.transform.LookAt(targetPosition);
                    attachedCamera.transform.Rotate(cameraRotationOffset);
                    break;
                    
                case CameraTargetType.FirstPerson:
                    attachedCamera.transform.position = targetPosition + cameraOffset;
                    attachedCamera.transform.rotation = target.rotation;
                    attachedCamera.transform.Rotate(cameraRotationOffset);
                    break;
                    
                case CameraTargetType.Fixed:
                    // Camera position is fixed, no updates needed
                    break;
            }
            
            lastTargetPosition = targetPosition;
        }
        
        private void UpdateCameraTarget() {
            Transform target = GetCameraTarget();
            if (target != null && attachedCamera != null) {
                UpdateCameraPosition(0f);
            }
        }
        
        private void UpdateCameraSettings() {
            if (attachedCamera == null) return;
            
            // Apply image processing settings
            if (enableImageFilters) {
                // Note: Unity's built-in post-processing would be used here
                // This is a simplified example
            }
            
            // Update frustum culling if enabled
            if (enableFrustumCulling) {
                attachedCamera.useOcclusionCulling = true;
            }
        }
        
        private Transform GetCameraTarget() {
            return targetType switch {
                CameraTargetType.Custom => customTarget,
                CameraTargetType.Agent => GetAgentTransform(),
                CameraTargetType.FirstPerson => GetAgentTransform(),
                CameraTargetType.ThirdPerson => GetAgentTransform(),
                CameraTargetType.Fixed => transform,
                _ => GetAgentTransform()
            };
        }
        
        private float CalculateImportanceDistance() {
            if (Camera.main != null) {
                return Vector3.Distance(transform.position, Camera.main.transform.position);
            }
            
            return 10f; // Default distance
        }
        
        private void ResetCameraState() {
            lastCameraUpdate = 0f;
            lastTargetPosition = Vector3.zero;
            totalRenderTime = 0f;
            renderCount = 0;
        }
        
        public override string GetDebugInfo() {
            return base.GetDebugInfo() + 
                   $", Resolution={currentWidth}x{currentHeight}, Target={targetType}, " +
                   $"LOD={currentLODLevel}, AvgRenderTime={AverageRenderTime:F2}ms";
        }
        
        // Public configuration methods
        public void SetResolution(int newWidth, int newHeight) {
            if (newWidth <= 0 || newHeight <= 0) return;
            
            currentWidth = newWidth;
            currentHeight = newHeight;
            
            SetupRenderTexture();
            
            // Update camera sensor component if it exists
            if (cameraSensorComponent != null) {
                var widthField = typeof(CameraSensorComponent).GetField("m_Width",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                widthField?.SetValue(cameraSensorComponent, currentWidth);
                
                var heightField = typeof(CameraSensorComponent).GetField("m_Height",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                heightField?.SetValue(cameraSensorComponent, currentHeight);
            }
            
            LogDebug($"Camera resolution updated to {currentWidth}x{currentHeight}");
        }
        
        public void SetTargetType(CameraTargetType type) {
            targetType = type;
            UpdateCameraTarget();
        }
        
        public void SetCustomTarget(Transform target) {
            customTarget = target;
            if (targetType == CameraTargetType.Custom) {
                UpdateCameraTarget();
            }
        }
        
        public void SetCameraOffset(Vector3 offset) {
            cameraOffset = offset;
            UpdateCameraTarget();
        }
        
        public void SetUpdateFrequency(float frequency) {
            updateFrequency = Mathf.Max(1f, frequency);
        }
        
        public void SetLODLevel(int level) {
            currentLODLevel = Mathf.Clamp(level, 0, 3);
            UpdateLODSettings();
        }
        
        public void EnableImageFilters(bool enabled) {
            enableImageFilters = enabled;
        }
        
        public void SetImageProcessingParams(float brightnessValue, float contrastValue, float saturationValue) {
            brightness = Mathf.Clamp01(brightnessValue);
            contrast = Mathf.Clamp01(contrastValue);
            saturation = Mathf.Clamp01(saturationValue);
        }
        
        // Utility methods
        public Texture2D CaptureScreenshot() {
            if (attachedCamera == null || renderTexture == null) return null;
            
            RenderTexture.active = renderTexture;
            attachedCamera.Render();
            
            Texture2D screenshot = new Texture2D(currentWidth, currentHeight, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new Rect(0, 0, currentWidth, currentHeight), 0, 0);
            screenshot.Apply();
            
            RenderTexture.active = null;
            
            return screenshot;
        }
        
        public void SaveScreenshot(string filename) {
            var screenshot = CaptureScreenshot();
            if (screenshot != null) {
                byte[] data = screenshot.EncodeToPNG();
                System.IO.File.WriteAllBytes(filename, data);
                Destroy(screenshot);
                LogDebug($"Screenshot saved: {filename}");
            }
        }
        
        public bool IsTargetVisible(Transform target) {
            if (attachedCamera == null || target == null) return false;
            
            Vector3 screenPoint = attachedCamera.WorldToScreenPoint(target.position);
            return screenPoint.z > 0 && screenPoint.x >= 0 && screenPoint.x <= attachedCamera.pixelWidth &&
                   screenPoint.y >= 0 && screenPoint.y <= attachedCamera.pixelHeight;
        }
        
        private void OnDrawGizmos() {
            if (!showCameraGizmos || attachedCamera == null) return;
            
            Gizmos.color = Color.yellow;
            Gizmos.matrix = attachedCamera.transform.localToWorldMatrix;
            
            // Draw camera frustum
            Gizmos.DrawFrustum(Vector3.zero, attachedCamera.fieldOfView, attachedCamera.farClipPlane,
                attachedCamera.nearClipPlane, attachedCamera.aspect);
        }
        
        private void OnGUI() {
            if (!showCameraPreview || renderTexture == null) return;
            
            // Draw camera preview in corner of screen
            Rect previewRect = new Rect(Screen.width - 200, 10, 180, 135);
            GUI.DrawTexture(previewRect, renderTexture, ScaleMode.ScaleToFit);
            GUI.Box(previewRect, $"{SensorName}\n{currentWidth}x{currentHeight}");
        }
        
        private void OnDestroy() {
            if (renderTexture != null) {
                renderTexture.Release();
                renderTexture = null;
            }
            
            if (cameraGameObject != null && cameraGameObject != gameObject) {
                DestroyImmediate(cameraGameObject);
            }
        }
    }
}