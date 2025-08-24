using TCS.MLAgents.Core;
using Unity.MLAgents.Sensors;
namespace TCS.MLAgents.Interfaces {
    /// <summary>
    /// Interface for components that provide observations to the ML agent.
    /// Observation providers can contribute data that the neural network uses for decision making.
    /// </summary>
    public interface IObservationProvider {
        /// <summary>
        /// The number of float values this provider will add to observations.
        /// Used for validation and neural network input size calculation.
        /// </summary>
        int ObservationSize { get; }
        
        /// <summary>
        /// A descriptive name for this observation provider for debugging and logging.
        /// </summary>
        string ProviderName { get; }
        
        /// <summary>
        /// Whether this provider is currently active and should provide observations.
        /// </summary>
        bool IsActive { get; }
        
        /// <summary>
        /// Called to add observations to the sensor.
        /// Should add exactly ObservationSize number of float values.
        /// </summary>
        /// <param name="sensor">The vector sensor to add observations to</param>
        /// <param name="context">Agent context for accessing shared data</param>
        void CollectObservations(VectorSensor sensor, AgentContext context);
        
        /// <summary>
        /// Called to validate that the provider can function correctly.
        /// Should check dependencies and configuration.
        /// </summary>
        /// <param name="context">Agent context for validation</param>
        /// <returns>True if the provider is valid and ready to use</returns>
        bool ValidateProvider(AgentContext context);
        
        /// <summary>
        /// Called when the provider is first initialized.
        /// Use this to set up dependencies and cache references.
        /// </summary>
        /// <param name="context">Agent context for initialization</param>
        void Initialize(AgentContext context);
        
        /// <summary>
        /// Called at the start of each episode.
        /// Use this to reset any episode-specific state.
        /// </summary>
        /// <param name="context">Agent context for episode reset</param>
        void OnEpisodeBegin(AgentContext context);
    }
    
    /// <summary>
    /// Base implementation of IObservationProvider with common functionality.
    /// Provides standard implementations for name, active state, and validation.
    /// </summary>
    public abstract class ObservationProviderBase : IObservationProvider {
        [SerializeField] protected string providerName;
        [SerializeField] protected bool isActive = true;
        [SerializeField] protected int observationSize;
        
        public virtual string ProviderName => string.IsNullOrEmpty(providerName) ? GetType().Name : providerName;
        public virtual bool IsActive => isActive;
        public abstract int ObservationSize { get; }
        
        protected bool isInitialized = false;
        
        public virtual void Initialize(AgentContext context) {
            if (isInitialized) return;
            
            OnInitialize(context);
            isInitialized = true;
        }
        
        public virtual bool ValidateProvider(AgentContext context) {
            if (context?.AgentGameObject == null) {
                Debug.LogWarning($"[{ProviderName}] AgentContext or GameObject is null");
                return false;
            }
            
            return OnValidate(context);
        }
        
        public abstract void CollectObservations(VectorSensor sensor, AgentContext context);
        
        public virtual void OnEpisodeBegin(AgentContext context) {
            // Override in derived classes if needed
        }
        
        /// <summary>
        /// Override this method to implement provider-specific initialization.
        /// </summary>
        protected virtual void OnInitialize(AgentContext context) {
            // Override in derived classes
        }
        
        /// <summary>
        /// Override this method to implement provider-specific validation.
        /// </summary>
        /// <param name="context">Agent context for validation</param>
        /// <returns>True if provider is valid</returns>
        protected virtual bool OnValidate(AgentContext context) {
            return true;
        }
        
        /// <summary>
        /// Safely adds observations to the sensor with bounds checking.
        /// </summary>
        /// <param name="sensor">Vector sensor to add to</param>
        /// <param name="values">Values to add</param>
        /// <param name="expectedCount">Expected number of values for validation</param>
        protected void SafeAddObservations(VectorSensor sensor, float[] values, int expectedCount) {
            if (values == null) {
                Debug.LogWarning($"[{ProviderName}] Null observation values, padding with zeros");
                for (int i = 0; i < expectedCount; i++) {
                    sensor.AddObservation(0f);
                }
                return;
            }
            
            if (values.Length != expectedCount) {
                Debug.LogWarning($"[{ProviderName}] Expected {expectedCount} observations but got {values.Length}");
            }
            
            int count = Mathf.Min(values.Length, expectedCount);
            for (int i = 0; i < count; i++) {
                sensor.AddObservation(values[i]);
            }
            
            // Pad with zeros if we didn't have enough values
            for (int i = count; i < expectedCount; i++) {
                sensor.AddObservation(0f);
            }
        }
        
        /// <summary>
        /// Safely adds a single observation with NaN/Infinity checking.
        /// </summary>
        /// <param name="sensor">Vector sensor to add to</param>
        /// <param name="value">Value to add</param>
        /// <param name="defaultValue">Default value to use if input is invalid</param>
        protected void SafeAddObservation(VectorSensor sensor, float value, float defaultValue = 0f) {
            if (float.IsNaN(value) || float.IsInfinity(value)) {
                Debug.LogWarning($"[{ProviderName}] Invalid observation value: {value}, using default: {defaultValue}");
                sensor.AddObservation(defaultValue);
            } else {
                sensor.AddObservation(value);
            }
        }
    }
}