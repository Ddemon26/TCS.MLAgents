using System;
using System.Collections.Generic;
using UnityEngine;
using TCS.MLAgents.Core;
using TCS.MLAgents.Configuration;
using TCS.MLAgents.Observations;
using TCS.MLAgents.Actions;
using TCS.MLAgents.Rewards;
using TCS.MLAgents.Episodes;
using TCS.MLAgents.Sensors;
using TCS.MLAgents.Decision;
using TCS.MLAgents.Utilities;

namespace TCS.MLAgents.Configuration {
    /// <summary>
    /// Applies behavior configuration to agent components and manages the setup process.
    /// </summary>
    public class BehaviorApplicator {
        [Header("Configuration")]
        [SerializeField] private MLBehaviorConfig m_Configuration;
        [SerializeField] private bool m_EnableLogging = true;
        [SerializeField] private bool m_ValidateOnApply = true;
        
        // Component references
        private AgentContext m_Context;
        private GameObject m_AgentGameObject;
        private MLAgentComposer m_AgentComposer;
        
        // System components
        private VectorObservationCollector m_ObservationCollector;
        private ActionDistributor m_ActionDistributor;
        private RewardCalculator m_RewardCalculator;
        
        public MLBehaviorConfig Configuration => m_Configuration;
        public bool IsApplied { get; private set; }
        
        public BehaviorApplicator(MLBehaviorConfig config) {
            m_Configuration = config;
        }
        
        public void Initialize(AgentContext context, GameObject agentGameObject) {
            m_Context = context;
            m_AgentGameObject = agentGameObject;
            m_AgentComposer = agentGameObject.GetComponent<MLAgentComposer>();
            
            if (m_AgentComposer == null) {
                m_AgentComposer = agentGameObject.AddComponent<MLAgentComposer>();
            }
            
            LogDebug($"BehaviorApplicator initialized for {agentGameObject.name}");
        }
        
        /// <summary>
        /// Apply the behavior configuration to the agent
        /// </summary>
        public bool ApplyConfiguration() {
            if (m_Configuration == null) {
                Debug.LogError("BehaviorApplicator: No configuration provided");
                return false;
            }
            
            // Validate configuration if enabled
            if (m_ValidateOnApply) {
                if (!m_Configuration.Validate(out string validationError)) {
                    Debug.LogError($"BehaviorApplicator: Configuration validation failed - {validationError}");
                    return false;
                }
            }
            
            try {
                // Apply time scale
                Time.timeScale = m_Configuration.TimeScale;
                
                // Setup core systems
                SetupObservationSystem();
                SetupActionSystem();
                SetupRewardSystem();
                
                IsApplied = true;
                LogDebug($"Behavior configuration '{m_Configuration.BehaviorName}' applied successfully");
                return true;
            }
            catch (Exception ex) {
                Debug.LogError($"BehaviorApplicator: Error applying configuration - {ex.Message}");
                return false;
            }
        }
        
        private void SetupObservationSystem() {
            // Get or create observation collector
            m_ObservationCollector = m_AgentGameObject.GetComponent<VectorObservationCollector>();
            if (m_ObservationCollector == null) {
                m_ObservationCollector = m_AgentGameObject.AddComponent<VectorObservationCollector>();
            }
            
            // Note: VectorObservationCollector doesn't have methods to set observation size directly
            // We'll need to add observation providers that define the observation space
            
            // Add observation providers
            foreach (string providerName in m_Configuration.ObservationProviders) {
                AddObservationProvider(providerName);
            }
            
            LogDebug($"Observation system configured - Providers: {m_Configuration.ObservationProviders.Count}");
        }
        
        private void SetupActionSystem() {
            // Get or create action distributor
            m_ActionDistributor = m_AgentGameObject.GetComponent<ActionDistributor>();
            if (m_ActionDistributor == null) {
                m_ActionDistributor = m_AgentGameObject.AddComponent<ActionDistributor>();
            }
            
            // Note: ActionDistributor doesn't have a SetActionSpace method
            // Action space is determined by registered action receivers
            
            // Add action receivers
            foreach (string receiverName in m_Configuration.ActionReceivers) {
                AddActionReceiver(receiverName);
            }
            
            LogDebug($"Action system configured - Receivers: {m_Configuration.ActionReceivers.Count}");
        }
        
        private void SetupRewardSystem() {
            // Get or create reward calculator
            m_RewardCalculator = m_AgentGameObject.GetComponent<RewardCalculator>();
            if (m_RewardCalculator == null) {
                m_RewardCalculator = m_AgentGameObject.AddComponent<RewardCalculator>();
            }
            
            // Note: RewardCalculator doesn't have methods to set reward parameters directly
            // These would be configured through reward providers or the component's inspector
            
            // Add reward providers
            foreach (string providerName in m_Configuration.RewardProviders) {
                AddRewardProvider(providerName);
            }
            
            LogDebug($"Reward system configured - Providers: {m_Configuration.RewardProviders.Count}");
        }
        
        // Component addition methods
        private void AddObservationProvider(string providerName) {
            // This would typically instantiate and configure specific observation providers
            // based on the provider name. For now, we'll just log the addition.
            LogDebug($"Adding observation provider: {providerName}");
        }
        
        private void AddActionReceiver(string receiverName) {
            // This would typically instantiate and configure specific action receivers
            // based on the receiver name. For now, we'll just log the addition.
            LogDebug($"Adding action receiver: {receiverName}");
        }
        
        private void AddRewardProvider(string providerName) {
            // This would typically instantiate and configure specific reward providers
            // based on the provider name. For now, we'll just log the addition.
            LogDebug($"Adding reward provider: {providerName}");
        }
        
        /// <summary>
        /// Update the configuration and reapply if needed
        /// </summary>
        public void UpdateConfiguration(MLBehaviorConfig newConfig) {
            if (newConfig == null) return;
            
            m_Configuration = newConfig;
            
            if (IsApplied) {
                ApplyConfiguration();
            }
        }
        
        /// <summary>
        /// Get a summary of the applied configuration
        /// </summary>
        public string GetAppliedConfigurationSummary() {
            if (!IsApplied) {
                return "Configuration not yet applied";
            }
            
            return m_Configuration.GetSummary();
        }
        
        private void LogDebug(string message) {
            if (m_EnableLogging) {
                Debug.Log($"[BehaviorApplicator] {message}");
            }
        }
    }
}