using TCS.MLAgents.Core;
namespace TCS.MLAgents.Interfaces {
    /// <summary>
    /// Interface for components that receive and process actions from the ML agent.
    /// Action receivers handle specific types of actions (movement, rotation, discrete choices, etc.).
    /// </summary>
    public interface IActionReceiver {
        /// <summary>
        /// The number of continuous actions this receiver expects to consume.
        /// </summary>
        int ContinuousActionCount { get; }
        
        /// <summary>
        /// The number of discrete action branches this receiver expects to consume.
        /// Each branch can have multiple possible values.
        /// </summary>
        int DiscreteActionBranchCount { get; }
        
        /// <summary>
        /// The sizes of each discrete action branch (number of possible values per branch).
        /// Only relevant if DiscreteActionBranchCount > 0.
        /// </summary>
        int[] DiscreteActionBranchSizes { get; }
        
        /// <summary>
        /// A descriptive name for this action receiver for debugging and logging.
        /// </summary>
        string ReceiverName { get; }
        
        /// <summary>
        /// Whether this receiver is currently active and should process actions.
        /// </summary>
        bool IsActive { get; }
        
        /// <summary>
        /// The priority of this receiver when multiple receivers compete for the same actions.
        /// Higher priority receivers get actions first.
        /// </summary>
        int Priority { get; }
        
        /// <summary>
        /// Called to process continuous actions from the neural network.
        /// </summary>
        /// <param name="actions">The continuous actions array</param>
        /// <param name="startIndex">Starting index in the actions array for this receiver</param>
        /// <param name="context">Agent context for accessing shared data</param>
        void ReceiveContinuousActions(float[] actions, int startIndex, AgentContext context);
        
        /// <summary>
        /// Called to process discrete actions from the neural network.
        /// </summary>
        /// <param name="actions">The discrete actions array</param>
        /// <param name="startIndex">Starting index in the actions array for this receiver</param>
        /// <param name="context">Agent context for accessing shared data</param>
        void ReceiveDiscreteActions(int[] actions, int startIndex, AgentContext context);
        
        /// <summary>
        /// Called to provide heuristic actions when in manual/testing mode.
        /// </summary>
        /// <param name="continuousActions">Continuous actions buffer to write to</param>
        /// <param name="discreteActions">Discrete actions buffer to write to</param>
        /// <param name="continuousStartIndex">Starting index for continuous actions</param>
        /// <param name="discreteStartIndex">Starting index for discrete actions</param>
        /// <param name="context">Agent context for heuristic logic</param>
        void ProvideHeuristicActions(float[] continuousActions, int[] discreteActions, 
            int continuousStartIndex, int discreteStartIndex, AgentContext context);
        
        /// <summary>
        /// Called to validate that the receiver can function correctly.
        /// Should check dependencies and configuration.
        /// </summary>
        /// <param name="context">Agent context for validation</param>
        /// <returns>True if the receiver is valid and ready to use</returns>
        bool ValidateReceiver(AgentContext context);
        
        /// <summary>
        /// Called when the receiver is first initialized.
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
        
        /// <summary>
        /// Called every fixed update step for physics and continuous processing.
        /// </summary>
        /// <param name="context">Agent context for fixed update logic</param>
        void FixedUpdate(AgentContext context);
    }
    
    /// <summary>
    /// Base implementation of IActionReceiver with common functionality.
    /// Provides standard implementations for name, active state, and validation.
    /// </summary>
    public abstract class ActionReceiverBase : IActionReceiver {
        [SerializeField] protected string receiverName;
        [SerializeField] protected bool isActive = true;
        [SerializeField] protected int priority = 0;
        
        public virtual string ReceiverName => string.IsNullOrEmpty(receiverName) ? GetType().Name : receiverName;
        public virtual bool IsActive => isActive;
        public virtual int Priority => priority;
        
        public abstract int ContinuousActionCount { get; }
        public abstract int DiscreteActionBranchCount { get; }
        public virtual int[] DiscreteActionBranchSizes => null;
        
        protected bool isInitialized = false;
        
        public virtual void Initialize(AgentContext context) {
            if (isInitialized) return;
            
            OnInitialize(context);
            isInitialized = true;
        }
        
        public virtual bool ValidateReceiver(AgentContext context) {
            if (context?.AgentGameObject == null) {
                UnityEngine.Debug.LogWarning($"[{ReceiverName}] AgentContext or GameObject is null");
                return false;
            }
            
            return OnValidate(context);
        }
        
        public abstract void ReceiveContinuousActions(float[] actions, int startIndex, AgentContext context);
        
