using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using TCS.MLAgents.Core;
using TCS.MLAgents.Interfaces;

namespace TCS.MLAgents.Decision {
    /// <summary>
    /// Routes decisions between different decision providers based on priority and conditions.
    /// Manages the selection of active decision providers for each step.
    /// </summary>
    public class DecisionRouter {
        [Header("Decision Configuration")]
        [SerializeField] private bool enableLogging = false;
        [SerializeField] private DecisionMode decisionMode = DecisionMode.Auto;
        [SerializeField] private string defaultDecisionProviderId = "inference";
        
        // Decision providers
        private List<IDecisionProvider> m_DecisionProviders;
        private IDecisionProvider m_ActiveProvider;
        private IDecisionProvider m_DefaultProvider;
        
        // Agent context
        private AgentContext m_Context;
        
        // Performance tracking
        private float m_TotalDecisionTime = 0f;
        private int m_DecisionCount = 0;
        
        public enum DecisionMode {
            Auto,           // Automatically select the highest priority active provider
            Manual,         // Use a specific provider specified by ID
            PriorityOnly    // Only use priority-based selection
        }
        
        public IDecisionProvider ActiveProvider => m_ActiveProvider;
        public List<IDecisionProvider> DecisionProviders => m_DecisionProviders;
        public float AverageDecisionTime => m_DecisionCount > 0 ? m_TotalDecisionTime / m_DecisionCount : 0f;
        
        public DecisionRouter() {
            m_DecisionProviders = new List<IDecisionProvider>();
        }
        
        public void Initialize(AgentContext context) {
            m_Context = context;
            
            // Initialize all decision providers
            foreach (var provider in m_DecisionProviders) {
                try {
                    provider.Initialize(context);
                    LogDebug($"Initialized decision provider: {provider.Id}");
                }
                catch (Exception ex) {
                    Debug.LogError($"DecisionRouter: Error initializing provider {provider.Id} - {ex.Message}");
                }
            }
            
            // Find the default provider
            m_DefaultProvider = m_DecisionProviders.FirstOrDefault(p => p.Id == defaultDecisionProviderId);
            if (m_DefaultProvider == null && m_DecisionProviders.Count > 0) {
                m_DefaultProvider = m_DecisionProviders[0];
            }
            
            LogDebug($"DecisionRouter initialized with {m_DecisionProviders.Count} providers, default: {m_DefaultProvider?.Id ?? "none"}");
        }
        
        /// <summary>
        /// Add a decision provider to the router
        /// </summary>
        public void AddDecisionProvider(IDecisionProvider provider) {
            if (provider == null) {
                Debug.LogWarning("DecisionRouter: Attempted to add null decision provider");
                return;
            }
            
            if (m_DecisionProviders.Any(p => p.Id == provider.Id)) {
                Debug.LogWarning($"DecisionRouter: Decision provider with ID {provider.Id} already exists");
                return;
            }
            
            m_DecisionProviders.Add(provider);
            
            // If this is the default provider, update the reference
            if (provider.Id == defaultDecisionProviderId) {
                m_DefaultProvider = provider;
            }
            
            // Sort by priority (highest first)
            m_DecisionProviders.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            
            LogDebug($"Added decision provider: {provider.Id} (Priority: {provider.Priority})");
        }
        
        /// <summary>
        /// Remove a decision provider from the router
        /// </summary>
        public void RemoveDecisionProvider(string providerId) {
            if (string.IsNullOrEmpty(providerId)) {
                Debug.LogWarning("DecisionRouter: Attempted to remove provider with null/empty ID");
                return;
            }
            
            var provider = m_DecisionProviders.FirstOrDefault(p => p.Id == providerId);
            if (provider != null) {
                m_DecisionProviders.Remove(provider);
                
                // Update default provider if needed
                if (m_DefaultProvider?.Id == providerId) {
                    m_DefaultProvider = m_DecisionProviders.FirstOrDefault(p => p.Id == defaultDecisionProviderId) 
                                     ?? m_DecisionProviders.FirstOrDefault();
                }
                
                LogDebug($"Removed decision provider: {providerId}");
            }
        }
        
        /// <summary>
        /// Get a decision provider by ID
        /// </summary>
        public T GetDecisionProvider<T>(string providerId) where T : class, IDecisionProvider {
            return m_DecisionProviders.FirstOrDefault(p => p.Id == providerId) as T;
        }
        
        /// <summary>
        /// Select the active decision provider for the current step
        /// </summary>
        public IDecisionProvider SelectActiveProvider(List<ISensor> sensors) {
            // In manual mode, use the specified provider
            if (decisionMode == DecisionMode.Manual) {
                var manualProvider = m_DecisionProviders.FirstOrDefault(p => p.Id == defaultDecisionProviderId);
                if (manualProvider != null && manualProvider.IsActive) {
                    return m_ActiveProvider = manualProvider;
                }
            }
            
            // Find the highest priority active provider that wants to decide
            IDecisionProvider selectedProvider = null;
            
            foreach (var provider in m_DecisionProviders) {
                if (provider.IsActive && provider.ShouldDecide(m_Context, sensors)) {
                    if (selectedProvider == null || provider.Priority > selectedProvider.Priority) {
                        selectedProvider = provider;
                    }
                }
            }
            
            // If no provider was selected, use the default
            if (selectedProvider == null) {
                selectedProvider = m_DefaultProvider;
            }
            
            m_ActiveProvider = selectedProvider;
            return selectedProvider;
        }
        
