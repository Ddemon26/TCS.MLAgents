namespace TCS.MLAgents.Observations {
    /// <summary>
    /// High-level manager for vision systems with advanced performance optimization,
    /// multi-agent coordination, and centralized vision processing.
    /// </summary>
    [Serializable]
    public class VisionSystemManager : MonoBehaviour {
        [Header("System Configuration")]
        [SerializeField] private bool enableVisionSystem = true;
        [SerializeField] private VisionOptimizationMode optimizationMode = VisionOptimizationMode.Adaptive;
        [SerializeField] private int maxConcurrentRaycasts = 100;
        [SerializeField] private float globalUpdateInterval = 0.05f;
        
        [Header("Performance Monitoring")]
        [SerializeField] private bool enablePerformanceMonitoring = true;
        [SerializeField] private float performanceUpdateInterval = 1f;
        [SerializeField] private float maxFrameTimeMs = 16.67f; // 60 FPS target
        [SerializeField] private bool autoOptimizePerformance = true;
        
        [Header("Level of Detail (LOD)")]
        [SerializeField] private bool enableLOD = true;
        [SerializeField] private List<VisionLODLevel> lodLevels = new List<VisionLODLevel>();
        [SerializeField] private float lodUpdateInterval = 0.5f;
        
        [Header("Spatial Optimization")]
        [SerializeField] private bool enableSpatialPartitioning = true;
        [SerializeField] private float spatialGridSize = 10f;
        [SerializeField] private bool enableOcclusion = true;
        [SerializeField] private LayerMask occlusionLayers = -1;
        
        [Header("Debug Settings")]
        [SerializeField] private bool showDebugInfo = false;
        [SerializeField] private bool enableGlobalDebugger = false;
        [SerializeField] private VisionDebugger globalDebugger = new VisionDebugger();
        
        public enum VisionOptimizationMode {
            None,           // No optimization
            Static,         // Fixed optimization settings
            Adaptive,       // Dynamic optimization based on performance
            Aggressive      // Maximum optimization, may reduce quality
        }
        
        [Serializable]
        public class VisionLODLevel {
            [SerializeField] public string levelName;
            [SerializeField] public float minDistance = 0f;
            [SerializeField] public float maxDistance = 50f;
            [SerializeField] public float rayCountMultiplier = 1f;
            [SerializeField] public float updateFrequencyMultiplier = 1f;
            [SerializeField] public bool enableVerticalRays = true;
            [SerializeField] public VisionObservationProvider.VisionMode visionMode = VisionObservationProvider.VisionMode.Raycast;
        }
        
        // Internal state
        private static VisionSystemManager instance;
        private List<VisionObservationProvider> registeredProviders = new List<VisionObservationProvider>();
        private Dictionary<VisionObservationProvider, VisionLODLevel> providerLODLevels = new Dictionary<VisionObservationProvider, VisionLODLevel>();
        private Dictionary<Vector3, List<VisionObservationProvider>> spatialGrid = new Dictionary<Vector3, List<VisionObservationProvider>>();
        
        // Performance tracking
        private float lastPerformanceUpdate = 0f;
        private float lastLODUpdate = 0f;
        private float averageFrameTime = 0f;
        private int totalRaycastsThisFrame = 0;
        private float raycastBudget = 100f;
        
        // Optimization state
        private Queue<Action> deferredRaycasts = new Queue<Action>();
        private Dictionary<string, object> cachedResults = new Dictionary<string, object>();
        private float lastCacheClear = 0f;
        Camera m_camera;

        public static VisionSystemManager Instance {
            get {
                if (instance == null) {
                    instance = FindObjectOfType<VisionSystemManager>();
                    if (instance == null) {
                        var go = new GameObject("VisionSystemManager");
                        instance = go.AddComponent<VisionSystemManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }
        
        public bool IsVisionSystemEnabled => enableVisionSystem;
        public int RegisteredProviderCount => registeredProviders.Count;
        public float CurrentFrameTime => averageFrameTime;
        public int TotalRaycastsThisFrame => totalRaycastsThisFrame;
        
        private void Awake() {
            m_camera = Camera.main;
            if (instance == null) {
                instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            } else if (instance != this) {
                Destroy(gameObject);
            }
        }
        
        private void Initialize() {
            SetupDefaultLODLevels();
            
            if (enableGlobalDebugger) {
                globalDebugger.Initialize(null);
            }
            
            Debug.Log("[VisionSystemManager] Initialized vision system manager");
        }
        
        private void Update() {
            if (!enableVisionSystem) return;
            
            UpdatePerformanceTracking();
            UpdateLODSystem();
            ProcessDeferredRaycasts();
            UpdateSpatialPartitioning();
            
            if (enableGlobalDebugger) {
                globalDebugger.Update(null);
            }
        }
        
        private void OnGUI() {
            if (showDebugInfo) {
                DrawSystemDebugInfo();
            }
            
            if (enableGlobalDebugger) {
                globalDebugger.OnGUI();
            }
        }
        
        public void RegisterVisionProvider(VisionObservationProvider provider) {
            if (!registeredProviders.Contains(provider)) {
                registeredProviders.Add(provider);
                AssignLODLevel(provider);
                UpdateSpatialGrid(provider);
                
                Debug.Log($"[VisionSystemManager] Registered vision provider: {provider.ProviderName}");
            }
        }
        
        public void UnregisterVisionProvider(VisionObservationProvider provider) {
            registeredProviders.Remove(provider);
            providerLODLevels.Remove(provider);
            RemoveFromSpatialGrid(provider);
            
            Debug.Log($"[VisionSystemManager] Unregistered vision provider: {provider.ProviderName}");
        }
        
        public bool CanPerformRaycast(VisionObservationProvider provider) {
            if (!enableVisionSystem) return false;
            
            // Check raycast budget
            if (totalRaycastsThisFrame >= maxConcurrentRaycasts) {
                return false;
            }
            
            // Check performance constraints
            if (autoOptimizePerformance && averageFrameTime > maxFrameTimeMs) {
                return false;
            }
            
            return true;
        }
        
        public void ReportRaycast(VisionObservationProvider provider, float raycastTime) {
            totalRaycastsThisFrame++;
        }
        
        public void DeferRaycast(Action raycastAction) {
            deferredRaycasts.Enqueue(raycastAction);
        }
        
        public VisionLODLevel GetLODLevel(VisionObservationProvider provider) {
            return providerLODLevels.GetValueOrDefault(provider, GetDefaultLODLevel());
        }
        
        public void SetProviderLODLevel(VisionObservationProvider provider, string lodLevelName) {
            var lodLevel = lodLevels.Find(l => l.levelName == lodLevelName);
            if (lodLevel != null) {
                providerLODLevels[provider] = lodLevel;
            }
        }
        
        private void UpdatePerformanceTracking() {
            if (!enablePerformanceMonitoring) return;
            
            float currentTime = Time.time;
            if (currentTime - lastPerformanceUpdate >= performanceUpdateInterval) {
                // Update average frame time
                averageFrameTime = Time.deltaTime * 1000f; // Convert to milliseconds
                
                // Reset raycast counter
                totalRaycastsThisFrame = 0;
                
                // Auto-optimize if needed
                if (autoOptimizePerformance) {
                    AutoOptimizeSystem();
                }
                
                lastPerformanceUpdate = currentTime;
            }
        }
        
        private void UpdateLODSystem() {
            if (!enableLOD) return;
            
            float currentTime = Time.time;
            if (currentTime - lastLODUpdate >= lodUpdateInterval) {
                foreach (var provider in registeredProviders) {
                    UpdateProviderLOD(provider);
                }
                
                lastLODUpdate = currentTime;
            }
        }
        
        private void UpdateProviderLOD(VisionObservationProvider provider) {
            // Calculate distance to nearest important target or camera
            float distance = CalculateProviderImportanceDistance(provider);
            
            // Find appropriate LOD level
            VisionLODLevel appropriateLOD = null;
            foreach (var lodLevel in lodLevels) {
                if (distance >= lodLevel.minDistance && distance <= lodLevel.maxDistance) {
                    appropriateLOD = lodLevel;
                    break;
                }
            }
            
            if (appropriateLOD != null && providerLODLevels.GetValueOrDefault(provider) != appropriateLOD) {
                providerLODLevels[provider] = appropriateLOD;
                ApplyLODToProvider(provider, appropriateLOD);
            }
        }
        
        private void ApplyLODToProvider(VisionObservationProvider provider, VisionLODLevel lodLevel) {
            // This would require extending VisionObservationProvider to accept LOD parameters
            // For now, we'll log the change
            Debug.Log($"[VisionSystemManager] Applied LOD '{lodLevel.levelName}' to {provider.ProviderName}");
        }
        
        private float CalculateProviderImportanceDistance(VisionObservationProvider provider) {
            // Simple implementation: distance to main camera
            if (m_camera != null && provider.Context?.AgentGameObject != null) {
                return Vector3.Distance(m_camera.transform.position, 
                                        provider.Context.AgentGameObject.transform.position);
            }
            
            return 10f; // Default distance
        }
        
        private void ProcessDeferredRaycasts() {
            int processedCount = 0;
            int maxProcessThisFrame = Mathf.Max(1, maxConcurrentRaycasts - totalRaycastsThisFrame);
            
            while (deferredRaycasts.Count > 0 && processedCount < maxProcessThisFrame) {
                var raycast = deferredRaycasts.Dequeue();
                raycast?.Invoke();
                processedCount++;
            }
        }
        
        private void UpdateSpatialPartitioning() {
            if (!enableSpatialPartitioning) return;
            
            // Update spatial grid for all providers
            foreach (var provider in registeredProviders) {
                UpdateSpatialGrid(provider);
            }
        }
        
        private void UpdateSpatialGrid(VisionObservationProvider provider) {
            if (provider.Context?.AgentGameObject == null) return;
            
            Vector3 position = provider.Context.AgentGameObject.transform.position;
            Vector3 gridCell = new Vector3(
                Mathf.Floor(position.x / spatialGridSize) * spatialGridSize,
                Mathf.Floor(position.y / spatialGridSize) * spatialGridSize,
                Mathf.Floor(position.z / spatialGridSize) * spatialGridSize
            );
            
            if (!spatialGrid.ContainsKey(gridCell)) {
                spatialGrid[gridCell] = new List<VisionObservationProvider>();
            }
            
            if (!spatialGrid[gridCell].Contains(provider)) {
                spatialGrid[gridCell].Add(provider);
            }
        }
        
        private void RemoveFromSpatialGrid(VisionObservationProvider provider) {
            foreach (var cell in spatialGrid.Values) {
                cell.Remove(provider);
            }
        }
        
        private void AutoOptimizeSystem() {
            if (averageFrameTime <= maxFrameTimeMs) return;
            
            float performanceRatio = averageFrameTime / maxFrameTimeMs;
            
            if (performanceRatio > 1.5f) {
                // Aggressive optimization
                globalUpdateInterval = Mathf.Min(globalUpdateInterval * 1.2f, 0.2f);
                maxConcurrentRaycasts = Mathf.Max(maxConcurrentRaycasts - 10, 20);
            } else if (performanceRatio > 1.2f) {
                // Moderate optimization
                globalUpdateInterval = Mathf.Min(globalUpdateInterval * 1.1f, 0.15f);
                maxConcurrentRaycasts = Mathf.Max(maxConcurrentRaycasts - 5, 30);
            }
            
            Debug.Log($"[VisionSystemManager] Auto-optimized: UpdateInterval={globalUpdateInterval:F3}, MaxRaycasts={maxConcurrentRaycasts}");
        }
        
        private void AssignLODLevel(VisionObservationProvider provider) {
            if (lodLevels.Count > 0) {
                providerLODLevels[provider] = lodLevels[0]; // Default to first LOD level
            }
        }
        
        private VisionLODLevel GetDefaultLODLevel() {
            return lodLevels.Count > 0 ? lodLevels[0] : null;
        }
        
        private void SetupDefaultLODLevels() {
            if (lodLevels.Count == 0) {
                lodLevels.AddRange(new[] {
                    new VisionLODLevel {
                        levelName = "High",
                        minDistance = 0f,
                        maxDistance = 10f,
                        rayCountMultiplier = 1f,
                        updateFrequencyMultiplier = 1f,
                        enableVerticalRays = true,
                        visionMode = VisionObservationProvider.VisionMode.Raycast
                    },
                    new VisionLODLevel {
                        levelName = "Medium",
                        minDistance = 10f,
                        maxDistance = 25f,
                        rayCountMultiplier = 0.7f,
                        updateFrequencyMultiplier = 0.8f,
                        enableVerticalRays = false,
                        visionMode = VisionObservationProvider.VisionMode.Raycast
                    },
                    new VisionLODLevel {
                        levelName = "Low",
                        minDistance = 25f,
                        maxDistance = 50f,
                        rayCountMultiplier = 0.5f,
                        updateFrequencyMultiplier = 0.5f,
                        enableVerticalRays = false,
                        visionMode = VisionObservationProvider.VisionMode.Spherecast
                    }
                });
            }
        }
        
        private void DrawSystemDebugInfo() {
            GUI.Box(new Rect(10, 10, 300, 150), "Vision System Manager");
            
            int yPos = 35;
            GUI.Label(new Rect(20, yPos, 280, 20), $"Providers: {registeredProviders.Count}");
            yPos += 20;
            GUI.Label(new Rect(20, yPos, 280, 20), $"Frame Time: {averageFrameTime:F2}ms");
            yPos += 20;
            GUI.Label(new Rect(20, yPos, 280, 20), $"Raycasts: {totalRaycastsThisFrame}/{maxConcurrentRaycasts}");
            yPos += 20;
            GUI.Label(new Rect(20, yPos, 280, 20), $"Deferred: {deferredRaycasts.Count}");
            yPos += 20;
            GUI.Label(new Rect(20, yPos, 280, 20), $"Update Interval: {globalUpdateInterval:F3}s");
            yPos += 20;
            GUI.Label(new Rect(20, yPos, 280, 20), $"Optimization: {optimizationMode}");
        }
        
        // Public configuration methods
        public void SetOptimizationMode(VisionOptimizationMode mode) {
            optimizationMode = mode;
        }
        
        public void SetMaxConcurrentRaycasts(int maxRaycasts) {
            maxConcurrentRaycasts = Mathf.Max(1, maxRaycasts);
        }
        
        public void SetGlobalUpdateInterval(float interval) {
            globalUpdateInterval = Mathf.Max(0.01f, interval);
        }
        
        public void EnableLOD(bool enabled) {
            enableLOD = enabled;
        }
        
        public void EnableSpatialPartitioning(bool enabled) {
            enableSpatialPartitioning = enabled;
        }
        
        // Utility methods
        public List<VisionObservationProvider> GetProvidersInArea(Vector3 center, float radius) {
            var result = new List<VisionObservationProvider>();
            
            foreach (var provider in registeredProviders) {
                if (provider.Context?.AgentGameObject != null) {
                    float distance = Vector3.Distance(center, provider.Context.AgentGameObject.transform.position);
                    if (distance <= radius) {
                        result.Add(provider);
                    }
                }
            }
            
            return result;
        }
        
        public void ClearCache() {
            cachedResults.Clear();
            lastCacheClear = Time.time;
        }
        
        public Dictionary<string, object> GetSystemStats() {
            return new Dictionary<string, object> {
                ["ProviderCount"] = registeredProviders.Count,
                ["FrameTime"] = averageFrameTime,
                ["RaycastCount"] = totalRaycastsThisFrame,
                ["DeferredCount"] = deferredRaycasts.Count,
                ["OptimizationMode"] = optimizationMode.ToString(),
                ["LODEnabled"] = enableLOD,
                ["SpatialPartitioning"] = enableSpatialPartitioning
            };
        }
    }
}