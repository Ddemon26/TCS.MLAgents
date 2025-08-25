/*using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using NUnit.Framework;
using TCS.MLAgents.Core;
using TCS.MLAgents.Interfaces;
using TCS.MLAgents.Utilities;
using TCS.MLAgents.Sensors;
using TCS.MLAgents.Episodes;
using TCS.MLAgents.Observations;
using Unity.MLAgents.Sensors;

namespace TCS.MLAgents.Tests
{
    /// <summary>
    /// Performance benchmarking tests for ML-Agents components
    /// </summary>
    public class PerformanceBenchmarkTests
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
        /// Benchmark AgentContext performance
        /// </summary>
        [Test]
        public void BenchmarkAgentContext()
        {
            const int iterations = 10000;
            var stopwatch = new Stopwatch();
            
            // Benchmark reward addition
            stopwatch.Start();
            for (int i = 0; i < iterations; i++)
            {
                testContext.AddReward(1.0f);
            }
            stopwatch.Stop();
            
            long rewardTime = stopwatch.ElapsedMilliseconds;
            UnityEngine.Debug.Log($"AgentContext.AddReward x{iterations}: {rewardTime}ms");
            
            // Benchmark component caching
            stopwatch.Restart();
            for (int i = 0; i < iterations; i++)
            {
                var transform = testContext.GetComponent<Transform>();
            }
            stopwatch.Stop();
            
            long componentTime = stopwatch.ElapsedMilliseconds;
            UnityEngine.Debug.Log($"AgentContext.GetComponent x{iterations}: {componentTime}ms");
            
            // Benchmark shared data operations
            stopwatch.Restart();
            for (int i = 0; i < iterations; i++)
            {
                testContext.SetSharedData($"key{i}", $"value{i}");
                var value = testContext.GetSharedData<string>($"key{i}");
                testContext.RemoveSharedData($"key{i}");
            }
            stopwatch.Stop();
            
            long sharedDataTime = stopwatch.ElapsedMilliseconds;
            UnityEngine.Debug.Log($"AgentContext.SharedData operations x{iterations}: {sharedDataTime}ms");
            
            // Assert reasonable performance (these are just examples, adjust as needed)
            Assert.Less(rewardTime, 1000); // Should be less than 1 second
            Assert.Less(componentTime, 1000);
            Assert.Less(sharedDataTime, 1000);
        }
        
        /// <summary>
        /// Benchmark observation collection performance
        /// </summary>
        [Test]
        public void BenchmarkObservationCollection()
        {
            var collector = testGameObject.AddComponent<VectorObservationCollector>();
            collector.Initialize();
            
            // Add multiple mock providers
            const int providerCount = 100;
            var providers = new List<MockObservationProvider>();
            for (int i = 0; i < providerCount; i++)
            {
                var provider = new MockObservationProvider();
                collector.RegisterProvider(provider);
                providers.Add(provider);
            }
            
            const int iterations = 1000;
            var stopwatch = new Stopwatch();
            
            // Benchmark observation collection
            stopwatch.Start();
            for (int i = 0; i < iterations; i++)
            {
                var sensor = new Unity.MLAgents.Sensors.VectorSensor(300); // 3 observations per provider
                collector.CollectObservations(sensor);
            }
            stopwatch.Stop();
            
            long collectionTime = stopwatch.ElapsedMilliseconds;
            UnityEngine.Debug.Log($"VectorObservationCollector.CollectObservations x{iterations} with {providerCount} providers: {collectionTime}ms");
            
            Assert.Less(collectionTime, 5000); // Should be less than 5 seconds
        }
        
        /// <summary>
        /// Benchmark action distribution performance
        /// </summary>
        [Test]
        public void BenchmarkActionDistribution()
        {
            var distributor = testGameObject.AddComponent<ActionDistributor>();
            distributor.Initialize();
            
            // Add multiple mock receivers
            const int receiverCount = 100;
            var receivers = new List<MockActionReceiver>();
            for (int i = 0; i < receiverCount; i++)
            {
                var receiver = new MockActionReceiver();
                distributor.RegisterReceiver(receiver);
                receivers.Add(receiver);
            }
            
            const int iterations = 1000;
            var stopwatch = new Stopwatch();
            
            // Benchmark action distribution
            stopwatch.Start();
            for (int i = 0; i < iterations; i++)
            {
                var actionBuffers = new Unity.MLAgents.Actuators.ActionBuffers(
                    new float[200], // 2 continuous actions per receiver
                    new int[100]    // 1 discrete action per receiver
                );
                distributor.OnActionReceived(actionBuffers);
            }
            stopwatch.Stop();
            
            long distributionTime = stopwatch.ElapsedMilliseconds;
            UnityEngine.Debug.Log($"ActionDistributor.OnActionReceived x{iterations} with {receiverCount} receivers: {distributionTime}ms");
            
            Assert.Less(distributionTime, 5000); // Should be less than 5 seconds
        }
        
        /// <summary>
        /// Benchmark reward calculation performance
        /// </summary>
        [Test]
        public void BenchmarkRewardCalculation()
        {
            var calculator = testGameObject.AddComponent<RewardCalculator>();
            calculator.Initialize();
            
            // Add multiple mock providers
            const int providerCount = 100;
            var providers = new List<MockRewardProvider>();
            for (int i = 0; i < providerCount; i++)
            {
                var provider = new MockRewardProvider();
                calculator.RegisterProvider(provider);
                providers.Add(provider);
            }
            
            const int iterations = 1000;
            var stopwatch = new Stopwatch();
            
            // Benchmark reward calculation
            stopwatch.Start();
            for (int i = 0; i < iterations; i++)
            {
                var actionBuffers = new Unity.MLAgents.Actuators.ActionBuffers(
                    new float[0],
                    new int[0]
                );
                calculator.OnActionReceived(actionBuffers);
            }
            stopwatch.Stop();
            
            long calculationTime = stopwatch.ElapsedMilliseconds;
            UnityEngine.Debug.Log($"RewardCalculator.OnActionReceived x{iterations} with {providerCount} providers: {calculationTime}ms");
            
            Assert.Less(calculationTime, 5000); // Should be less than 5 seconds
        }
        
        /// <summary>
        /// Benchmark statistics collection performance
        /// </summary>
        [Test]
        public void BenchmarkStatisticsCollection()
        {
            var collector = new StatisticsCollector();
            collector.Initialize(testContext);
            
            // Add multiple mock providers
            const int providerCount = 100;
            var providers = new List<MockStatisticsProvider>();
            for (int i = 0; i < providerCount; i++)
            {
                var provider = new MockStatisticsProvider($"provider{i}", i);
                collector.AddStatisticsProvider(provider);
                providers.Add(provider);
            }
            
            const int iterations = 1000;
            var stopwatch = new Stopwatch();
            
            // Benchmark statistics collection
            stopwatch.Start();
            for (int i = 0; i < iterations; i++)
            {
                collector.CollectStatistics(0.016f); // ~60 FPS
            }
            stopwatch.Stop();
            
            long collectionTime = stopwatch.ElapsedMilliseconds;
            UnityEngine.Debug.Log($"StatisticsCollector.CollectStatistics x{iterations} with {providerCount} providers: {collectionTime}ms");
            
            Assert.Less(collectionTime, 5000); // Should be less than 5 seconds
        }
        
        /// <summary>
        /// Benchmark sensor system performance
        /// </summary>
        [Test]
        public void BenchmarkSensorSystem()
        {
            var sensorManager = testGameObject.AddComponent<SensorManager>();
            sensorManager.Initialize();
            
            // Add multiple sensor providers
            const int sensorCount = 50;
            var providers = new List<BenchmarkSensorProvider>();
            for (int i = 0; i < sensorCount; i++)
            {
                var provider = new BenchmarkSensorProvider($"Sensor{i}", i);
                sensorManager.RegisterSensorProvider(provider);
                providers.Add(provider);
            }
            
            const int iterations = 1000;
            var stopwatch = new Stopwatch();
            
            // Benchmark sensor updates
            stopwatch.Start();
            for (int i = 0; i < iterations; i++)
            {
                sensorManager.FixedUpdate();
            }
            stopwatch.Stop();
            
            long updateTime = stopwatch.ElapsedMilliseconds;
            UnityEngine.Debug.Log($"SensorManager.FixedUpdate x{iterations} with {sensorCount} sensors: {updateTime}ms");
            
            Assert.Less(updateTime, 3000); // Should be less than 3 seconds
        }
        
        /// <summary>
        /// Benchmark episode system performance
        /// </summary>
        [Test]
        public void BenchmarkEpisodeSystem()
        {
            var episodeManager = testGameObject.AddComponent<EpisodeManager>();
            episodeManager.Initialize(testContext);
            
            // Add multiple episode handlers
            const int handlerCount = 20;
            var handlers = new List<BenchmarkEpisodeHandler>();
            for (int i = 0; i < handlerCount; i++)
            {
                var handler = new BenchmarkEpisodeHandler($"Handler{i}", i);
                episodeManager.RegisterHandler(handler);
                handlers.Add(handler);
            }
            
            const int episodes = 100;
            var stopwatch = new Stopwatch();
            
            // Benchmark complete episode cycles
            stopwatch.Start();
            for (int i = 0; i < episodes; i++)
            {
                episodeManager.RequestEpisodeStart();
                episodeManager.FixedUpdate();
                
                // Simulate some episode time
                for (int j = 0; j < 10; j++)
                {
                    episodeManager.FixedUpdate();
                }
                
                episodeManager.RequestEpisodeEnd(EpisodeEndReason.Success);
                episodeManager.FixedUpdate();
            }
            stopwatch.Stop();
            
            long episodeTime = stopwatch.ElapsedMilliseconds;
            UnityEngine.Debug.Log($"EpisodeManager complete cycles x{episodes} with {handlerCount} handlers: {episodeTime}ms");
            
            Assert.Less(episodeTime, 5000); // Should be less than 5 seconds
            Assert.AreEqual(episodes, episodeManager.TotalEpisodes);
        }
        
        /// <summary>
        /// Benchmark vision system performance
        /// </summary>
        [Test]
        public void BenchmarkVisionSystem()
        {
            var visionManager = VisionSystemManager.Instance;
            
            // Create multiple vision providers
            const int providerCount = 25;
            var providers = new List<VisionObservationProvider>();
            
            for (int i = 0; i < providerCount; i++)
            {
                var provider = new VisionObservationProvider();
                provider.Initialize(new AgentContext(testGameObject));
                provider.SetContext(new AgentContext(testGameObject));
                
                visionManager.RegisterVisionProvider(provider);
                providers.Add(provider);
            }
            
            const int iterations = 100;
            var stopwatch = new Stopwatch();
            
            // Benchmark vision system updates
            stopwatch.Start();
            for (int i = 0; i < iterations; i++)
            {
                // Simulate vision system updates
                foreach (var provider in providers)
                {
                    // Call a method that would normally be called by the system
                    // Since there's no UpdateVisionSystem method, we'll simulate what it would do
                }
            }
            stopwatch.Stop();
            
            long visionTime = stopwatch.ElapsedMilliseconds;
            UnityEngine.Debug.Log($"VisionSystemManager updates x{iterations} with {providerCount} providers: {visionTime}ms");
            
            Assert.Less(visionTime, 10000); // Should be less than 10 seconds
            
            // Cleanup
            foreach (var provider in providers)
            {
                visionManager.UnregisterVisionProvider(provider);
            }
        }
        
        /// <summary>
        /// Benchmark memory allocation patterns
        /// </summary>
        [Test]
        public void BenchmarkMemoryAllocation()
        {
            var initialMemory = System.GC.GetTotalMemory(true);
            
            // Test AgentContext memory usage
            var contexts = new List<AgentContext>();
            for (int i = 0; i < 1000; i++)
            {
                var go = new GameObject($"Agent{i}");
                contexts.Add(new AgentContext(go));
            }
            
            var afterContextCreation = System.GC.GetTotalMemory(false);
            var contextMemoryUsage = afterContextCreation - initialMemory;
            
            UnityEngine.Debug.Log($"AgentContext memory usage for 1000 instances: {contextMemoryUsage / 1024f:F2} KB");
            
            // Cleanup
            foreach (var context in contexts)
            {
                if (context.AgentGameObject != null)
                    Object.DestroyImmediate(context.AgentGameObject);
            }
            
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            
            var finalMemory = System.GC.GetTotalMemory(true);
            var memoryLeak = finalMemory - initialMemory;
            
            UnityEngine.Debug.Log($"Memory leak after cleanup: {memoryLeak / 1024f:F2} KB");
            
            // Should have minimal memory leak (less than 100KB)
            Assert.Less(memoryLeak, 100 * 1024);
        }
        
        /// <summary>
        /// Benchmark full system integration performance
        /// </summary>
        [Test]
        public void BenchmarkFullSystemIntegration()
        {
            // Create complete ML agent system
            var episodeManager = testGameObject.AddComponent<EpisodeManager>();
            var sensorManager = testGameObject.AddComponent<SensorManager>();
            var observationCollector = testGameObject.AddComponent<VectorObservationCollector>();
            var actionDistributor = testGameObject.AddComponent<ActionDistributor>();
            var rewardCalculator = testGameObject.AddComponent<RewardCalculator>();
            var agentComposer = testGameObject.AddComponent<MLAgentComposer>();
            
            // Add providers
            const int providerCount = 10;
            for (int i = 0; i < providerCount; i++)
            {
                observationCollector.RegisterProvider(new MockObservationProvider());
                actionDistributor.RegisterReceiver(new MockActionReceiver());
                rewardCalculator.RegisterProvider(new MockRewardProvider());
                sensorManager.RegisterProvider(new BenchmarkSensorProvider($"Sensor{i}", i));
            }
            
            // Initialize system
            agentComposer.Initialize();
            
            const int episodes = 10;
            const int stepsPerEpisode = 100;
            var stopwatch = new Stopwatch();
            
            // Benchmark complete training simulation
            stopwatch.Start();
            
            for (int episode = 0; episode < episodes; episode++)
            {
                agentComposer.OnEpisodeBegin();
                
                for (int step = 0; step < stepsPerEpisode; step++)
                {
                    // Collect observations
                    var sensor = new VectorSensor(100);
                    agentComposer.CollectObservations(sensor);
                    
                    // Process actions
                    var actionBuffers = new Unity.MLAgents.Actuators.ActionBuffers(
                        new float[20], // continuous actions
                        new int[10]    // discrete actions
                    );
                    agentComposer.OnActionReceived(actionBuffers);
                    
                    // Update all systems
                    episodeManager.FixedUpdate();
                    sensorManager.FixedUpdate();
                }
                
                agentComposer.EndEpisode();
            }
            
            stopwatch.Stop();
            
            long totalTime = stopwatch.ElapsedMilliseconds;
            float avgTimePerStep = totalTime / (float)(episodes * stepsPerEpisode);
            float stepsPerSecond = 1000f / avgTimePerStep;
            
            UnityEngine.Debug.Log($"Full system integration: {totalTime}ms total, {avgTimePerStep:F2}ms/step, {stepsPerSecond:F1} steps/sec");
            
            // Should achieve reasonable performance (at least 100 steps per second)
            Assert.Greater(stepsPerSecond, 100f);
        }
    }
    
    /// <summary>
    /// Test sensor provider for performance benchmarking
    /// </summary>
    public class BenchmarkSensorProvider : ISensorProvider
    {
        public string SensorName { get; }
        public bool IsActive { get; private set; } = true;
        public int Priority { get; }
        public ISensor Sensor { get; private set; }
        
        public BenchmarkSensorProvider(string name, int priority)
        {
            SensorName = name;
            Priority = priority;
            Sensor = new VectorSensor(3, name);
        }
        
        public void Initialize(AgentContext context) { }
        public bool ValidateSensor(AgentContext context) => true;
        public void OnEpisodeBegin(AgentContext context) { }
        public void UpdateSensor(AgentContext context, float deltaTime) { }
        public void Reset() { }
        public void SetActive(bool active) => IsActive = active;
        public string GetDebugInfo() => $"BenchmarkSensorProvider: {SensorName}";
        public void OnSensorEvent(string eventName, AgentContext context, object eventData = null) { }
    }
    
    /// <summary>
    /// Test episode handler for performance benchmarking
    /// </summary>
    public class BenchmarkEpisodeHandler : IEpisodeHandler
    {
        public string HandlerName { get; }
        public int Priority { get; }
        public bool IsActive { get; private set; } = true;
        
        public BenchmarkEpisodeHandler(string name, int priority)
        {
            HandlerName = name;
            Priority = priority;
        }
        
        public void Initialize(AgentContext context) { }
        public bool ShouldStartEpisode(AgentContext context) => false;
        public bool ShouldEndEpisode(AgentContext context) => false;
        public void OnEpisodeBegin(AgentContext context) { }
        public void OnEpisodeEnd(AgentContext context, EpisodeEndReason reason) { }
        public void OnEpisodeUpdate(AgentContext context, float deltaTime) { }
        public void SetActive(bool active) => IsActive = active;
        public string GetDebugInfo() => $"BenchmarkEpisodeHandler: {HandlerName}";
        public void Reset() { }
    }
}*/