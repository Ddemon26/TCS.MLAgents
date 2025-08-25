/*using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;
using TCS.MLAgents.Core;
using TCS.MLAgents.Interfaces;
using TCS.MLAgents.Sensors;
using TCS.MLAgents.Observations;
using Unity.MLAgents.Sensors;

namespace TCS.MLAgents.Tests
{
    /// <summary>
    /// Comprehensive tests for the sensor system components
    /// </summary>
    public class SensorSystemTests
    {
        private GameObject testGameObject;
        private AgentContext testContext;
        private SensorManager sensorManager;
        
        [SetUp]
        public void Setup()
        {
            testGameObject = new GameObject("TestAgent");
            testContext = new AgentContext(testGameObject);
            sensorManager = testGameObject.AddComponent<SensorManager>();
        }
        
        [TearDown]
        public void Teardown()
        {
            if (testGameObject != null)
                Object.DestroyImmediate(testGameObject);
        }
        
        /// <summary>
        /// Test SensorManager initialization and basic functionality
        /// </summary>
        [Test]
        public void TestSensorManagerInitialization()
        {
            sensorManager.Initialize();
            
            Assert.IsNotNull(sensorManager.Context);
            Assert.AreEqual(0, sensorManager.SensorProviders.Count);
            Assert.IsTrue(sensorManager.IsSensorSystemEnabled);
        }
        
        /// <summary>
        /// Test sensor provider registration and management
        /// </summary>
        [Test]
        public void SensorTestProviderRegistration()
        {
            sensorManager.Initialize();
            
            var mockProvider1 = new SensorTestProvider("TestSensor1", 10);
            var mockProvider2 = new SensorTestProvider("TestSensor2", 5);
            
            // Test registration
            sensorManager.RegisterSensorProvider(mockProvider1);
            Assert.AreEqual(1, sensorManager.SensorProviders.Count);
            
            sensorManager.RegisterSensorProvider(mockProvider2);
            Assert.AreEqual(2, sensorManager.SensorProviders.Count);
            
            // Test priority ordering
            var providers = sensorManager.SensorProviders;
            Assert.AreEqual(mockProvider1, providers[0]); // Higher priority first
            Assert.AreEqual(mockProvider2, providers[1]);
            
            // Test unregistration
            sensorManager.UnregisterSensorProvider(mockProvider1);
            Assert.AreEqual(1, sensorManager.SensorProviders.Count);
            Assert.AreEqual(mockProvider2, providers[0]);
        }
        
        /// <summary>
        /// Test sensor performance monitoring
        /// </summary>
        [Test]
        public void TestSensorPerformanceMonitoring()
        {
            sensorManager.Initialize();
            
            var provider = new SensorTestProvider("PerformanceTest", 1);
            sensorManager.RegisterSensorProvider(provider);
            
            // Simulate some sensor updates
            for (int i = 0; i < 10; i++)
            {
                sensorManager.FixedUpdate();
            }
            
            var stats = sensorManager.GetSensorPerformanceStats();
            Assert.IsNotNull(stats);
            Assert.IsTrue(stats.ContainsKey("PerformanceTest"));
        }
        
        /// <summary>
        /// Test RaycastSensorProvider functionality
        /// </summary>
        [Test]
        public void TestRaycastSensorProvider()
        {
            var raycastProvider = testGameObject.AddComponent<RaycastSensorProvider>();
            Assert.IsNotNull(raycastProvider);
            
            raycastProvider.Initialize(testContext);
            Assert.IsTrue(raycastProvider.ValidateSensor(testContext));
            Assert.IsNotNull(raycastProvider.Sensor);
            Assert.AreEqual("RaycastSensor", raycastProvider.SensorName);
            Assert.IsTrue(raycastProvider.IsActive);
        }
        
        /// <summary>
        /// Test CameraSensorProvider functionality
        /// </summary>
        [Test]
        public void TestCameraSensorProvider()
        {
            // Add camera to test object
            var camera = testGameObject.AddComponent<Camera>();
            var cameraProvider = testGameObject.AddComponent<CameraSensorProvider>();
            
            Assert.IsNotNull(cameraProvider);
            
            cameraProvider.Initialize(testContext);
            Assert.IsTrue(cameraProvider.ValidateSensor(testContext));
            Assert.AreEqual("CameraSensor", cameraProvider.SensorName);
            Assert.IsTrue(cameraProvider.IsActive);
        }
        
        /// <summary>
        /// Test VisionSystemManager integration
        /// </summary>
        [Test]
        public void TestVisionSystemManagerIntegration()
        {
            var visionManager = VisionSystemManager.Instance;
            Assert.IsNotNull(visionManager);
            
            // Test provider registration
            var visionProvider = new VisionObservationProvider();
            visionProvider.Initialize(testContext);
            visionProvider.SetContext(testContext);
            
            visionManager.RegisterVisionProvider(visionProvider);
            Assert.AreEqual(1, visionManager.RegisteredProviderCount);
            
            // Test vision system capabilities
            Assert.IsTrue(visionManager.CanPerformRaycast(visionProvider));
            
            // Test system stats
            var stats = visionManager.GetSystemStats();
            Assert.IsNotNull(stats);
            Assert.IsTrue(stats.ContainsKey("ProviderCount"));
            
            // Cleanup
            visionManager.UnregisterVisionProvider(visionProvider);
            Assert.AreEqual(0, visionManager.RegisteredProviderCount);
        }
        
        /// <summary>
        /// Test sensor system performance under load
        /// </summary>
        [Test]
        public void TestSensorSystemPerformance()
        {
            sensorManager.Initialize();
            
            const int providerCount = 50;
            var providers = new List<SensorTestProvider>();
            
            // Add multiple providers
            for (int i = 0; i < providerCount; i++)
            {
                var provider = new SensorTestProvider($"Sensor{i}", i);
                providers.Add(provider);
                sensorManager.RegisterProvider(provider);
            }
            
            Assert.AreEqual(providerCount, sensorManager.SensorProviders.Count);
            
            // Test performance under load
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            
            for (int i = 0; i < 100; i++)
            {
                sensorManager.FixedUpdate();
            }
            
            stopwatch.Stop();
            
            // Should complete in reasonable time
            Assert.Less(stopwatch.ElapsedMilliseconds, 5000);
            
            // Test system stats
            var stats = sensorManager.GetSystemStats();
            Assert.AreEqual(providerCount, stats["ProviderCount"]);
        }
        
        /// <summary>
        /// Test sensor caching mechanism
        /// </summary>
        [Test]
        public void TestSensorCaching()
        {
            sensorManager.Initialize();
            sensorManager.EnableSensorCaching(true);
            
            var provider = new SensorTestProvider("CacheTest", 1);
            sensorManager.RegisterSensorProvider(provider);
            
            // Cache some data
            sensorManager.CacheSensorData("test_key", "test_value");
            
            // Retrieve cached data
            var cachedValue = sensorManager.GetCachedSensorData<string>("test_key");
            Assert.AreEqual("test_value", cachedValue);
        }
        
        /// <summary>
        /// Test async sensor processing
        /// </summary>
        [Test]
        public void TestAsyncSensorProcessing()
        {
            sensorManager.Initialize();
            sensorManager.EnableAsyncUpdates(true);
            
            var provider = new SensorTestAsyncProvider("AsyncTest", 1);
            sensorManager.RegisterSensorProvider(provider);
            
            // Process sensors asynchronously
            sensorManager.FixedUpdate();
            
            // Verify processing was initiated
            Assert.IsTrue(provider.ProcessingStarted);
        }
    }
    
    /// <summary>
    /// Test sensor provider for unit testing
    /// </summary>
    public class SensorTestProvider : ISensorProvider
    {
        public string SensorName { get; }
        public bool IsActive { get; private set; } = true;
        public int Priority { get; }
        public ISensor Sensor { get; private set; }
        
        public SensorTestProvider(string name, int priority)
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
        
        public string GetDebugInfo() => $"SensorTestProvider: {SensorName}";
        
        public void OnSensorEvent(string eventName, AgentContext context, object eventData = null) { }
    }
    
    /// <summary>
    /// Test async sensor provider for unit testing
    /// </summary>
    public class SensorTestAsyncProvider : SensorTestProvider
    {
        public bool ProcessingStarted { get; private set; }
        
        public SensorTestAsyncProvider(string name, int priority) : base(name, priority) { }
        
        public new void UpdateSensor(AgentContext context, float deltaTime)
        {
            ProcessingStarted = true;
            base.UpdateSensor(context, deltaTime);
        }
    }
}*/