using System.Collections.Generic;
using UnityEngine;
using TCS.MLAgents.Core;
using TCS.MLAgents.Utilities;
using TCS.MLAgents.Interfaces;

namespace TCS.MLAgents.Tests {
    /// <summary>
    /// Testing framework for the statistics and monitoring system
    /// </summary>
    public class StatisticsSystemTests {
        
        /// <summary>
        /// Test the StatisticsCollector functionality
        /// </summary>
        public static bool TestStatisticsCollector() {
            Debug.Log("Testing StatisticsCollector...");
            
            try {
                // Create a mock agent context
                var testGameObject = new GameObject("TestAgent");
                var agentContext = new AgentContext(testGameObject);
                
                // Create statistics collector
                var collector = new StatisticsCollector();
                collector.Initialize(agentContext);
                
                // Create mock statistics provider
                var mockProvider = new StatsTestProvider("mock", 50);
                collector.AddStatisticsProvider(mockProvider);
                
                // Test provider management
                var providers = collector.StatisticsProviders;
                if (providers.Count != 1) {
                    Debug.LogError($"Expected 1 provider, got {providers.Count}");
                    Object.Destroy(testGameObject);
                    return false;
                }
                
                // Test statistics collection
                collector.CollectStatistics(0.016f); // ~60 FPS
                
                // Test statistics retrieval
                var currentStats = collector.CurrentStatistics;
                var changedStats = collector.GetChangedStatistics();
                
                // Test episode lifecycle
                collector.OnEpisodeBegin();
                collector.OnEpisodeEnd();
                
                Debug.Log("StatisticsCollector test passed");
                Object.Destroy(testGameObject);
                return true;
            }
            catch (System.Exception ex) {
                Debug.LogError($"StatisticsCollector test failed: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Test the PerformanceMonitor functionality
        /// </summary>
        public static bool TestPerformanceMonitor() {
            Debug.Log("Testing PerformanceMonitor...");
            
            try {
                // Create test objects
                var testGameObject = new GameObject("TestAgent");
                var agentContext = new AgentContext(testGameObject);
                var monitor = testGameObject.AddComponent<PerformanceMonitor>();
                
                // Initialize
                monitor.Initialize(agentContext);
                
                // Test properties
                if (string.IsNullOrEmpty(monitor.Id)) {
                    Debug.LogError("Monitor ID should not be null or empty");
                    Object.Destroy(testGameObject);
                    return false;
                }
                
                if (monitor.Priority < 0) {
                    Debug.LogError("Monitor priority should be non-negative");
                    Object.Destroy(testGameObject);
                    return false;
                }
                
                // Test activation
                monitor.SetActive(true);
                if (!monitor.IsActive) {
                    Debug.LogError("Monitor should be active");
                    Object.Destroy(testGameObject);
                    return false;
                }
                
                // Test statistics retrieval
                var stats = monitor.GetStatistics();
                var changedStats = monitor.GetChangedStatistics();
                
                // Test episode lifecycle
                monitor.OnEpisodeBegin(agentContext);
                monitor.OnEpisodeEnd(agentContext);
                
                Debug.Log("PerformanceMonitor test passed");
                Object.Destroy(testGameObject);
                return true;
            }
            catch (System.Exception ex) {
                Debug.LogError($"PerformanceMonitor test failed: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Test the StatisticsVisualizer functionality
        /// </summary>
        public static bool TestStatisticsVisualizer() {
            Debug.Log("Testing StatisticsVisualizer...");
            
            try {
                // Create test objects
                var testGameObject = new GameObject("TestVisualizer");
                var visualizer = testGameObject.AddComponent<StatisticsVisualizer>();
                
                // Test that visualizer was created
                if (visualizer == null) {
                    Debug.LogError("Failed to create StatisticsVisualizer");
                    Object.Destroy(testGameObject);
                    return false;
                }
                
                // Test configuration methods
                visualizer.SetUpdateInterval(1.0f);
                visualizer.SetMaxDisplayItems(10);
                visualizer.SetShowOnlyChanged(true);
                
                Debug.Log("StatisticsVisualizer test passed");
                Object.Destroy(testGameObject);
                return true;
            }
            catch (System.Exception ex) {
                Debug.LogError($"StatisticsVisualizer test failed: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Run all statistics system tests
        /// </summary>
        public static bool RunAllTests() {
            Debug.Log("Running Statistics System Tests...");
            
            bool allPassed = true;
            
            allPassed &= TestPerformanceMonitor();
            allPassed &= TestStatisticsCollector();
            allPassed &= TestStatisticsVisualizer();
            
            if (allPassed) {
                Debug.Log("All Statistics System Tests Passed!");
            } else {
                Debug.LogError("Some Statistics System Tests Failed!");
            }
            
            return allPassed;
        }
    }
    
    /// <summary>
    /// Mock statistics provider for testing
    /// </summary>
    public class StatsTestProvider : IStatisticsProvider {
        private string m_Id;
        private int m_Priority;
        private bool m_IsActive;
        private Dictionary<string, float> m_Statistics;
        
        public StatsTestProvider(string id, int priority) {
            m_Id = id;
            m_Priority = priority;
            m_IsActive = true;
            m_Statistics = new Dictionary<string, float>();
            
            // Add some mock statistics
            m_Statistics["test.metric1"] = 1.0f;
            m_Statistics["test.metric2"] = 2.0f;
        }
        
        public string Id => m_Id;
        public int Priority => m_Priority;
        public bool IsActive => m_IsActive;
        
        public void Initialize(AgentContext context) { }
        
        public void CollectStatistics(AgentContext context, float deltaTime) {
            // Update mock statistics
            m_Statistics["test.metric1"] += deltaTime;
            m_Statistics["test.metric2"] += deltaTime * 2;
        }
        
        public void OnEpisodeBegin(AgentContext context) {
            // Reset mock statistics
            m_Statistics["test.metric1"] = 0f;
            m_Statistics["test.metric2"] = 0f;
        }
        
        public void OnEpisodeEnd(AgentContext context) { }
        
        public Dictionary<string, float> GetStatistics() {
            return new Dictionary<string, float>(m_Statistics);
        }
        
        public Dictionary<string, float> GetChangedStatistics() {
            return new Dictionary<string, float>(m_Statistics);
        }
        
        public void ResetStatistics() {
            m_Statistics.Clear();
        }
        
        public void SetActive(bool active) {
            m_IsActive = active;
        }
        
        public string GetDebugInfo() {
            return $"StatsTestProvider[{m_Id}] - Active: {m_IsActive}";
        }
    }
}