        public virtual void ReceiveDiscreteActions(int[] actions, int startIndex, AgentContext context) {
            // Override in derived classes if discrete actions are needed
        }
        
        public virtual void ProvideHeuristicActions(float[] continuousActions, int[] discreteActions, 
            int continuousStartIndex, int discreteStartIndex, AgentContext context) {
            // Override in derived classes to provide heuristic behavior
        }
        
        public virtual void OnEpisodeBegin(AgentContext context) {
            // Override in derived classes if needed
        }
        
        public virtual void FixedUpdate(AgentContext context) {
            // Override in derived classes if needed
        }
        
        /// <summary>
        /// Override this method to implement receiver-specific initialization.
        /// </summary>
        protected virtual void OnInitialize(AgentContext context) {
            // Override in derived classes
        }
        
        /// <summary>
        /// Override this method to implement receiver-specific validation.
        /// </summary>
        /// <param name="context">Agent context for validation</param>
        /// <returns>True if receiver is valid</returns>
        protected virtual bool OnValidate(AgentContext context) {
            return true;
        }
        
        /// <summary>
        /// Safely gets a continuous action value with bounds checking.
        /// </summary>
        /// <param name="actions">Actions array</param>
        /// <param name="index">Index to access</param>
        /// <param name="defaultValue">Default value if index is invalid</param>
        /// <returns>Action value or default</returns>
        protected float SafeGetContinuousAction(float[] actions, int index, float defaultValue = 0f) {
            if (actions == null || index < 0 || index >= actions.Length) {
                if (actions == null) {
                    UnityEngine.Debug.LogWarning($"[{ReceiverName}] Actions array is null");
                } else {
                    UnityEngine.Debug.LogWarning($"[{ReceiverName}] Action index {index} out of bounds (length: {actions.Length})");
                }
                return defaultValue;
            }
            
            float value = actions[index];
            if (float.IsNaN(value) || float.IsInfinity(value)) {
                UnityEngine.Debug.LogWarning($"[{ReceiverName}] Invalid action value at index {index}: {value}");
                return defaultValue;
            }
            
            return value;
        }
        
        /// <summary>
        /// Safely gets a discrete action value with bounds checking.
        /// </summary>
        /// <param name="actions">Actions array</param>
        /// <param name="index">Index to access</param>
        /// <param name="defaultValue">Default value if index is invalid</param>
        /// <returns>Action value or default</returns>
        protected int SafeGetDiscreteAction(int[] actions, int index, int defaultValue = 0) {
            if (actions == null || index < 0 || index >= actions.Length) {
                if (actions == null) {
                    UnityEngine.Debug.LogWarning($"[{ReceiverName}] Actions array is null");
                } else {
                    UnityEngine.Debug.LogWarning($"[{ReceiverName}] Action index {index} out of bounds (length: {actions.Length})");
                }
                return defaultValue;
            }
            
            return actions[index];
        }
        
        /// <summary>
        /// Safely sets a continuous action value with bounds checking.
        /// </summary>
        /// <param name="actions">Actions array to write to</param>
        /// <param name="index">Index to write at</param>
        /// <param name="value">Value to write</param>
        protected void SafeSetContinuousAction(float[] actions, int index, float value) {
            if (actions == null || index < 0 || index >= actions.Length) {
                UnityEngine.Debug.LogWarning($"[{ReceiverName}] Cannot set action at index {index}");
                return;
            }
            
            if (float.IsNaN(value) || float.IsInfinity(value)) {
                UnityEngine.Debug.LogWarning($"[{ReceiverName}] Attempting to set invalid action value: {value}");
                value = 0f;
            }
            
            actions[index] = value;
        }
        
        /// <summary>
        /// Safely sets a discrete action value with bounds checking.
        /// </summary>
        /// <param name="actions">Actions array to write to</param>
        /// <param name="index">Index to write at</param>
        /// <param name="value">Value to write</param>
        protected void SafeSetDiscreteAction(int[] actions, int index, int value) {
            if (actions == null || index < 0 || index >= actions.Length) {
                UnityEngine.Debug.LogWarning($"[{ReceiverName}] Cannot set action at index {index}");
                return;
            }
            
            actions[index] = value;
        }
        
        /// <summary>
        /// Clamps a continuous action to a specified range.
        /// </summary>
        /// <param name="value">Value to clamp</param>
        /// <param name="min">Minimum value</param>
        /// <param name="max">Maximum value</param>
        /// <returns>Clamped value</returns>
        protected float ClampAction(float value, float min = -1f, float max = 1f) {
            return UnityEngine.Mathf.Clamp(value, min, max);
        }
    }
}