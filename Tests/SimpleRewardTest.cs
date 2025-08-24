using UnityEngine;
using TCS.MLAgents.Core;
using TCS.MLAgents.Rewards;

namespace TCS.MLAgents.Core.Tests {
    /// <summary>
    /// Simple test to verify reward providers work without Unity Test Runner
    /// </summary>
    public class SimpleRewardTest : MonoBehaviour {
        void Start() {
            TestRewardProviders();
        }
        
        private void TestRewardProviders() {
            GameObject testAgent = new GameObject("TestAgent");
            AgentContext context = new AgentContext(testAgent);
            
            // Test TimeRewardProvider
            Debug.Log("Testing TimeRewardProvider...");
            var timeProvider = new TimeRewardProvider();
            timeProvider.SetTimePenalty(-0.001f);
            timeProvider.SetRewardWeight(1f);
            timeProvider.Initialize(context);
            
            float timeReward = timeProvider.CalculateReward(context, 1f);
            Debug.Log($"Time reward: {timeReward} (expected: -0.001)");
            
            // Test TaskCompletionRewardProvider  
            Debug.Log("Testing TaskCompletionRewardProvider...");
            var taskProvider = new TaskCompletionRewardProvider();
            taskProvider.SetTaskType(TaskCompletionRewardProvider.TaskType.CollectItems);
            taskProvider.SetCompletionReward(10f);
            taskProvider.SetRewardWeight(1f);
            taskProvider.Initialize(context);
            
            taskProvider.OnRewardEvent("ItemCollected", context);
            float taskReward = taskProvider.CalculateReward(context, 0.1f);
            Debug.Log($"Task completion reward: {taskReward} (expected: > 0)");
            
            // Test BoundaryRewardProvider
            Debug.Log("Testing BoundaryRewardProvider...");
            testAgent.transform.position = Vector3.forward * 20f; // Outside boundary
            
            var boundaryProvider = new BoundaryRewardProvider();
            boundaryProvider.SetSphereBoundary(Vector3.zero, 5f);
            boundaryProvider.SetViolationPenalty(-1f, false, false);
            boundaryProvider.SetGracePeriod(0f); // Disable grace period
            boundaryProvider.SetRewardWeight(1f);
            boundaryProvider.Initialize(context);
            
            // Check if agent is outside boundary
            bool isOutside = !boundaryProvider.IsAgentInsideBoundary(context);
            Debug.Log($"Agent is outside boundary: {isOutside}");
            Debug.Log($"Agent position: {testAgent.transform.position}");
            
            float boundaryReward = boundaryProvider.CalculateReward(context, 0.1f);
            Debug.Log($"Boundary violation reward: {boundaryReward} (expected: < 0)");
            
            Debug.Log("Simple reward test completed!");
            
            DestroyImmediate(testAgent);
        }
    }
}