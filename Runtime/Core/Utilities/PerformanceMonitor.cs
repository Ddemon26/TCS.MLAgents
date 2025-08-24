using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.Profiling;
#endif
using TCS.MLAgents.Core;
using TCS.MLAgents.Interfaces;

namespace TCS.MLAgents.Utilities {
    /// <summary>
    /// Performance monitoring component that tracks agent and system performance metrics.
    /// </summary>
    public class PerformanceMonitor : MonoBehaviour, TCS.MLAgents.Interfaces.IStatisticsProvider {
        [Header("Performance Configuration")]
        [SerializeField] private string m_Id = "performance";
        [SerializeField] private int m_Priority = 50;
        [SerializeField] private bool m_IsActive = true;
        [SerializeField] private float m_SamplingInterval = 0.5f; // Sample every 0.5 seconds
        
        // Performance metrics
        private float m_LastSampleTime = 0f;
        private int m_FrameCount = 0;
        private int m_LastFrameCount = 0;
        private float m_LastDeltaTime = 0f;
        private float m_AccumulatedDeltaTime = 0f;
        private int m_SampleCount = 0;
        
        // FPS tracking
        private float m_CurrentFPS = 0f;
        private float m_AverageFPS = 0f;
        private float m_MinFPS = float.MaxValue;
        private float m_MaxFPS = 0f;
        
        // Memory tracking
        private long m_LastMonoUsedSize = 0;
        private long m_LastTotalUsedSize = 0;
        private long m_PeakMonoUsedSize = 0;
        private long m_PeakTotalUsedSize = 0;
        
        // Component performance
        private Dictionary<string, float> m_ComponentExecutionTimes;
        private Dictionary<string, int> m_ComponentCallCounts;
        
        // Agent context
        private AgentContext m_Context;
        
        // Statistics storage
        private Dictionary<string, float> m_Statistics;
        private Dictionary<string, float> m_ChangedStatistics;
        
        public string Id => m_Id;
        public int Priority => m_Priority;
        public bool IsActive => m_IsActive;
        public float CurrentFPS => m_CurrentFPS;
        public float AverageFPS => m_AverageFPS;
        public float MinFPS => m_MinFPS;
        public float MaxFPS => m_MaxFPS;
        
        void Awake() {
            m_ComponentExecutionTimes = new Dictionary<string, float>();
            m_ComponentCallCounts = new Dictionary<string, int>();
            m_Statistics = new Dictionary<string, float>();
            m_ChangedStatistics = new Dictionary<string, float>();
        }
        
        public void Initialize(AgentContext context) {
            m_Context = context;
            ResetStatistics();
            
            LogDebug($"PerformanceMonitor {m_Id} initialized");
        }
        
        void Update() {
            // Update frame counters
            m_FrameCount++;
            m_LastDeltaTime = Time.deltaTime;
            m_AccumulatedDeltaTime += Time.deltaTime;
            m_SampleCount++;
            
            // Sample performance metrics at intervals
            if (Time.time - m_LastSampleTime >= m_SamplingInterval) {
                UpdatePerformanceMetrics();
                m_LastSampleTime = Time.time;
            }
            
            // Update statistics dictionary
            UpdateStatisticsDictionary();
        }
        
        public void CollectStatistics(AgentContext context, float deltaTime) {
            // For MonoBehaviour version, statistics are collected in Update()
            // This method is for interface compatibility
        }
        
        private void UpdatePerformanceMetrics() {
            // Calculate FPS
            if (m_AccumulatedDeltaTime > 0f) {
                float fps = (m_FrameCount - m_LastFrameCount) / m_AccumulatedDeltaTime;
                m_CurrentFPS = fps;
                
                // Update FPS tracking
                if (fps < m_MinFPS) m_MinFPS = fps;
                if (fps > m_MaxFPS) m_MaxFPS = fps;
                
                // Update average FPS
                m_AverageFPS = (m_AverageFPS * (m_SampleCount - 1) + fps) / m_SampleCount;
            }
            
            // Update memory tracking
            UpdateMemoryStatistics();
            
            // Reset frame counters for next sample
            m_LastFrameCount = m_FrameCount;
            m_AccumulatedDeltaTime = 0f;
        }
        
        private void UpdateMemoryStatistics() {
            // Get current memory usage
            long monoUsedSize = GC.GetTotalMemory(false);
#if UNITY_EDITOR
            long totalUsedSize = Profiler.GetTotalAllocatedMemoryLong();
#else
            long totalUsedSize = monoUsedSize; // Fallback for builds
#endif
            
            // Update peak memory usage
            if (monoUsedSize > m_PeakMonoUsedSize) m_PeakMonoUsedSize = monoUsedSize;
            if (totalUsedSize > m_PeakTotalUsedSize) m_PeakTotalUsedSize = totalUsedSize;
            
            // Store current values
            m_LastMonoUsedSize = monoUsedSize;
            m_LastTotalUsedSize = totalUsedSize;
        }
        
