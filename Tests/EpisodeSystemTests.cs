using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;
using TCS.MLAgents.Core;
using TCS.MLAgents.Interfaces;
using TCS.MLAgents.Episodes;
using TCS.MLAgents.Configuration;

namespace TCS.MLAgents.Tests
{
    /// <summary>
    /// Comprehensive tests for the episode system components
    /// </summary>
    public class EpisodeSystemTests
    {
        private GameObject testGameObject;
        private AgentContext testContext;
        private EpisodeManager episodeManager;
        
        [SetUp]
        public void Setup()
        {
            testGameObject = new GameObject("TestAgent");
            testContext = new AgentContext(testGameObject);
            episodeManager = testGameObject.AddComponent<EpisodeManager>();
        }
        
        [TearDown]
        public void Teardown()
        {
            if (testGameObject != null)
                Object.DestroyImmediate(testGameObject);
        }
        
        /// <summary>
        /// Test EpisodeManager initialization
        /// </summary>
        [Test]
        public void TestEpisodeManagerInitialization()
        {
            episodeManager.Initialize(testContext);
            
            Assert.IsNotNull(episodeManager.Context);
            Assert.IsFalse(episodeManager.IsEpisodeActive);
            Assert.AreEqual(0, episodeManager.TotalEpisodes);
            Assert.AreEqual(0f, episodeManager.CurrentEpisodeDuration, 0.001f);
        }
        
        /// <summary>
        /// Test episode lifecycle - start, update, end
        /// </summary>
        [Test]
        public void TestEpisodeLifecycle()
        {
            episodeManager.Initialize(testContext);
            
            // Test episode start
            episodeManager.RequestEpisodeStart();
            
            // Simulate episode start processing (normally done in FixedUpdate)
            episodeManager.FixedUpdate();
            
            Assert.IsTrue(episodeManager.IsEpisodeActive);
            Assert.AreEqual(1, episodeManager.TotalEpisodes);
            Assert.IsTrue(episodeManager.CurrentEpisodeDuration >= 0f);
            
            // Test episode end
            episodeManager.RequestEpisodeEnd(EpisodeEndReason.Success);
            
            // Simulate episode end processing
            episodeManager.FixedUpdate();
            
            Assert.IsFalse(episodeManager.IsEpisodeActive);
            Assert.AreEqual(1, episodeManager.GetEndReasonCount(EpisodeEndReason.Success));
        }
        
        /// <summary>
        /// Test episode handler registration and management
        /// </summary>
        [Test]
        public void EpisodeTestHandlerManagement()
        {
            episodeManager.Initialize(testContext);
            
            var handler1 = new EpisodeTestHandler("Handler1", 10);
            var handler2 = new EpisodeTestHandler("Handler2", 5);
            
            // Test handler registration
            episodeManager.RegisterHandler(handler1);
            episodeManager.RegisterHandler(handler2);
            
            Assert.AreEqual(2, episodeManager.EpisodeHandlers.Count);
            
            // Test priority ordering (higher priority first)
            var handlers = episodeManager.EpisodeHandlers;
            Assert.AreEqual(handler1, handlers[0]);
            Assert.AreEqual(handler2, handlers[1]);
            
            // Test handler unregistration
            episodeManager.UnregisterHandler(handler1);
            Assert.AreEqual(1, episodeManager.EpisodeHandlers.Count);
            Assert.AreEqual(handler2, handlers[0]);
        }
        
        /// <summary>
        /// Test episode configuration application
        /// </summary>
        [Test]
        public void TestEpisodeConfiguration()
        {
            var config = ScriptableObject.CreateInstance<EpisodeConfig>();
            config.autoStartEpisodes = false;
            config.episodeStartDelay = 0.5f;
            config.logEpisodeEvents = true;
            
            episodeManager.SetConfiguration(config);
            episodeManager.Initialize(testContext);
            
            // Verify configuration was applied
            Assert.IsFalse(episodeManager.IsEpisodeActive); // Auto-start is disabled
        }
        
