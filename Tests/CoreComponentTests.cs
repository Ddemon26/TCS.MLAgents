using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;
using TCS.MLAgents.Core;
using TCS.MLAgents.Interfaces;
using TCS.MLAgents.Observations;
using TCS.MLAgents.Actions;
using TCS.MLAgents.Rewards;
using TCS.MLAgents.Episodes;
using TCS.MLAgents.Sensors;
using TCS.MLAgents.Decision;
using TCS.MLAgents.Utilities;
using TCS.MLAgents.Configuration;
using TCS.MLAgents.Validation;
using Unity.MLAgents.Sensors;

namespace TCS.MLAgents.Tests
{
    /// <summary>
    /// Unit tests for all core ML-Agents components
    /// </summary>
    public class CoreComponentTests
    {
        private GameObject testGameObject;
        private AgentContext testContext;
        
        [SetUp]
        public void Setup()
        {
            testGameObject = new GameObject("TestAgent");
            testContext = new AgentContext(testGameObject);
        }
        
        [TearDown]
        public void Teardown()
        {
            if (testGameObject != null)
                Object.DestroyImmediate(testGameObject);
        }
        
        /// <summary>
        /// Test AgentContext functionality
        /// </summary>
        [Test]
        public void TestAgentContext()
        {
            // Test initialization
            Assert.IsNotNull(testContext);
            Assert.AreEqual(testGameObject, testContext.AgentGameObject);
            Assert.AreEqual(0, testContext.EpisodeCount);
            Assert.AreEqual(0f, testContext.StepCount);
            
            // Test episode management
            testContext.StartEpisode();
            Assert.AreEqual(1, testContext.EpisodeCount);
            Assert.IsTrue(testContext.IsEpisodeActive);
            
            testContext.EndEpisode();
            Assert.IsFalse(testContext.IsEpisodeActive);
            
            // Test reward management
            testContext.AddReward(1.0f);
            Assert.AreEqual(1.0f, testContext.CumulativeReward);
            
            testContext.SetReward(2.0f);
            Assert.AreEqual(2.0f, testContext.CumulativeReward);
            
            // Test component caching
            var transform = testContext.GetComponent<Transform>();
            Assert.IsNotNull(transform);
            
            var cachedTransform = testContext.GetComponent<Transform>();
            Assert.AreEqual(transform, cachedTransform);
            
            // Test shared data
            testContext.SetSharedData("testKey", "testValue");
            Assert.IsTrue(testContext.HasSharedData("testKey"));
            
            var value = testContext.GetSharedData<string>("testKey");
            Assert.AreEqual("testValue", value);
            
            testContext.RemoveSharedData("testKey");
            Assert.IsFalse(testContext.HasSharedData("testKey"));
        }
        
        /// <summary>
        /// Test MLAgentComposer functionality
        /// </summary>
        [Test]
        public void TestMLAgentComposer()
        {
            var composer = testGameObject.AddComponent<MLAgentComposer>();
            Assert.IsNotNull(composer);
            
            // Test initialization
            composer.Initialize();
            Assert.IsNotNull(composer.Context);
            Assert.AreEqual(0, composer.AgentComponents.Count);
            
            // Test component registration
            var mockComponent = new MockMLAgent();
            composer.RegisterComponent(mockComponent);
            Assert.AreEqual(1, composer.AgentComponents.Count);
            
            // Test component retrieval
            var retrievedComponent = composer.GetAgentComponent<MockMLAgent>();
            Assert.AreEqual(mockComponent, retrievedComponent);
        }
        
        /// <summary>
        /// Test VectorObservationCollector functionality
        /// </summary>
        [Test]
        public void TestVectorObservationCollector()
        {
            var collector = testGameObject.AddComponent<VectorObservationCollector>();
            Assert.IsNotNull(collector);
            
            collector.Initialize();
            Assert.IsNotNull(collector.Context);
            
            // Test provider management
            var mockProvider = new MockObservationProvider();
            collector.RegisterProvider(mockProvider);
            Assert.AreEqual(1, collector.ObservationProviders.Count);
            
            collector.UnregisterProvider(mockProvider);
            Assert.AreEqual(0, collector.ObservationProviders.Count);
        }
        
