using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using TCS.MLAgents.Core;
using TCS.MLAgents.Interfaces;
using TCS.MLAgents.Rewards;
using TCS.MLAgents.Configuration;
using Object = UnityEngine.Object;

namespace TCS.MLAgents.Core.Tests {
    /// <summary>
    /// Integration tests for the Reward System components.
    /// Tests the interaction between RewardCalculator, reward providers, and configuration.
    /// </summary>
    public class RewardSystemTests {
        
        private GameObject testGameObject;
        private RewardCalculator rewardCalculator;
        private AgentContext context;
        private RewardConfig testConfig;
        
        [SetUp]
        public void SetUp() {
            testGameObject = new GameObject("TestAgent");
            rewardCalculator = testGameObject.AddComponent<RewardCalculator>();
            context = new AgentContext(testGameObject);
            
            // Create test configuration
            testConfig = ScriptableObject.CreateInstance<RewardConfig>();
        }
        
        [TearDown]
        public void TearDown() {
            if (testGameObject != null) {
                Object.DestroyImmediate(testGameObject);
            }
            
            if (testConfig != null) {
                Object.DestroyImmediate(testConfig);
            }
        }
        
        [Test]
        public void RewardCalculator_Initialize_CreatesCorrectState() {
            // Act
            rewardCalculator.Initialize();
            
            // Assert
            Assert.IsNotNull(rewardCalculator.RewardProviders);
            Assert.AreEqual(0, rewardCalculator.RewardProviders.Count);
            Assert.AreEqual(0f, rewardCalculator.CurrentStepReward);
            Assert.AreEqual(0f, rewardCalculator.TotalEpisodeReward);
        }
        
        [Test]
        public void RewardCalculator_RegisterProvider_AddsToList() {
            // Arrange
            rewardCalculator.Initialize();
            var mockProvider = new MockRewardProvider();
            
            // Act
            rewardCalculator.RegisterProvider(mockProvider);
            
            // Assert
            Assert.AreEqual(1, rewardCalculator.RewardProviders.Count);
            Assert.IsTrue(rewardCalculator.RewardProviders.Contains(mockProvider));
        }
        
        [Test]
        public void RewardCalculator_UnregisterProvider_RemovesFromList() {
            // Arrange
            rewardCalculator.Initialize();
            var mockProvider = new MockRewardProvider();
            rewardCalculator.RegisterProvider(mockProvider);
            
            // Act
            rewardCalculator.UnregisterProvider(mockProvider);
            
            // Assert
            Assert.AreEqual(0, rewardCalculator.RewardProviders.Count);
            Assert.IsFalse(rewardCalculator.RewardProviders.Contains(mockProvider));
        }
        
        [Test]
        public void ProximityRewardProvider_CalculateReward_ReturnsCorrectValues() {
            // Arrange
            var targetObject = new GameObject("Target");
            targetObject.transform.position = Vector3.forward * 10f;
            
            var provider = new ProximityRewardProvider();
            provider.SetTarget(targetObject.transform);
            provider.SetRewardWeight(1f);
            provider.Initialize(context);
            
            // Act
            float reward = provider.CalculateReward(context, 0.1f);
            
            // Assert
            Assert.Greater(reward, 0f); // Should give positive reward for being closer than max distance
            
            // Cleanup
            Object.DestroyImmediate(targetObject);
        }
        
        [Test]
        public void TimeRewardProvider_CalculateReward_AppliesTimePenalty() {
            // Arrange
            var provider = new TimeRewardProvider();
            provider.SetTimePenalty(-0.001f);
            provider.SetRewardWeight(1f);
            provider.Initialize(context);
            
            // Act
            float reward = provider.CalculateReward(context, 1f); // 1 second delta
            
            // Assert
            Assert.Less(reward, 0f); // Should give negative reward for time penalty
            Assert.AreEqual(-0.001f, reward, 0.0001f);
        }
        
        [Test]
        public void BoundaryRewardProvider_CalculateReward_PenalizesViolations() {
            // Arrange
            testGameObject.transform.position = Vector3.forward * 20f; // Outside boundary
            
            var provider = new BoundaryRewardProvider();
            provider.SetSphereBoundary(Vector3.zero, 5f);
            provider.SetViolationPenalty(-1f, false, false);
            provider.SetGracePeriod(0f); // Disable grace period for testing
            provider.SetRewardWeight(1f);
            provider.Initialize(context);
            
            // Act
            float reward = provider.CalculateReward(context, 0.1f);
            
            // Assert
            Assert.Less(reward, 0f); // Should penalize for being outside boundary
        }
        