        /// <summary>
        /// Test different episode end reasons
        /// </summary>
        [Test]
        public void TestEpisodeEndReasons()
        {
            episodeManager.Initialize(testContext);
            
            var endReasons = new[]
            {
                EpisodeEndReason.Success,
                EpisodeEndReason.Failure,
                EpisodeEndReason.TimeLimit,
                EpisodeEndReason.BoundaryViolation,
                EpisodeEndReason.ManualReset
            };
            
            foreach (var reason in endReasons)
            {
                // Start episode
                episodeManager.RequestEpisodeStart();
                episodeManager.FixedUpdate();
                Assert.IsTrue(episodeManager.IsEpisodeActive);
                
                // End episode with specific reason
                episodeManager.RequestEpisodeEnd(reason);
                episodeManager.FixedUpdate();
                Assert.IsFalse(episodeManager.IsEpisodeActive);
                
                // Verify end reason was recorded
                Assert.AreEqual(1, episodeManager.GetEndReasonCount(reason));
                
                // Reset for next test
                episodeManager.ResetAllHandlers();
            }
        }
        
        /// <summary>
        /// Test episode statistics tracking
        /// </summary>
        [Test]
        public void TestEpisodeStatistics()
        {
            episodeManager.Initialize(testContext);
            
            const int episodeCount = 5;
            
            for (int i = 0; i < episodeCount; i++)
            {
                episodeManager.RequestEpisodeStart();
                episodeManager.FixedUpdate();
                
                // Simulate some time passing
                System.Threading.Thread.Sleep(10);
                
                episodeManager.RequestEpisodeEnd(EpisodeEndReason.Success);
                episodeManager.FixedUpdate();
            }
            
            Assert.AreEqual(episodeCount, episodeManager.TotalEpisodes);
            Assert.AreEqual(episodeCount, episodeManager.GetEndReasonCount(EpisodeEndReason.Success));
            Assert.IsTrue(episodeManager.GetAverageEpisodeDuration() > 0f);
            
            var endReasonStats = episodeManager.GetEndReasonStats();
            Assert.IsNotNull(endReasonStats);
            Assert.AreEqual(episodeCount, endReasonStats[EpisodeEndReason.Success]);
        }
        
        /// <summary>
        /// Test BoundaryHandler functionality
        /// </summary>
        [Test]
        public void TestBoundaryHandler()
        {
            var boundaryHandler = new BoundaryHandler();
            boundaryHandler.Initialize(testContext);
            
            Assert.AreEqual("BoundaryHandler", boundaryHandler.HandlerName);
            Assert.IsTrue(boundaryHandler.IsActive);
            
            // Test boundary checking
            testGameObject.transform.position = Vector3.zero;
            Assert.IsFalse(boundaryHandler.ShouldEndEpisode(testContext));
            
            // Move outside boundary (assuming default boundary)
            testGameObject.transform.position = new Vector3(1000f, 1000f, 1000f);
            // Note: This might return true depending on boundary configuration
        }
        
        /// <summary>
        /// Test TimeLimitHandler functionality
        /// </summary>
        [Test]
        public void TestTimeLimitHandler()
        {
            var timeLimitHandler = new TimeLimitHandler();
            timeLimitHandler.Initialize(testContext);
            
            Assert.AreEqual("TimeLimitHandler", timeLimitHandler.HandlerName);
            Assert.IsTrue(timeLimitHandler.IsActive);
            
            // Test episode begin
            timeLimitHandler.OnEpisodeBegin(testContext);
            
            // Test should not end episode immediately
            Assert.IsFalse(timeLimitHandler.ShouldEndEpisode(testContext));
        }
        
