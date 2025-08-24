using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using TCS.MLAgents.Core;
using TCS.MLAgents.Interfaces;

namespace TCS.MLAgents.Utilities {
    /// <summary>
    /// Central collector that manages multiple statistics providers and aggregates their data.
    /// </summary>
    public class StatisticsCollector {
        [Header("Statistics Configuration")]
        [SerializeField] private bool enableLogging = false;
        [SerializeField] private float statisticsUpdateInterval = 1.0f; // Update every second
        [SerializeField] private int maxHistoryLength = 1000;
        
        // Statistics providers
        private List<IStatisticsProvider> m_StatisticsProviders;
        private Dictionary<string, IStatisticsProvider> m_ProvidersById;
        
        // Collected statistics
        private Dictionary<string, float> m_CurrentStatistics;
        private Dictionary<string, List<float>> m_StatisticsHistory;
        private Dictionary<string, float> m_LastReportedStatistics;
        
        // Agent context
        private AgentContext m_Context;
        
        // Timing
        private float m_LastUpdate_time = 0f;
        private int m_UpdateCount = 0;
        
        // Performance tracking
        private float m_TotalCollectionTime = 0f;
        private int m_CollectionCount = 0;
        
        public Dictionary<string, float> CurrentStatistics => new Dictionary<string, float>(m_CurrentStatistics);
        public Dictionary<string, List<float>> StatisticsHistory => new Dictionary<string, List<float>>(m_StatisticsHistory);
        public List<IStatisticsProvider> StatisticsProviders => new List<IStatisticsProvider>(m_StatisticsProviders);
        public float AverageCollectionTime => m_CollectionCount > 0 ? m_TotalCollectionTime / m_CollectionCount : 0f;
        public int UpdateCount => m_UpdateCount;
        
        public StatisticsCollector() {
            m_StatisticsProviders = new List<IStatisticsProvider>();
            m_ProvidersById = new Dictionary<string, IStatisticsProvider>();
            m_CurrentStatistics = new Dictionary<string, float>();
            m_StatisticsHistory = new Dictionary<string, List<float>>();
            m_LastReportedStatistics = new Dictionary<string, float>();
        }
        
        public void Initialize(AgentContext context) {
            m_Context = context;
            
            // Initialize all statistics providers
            foreach (var provider in m_StatisticsProviders) {
                try {
                    provider.Initialize(context);
                    LogDebug($"Initialized statistics provider: {provider.Id}");
                }
                catch (Exception ex) {
                    Debug.LogError($"StatisticsCollector: Error initializing provider {provider.Id} - {ex.Message}");
                }
            }
            
            LogDebug($"StatisticsCollector initialized with {m_StatisticsProviders.Count} providers");
        }
        
        /// <summary>
        /// Add a statistics provider to the collector
        /// </summary>
        public void AddStatisticsProvider(IStatisticsProvider provider) {
            if (provider == null) {
                Debug.LogWarning("StatisticsCollector: Attempted to add null statistics provider");
                return;
            }
            
            if (m_ProvidersById.ContainsKey(provider.Id)) {
                Debug.LogWarning($"StatisticsCollector: Statistics provider with ID {provider.Id} already exists");
                return;
            }
            
            m_StatisticsProviders.Add(provider);
            m_ProvidersById[provider.Id] = provider;
            
            // Sort by priority (highest first)
            m_StatisticsProviders.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            
            LogDebug($"Added statistics provider: {provider.Id} (Priority: {provider.Priority})");
        }
        
        /// <summary>
        /// Remove a statistics provider from the collector
        /// </summary>
        public void RemoveStatisticsProvider(string providerId) {
            if (string.IsNullOrEmpty(providerId)) {
                Debug.LogWarning("StatisticsCollector: Attempted to remove provider with null/empty ID");
                return;
            }
            
            if (m_ProvidersById.TryGetValue(providerId, out IStatisticsProvider provider)) {
                m_StatisticsProviders.Remove(provider);
                m_ProvidersById.Remove(providerId);
                
                LogDebug($"Removed statistics provider: {providerId}");
            }
        }
        
