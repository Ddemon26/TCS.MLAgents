using TCS.MLAgents.Core;
using Unity.MLAgents.Sensors;

namespace TCS.MLAgents.Interfaces {
    /// <summary>
    /// Interface for sensor providers that extend Unity ML-Agents sensor capabilities.
    /// Handles specialized sensors like raycast sensors, camera sensors, and custom sensors.
    /// </summary>
    public interface ISensorProvider {
        /// <summary>
        /// Unique identifier for this sensor provider
        /// </summary>
        string SensorName { get; }
        
        /// <summary>
        /// Whether this sensor provider is currently active
        /// </summary>
        bool IsActive { get; }
        
        /// <summary>
        /// Priority for sensor processing order (higher = processed first)
        /// </summary>
        int Priority { get; }
        
        /// <summary>
        /// The Unity ML-Agents sensor instance
        /// </summary>
        ISensor Sensor { get; }
        
        /// <summary>
        /// Initialize the sensor provider with agent context
        /// </summary>
        void Initialize(AgentContext context);
        
        /// <summary>
        /// Validate sensor configuration and setup
        /// </summary>
        bool ValidateSensor(AgentContext context);
        
        /// <summary>
        /// Called when an episode begins
        /// </summary>
        void OnEpisodeBegin(AgentContext context);
        
        /// <summary>
        /// Update sensor data (called before sensor writes observations)
        /// </summary>
        void UpdateSensor(AgentContext context, float deltaTime);
        
        /// <summary>
        /// Reset sensor state
        /// </summary>
        void Reset();
        
        /// <summary>
        /// Get debug information about the sensor
        /// </summary>
        string GetDebugInfo();
        
        /// <summary>
        /// Handle sensor-related events
        /// </summary>
        void OnSensorEvent(string eventName, AgentContext context, object eventData = null);
    }
    
    /// <summary>
    /// Base implementation of ISensorProvider with common functionality
    /// </summary>
    public abstract class SensorProviderBase : MonoBehaviour, ISensorProvider {
        [SerializeField] protected string sensorName;
        [SerializeField] protected bool isActive = true;
        [SerializeField] protected int priority = 0;
        [SerializeField] protected bool enableDebugMode = false;
        
        public virtual string SensorName => string.IsNullOrEmpty(sensorName) ? GetType().Name : sensorName;
        public virtual bool IsActive => isActive;
        public virtual int Priority => priority;
        public abstract ISensor Sensor { get; }
        
        protected bool isInitialized = false;
        protected AgentContext agentContext;
        
        protected virtual void Awake() {
            if (string.IsNullOrEmpty(sensorName)) {
                sensorName = GetType().Name;
            }
        }
        
        public virtual void Initialize(AgentContext context) {
            if (isInitialized) return;
            
            agentContext = context;
            OnInitialize(context);
            isInitialized = true;
            
            if (enableDebugMode) {
                Debug.Log($"[{SensorName}] Initialized sensor provider");
            }
        }
        
        public virtual bool ValidateSensor(AgentContext context) {
            if (context?.AgentGameObject == null) {
                Debug.LogError($"[{SensorName}] AgentContext or GameObject is null");
                return false;
            }
            
            return OnValidate(context);
        }
        
        public virtual void OnEpisodeBegin(AgentContext context) {
            OnEpisodeStart(context);
        }
        
        public virtual void UpdateSensor(AgentContext context, float deltaTime) {
            if (!isActive) return;
            
            try {
                OnUpdateSensor(context, deltaTime);
            } catch (Exception e) {
                Debug.LogError($"[{SensorName}] Error updating sensor: {e.Message}");
            }
        }
        
        public virtual void Reset() {
            OnReset();
        }
        
        public virtual string GetDebugInfo() {
            return $"{SensorName}: Active={IsActive}, Priority={Priority}";
        }
        
        public virtual void OnSensorEvent(string eventName, AgentContext context, object eventData = null) {
            OnEvent(eventName, context, eventData);
        }
        
        // Protected virtual methods for subclasses to override
        protected virtual void OnInitialize(AgentContext context) {
            // Override in derived classes
        }
        
        protected virtual bool OnValidate(AgentContext context) {
            return true;
        }
        
        protected virtual void OnEpisodeStart(AgentContext context) {
            // Override in derived classes
        }
        
        protected virtual void OnUpdateSensor(AgentContext context, float deltaTime) {
            // Override in derived classes
        }
        
        protected virtual void OnReset() {
            // Override in derived classes
        }
        
        protected virtual void OnEvent(string eventName, AgentContext context, object eventData = null) {
            // Override in derived classes
        }
        
        // Utility methods
        public void SetActive(bool active) {
            isActive = active;
        }
        
        public void SetPriority(int newPriority) {
            priority = newPriority;
        }
        
        public void SetSensorName(string name) {
            sensorName = name;
        }
        
        public void EnableDebugMode(bool enabled) {
            enableDebugMode = enabled;
        }
        
        // Helper methods for sensor management
        protected Transform GetAgentTransform() {
            return agentContext?.AgentGameObject?.transform;
        }
        
        protected Vector3 GetAgentPosition() {
            return GetAgentTransform()?.position ?? Vector3.zero;
        }
        
        protected Quaternion GetAgentRotation() {
            return GetAgentTransform()?.rotation ?? Quaternion.identity;
        }
        
        protected T GetAgentComponent<T>() where T : Component {
            return agentContext?.GetComponent<T>();
        }
        
        protected void LogDebug(string message) {
            if (enableDebugMode) {
                Debug.Log($"[{SensorName}] {message}");
            }
        }
        
        protected void LogWarning(string message) {
            Debug.LogWarning($"[{SensorName}] {message}");
        }
        
        protected void LogError(string message) {
            Debug.LogError($"[{SensorName}] {message}");
        }
    }
}