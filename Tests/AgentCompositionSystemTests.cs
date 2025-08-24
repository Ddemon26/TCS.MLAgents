using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using TCS.MLAgents.Core;
using TCS.MLAgents.Core.Interfaces;
using Object = UnityEngine.Object;

namespace TCS.MLAgents.Core.Tests {
    /// <summary>
    /// Unit tests for the Agent Composition System core components.
    /// Tests the IMLAgent interface, MLAgentComposer, and AgentContext functionality.
    /// </summary>
    public class AgentCompositionSystemTests {
        
        private GameObject testGameObject;
        private MLAgentComposer composer;
        private AgentContext context;
        
        [SetUp]
        public void SetUp() {
            testGameObject = new GameObject("TestAgent");
            composer = testGameObject.AddComponent<MLAgentComposer>();
            context = new AgentContext(testGameObject);
        }
        
        [TearDown]
        public void TearDown() {
            if (testGameObject != null) {
                Object.DestroyImmediate(testGameObject);
            }
        }
        
        [Test]
        public void AgentContext_Constructor_SetsCorrectValues() {
            // Arrange
            var gameObject = new GameObject("TestAgent");
            
            // Act
            var context = new AgentContext(gameObject);
            
            // Assert
            Assert.IsNotNull(context.AgentGameObject);
            Assert.AreEqual(gameObject, context.AgentGameObject);
            Assert.IsTrue(context.AgentId.Contains("TestAgent"));
            Assert.AreEqual(0, context.EpisodeCount);
            Assert.IsFalse(context.IsEpisodeActive);
            Assert.AreEqual(0f, context.CumulativeReward);
            Assert.AreEqual(0f, context.StepCount);
            
            // Cleanup
            Object.DestroyImmediate(gameObject);
        }
        
        [Test]
        public void AgentContext_StartEpisode_UpdatesStateCorrectly() {
            // Act
            context.StartEpisode();
            
            // Assert
            Assert.AreEqual(1, context.EpisodeCount);
            Assert.IsTrue(context.IsEpisodeActive);
            Assert.AreEqual(0f, context.CumulativeReward);
            Assert.AreEqual(0f, context.StepCount);
            Assert.Greater(context.EpisodeStartTime, 0f);
        }
        
        [Test]
        public void AgentContext_EndEpisode_UpdatesStateCorrectly() {
            // Arrange
            context.StartEpisode();
            
            // Act
            context.EndEpisode();
            
            // Assert
            Assert.IsFalse(context.IsEpisodeActive);
        }
        
        [Test]
        public void AgentContext_AddReward_UpdatesCumulativeReward() {
            // Arrange
            context.StartEpisode();
            
            // Act
            context.AddReward(1.5f);
            context.AddReward(-0.5f);
            
            // Assert
            Assert.AreEqual(1.0f, context.CumulativeReward, 0.001f);
        }
        
        [Test]
        public void AgentContext_SetReward_ReplacesCumulativeReward() {
            // Arrange
            context.StartEpisode();
            context.AddReward(1.5f);
            
            // Act
            context.SetReward(2.0f);
            
            // Assert
            Assert.AreEqual(2.0f, context.CumulativeReward, 0.001f);
        }
        
        [Test]
        public void AgentContext_UpdateStep_IncreasesStepCount() {
            // Arrange
            context.StartEpisode();
            context.AddReward(2.0f);
            
            // Act
            context.UpdateStep();
            context.UpdateStep();
            
            // Assert
            Assert.AreEqual(2f, context.StepCount);
            Assert.AreEqual(1.0f, context.AverageRewardPerStep, 0.001f);
        }
        
        [Test]
        public void AgentContext_SharedData_StoresAndRetrievesCorrectly() {
            // Act & Assert
            context.SetSharedData("testKey", "testValue");
            Assert.AreEqual("testValue", context.GetSharedData<string>("testKey"));
            Assert.IsTrue(context.HasSharedData("testKey"));
            
            context.SetSharedData("numericKey", 42);
            Assert.AreEqual(42, context.GetSharedData<int>("numericKey"));
            
            context.RemoveSharedData("testKey");
            Assert.IsFalse(context.HasSharedData("testKey"));
            Assert.AreEqual("default", context.GetSharedData("testKey", "default"));
        }
        
        [Test]
        public void AgentContext_EpisodeCallbacks_ExecuteCorrectly() {
            // Arrange
            bool startCallbackExecuted = false;
            bool endCallbackExecuted = false;
            
            context.RegisterEpisodeStartCallback(() => startCallbackExecuted = true);
            context.RegisterEpisodeEndCallback(() => endCallbackExecuted = true);
            
            // Act
            context.StartEpisode();
            context.EndEpisode();
            
            // Assert
            Assert.IsTrue(startCallbackExecuted);
            Assert.IsTrue(endCallbackExecuted);
        }
        
        [Test]
        public void MLAgentComposer_Initialize_CreatesContext() {
            // Act
            composer.Initialize();
            
            // Assert
            Assert.IsNotNull(composer.Context);
            Assert.AreEqual(testGameObject, composer.Context.AgentGameObject);
        }
        