        /// <summary>
        /// Get a statistics provider by ID
        /// </summary>
        public T GetStatisticsProvider<T>(string providerId) where T : class, IStatisticsProvider {
            return m_ProvidersById.TryGetValue(providerId, out IStatisticsProvider provider) ? provider as T : null;
        }
        
        /// <summary>
        /// Collect statistics from all active providers
        /// </summary>
        public void CollectStatistics(float deltaTime) {
            if (m_Context == null) {
                Debug.LogError("StatisticsCollector: Not initialized - call Initialize() first");
                return;
            }
            
            // Check if it's time to update statistics
            if (Time.time - m_LastUpdate_time < statisticsUpdateInterval) {
                return;
            }
            
            float startTime = Time.realtimeSinceStartup;
            
            // Clear current statistics
            m_CurrentStatistics.Clear();
            
            // Collect from all active providers
            foreach (var provider in m_StatisticsProviders) {
                if (provider.IsActive) {
                    try {
                        provider.CollectStatistics(m_Context, deltaTime);
                        
                        // Get statistics from provider
                        var providerStats = provider.GetStatistics();
                        foreach (var kvp in providerStats) {
                            string key = $"{provider.Id}.{kvp.Key}";
                            m_CurrentStatistics[key] = kvp.Value;
                        }
                    }
                    catch (Exception ex) {
                        Debug.LogError($"StatisticsCollector: Error collecting from provider {provider.Id} - {ex.Message}");
                    }
                }
            }
            
            // Update history
            UpdateStatisticsHistory();
            
            // Track performance
            float collectionTime = Time.realtimeSinceStartup - startTime;
            m_TotalCollectionTime += collectionTime;
            m_CollectionCount++;
            m_UpdateCount++;
            m_LastUpdate_time = Time.time;
            
            if (enableLogging && m_UpdateCount % 100 == 0) {
                LogDebug($"Statistics collection - Time: {collectionTime:F4}s, Avg: {AverageCollectionTime:F4}s");
            }
        }
        
        /// <summary>
        /// Update the statistics history with current values
        /// </summary>
        private void UpdateStatisticsHistory() {
            foreach (var kvp in m_CurrentStatistics) {
                if (!m_StatisticsHistory.ContainsKey(kvp.Key)) {
                    m_StatisticsHistory[kvp.Key] = new List<float>();
                }
                
                var history = m_StatisticsHistory[kvp.Key];
                history.Add(kvp.Value);
                
                // Limit history length
                if (history.Count > maxHistoryLength) {
                    history.RemoveAt(0);
                }
            }
        }
        
        /// <summary>
        /// Get statistics that have changed since the last call
        /// </summary>
        public Dictionary<string, float> GetChangedStatistics() {
            var changedStats = new Dictionary<string, float>();
            
            foreach (var kvp in m_CurrentStatistics) {
                if (!m_LastReportedStatistics.ContainsKey(kvp.Key) || 
                    Mathf.Abs(m_LastReportedStatistics[kvp.Key] - kvp.Value) > 0.001f) {
                    changedStats[kvp.Key] = kvp.Value;
                }
            }
            
            // Update last reported statistics
            m_LastReportedStatistics = new Dictionary<string, float>(m_CurrentStatistics);
            
            return changedStats;
        }
        
        /// <summary>
        /// Called when an episode begins
        /// </summary>
        public void OnEpisodeBegin() {
            if (m_Context == null) return;
            
            foreach (var provider in m_StatisticsProviders) {
                if (provider.IsActive) {
                    try {
                        provider.OnEpisodeBegin(m_Context);
                    }
                    catch (Exception ex) {
                        Debug.LogError($"StatisticsCollector: Error in provider {provider.Id} OnEpisodeBegin - {ex.Message}");
                    }
                }
            }
            
            LogDebug("Episode begin - statistics reset");
        }
        
