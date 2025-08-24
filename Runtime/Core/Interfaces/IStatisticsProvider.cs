using System.Collections.Generic;
using TCS.MLAgents.Core;

namespace TCS.MLAgents.Interfaces {
    /// <summary>
    /// Interface for components that collect and provide statistics about agent performance.
    /// </summary>
    public interface IStatisticsProvider {
        /// <summary>
        /// Unique identifier for this statistics provider
        /// </summary>
        string Id { get; }
        
        /// <summary>
        /// Priority level for this statistics provider (higher values are processed first)
        /// </summary>
        int Priority { get; }
        
        /// <summary>
        /// Whether this statistics provider is currently active
        /// </summary>
        bool IsActive { get; }
        
        /// <summary>
        /// Initialize the statistics provider with the agent context
        /// </summary>
        void Initialize(AgentContext context);
        
        /// <summary>
        /// Collect statistics for the current step
        /// </summary>
        void CollectStatistics(AgentContext context, float deltaTime);
        
        /// <summary>
        /// Called when an episode begins
        /// </summary>
        void OnEpisodeBegin(AgentContext context);
        
        /// <summary>
        /// Called when an episode ends
        /// </summary>
        void OnEpisodeEnd(AgentContext context);
        
        /// <summary>
        /// Get the current statistics as a dictionary
        /// </summary>
        Dictionary<string, float> GetStatistics();
        
        /// <summary>
        /// Get statistics that have changed since the last call
        /// </summary>
        Dictionary<string, float> GetChangedStatistics();
        
        /// <summary>
        /// Reset all statistics
        /// </summary>
        void ResetStatistics();
        
        /// <summary>
        /// Enable or disable this statistics provider
        /// </summary>
        void SetActive(bool active);
        
        /// <summary>
        /// Get debug information about this statistics provider
        /// </summary>
        string GetDebugInfo();
    }
}