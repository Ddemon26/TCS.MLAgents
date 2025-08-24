using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using TCS.MLAgents.Core;
using TCS.MLAgents.Interfaces;
using TCS.MLAgents.Decision;

namespace TCS.MLAgents.Tests {
    /// <summary>
    /// Testing framework for the decision system components
    /// </summary>
    public class DecisionSystemTests {
        
        /// <summary>
        /// Test the DecisionRouter functionality
        /// </summary>
        public static bool TestDecisionRouter() {
            Debug.Log("Testing DecisionRouter...");
            
            try {
                // Create a mock agent context
                var testGameObject = new GameObject("TestAgent");
                var agentContext = new AgentContext(testGameObject);
                
                // Create decision router
                var router = new DecisionRouter();
                router.Initialize(agentContext);
                
                // Create mock decision providers
                var heuristicController = testGameObject.AddComponent<HeuristicController>();
                var mockProvider = new MockDecisionProvider("mock", 50);
                
                // Add providers to router
                router.AddDecisionProvider(heuristicController);
                router.AddDecisionProvider(mockProvider);
                
                // Test provider management
                var providers = router.DecisionProviders;
                if (providers.Count != 2) {
                    Debug.LogError($"Expected 2 providers, got {providers.Count}");
                    Object.Destroy(testGameObject);
                    return false;
                }
                
                // Test active provider selection
                var sensors = new List<ISensor>();
                var actions = new ActionBuffers(new float[2], new int[1]);
                
                // Test with heuristic controller active
                heuristicController.SetActive(true);
                var activeProvider = router.SelectActiveProvider(sensors);
                if (activeProvider != heuristicController) {
                    Debug.LogError("Expected heuristic controller to be active");
                    Object.Destroy(testGameObject);
                    return false;
                }
                
                // Test decision making
                router.DecideAction(sensors, actions);
                
                Debug.Log("DecisionRouter test passed");
                Object.Destroy(testGameObject);
                return true;
            }
            catch (System.Exception ex) {
                Debug.LogError($"DecisionRouter test failed: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Test the HeuristicController functionality
        /// </summary>
        public static bool TestHeuristicController() {
            Debug.Log("Testing HeuristicController...");
            
            try {
                // Create test objects
                var testGameObject = new GameObject("TestAgent");
                var agentContext = new AgentContext(testGameObject);
                var controller = testGameObject.AddComponent<HeuristicController>();
                
                // Initialize
                controller.Initialize(agentContext);
                
                // Test properties
                if (string.IsNullOrEmpty(controller.Id)) {
                    Debug.LogError("Controller ID should not be null or empty");
                    Object.Destroy(testGameObject);
                    return false;
                }
                
                if (controller.Priority < 0) {
                    Debug.LogError("Controller priority should be non-negative");
                    Object.Destroy(testGameObject);
                    return false;
                }
                
                // Test activation
                controller.SetActive(true);
                if (!controller.IsActive) {
                    Debug.LogError("Controller should be active");
                    Object.Destroy(testGameObject);
                    return false;
                }
                
                // Test decision making
                var sensors = new List<ISensor>();
                var actions = new ActionBuffers(new float[2], new int[1]);
                
                controller.DecideAction(agentContext, sensors, actions);
                
                // Test episode lifecycle
                controller.OnEpisodeBegin(agentContext);
                controller.OnUpdate(agentContext, 0.016f); // ~60 FPS
                controller.OnEpisodeEnd(agentContext);
                
                Debug.Log("HeuristicController test passed");
                Object.Destroy(testGameObject);
                return true;
            }
            catch (System.Exception ex) {
                Debug.LogError($"HeuristicController test failed: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Run all decision system tests
        /// </summary>
        public static bool RunAllTests() {
            Debug.Log("Running Decision System Tests...");
            
            bool allPassed = true;
            
            allPassed &= TestHeuristicController();
            allPassed &= TestDecisionRouter();
            
            if (allPassed) {
                Debug.Log("All Decision System Tests Passed!");
            } else {
                Debug.LogError("Some Decision System Tests Failed!");
            }
            
            return allPassed;
        }
    }
    
    /// <summary>
    /// Mock decision provider for testing
    /// </summary>
    public class MockDecisionProvider : IDecisionProvider {
        private string m_Id;
        private int m_Priority;
        private bool m_IsActive;
        
        public MockDecisionProvider(string id, int priority) {
            m_Id = id;
            m_Priority = priority;
            m_IsActive = true;
        }
        
        public string Id => m_Id;
        public int Priority => m_Priority;
        public bool IsActive => m_IsActive;
        
        public void Initialize(AgentContext context) { }
        
        public bool ShouldDecide(AgentContext context, List<ISensor> sensors) {
            return m_IsActive;
        }
        
        public void DecideAction(AgentContext context, List<ISensor> sensors, ActionBuffers actions) { }
        
        public void OnEpisodeBegin(AgentContext context) { }
        
        public void OnEpisodeEnd(AgentContext context) { }
        
        public void OnUpdate(AgentContext context, float deltaTime) { }
        
        public void SetActive(bool active) {
            m_IsActive = active;
        }
        
        public string GetDebugInfo() {
            return $"MockDecisionProvider[{m_Id}] - Active: {m_IsActive}";
        }
    }
}