        [Test]
        public void TaskCompletionRewardProvider_OnRewardEvent_HandlesItemCollection() {
            // Arrange
            var provider = new TaskCompletionRewardProvider();
            provider.SetTaskType(TaskCompletionRewardProvider.TaskType.CollectItems);
            provider.SetCompletionReward(10f);
            provider.SetRewardWeight(1f);
            
            // Initialize without trying to find any tags
            provider.Initialize(context);
            
            // Act
            provider.OnRewardEvent("ItemCollected", context);
            float reward = provider.CalculateReward(context, 0.1f);
            
            // Assert
            Assert.Greater(reward, 0f); // Should give completion reward
            Assert.IsTrue(provider.IsTaskCompleted());
        }
        
        [UnityTest]
        public IEnumerator RewardCalculator_OnActionReceived_CalculatesRewards() {
            // Arrange
            rewardCalculator.Initialize();
            var mockProvider = new MockRewardProvider { TestReward = 0.5f };
            rewardCalculator.RegisterProvider(mockProvider);
            
            // Act
            rewardCalculator.OnActionReceived(Unity.MLAgents.Actuators.ActionBuffers.Empty);
            
            yield return null;
            
            // Assert
            Assert.AreEqual(0.5f, rewardCalculator.CurrentStepReward, 0.001f);
            Assert.AreEqual(0.5f, rewardCalculator.TotalEpisodeReward, 0.001f);
            Assert.IsTrue(mockProvider.CalculateRewardCalled);
        }
        
        [Test]
        public void RewardCalculator_TriggerRewardEvent_NotifiesAllProviders() {
            // Arrange
            rewardCalculator.Initialize();
            var mockProvider1 = new MockRewardProvider();
            var mockProvider2 = new MockRewardProvider();
            
            rewardCalculator.RegisterProvider(mockProvider1);
            rewardCalculator.RegisterProvider(mockProvider2);
            
            // Act
            rewardCalculator.TriggerRewardEvent("TestEvent", "TestData");
            
            // Assert
            Assert.IsTrue(mockProvider1.OnRewardEventCalled);
            Assert.IsTrue(mockProvider2.OnRewardEventCalled);
            Assert.AreEqual("TestEvent", mockProvider1.LastEventName);
            Assert.AreEqual("TestEvent", mockProvider2.LastEventName);
        }
        
        [Test]
        public void RewardConfig_ValidateConfig_DetectsInvalidSettings() {
            // Arrange
            testConfig.name = "TestConfig";
            
            // Set invalid configuration
            var providerConfig1 = new RewardConfig.RewardProviderConfig {
                providerName = "Provider1",
                weight = 1f,
                maxValue = 1f,
                minValue = 2f // Invalid: min > max
            };
            
            var providerConfig2 = new RewardConfig.RewardProviderConfig {
                providerName = "Provider1", // Duplicate name
                weight = 1f
            };
            
            testConfig.AddProviderConfig(providerConfig1);
            testConfig.AddProviderConfig(providerConfig2);
            
            // Act
            bool isValid = testConfig.ValidateConfig();
            
            // Assert
            Assert.IsFalse(isValid);
        }
        
        [Test]
        public void RewardConfig_GetProviderConfig_ReturnsCorrectConfig() {
            // Arrange
            var providerConfig = new RewardConfig.RewardProviderConfig {
                providerName = "TestProvider",
                weight = 2f,
                isActive = true
            };
            
            testConfig.AddProviderConfig(providerConfig);
            
            // Act
            var retrievedConfig = testConfig.GetProviderConfig("TestProvider");
            
            // Assert
            Assert.IsNotNull(retrievedConfig);
            Assert.AreEqual("TestProvider", retrievedConfig.providerName);
            Assert.AreEqual(2f, retrievedConfig.weight);
            Assert.IsTrue(retrievedConfig.isActive);
        }
        