        /// <summary>
        /// Test TaskCompletionHandler functionality
        /// </summary>
        [Test]
        public void TestTaskCompletionHandler()
        {
            var taskHandler = new TaskCompletionHandler();
            taskHandler.Initialize(testContext);
            
            Assert.AreEqual("TaskCompletionHandler", taskHandler.HandlerName);
            Assert.IsTrue(taskHandler.IsActive);
            
            // Test task not completed initially
            Assert.IsFalse(taskHandler.ShouldEndEpisode(testContext));
            
            // Set task completion flag
            testContext.SetSharedData("TaskCompleted", true);
            
            // Should now end episode
            Assert.IsTrue(taskHandler.ShouldEndEpisode(testContext));
        }
        
        /// <summary>
        /// Test StepLimitHandler functionality
        /// </summary>
        [Test]
        public void TestStepLimitHandler()
        {
            var stepHandler = new StepLimitHandler();
            stepHandler.Initialize(testContext);
            
            Assert.AreEqual("StepLimitHandler", stepHandler.HandlerName);
            Assert.IsTrue(stepHandler.IsActive);
            
            // Test episode begin
            stepHandler.OnEpisodeBegin(testContext);
            
            // Should not end episode at start
            Assert.IsFalse(stepHandler.ShouldEndEpisode(testContext));
        }
        
        /// <summary>
        /// Test multiple handlers working together
        /// </summary>
        [Test]
        public void TestMultipleHandlersIntegration()
        {
            episodeManager.Initialize(testContext);
            
            var boundaryHandler = new BoundaryHandler();
            var timeLimitHandler = new TimeLimitHandler();
            var taskHandler = new TaskCompletionHandler();
            
            // Register handlers manually
            episodeManager.RegisterHandler(boundaryHandler);
            episodeManager.RegisterHandler(timeLimitHandler);
            episodeManager.RegisterHandler(taskHandler);
            
            Assert.AreEqual(3, episodeManager.EpisodeHandlers.Count);
            
            // Start episode
            episodeManager.RequestEpisodeStart();
            episodeManager.FixedUpdate();
            Assert.IsTrue(episodeManager.IsEpisodeActive);
            
            // Trigger task completion
            testContext.SetSharedData("TaskCompleted", true);
            
            // Update episode manager to check end conditions
            episodeManager.FixedUpdate();
            
            // Episode should end due to task completion
            Assert.IsFalse(episodeManager.IsEpisodeActive);
            Assert.AreEqual(1, episodeManager.GetEndReasonCount(EpisodeEndReason.Success));
        }
    }
    
    /// <summary>
    /// Test episode handler for unit testing
    /// </summary>
    public class EpisodeTestHandler : IEpisodeHandler
    {
        public string HandlerName { get; }
        public int Priority { get; }
        public bool IsActive { get; private set; } = true;
        
        public bool ShouldStartCalled { get; private set; }
        public bool ShouldEndCalled { get; private set; }
        public bool OnBeginCalled { get; private set; }
        public bool OnEndCalled { get; private set; }
        public bool OnUpdateCalled { get; private set; }
        
        public EpisodeTestHandler(string name, int priority)
        {
            HandlerName = name;
            Priority = priority;
        }
        
        public void Initialize(AgentContext context) { }
        
        public bool ValidateHandler(AgentContext context) => true;
        
        public bool ShouldStartEpisode(AgentContext context)
        {
            ShouldStartCalled = true;
            return false;
        }
        
        public bool ShouldEndEpisode(AgentContext context)
        {
            ShouldEndCalled = true;
            return false;
        }
        
        public void OnEpisodeBegin(AgentContext context)
        {
            OnBeginCalled = true;
        }
        
        public void OnEpisodeEnd(AgentContext context, EpisodeEndReason reason)
        {
            OnEndCalled = true;
        }
        
        public void OnEpisodeUpdate(AgentContext context, float deltaTime)
        {
            OnUpdateCalled = true;
        }
        
        public void Reset()
        {
            ShouldStartCalled = false;
            ShouldEndCalled = false;
            OnBeginCalled = false;
            OnEndCalled = false;
            OnUpdateCalled = false;
        }
        
        public void SetActive(bool active) => IsActive = active;
        
        public string GetDebugInfo() => $"EpisodeTestHandler: {HandlerName}";
    }
}