        /// <summary>
        /// Called when an episode ends
        /// </summary>
        public void OnEpisodeEnd() {
            if (m_Context == null) return;
            
            foreach (var provider in m_StatisticsProviders) {
                if (provider.IsActive) {
                    try {
                        provider.OnEpisodeEnd(m_Context);
                    }
                    catch (Exception ex) {
                        Debug.LogError($"StatisticsCollector: Error in provider {provider.Id} OnEpisodeEnd - {ex.Message}");
                    }
                }
            }
            
            LogDebug($"Episode end - Avg collection time: {AverageCollectionTime:F4}s");
        }
        
        /// <summary>
        /// Reset all statistics
        /// </summary>
        public void ResetStatistics() {
            m_CurrentStatistics.Clear();
            m_StatisticsHistory.Clear();
            m_LastReportedStatistics.Clear();
            m_UpdateCount = 0;
            m_TotalCollectionTime = 0f;
            m_CollectionCount = 0;
            
            foreach (var provider in m_StatisticsProviders) {
                try {
                    provider.ResetStatistics();
                }
                catch (Exception ex) {
                    Debug.LogError($"StatisticsCollector: Error resetting provider {provider.Id} - {ex.Message}");
                }
            }
            
            LogDebug("All statistics reset");
        }
        
        /// <summary>
        /// Enable or disable a specific statistics provider
        /// </summary>
        public void SetProviderActive(string providerId, bool active) {
            if (m_ProvidersById.TryGetValue(providerId, out IStatisticsProvider provider)) {
                provider.SetActive(active);
                LogDebug($"Set provider {providerId} active: {active}");
            }
        }
        
        /// <summary>
        /// Get statistics for a specific provider
        /// </summary>
        public Dictionary<string, float> GetProviderStatistics(string providerId) {
            if (m_ProvidersById.TryGetValue(providerId, out IStatisticsProvider provider)) {
                return provider.GetStatistics();
            }
            return new Dictionary<string, float>();
        }
        
        /// <summary>
        /// Get statistics history for a specific metric
        /// </summary>
        public List<float> GetStatisticsHistory(string metricName) {
            return m_StatisticsHistory.TryGetValue(metricName, out List<float> history) ? 
                   new List<float>(history) : new List<float>();
        }
        
        /// <summary>
        /// Export statistics to JSON format
        /// </summary>
        public string ExportToJSON() {
            var exportData = new StatisticsExportData {
                Timestamp = DateTime.UtcNow,
                EpisodeCount = m_Context?.EpisodeCount ?? 0,
                StepCount = m_Context?.StepCount ?? 0,
                Statistics = m_CurrentStatistics,
                History = m_StatisticsHistory
            };
            
            return JsonUtility.ToJson(exportData, true);
        }
        
        /// <summary>
        /// Get debug information about the statistics collector
        /// </summary>
        public string GetDebugInfo() {
            var sb = new StringBuilder();
            sb.AppendLine($"StatisticsCollector - Providers: {m_StatisticsProviders.Count}");
            sb.AppendLine($"Active: {m_StatisticsProviders.Count(p => p.IsActive)}, Updates: {m_UpdateCount}");
            sb.AppendLine($"Avg Collection Time: {AverageCollectionTime:F4}s");
            sb.AppendLine($"Current Stats Count: {m_CurrentStatistics.Count}");
            
            foreach (var kvp in m_CurrentStatistics.Take(5)) {
                sb.AppendLine($"  {kvp.Key}: {kvp.Value:F3}");
            }
            
            if (m_CurrentStatistics.Count > 5) {
                sb.AppendLine($"  ... and {m_CurrentStatistics.Count - 5} more");
            }
            
            return sb.ToString();
        }
        
        private void LogDebug(string message) {
            if (enableLogging) {
                Debug.Log($"[StatisticsCollector] {message}");
            }
        }
        
        // Data structure for JSON export
        [Serializable]
        private struct StatisticsExportData {
            public DateTime Timestamp;
            public int EpisodeCount;
            public float StepCount;
            public Dictionary<string, float> Statistics;
            public Dictionary<string, List<float>> History;
        }
    }
}