        /// <summary>
        /// Make a decision using the active decision provider
        /// </summary>
        public void DecideAction(List<ISensor> sensors, ActionBuffers actions) {
            if (m_Context == null) {
                Debug.LogError("DecisionRouter: Not initialized - call Initialize() first");
                return;
            }
            
            float startTime = Time.realtimeSinceStartup;
            
            // Select the active provider
            var provider = SelectActiveProvider(sensors);
            
            if (provider != null) {
                try {
                    provider.DecideAction(m_Context, sensors, actions);
                }
                catch (Exception ex) {
                    Debug.LogError($"DecisionRouter: Error in provider {provider.Id} - {ex.Message}");
                }
            }
            else {
                Debug.LogWarning("DecisionRouter: No active decision provider available");
                // Clear actions if no provider is available
                ClearActions(actions);
            }
            
            // Track performance
            float decisionTime = Time.realtimeSinceStartup - startTime;
            m_TotalDecisionTime += decisionTime;
            m_DecisionCount++;
            
            if (enableLogging && m_DecisionCount % 100 == 0) {
                LogDebug($"Decision time stats: Avg {AverageDecisionTime:F4}s, Current {decisionTime:F4}s");
            }
        }
        
        /// <summary>
        /// Clear all actions in the action buffers
        /// </summary>
        private void ClearActions(ActionBuffers actions) {
            var continuousActions = actions.ContinuousActions;
            for (int i = 0; i < continuousActions.Length; i++) {
                continuousActions[i] = 0f;
            }
            
            var discreteActions = actions.DiscreteActions;
            for (int i = 0; i < discreteActions.Length; i++) {
                discreteActions[i] = 0;
            }
        }
        
        /// <summary>
        /// Called when an episode begins
        /// </summary>
        public void OnEpisodeBegin() {
            if (m_Context == null) return;
            
            foreach (var provider in m_DecisionProviders) {
                if (provider.IsActive) {
                    try {
                        provider.OnEpisodeBegin(m_Context);
                    }
                    catch (Exception ex) {
                        Debug.LogError($"DecisionRouter: Error in provider {provider.Id} OnEpisodeBegin - {ex.Message}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Called when an episode ends
        /// </summary>
        public void OnEpisodeEnd() {
            if (m_Context == null) return;
            
            foreach (var provider in m_DecisionProviders) {
                if (provider.IsActive) {
                    try {
                        provider.OnEpisodeEnd(m_Context);
                    }
                    catch (Exception ex) {
                        Debug.LogError($"DecisionRouter: Error in provider {provider.Id} OnEpisodeEnd - {ex.Message}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Called each step to update decision providers
        /// </summary>
        public void OnUpdate(float deltaTime) {
            if (m_Context == null) return;
            
            foreach (var provider in m_DecisionProviders) {
                if (provider.IsActive) {
                    try {
                        provider.OnUpdate(m_Context, deltaTime);
                    }
                    catch (Exception ex) {
                        Debug.LogError($"DecisionRouter: Error in provider {provider.Id} OnUpdate - {ex.Message}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Enable or disable a specific decision provider
        /// </summary>
        public void SetProviderActive(string providerId, bool active) {
            var provider = m_DecisionProviders.FirstOrDefault(p => p.Id == providerId);
            if (provider != null) {
                provider.SetActive(active);
                LogDebug($"Set provider {providerId} active: {active}");
            }
        }
        
        /// <summary>
        /// Set the decision mode
        /// </summary>
        public void SetDecisionMode(DecisionMode mode, string providerId = null) {
            decisionMode = mode;
            if (!string.IsNullOrEmpty(providerId)) {
                defaultDecisionProviderId = providerId;
            }
            LogDebug($"Decision mode set to {mode}, provider: {defaultDecisionProviderId}");
        }
        
        /// <summary>
        /// Get debug information about the decision router
        /// </summary>
        public string GetDebugInfo() {
            var activeProviderId = m_ActiveProvider?.Id ?? "none";
            var defaultProviderId = m_DefaultProvider?.Id ?? "none";
            
            return $"DecisionRouter - Active: {activeProviderId}, Default: {defaultProviderId}, " +
                   $"Providers: {m_DecisionProviders.Count}, Mode: {decisionMode}, " +
                   $"AvgTime: {AverageDecisionTime:F4}s";
        }
        
        private void LogDebug(string message) {
            if (enableLogging) {
                Debug.Log($"[DecisionRouter] {message}");
            }
        }
    }
}