        /// <summary>
        /// Test ActionDistributor functionality
        /// </summary>
        [Test]
        public void TestActionDistributor()
        {
            var distributor = testGameObject.AddComponent<ActionDistributor>();
            Assert.IsNotNull(distributor);
            
            distributor.Initialize();
            Assert.IsNotNull(distributor.Context);
            
            // Test receiver management
            var mockReceiver = new MockActionReceiver();
            distributor.RegisterReceiver(mockReceiver);
            Assert.AreEqual(1, distributor.ActionReceivers.Count);
            
            distributor.UnregisterReceiver(mockReceiver);
            Assert.AreEqual(0, distributor.ActionReceivers.Count);
        }
        
        /// <summary>
        /// Test RewardCalculator functionality
        /// </summary>
        [Test]
        public void TestRewardCalculator()
        {
            var calculator = testGameObject.AddComponent<RewardCalculator>();
            Assert.IsNotNull(calculator);
            
            calculator.Initialize();
            Assert.IsNotNull(calculator.Context);
            
            // Test provider management
            var mockProvider = new MockRewardProvider();
            calculator.RegisterProvider(mockProvider);
            Assert.AreEqual(1, calculator.RewardProviders.Count);
            
            calculator.UnregisterProvider(mockProvider);
            Assert.AreEqual(0, calculator.RewardProviders.Count);
        }
        
        /// <summary>
        /// Test configuration validation
        /// </summary>
        [Test]
        public void TestConfigurationValidation()
        {
            var config = ScriptableObject.CreateInstance<MLBehaviorConfig>();
            config.SetBehaviorName("TestBehavior");
            config.SetBehaviorType(MLBehaviorConfig.BehaviorType.Inference);
            config.SetModelPath("test_model.onnx");
            
            // Valid configuration
            var result = ConfigurationValidator.ValidateConfiguration(config);
            Assert.IsTrue(result.IsValid);
            
            // Invalid configuration - empty behavior name
            config.SetBehaviorName("");
            result = ConfigurationValidator.ValidateConfiguration(config);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Count > 0);
            
