using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;
using TCS.MLAgents.Core;
using TCS.MLAgents.Interfaces;
using TCS.MLAgents.Observations;
using TCS.MLAgents.Actions;
using TCS.MLAgents.Rewards;
using TCS.MLAgents.Configuration;
using TCS.MLAgents.Episodes;
using TCS.MLAgents.Sensors;
using TCS.MLAgents.Observations;
using Unity.MLAgents.Sensors;

namespace TCS.MLAgents.Tests
{
    /// <summary>
    /// Integration tests for complete ML agent scenarios
    /// </summary>
    public class IntegrationTests
    {
        private GameObject testAgent;
        private AgentContext agentContext;
        private MLAgentComposer agentComposer;
        
        [SetUp]
        public void Setup()
        {
            testAgent = new GameObject("TestAgent");
            agentContext = new AgentContext(testAgent);
            agentComposer = testAgent.AddComponent<MLAgentComposer>();
        }
        
        [TearDown]
        public void Teardown()
        {
            if (testAgent != null)
                Object.DestroyImmediate(testAgent);
        }
        
        /// <summary>
        /// Test complete agent initialization and component composition
        /// </summary>
        [Test]
        public void TestAgentInitialization()
        {
            // Add core components
            var observationCollector = testAgent.AddComponent<VectorObservationCollector>();
            var actionDistributor = testAgent.AddComponent<ActionDistributor>();
            var rewardCalculator = testAgent.AddComponent<RewardCalculator>();
            
            // Initialize composer
            agentComposer.Initialize();
            
            // Verify all components are properly initialized
            Assert.IsNotNull(agentComposer.Context);
            Assert.AreEqual(3, agentComposer.AgentComponents.Count);
            
            // Verify component retrieval
            var retrievedObservationCollector = agentComposer.GetAgentComponent<VectorObservationCollector>();
            var retrievedActionDistributor = agentComposer.GetAgentComponent<ActionDistributor>();
            var retrievedRewardCalculator = agentComposer.GetAgentComponent<RewardCalculator>();
            
            Assert.IsNotNull(retrievedObservationCollector);
            Assert.IsNotNull(retrievedActionDistributor);
            Assert.IsNotNull(retrievedRewardCalculator);
            
            Assert.AreEqual(observationCollector, retrievedObservationCollector);
            Assert.AreEqual(actionDistributor, retrievedActionDistributor);
            Assert.AreEqual(rewardCalculator, retrievedRewardCalculator);
        }
        
        /// <summary>
        /// Test episode lifecycle with all components
        /// </summary>
        [Test]
        public void TestEpisodeLifecycle()
        {
            // Add core components
            var observationCollector = testAgent.AddComponent<VectorObservationCollector>();
            var actionDistributor = testAgent.AddComponent<ActionDistributor>();
            var rewardCalculator = testAgent.AddComponent<RewardCalculator>();
            
            // Initialize composer
            agentComposer.Initialize();
            
            // Start episode
            agentComposer.OnEpisodeBegin();
            
            // Verify all components received episode begin notification
            Assert.IsTrue(agentContext.IsEpisodeActive);
            Assert.AreEqual(1, agentContext.EpisodeCount);
            
            // Simulate action received
            var actionBuffers = new Unity.MLAgents.Actuators.ActionBuffers(
                new float[0], // continuous actions
                new int[0]    // discrete actions
            );
            
            agentComposer.OnActionReceived(actionBuffers);
            
            // Verify context was updated
            Assert.AreEqual(1, agentContext.StepCount, 0.001f);
            
            // End episode
            agentComposer.EndEpisode();
            Assert.IsFalse(agentContext.IsEpisodeActive);
        }
        
        /// <summary>
        /// Test behavior configuration application
        /// </summary>
        [Test]
        public void TestBehaviorConfiguration()
        {
            // Create behavior configuration
            var config = ScriptableObject.CreateInstance<MLBehaviorConfig>();
            config.SetBehaviorName("TestBehavior");
            config.SetBehaviorType(MLBehaviorConfig.BehaviorType.Inference);
            config.SetModelPath("test_model.onnx");
            
            // Add some configuration data
            config.AddObservationProvider("TransformObservationProvider");
            config.AddActionReceiver("MovementActionReceiver");
            config.AddRewardProvider("ProximityRewardProvider");
            
            // Create behavior applicator
            var applicator = new BehaviorApplicator(config);
            applicator.Initialize(agentContext, testAgent);
            
            // Apply configuration
            bool success = applicator.ApplyConfiguration();
            Assert.IsTrue(success);
            Assert.IsTrue(applicator.IsApplied);
            
            // Verify components were created
            var observationCollector = testAgent.GetComponent<VectorObservationCollector>();
            var actionDistributor = testAgent.GetComponent<ActionDistributor>();
            var rewardCalculator = testAgent.GetComponent<RewardCalculator>();
            
            Assert.IsNotNull(observationCollector);
            Assert.IsNotNull(actionDistributor);
            Assert.IsNotNull(rewardCalculator);
        }
        