        [Test]
        public void RewardConfig_SetProviderWeight_UpdatesCorrectly() {
            // Arrange
            var providerConfig = new RewardConfig.RewardProviderConfig {
                providerName = "TestProvider",
                weight = 1f
            };
            
            testConfig.AddProviderConfig(providerConfig);
            
            // Act
            testConfig.SetProviderWeight("TestProvider", 5f);
            
            // Assert
            Assert.AreEqual(5f, providerConfig.weight);
        }
        
        [Test]
        public void RewardProviderBase_ProcessReward_AppliesWeightCorrectly() {
            // Arrange
            var provider = new MockRewardProvider();
            provider.SetRewardWeight(2f);
            provider.TestReward = 0.5f;
            provider.Initialize(context);
            
            // Act
            float reward = provider.CalculateReward(context, 0.1f);
            
            // Assert
            Assert.AreEqual(1f, reward, 0.001f); // 0.5 * 2.0 = 1.0
        }
        
        [Test]
        public void RewardProviderBase_ProcessReward_ClampsExtremeValues() {
            // Arrange
            var provider = new MockRewardProvider();
            provider.SetRewardWeight(1f);
            provider.TestReward = 1000f; // Extreme value
            provider.Initialize(context);
            
            // Act
            float reward = provider.CalculateReward(context, 0.1f);
            
            // Assert
            Assert.LessOrEqual(reward, 100f); // Should be clamped to max
        }
        
        [Test]
        public void MultipleProviders_Integration_CalculatesCorrectTotalReward() {
            // Arrange
            rewardCalculator.Initialize();
            
            var proximityProvider = new MockRewardProvider { TestReward = 0.3f };
            proximityProvider.SetRewardWeight(1f);
            proximityProvider.SetPriority(10);
            
            var timeProvider = new MockRewardProvider { TestReward = -0.001f };
            timeProvider.SetRewardWeight(1f);
            timeProvider.SetPriority(5);
            
            rewardCalculator.RegisterProvider(proximityProvider);
            rewardCalculator.RegisterProvider(timeProvider);
            
            // Act
            rewardCalculator.OnActionReceived(Unity.MLAgents.Actuators.ActionBuffers.Empty);
            
            // Assert
            float expectedTotal = 0.3f + (-0.001f);
            Assert.AreEqual(expectedTotal, rewardCalculator.CurrentStepReward, 0.0001f);
            Assert.AreEqual(2, rewardCalculator.ProviderContributions.Count);
        }
        
        [UnityTest]
        public IEnumerator RewardCalculator_OnEpisodeBegin_ResetsState() {
            // Arrange
            rewardCalculator.Initialize();
            var mockProvider = new MockRewardProvider { TestReward = 1f };
            rewardCalculator.RegisterProvider(mockProvider);
            
            // Add some rewards first
            rewardCalculator.OnActionReceived(Unity.MLAgents.Actuators.ActionBuffers.Empty);
            yield return null;
            
            Assert.Greater(rewardCalculator.TotalEpisodeReward, 0f);
            
            // Act
            rewardCalculator.OnEpisodeBegin();
            yield return null;
            
            // Assert
            Assert.AreEqual(0f, rewardCalculator.TotalEpisodeReward);
            Assert.AreEqual(0f, rewardCalculator.CurrentStepReward);
            Assert.IsTrue(mockProvider.OnEpisodeBeginCalled);
        }
    }
    
    /// <summary>
    /// Mock reward provider for testing purposes.
    /// </summary>
    public class MockRewardProvider : RewardProviderBase {
        public float TestReward = 0f;
        public bool CalculateRewardCalled = false;
        public bool OnRewardEventCalled = false;
        public bool OnEpisodeBeginCalled = false;
        public string LastEventName = "";
        
        public override float CalculateReward(AgentContext context, float deltaTime) {
            CalculateRewardCalled = true;
            return ProcessReward(TestReward);
        }
        
        public override void OnRewardEvent(string eventName, AgentContext context, object eventData = null) {
            OnRewardEventCalled = true;
            LastEventName = eventName;
        }
        
        public override void OnEpisodeBegin(AgentContext context) {
            base.OnEpisodeBegin(context);
            OnEpisodeBeginCalled = true;
            CalculateRewardCalled = false;
            OnRewardEventCalled = false;
            LastEventName = "";
        }
    }
}