        [Test]
        public void MLAgentComposer_RegisterComponent_AddsToList() {
            // Arrange
            composer.Initialize();
            var mockComponent = new MockMLAgentComponent();
            
            // Act
            composer.RegisterComponent(mockComponent);
            
            // Assert
            Assert.IsTrue(composer.AgentComponents.Contains(mockComponent));
            Assert.AreEqual(1, composer.AgentComponents.Count);
        }
        
        [Test]
        public void MLAgentComposer_UnregisterComponent_RemovesFromList() {
            // Arrange
            composer.Initialize();
            var mockComponent = new MockMLAgentComponent();
            composer.RegisterComponent(mockComponent);
            
            // Act
            composer.UnregisterComponent(mockComponent);
            
            // Assert
            Assert.IsFalse(composer.AgentComponents.Contains(mockComponent));
            Assert.AreEqual(0, composer.AgentComponents.Count);
        }
        
        [Test]
        public void MLAgentComposer_GetAgentComponent_ReturnsCorrectType() {
            // Arrange
            composer.Initialize();
            var mockComponent = new MockMLAgentComponent();
            composer.RegisterComponent(mockComponent);
            
            // Act
            var retrieved = composer.GetAgentComponent<MockMLAgentComponent>();
            
            // Assert
            Assert.AreEqual(mockComponent, retrieved);
        }
        
        [Test]
        public void MLAgentComposer_OnEpisodeBegin_CallsAllComponents() {
            // Arrange
            composer.Initialize();
            var mockComponent1 = new MockMLAgentComponent();
            var mockComponent2 = new MockMLAgentComponent();
            
            composer.RegisterComponent(mockComponent1);
            composer.RegisterComponent(mockComponent2);
            
            // Act
            composer.OnEpisodeBegin();
            
            // Assert
            Assert.IsTrue(mockComponent1.OnEpisodeBeginCalled);
            Assert.IsTrue(mockComponent2.OnEpisodeBeginCalled);
            Assert.AreEqual(1, composer.Context.EpisodeCount);
        }
        
        [Test]
        public void MLAgentComposer_CollectObservations_CallsAllComponents() {
            // Arrange
            composer.Initialize();
            var mockComponent = new MockMLAgentComponent();
            composer.RegisterComponent(mockComponent);
            
            var vectorSensor = new VectorSensor(10);
            
            // Act
            composer.CollectObservations(vectorSensor);
            
            // Assert
            Assert.IsTrue(mockComponent.CollectObservationsCalled);
        }
        
        [Test]
        public void MLAgentComposer_OnActionReceived_CallsAllComponents() {
            // Arrange
            composer.Initialize();
            var mockComponent = new MockMLAgentComponent();
            composer.RegisterComponent(mockComponent);
            
            var actionBuffers = ActionBuffers.Empty;
            
            // Act
            composer.OnActionReceived(actionBuffers);
            
            // Assert
            Assert.IsTrue(mockComponent.OnActionReceivedCalled);
        }
        
        [Test]
        public void MLAgentComposer_RewardMethods_UpdateContext() {
            // Arrange
            composer.Initialize();
            
            // Act
            composer.AddReward(1.5f);
            composer.SetReward(2.0f);
            
            // Assert
            Assert.AreEqual(2.0f, composer.Context.CumulativeReward, 0.001f);
        }
    }
    
    /// <summary>
    /// Mock implementation of IMLAgent for testing purposes.
    /// </summary>
    public class MockMLAgentComponent : IMLAgent {
        public AgentContext Context { get; private set; }
        
        public bool InitializeCalled { get; private set; }
        public bool OnEpisodeBeginCalled { get; private set; }
        public bool CollectObservationsCalled { get; private set; }
        public bool OnActionReceivedCalled { get; private set; }
        public bool HeuristicCalled { get; private set; }
        public bool FixedUpdateCalled { get; private set; }
        public bool OnDestroyCalled { get; private set; }
        
        public void Initialize() {
            InitializeCalled = true;
        }
        
        public void OnEpisodeBegin() {
            OnEpisodeBeginCalled = true;
        }
        
        public void CollectObservations(VectorSensor sensor) {
            CollectObservationsCalled = true;
            // Add a dummy observation for testing
            sensor.AddObservation(1.0f);
        }
        
        public void OnActionReceived(ActionBuffers actionBuffers) {
            OnActionReceivedCalled = true;
        }
        
        public void Heuristic(in ActionBuffers actionsOut) {
            HeuristicCalled = true;
        }
        
        public void FixedUpdate() {
            FixedUpdateCalled = true;
        }
        
        public void OnDestroy() {
            OnDestroyCalled = true;
        }
        
        public void EndEpisode() {
            // Mock implementation
        }
        
        public void AddReward(float reward) {
            // Mock implementation
        }
        
        public void SetReward(float reward) {
            // Mock implementation
        }
    }
}