            // Invalid configuration - missing model path for inference
            config.SetBehaviorName("TestBehavior");
            config.SetModelPath("");
            result = ConfigurationValidator.ValidateConfiguration(config);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Count > 0);
        }
        
        /// <summary>
        /// Test configuration comparison
        /// </summary>
        [Test]
        public void TestConfigurationComparison()
        {
            var config1 = ScriptableObject.CreateInstance<MLBehaviorConfig>();
            config1.SetBehaviorName("TestBehavior1");
            config1.SetBehaviorType(MLBehaviorConfig.BehaviorType.Inference);
            
            var config2 = ScriptableObject.CreateInstance<MLBehaviorConfig>();
            config2.SetBehaviorName("TestBehavior2");
            config2.SetBehaviorType(MLBehaviorConfig.BehaviorType.Inference);
            
            var comparison = ConfigurationValidator.CompareConfigurations(config1, config2);
            Assert.IsTrue(comparison.HasDifferences);
            Assert.IsTrue(comparison.Differences.Count > 0);
            
            // Test identical configurations
            var comparison2 = ConfigurationValidator.CompareConfigurations(config1, config1);
            Assert.IsFalse(comparison2.HasDifferences);
        }
        
        /// <summary>
        /// Test SensorManager functionality
        /// </summary>
        [Test]
        public void TestSensorManager()
        {
            var sensorManager = testGameObject.AddComponent<SensorManager>();
            Assert.IsNotNull(sensorManager);
            
            sensorManager.Initialize();
            Assert.IsNotNull(sensorManager.Context);
            
            // Test provider management
            var mockProvider = new MockSensorProvider();
            sensorManager.RegisterSensorProvider(mockProvider);
            Assert.AreEqual(1, sensorManager.SensorProviders.Count);
            
            sensorManager.UnregisterSensorProvider(mockProvider);
            Assert.AreEqual(0, sensorManager.SensorProviders.Count);
        }
        
        /// <summary>
        /// Test VisionSystemManager functionality
        /// </summary>
        [Test]
        public void TestVisionSystemManager()
        {
            var visionManager = VisionSystemManager.Instance;
            Assert.IsNotNull(visionManager);
            
            // Test vision system state
            Assert.IsTrue(visionManager.IsVisionSystemEnabled);
            Assert.AreEqual(0, visionManager.RegisteredProviderCount);
            
            // Test LOD configuration
            visionManager.EnableLOD(true);
            visionManager.EnableSpatialPartitioning(true);
            
            // Test optimization settings
            visionManager.SetOptimizationMode(VisionSystemManager.VisionOptimizationMode.Adaptive);
            visionManager.SetMaxConcurrentRaycasts(50);
            visionManager.SetGlobalUpdateInterval(0.1f);
        }
        
        /// <summary>
        /// Test EpisodeManager functionality
        /// </summary>
        [Test]
        public void TestEpisodeManager()
        {
            var episodeManager = testGameObject.AddComponent<EpisodeManager>();
            Assert.IsNotNull(episodeManager);
            
            episodeManager.Initialize();
            Assert.IsNotNull(episodeManager.Context);
            Assert.IsFalse(episodeManager.IsEpisodeActive);
            Assert.AreEqual(0, episodeManager.TotalEpisodes);
            
            // Test episode start
            episodeManager.RequestEpisodeStart();
            
            // Test episode end
            episodeManager.RequestEpisodeEnd(EpisodeEndReason.Success);
            
            // Test statistics
            Assert.AreEqual(0, episodeManager.GetEndReasonCount(EpisodeEndReason.Success));
            Assert.AreEqual(0f, episodeManager.GetAverageEpisodeDuration());
        }
        
        /// <summary>
        /// Test PerformanceMonitor functionality
        /// </summary>
        [Test]
        public void TestPerformanceMonitor()
        {
            var monitor = testGameObject.AddComponent<PerformanceMonitor>();
            monitor.Initialize(testContext);
            
            // Test metric recording
            monitor.BeginComponentTiming("test_component");
            monitor.EndComponentTiming("test_component", 0.01f);
            
            // Test statistics collection
            var stats = monitor.GetStatistics();
            Assert.IsTrue(stats.ContainsKey("component.test_component.time"));
            
            // Test metric retrieval
            var changedStats = monitor.GetChangedStatistics();
            Assert.IsTrue(changedStats.ContainsKey("component.test_component.time"));
            
            // Test reset
            monitor.ResetStatistics();
            var resetStats = monitor.GetStatistics();
            Assert.AreEqual(0, resetStats.Count);
        }
    }
    
    /// <summary>
    /// Mock ML agent for testing
    /// </summary>
    public class MockMLAgent : IMLAgent
    {
        public AgentContext Context { get; private set; }
        
        public void Initialize() { }
        public void OnEpisodeBegin() { }
        public void CollectObservations(Unity.MLAgents.Sensors.VectorSensor sensor) { }
        public void OnActionReceived(Unity.MLAgents.Actuators.ActionBuffers actionBuffers) { }
        public void Heuristic(in Unity.MLAgents.Actuators.ActionBuffers actionsOut) { }
        public void FixedUpdate() { }
        public void OnDestroy() { }
        public void EndEpisode() { }
        public void AddReward(float reward) { }
        public void SetReward(float reward) { }
    }
    
    /// <summary>
    /// Mock observation provider for testing
    /// </summary>
    public class MockObservationProvider : IObservationProvider
    {
        public string ProviderName => "MockProvider";
        public int Priority => 1;
        public bool IsActive => true;
        public int ObservationSize => 3;
        
        public void Initialize(AgentContext context) { }
        public bool ValidateProvider(AgentContext context) => true;
        public void CollectObservations(Unity.MLAgents.Sensors.VectorSensor sensor, AgentContext context) { }
        public void OnEpisodeBegin(AgentContext context) { }
        public void OnEpisodeEnd(AgentContext context) { }
        public void OnUpdate(AgentContext context, float deltaTime) { }
        public void SetActive(bool active) { }
        public string GetDebugInfo() => "MockObservationProvider";
    }
    
    /// <summary>
    /// Mock action receiver for testing
    /// </summary>
    public class MockActionReceiver : IActionHandler
    {
        public string ReceiverName => "MockReceiver";
        public int Priority => 1;
        public bool IsActive => true;
        public int ContinuousActionCount => 2;
        public int DiscreteActionBranchCount => 1;
        public int[] DiscreteActionBranchSizes => new int[] { 3 };
        
        public void Initialize(AgentContext context) { }
        public bool ValidateReceiver(AgentContext context) => true;
        public void ReceiveContinuousActions(float[] actions, int startIndex, AgentContext context) { }
        public void ReceiveDiscreteActions(int[] actions, int startIndex, AgentContext context) { }
        public void ProvideHeuristicActions(float[] continuousOut, int[] discreteOut, int continuousStartIndex, int discreteStartIndex, AgentContext context) { }
        public void OnEpisodeBegin(AgentContext context) { }
        public void FixedUpdate(AgentContext context) { }
        public void SetActive(bool active) { }
        public string GetDebugInfo() => "MockActionReceiver";
    }
    
    /// <summary>
    /// Mock reward provider for testing
    /// </summary>
    public class MockRewardProvider : IRewardProvider
    {
        public string ProviderName => "MockProvider";
        public int Priority => 1;
        public bool IsActive => true;
        public float RewardWeight => 1.0f;
        
        public void Initialize(AgentContext context) { }
        public bool ValidateProvider(AgentContext context) => true;
        public float CalculateReward(AgentContext context, float deltaTime) => 0.1f;
        public void OnEpisodeBegin(AgentContext context) { }
        public void OnEpisodeEnd(AgentContext context) { }
        public void OnUpdate(AgentContext context, float deltaTime) { }
        public void OnRewardEvent(string eventName, AgentContext context, object eventData = null) { }
        public void SetActive(bool active) { }
        public string GetDebugInfo() => "MockRewardProvider";
    }
    
    /// <summary>
    /// Mock sensor provider for testing
    /// </summary>
    public class MockSensorProvider : ISensorProvider
    {
        public string SensorName => "MockSensor";
        public bool IsActive => true;
        public int Priority => 1;
        public ISensor Sensor { get; private set; }
        
        public MockSensorProvider()
        {
            Sensor = new VectorSensor(3, "MockSensor");
        }
        
        public void Initialize(AgentContext context) { }
        public bool ValidateSensor(AgentContext context) => true;
        public void OnEpisodeBegin(AgentContext context) { }
        public void UpdateSensor(AgentContext context, float deltaTime) { }
        public void Reset() { }
        public string GetDebugInfo() => "MockSensorProvider";
        public void OnSensorEvent(string eventName, AgentContext context, object eventData = null) { }
        public void SetActive(bool active) { }
    }
    
    /// <summary>
    /// Mock statistics provider for testing
    /// </summary>
    public class MockStatisticsProvider : IStatisticsProvider
    {
        public string Id => ProviderName;
        public string ProviderName { get; }
        public int Priority => 1;
        public bool IsActive => true;
        
        private readonly float testValue;
        private Dictionary<string, float> statistics = new Dictionary<string, float>();
        private Dictionary<string, float> changedStatistics = new Dictionary<string, float>();
        
        public MockStatisticsProvider(string name, float value)
        {
            ProviderName = name;
            testValue = value;
        }
        
        public void Initialize(AgentContext context) { }
        public void CollectStatistics(AgentContext context, float deltaTime)
        {
            statistics["test_stat"] = testValue;
            statistics["delta_time"] = deltaTime;
            changedStatistics["test_stat"] = testValue;
            changedStatistics["delta_time"] = deltaTime;
        }
        public void OnEpisodeBegin(AgentContext context) { }
        public void OnEpisodeEnd(AgentContext context) { }
        public Dictionary<string, float> GetStatistics() => statistics;
        public Dictionary<string, float> GetChangedStatistics() => changedStatistics;
        public void ResetStatistics() { statistics.Clear(); changedStatistics.Clear(); }
        public void SetActive(bool active) { }
        public string GetDebugInfo() => $"{ProviderName}: {testValue}";
    }
}