        private void UpdateStatisticsDictionary() {
            // Store previous values to detect changes
            var previousStats = new Dictionary<string, float>(m_Statistics);
            
            // Clear changed statistics
            m_ChangedStatistics.Clear();
            
            // Update statistics
            m_Statistics["fps.current"] = m_CurrentFPS;
            m_Statistics["fps.average"] = m_AverageFPS;
            m_Statistics["fps.min"] = m_MinFPS;
            m_Statistics["fps.max"] = m_MaxFPS;
            m_Statistics["delta_time"] = m_LastDeltaTime;
            m_Statistics["memory.mono_used"] = m_LastMonoUsedSize;
            m_Statistics["memory.total_used"] = m_LastTotalUsedSize;
            m_Statistics["memory.peak_mono"] = m_PeakMonoUsedSize;
            m_Statistics["memory.peak_total"] = m_PeakTotalUsedSize;
            m_Statistics["frame_count"] = m_FrameCount;
            
            // Add component performance metrics if available
            foreach (var kvp in m_ComponentExecutionTimes) {
                string key = $"component.{kvp.Key}.time";
                m_Statistics[key] = kvp.Value;
                
                if (m_ComponentCallCounts.TryGetValue(kvp.Key, out int callCount)) {
                    string countKey = $"component.{kvp.Key}.calls";
                    m_Statistics[countKey] = callCount;
                }
            }
            
            // Detect changed statistics
            foreach (var kvp in m_Statistics) {
                if (!previousStats.ContainsKey(kvp.Key) || 
                    Mathf.Abs(previousStats[kvp.Key] - kvp.Value) > 0.001f) {
                    m_ChangedStatistics[kvp.Key] = kvp.Value;
                }
            }
        }
        
        public void OnEpisodeBegin(AgentContext context) {
            // Reset per-episode statistics
            m_MinFPS = float.MaxValue;
            m_MaxFPS = 0f;
            m_AverageFPS = 0f;
            m_SampleCount = 0;
            m_FrameCount = 0;
            m_LastFrameCount = 0;
            m_AccumulatedDeltaTime = 0f;
            
            LogDebug($"PerformanceMonitor {m_Id} episode begin");
        }
        
        public void OnEpisodeEnd(AgentContext context) {
            LogDebug($"PerformanceMonitor {m_Id} episode end - Avg FPS: {m_AverageFPS:F2}");
        }
        
        public Dictionary<string, float> GetStatistics() {
            return new Dictionary<string, float>(m_Statistics);
        }
        
        public Dictionary<string, float> GetChangedStatistics() {
            return new Dictionary<string, float>(m_ChangedStatistics);
        }
        
        public void ResetStatistics() {
            m_Statistics.Clear();
            m_ChangedStatistics.Clear();
            m_ComponentExecutionTimes.Clear();
            m_ComponentCallCounts.Clear();
            
            m_CurrentFPS = 0f;
            m_AverageFPS = 0f;
            m_MinFPS = float.MaxValue;
            m_MaxFPS = 0f;
            
            m_LastMonoUsedSize = 0;
            m_LastTotalUsedSize = 0;
            m_PeakMonoUsedSize = 0;
            m_PeakTotalUsedSize = 0;
            
            m_FrameCount = 0;
            m_LastFrameCount = 0;
            m_AccumulatedDeltaTime = 0f;
            m_SampleCount = 0;
        }
        
        public void SetActive(bool active) {
            m_IsActive = active;
            LogDebug($"PerformanceMonitor {m_Id} set active: {active}");
        }
        
        // Component performance tracking methods
        public void BeginComponentTiming(string componentName) {
            if (!m_IsActive) return;
            
            // In a real implementation, we would start timing here
            // For now, we'll just increment the call count
            if (!m_ComponentCallCounts.ContainsKey(componentName)) {
                m_ComponentCallCounts[componentName] = 0;
            }
            m_ComponentCallCounts[componentName]++;
        }
        
        public void EndComponentTiming(string componentName, float executionTime) {
            if (!m_IsActive) return;
            
            // Accumulate execution time for this component
            if (!m_ComponentExecutionTimes.ContainsKey(componentName)) {
                m_ComponentExecutionTimes[componentName] = 0f;
            }
            m_ComponentExecutionTimes[componentName] += executionTime;
        }
        
        // Memory management methods
        public void RecordMemoryAllocation(string allocationType, long size) {
            if (!m_IsActive) return;
            
            // Record memory allocation statistics
            string key = $"memory.allocations.{allocationType}";
            if (!m_Statistics.ContainsKey(key)) {
                m_Statistics[key] = 0;
            }
            m_Statistics[key] += size;
        }
        
        public void RecordGarbageCollection() {
            if (!m_IsActive) return;
            
            // Record garbage collection event
            if (!m_Statistics.ContainsKey("gc.count")) {
                m_Statistics["gc.count"] = 0;
            }
            m_Statistics["gc.count"]++;
        }
        
        public string GetDebugInfo() {
            var sb = new StringBuilder();
            sb.AppendLine($"PerformanceMonitor[{m_Id}] - Active: {m_IsActive}");
            sb.AppendLine($"FPS: Current={m_CurrentFPS:F2}, Avg={m_AverageFPS:F2}");
            sb.AppendLine($"Memory: Mono={m_LastMonoUsedSize / (1024*1024):F2}MB, Total={m_LastTotalUsedSize / (1024*1024):F2}MB");
            sb.AppendLine($"Frames: {m_FrameCount}, Samples: {m_SampleCount}");
            
            return sb.ToString();
        }
        
        private void LogDebug(string message) {
            Debug.Log($"[PerformanceMonitor({m_Id})] {message}");
        }
    }
}