using TCS.MLAgents.Core;
using UnityEngine;

namespace TCS.MLAgents.Interfaces {
    /// <summary>
    /// Interface for episode lifecycle management handlers.
    /// Handles episode initialization, termination conditions, and state management.
    /// </summary>
    public interface IEpisodeHandler {
        /// <summary>
        /// Unique identifier for this episode handler
        /// </summary>
        string HandlerName { get; }
        
        /// <summary>
        /// Whether this handler is currently active
        /// </summary>
        bool IsActive { get; }
        
        /// <summary>
        /// Priority for handler execution order (higher = earlier)
        /// </summary>
        int Priority { get; }
        
        /// <summary>
        /// Initialize the episode handler with agent context
        /// </summary>
        void Initialize(AgentContext context);
        
        /// <summary>
        /// Check if conditions for episode start are met
        /// </summary>
        bool ShouldStartEpisode(AgentContext context);
        
        /// <summary>
        /// Check if conditions for episode end are met
        /// </summary>
        bool ShouldEndEpisode(AgentContext context);
        
        /// <summary>
        /// Called when an episode begins
        /// </summary>
        void OnEpisodeBegin(AgentContext context);
        
        /// <summary>
        /// Called when an episode ends
        /// </summary>
        void OnEpisodeEnd(AgentContext context, EpisodeEndReason reason);
        
        /// <summary>
        /// Called every frame during an episode
        /// </summary>
        void OnEpisodeUpdate(AgentContext context, float deltaTime);
        
        /// <summary>
        /// Reset handler state
        /// </summary>
        void Reset();
        
        /// <summary>
        /// Get debug information about the handler
        /// </summary>
        string GetDebugInfo();
    }
    
    /// <summary>
    /// Reasons why an episode might end
    /// </summary>
    public enum EpisodeEndReason {
        Success,            // Task completed successfully
        Failure,            // Task failed (collision, timeout, etc.)
        MaxStepsReached,    // Maximum steps per episode reached
        TimeLimit,          // Time limit exceeded
        BoundaryViolation,  // Agent went out of bounds
        ManualReset,        // Manually triggered reset
        Error               // Error condition
    }
    
    /// <summary>
    /// Base implementation of IEpisodeHandler with common functionality
    /// </summary>
    public abstract class EpisodeHandlerBase : IEpisodeHandler {
        [SerializeField] protected string handlerName;
        [SerializeField] protected bool isActive = true;
        [SerializeField] protected int priority = 0;
        
        public virtual string HandlerName => string.IsNullOrEmpty(handlerName) ? GetType().Name : handlerName;
        public virtual bool IsActive => isActive;
        public virtual int Priority => priority;
        
        protected bool isInitialized = false;
        protected AgentContext agentContext;
        
        public virtual void Initialize(AgentContext context) {
            if (isInitialized) return;
            
            agentContext = context;
            OnInitialize(context);
            isInitialized = true;
        }
        
        public abstract bool ShouldStartEpisode(AgentContext context);
        public abstract bool ShouldEndEpisode(AgentContext context);
        
        public virtual void OnEpisodeBegin(AgentContext context) {
            OnEpisodeStart(context);
        }
        
        public virtual void OnEpisodeEnd(AgentContext context, EpisodeEndReason reason) {
            OnEpisodeComplete(context, reason);
        }
        
        public virtual void OnEpisodeUpdate(AgentContext context, float deltaTime) {
            if (!isActive) return;
            OnUpdate(context, deltaTime);
        }
        
        public virtual void Reset() {
            OnReset();
        }
        
        public virtual string GetDebugInfo() {
            return $"{HandlerName}: Active={IsActive}, Priority={Priority}";
        }
        
        // Protected virtual methods for subclasses to override
        protected virtual void OnInitialize(AgentContext context) { }
        protected virtual void OnEpisodeStart(AgentContext context) { }
        protected virtual void OnEpisodeComplete(AgentContext context, EpisodeEndReason reason) { }
        protected virtual void OnUpdate(AgentContext context, float deltaTime) { }
        protected virtual void OnReset() { }
        
        // Utility methods
        public void SetActive(bool active) {
            isActive = active;
        }
        
        public void SetPriority(int newPriority) {
            priority = newPriority;
        }
        
        public void SetHandlerName(string name) {
            handlerName = name;
        }
    }
}