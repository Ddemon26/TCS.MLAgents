using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

namespace TCS.MLAgents.Core.Interfaces {
    /// <summary>
    /// Core interface defining the lifecycle and capabilities of an ML Agent.
    /// This interface bridges Unity ML-Agents with the composition system.
    /// </summary>
    public interface IMLAgent {
        /// <summary>
        /// Called once when the agent is first created.
        /// Use this to initialize components and set up dependencies.
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Called at the start of each training episode.
        /// Use this to reset agent state and environment.
        /// </summary>
        void OnEpisodeBegin();
        
        /// <summary>
        /// Called every step to collect observations for the neural network.
        /// Components should add their observations to the sensor.
        /// </summary>
        /// <param name="sensor">Vector sensor to add observations to</param>
        void CollectObservations(VectorSensor sensor);
        
        /// <summary>
        /// Called when the neural network provides actions to execute.
        /// Components should read and execute their assigned actions.
        /// </summary>
        /// <param name="actionBuffers">Actions from the neural network</param>
        void OnActionReceived(ActionBuffers actionBuffers);
        
        /// <summary>
        /// Called when in heuristic mode to provide manual/scripted actions.
        /// Use this for testing and debugging agent behavior.
        /// </summary>
        /// <param name="actionsOut">Buffer to write heuristic actions to</param>
        void Heuristic(in ActionBuffers actionsOut);
        
        /// <summary>
        /// Called every fixed update step for physics and continuous logic.
        /// </summary>
        void FixedUpdate();
        
        /// <summary>
        /// Called when the agent is being destroyed.
        /// Use this to clean up resources and unregister callbacks.
        /// </summary>
        void OnDestroy();
        
        /// <summary>
        /// Gets the shared context that components can use to communicate.
        /// </summary>
        AgentContext Context { get; }
        
        /// <summary>
        /// Ends the current episode and triggers OnEpisodeBegin.
        /// </summary>
        void EndEpisode();
        
        /// <summary>
        /// Adds a reward to the agent's cumulative reward.
        /// </summary>
        /// <param name="reward">Reward value to add</param>
        void AddReward(float reward);
        
        /// <summary>
        /// Sets the agent's reward, replacing the current cumulative reward.
        /// </summary>
        /// <param name="reward">New reward value</param>
        void SetReward(float reward);
    }
}