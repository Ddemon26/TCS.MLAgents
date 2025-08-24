using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using TCS.MLAgents.Core;

namespace TCS.MLAgents.Interfaces {
    /// <summary>
    /// Interface for components that provide decision logic for ML agents.
    /// Can be used for heuristic control, manual control, or other decision-making systems.
    /// </summary>
    public interface IDecisionProvider {
        /// <summary>
        /// Unique identifier for this decision provider
        /// </summary>
        string Id { get; }
        
        /// <summary>
        /// Priority level for this decision provider (higher values take precedence)
        /// </summary>
        int Priority { get; }
        
        /// <summary>
        /// Whether this decision provider is currently active
        /// </summary>
        bool IsActive { get; }
        
        /// <summary>
        /// Initialize the decision provider with the agent context
        /// </summary>
        void Initialize(AgentContext context);
        
        /// <summary>
        /// Determine if this decision provider should handle the current decision
        /// </summary>
        bool ShouldDecide(AgentContext context, List<ISensor> sensors);
        
        /// <summary>
        /// Make a decision based on the current observations
        /// </summary>
        void DecideAction(AgentContext context, List<ISensor> sensors, ActionBuffers actions);
        
        /// <summary>
        /// Called when an episode begins
        /// </summary>
        void OnEpisodeBegin(AgentContext context);
        
        /// <summary>
        /// Called when an episode ends
        /// </summary>
        void OnEpisodeEnd(AgentContext context);
        
        /// <summary>
        /// Called each step to update the decision provider's internal state
        /// </summary>
        void OnUpdate(AgentContext context, float deltaTime);
        
        /// <summary>
        /// Enable or disable this decision provider
        /// </summary>
        void SetActive(bool active);
        
        /// <summary>
        /// Get debug information about this decision provider
        /// </summary>
        string GetDebugInfo();
    }
}