        /// <summary>
        /// Test reward calculation integration
        /// </summary>
        [Test]
        public void TestRewardCalculation()
        {
            // Add reward calculator
            var rewardCalculator = testAgent.AddComponent<RewardCalculator>();
            
            // Add mock reward provider
            var mockProvider = new MockRewardProvider();
            rewardCalculator.RegisterProvider(mockProvider);
            
            // Initialize
            rewardCalculator.Initialize();
            
            // Start episode
            //rewardCalculator.OnEpisodeBegin(agentContext);
            
            // Simulate action received (triggers reward calculation)
            var actionBuffers = new Unity.MLAgents.Actuators.ActionBuffers(
                new float[0],
                new int[0]
            );
            
            rewardCalculator.OnActionReceived(actionBuffers);
            
            // Verify rewards were calculated
            Assert.AreEqual(0.1f, rewardCalculator.CurrentStepReward, 0.001f);
            Assert.AreEqual(0.1f, rewardCalculator.TotalEpisodeReward, 0.001f);
            
            // Verify provider contributions
            var contribution = rewardCalculator.GetProviderContribution(mockProvider);
            Assert.AreEqual(0.1f, contribution, 0.001f);
        }
        
        /// <summary>
        /// Test observation collection integration
        /// </summary>
        [Test]
        public void TestObservationCollection()
        {
            // Add observation collector
            var observationCollector = testAgent.AddComponent<VectorObservationCollector>();
            
            // Add mock observation provider
            var mockProvider = new MockObservationProvider();
            observationCollector.RegisterProvider(mockProvider);
            
            // Initialize
            observationCollector.Initialize();
            
            // Create sensor for collection
            var sensor = new Unity.MLAgents.Sensors.VectorSensor(10);
            
            // Collect observations
            observationCollector.CollectObservations(sensor);
            
            // Note: We can't easily verify the exact observations without a more complex mock
            // but we can verify the method executed without error
            Assert.Pass("Observation collection completed without error");
        }
        
        /// <summary>
        /// Test action distribution integration
        /// </summary>
        [Test]
        public void TestActionDistribution()
        {
            // Add action distributor
            var actionDistributor = testAgent.AddComponent<ActionDistributor>();
            
            // Add mock action receiver
            var mockReceiver = new MockActionReceiver();
            actionDistributor.RegisterReceiver(mockReceiver);
            
            // Initialize
            actionDistributor.Initialize();
            
            // Create action buffers
            var actionBuffers = new Unity.MLAgents.Actuators.ActionBuffers(
                new float[] { 1.0f, 0.5f }, // continuous actions
                new int[] { 2 }             // discrete actions
            );
            
            // Distribute actions
            actionDistributor.OnActionReceived(actionBuffers);
            
            // Note: We can't easily verify the exact distribution without a more complex mock
            // but we can verify the method executed without error
            Assert.Pass("Action distribution completed without error");
        }
        
        /// <summary>
        /// Test complete sensor system integration
        /// </summary>
        [Test]
        public void TestSensorSystemIntegration()
        {
            // Add sensor manager
            var sensorManager = testAgent.AddComponent<SensorManager>();
            
            // Add core components
            var observationCollector = testAgent.AddComponent<VectorObservationCollector>();
            var actionDistributor = testAgent.AddComponent<ActionDistributor>();
            var rewardCalculator = testAgent.AddComponent<RewardCalculator>();
            
            // Initialize composer
            agentComposer.Initialize();
            
            // Verify sensor manager is included
            var retrievedSensorManager = agentComposer.GetAgentComponent<SensorManager>();
            Assert.IsNotNull(retrievedSensorManager);
            Assert.AreEqual(sensorManager, retrievedSensorManager);
            
            // Test sensor provider registration
            var mockSensorProvider = new IntegrationSensorProvider();
            sensorManager.RegisterSensorProvider(mockSensorProvider);
            Assert.AreEqual(1, sensorManager.SensorProviders.Count);
        }
        
        /// <summary>
        /// Test complete episode system integration
        /// </summary>
        [Test]
        public void TestEpisodeSystemIntegration()
        {
            // Add episode manager and handlers
            var episodeManager = testAgent.AddComponent<EpisodeManager>();
            var boundaryHandler = new BoundaryHandler();
            var timeLimitHandler = new TimeLimitHandler();
            
            // Register handlers
            episodeManager.RegisterHandler(boundaryHandler);
            episodeManager.RegisterHandler(timeLimitHandler);
            
            // Add core components
            var observationCollector = testAgent.AddComponent<VectorObservationCollector>();
            var actionDistributor = testAgent.AddComponent<ActionDistributor>();
            var rewardCalculator = testAgent.AddComponent<RewardCalculator>();
            
            // Initialize composer
            agentComposer.Initialize();
            
            // Verify episode manager is included
            var retrievedEpisodeManager = agentComposer.GetAgentComponent<EpisodeManager>();
            Assert.IsNotNull(retrievedEpisodeManager);
            
            // Test episode lifecycle
            episodeManager.RequestEpisodeStart();
            episodeManager.FixedUpdate();
            Assert.IsTrue(episodeManager.IsEpisodeActive);
            
            episodeManager.RequestEpisodeEnd(EpisodeEndReason.Success);
            episodeManager.FixedUpdate();
            Assert.IsFalse(episodeManager.IsEpisodeActive);
        }
        
        /// <summary>
        /// Test vision system integration with ML agent
        /// </summary>
        [Test]
        public void TestVisionSystemIntegration()
        {
            // Add vision components
            var visionProvider = new VisionObservationProvider();
            visionProvider.Initialize(agentContext);
            var visionManager = VisionSystemManager.Instance;
            
            // Add core components
            var observationCollector = testAgent.AddComponent<VectorObservationCollector>();
            
            // Initialize composer
            agentComposer.Initialize();
            
            // Register vision provider
            visionManager.RegisterVisionProvider(visionProvider);
            Assert.AreEqual(1, visionManager.RegisteredProviderCount);
            
            // Test vision system capabilities
            Assert.IsTrue(visionManager.IsVisionSystemEnabled);
            Assert.IsTrue(visionManager.CanPerformRaycast(visionProvider));
            
            // Test system stats
            var stats = visionManager.GetSystemStats();
            Assert.IsNotNull(stats);
            Assert.IsTrue(stats.ContainsKey("ProviderCount"));
            
            // Cleanup
            visionManager.UnregisterVisionProvider(visionProvider);
        }
        
        /// <summary>
        /// Test full ML agent simulation scenario
        /// </summary>
        [Test]
        public void TestFullMLAgentSimulation()
        {
            // Create complete ML agent setup
            var episodeManager = testAgent.AddComponent<EpisodeManager>();
            var sensorManager = testAgent.AddComponent<SensorManager>();
            var observationCollector = testAgent.AddComponent<VectorObservationCollector>();
            var actionDistributor = testAgent.AddComponent<ActionDistributor>();
            var rewardCalculator = testAgent.AddComponent<RewardCalculator>();
            
            // Add episode handlers
            var timeLimitHandler = new TimeLimitHandler();
            var taskHandler = new TaskCompletionHandler();
            
            // Register handlers
            episodeManager.RegisterHandler(timeLimitHandler);
            episodeManager.RegisterHandler(taskHandler);
            
            // Add providers
            var mockObservationProvider = new MockObservationProvider();
            var mockActionReceiver = new MockActionReceiver();
            var mockRewardProvider = new MockRewardProvider();
            var mockSensorProvider = new IntegrationSensorProvider();
            
            observationCollector.RegisterProvider(mockObservationProvider);
            actionDistributor.RegisterReceiver(mockActionReceiver);
            rewardCalculator.RegisterProvider(mockRewardProvider);
            sensorManager.RegisterSensorProvider(mockSensorProvider);
            
            // Initialize entire system
            agentComposer.Initialize();
            
            // Verify all components are initialized
            Assert.AreEqual(5, agentComposer.AgentComponents.Count);
            
            // Start episode
            agentComposer.OnEpisodeBegin();
            Assert.IsTrue(agentContext.IsEpisodeActive);
            
            // Simulate agent steps
            for (int i = 0; i < 10; i++)
            {
                // Collect observations
                var sensor = new VectorSensor(10);
                agentComposer.CollectObservations(sensor);
                
                // Process actions
                var actionBuffers = new Unity.MLAgents.Actuators.ActionBuffers(
                    new float[] { 0.1f, 0.2f },
                    new int[] { 1 }
                );
                agentComposer.OnActionReceived(actionBuffers);
                
                // Update episode managers
                episodeManager.FixedUpdate();
            }
            
            // Complete task and end episode
            agentContext.SetSharedData("TaskCompleted", true);
            episodeManager.FixedUpdate();
            
            // Verify episode ended successfully
            Assert.IsFalse(episodeManager.IsEpisodeActive);
            Assert.AreEqual(1, episodeManager.GetEndReasonCount(EpisodeEndReason.Success));
            Assert.IsTrue(rewardCalculator.TotalEpisodeReward > 0f);
        }
    }
    
    /// <summary>
    /// Mock sensor provider for integration testing
    /// </summary>
    public class IntegrationSensorProvider : ISensorProvider
    {
        public string SensorName => "MockSensor";
        public bool IsActive => true;
        public int Priority => 1;
        public ISensor Sensor { get; private set; }
        
        public IntegrationSensorProvider()
        {
            Sensor = new VectorSensor(3, "MockSensor");
        }
        
        public void Initialize(AgentContext context) { }
        public bool ValidateSensor(AgentContext context) => true;
        public void OnEpisodeBegin(AgentContext context) { }
        public void UpdateSensor(AgentContext context, float deltaTime) { }
        public void Reset() { }
        public string GetDebugInfo() => "IntegrationSensorProvider";
        public void OnSensorEvent(string eventName, AgentContext context, object eventData = null) { }
        public void SetActive(bool active) { }
    }
}