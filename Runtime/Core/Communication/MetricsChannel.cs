using System.Text;
using Unity.MLAgents.SideChannels;
using TCS.MLAgents.Core;

namespace TCS.MLAgents.Communication {
    /// <summary>
    /// Custom side channel for performance metrics communication
    /// </summary>
    public class MetricsChannel : SideChannel {
        private const string k_MetricsChannelId = "a8bda2a4-2e5e-4f2d-9d3b-6c8e7c111a9f";
        private AgentContext m_Context;
        private Dictionary<string, float> m_Metrics;
        
        public MetricsChannel() : base() {
            ChannelId = new Guid(k_MetricsChannelId);
            m_Metrics = new Dictionary<string, float>();
        }
        
        public void SetContext(AgentContext context) {
            m_Context = context;
        }
        
        /// <summary>
        /// Record a metric value
        /// </summary>
        public void RecordMetric(string name, float value) {
            m_Metrics[name] = value;
        }
        
        /// <summary>
        /// Record multiple metrics at once
        /// </summary>
        public void RecordMetrics(Dictionary<string, float> metrics) {
            foreach (var kvp in metrics) {
                m_Metrics[kvp.Key] = kvp.Value;
            }
        }
        
        /// <summary>
        /// Send all recorded metrics to Python
        /// </summary>
        public void SendMetrics() {
            try {
                var metricsData = new MetricsData {
                    Metrics = new Dictionary<string, float>(m_Metrics),
                    Timestamp = Time.time,
                    EpisodeCount = m_Context?.EpisodeCount ?? 0,
                    StepCount = (int)(m_Context?.StepCount ?? 0)
                };
                
                var json = JsonUtility.ToJson(metricsData);
                var bytes = Encoding.UTF8.GetBytes(json);
                
                var outgoingMessage = new OutgoingMessage();
                outgoingMessage.SetRawBytes(bytes);
                QueueMessageToSend(outgoingMessage);
                
                // Clear metrics after sending
                m_Metrics.Clear();
            }
            catch (Exception ex) {
                Debug.LogError($"MetricsChannel: Failed to send metrics - {ex.Message}");
            }
        }
        
        /// <summary>
        /// Send metrics periodically
        /// </summary>
        public void SendMetricsPeriodically(float interval) {
            // This would typically be called from a coroutine or Update method
            // For simplicity, we'll just send immediately in this example
            SendMetrics();
        }
        
        protected override void OnMessageReceived(IncomingMessage msg) {
            // Python can send metric configuration or requests
            try {
                var data = msg.GetRawBytes();
                var json = Encoding.UTF8.GetString(data);
                var config = JsonUtility.FromJson<MetricsConfig>(json);
                
                // Process configuration if needed
                Debug.Log($"MetricsChannel received configuration: {json}");
            }
            catch (Exception ex) {
                Debug.LogError($"MetricsChannel: Error processing incoming message - {ex.Message}");
            }
        }
        
        // Metrics data structure
        [Serializable]
        private struct MetricsData {
            public Dictionary<string, float> Metrics;
            public float Timestamp;
            public int EpisodeCount;
            public int StepCount;
        }
        
        // Metrics configuration structure
        [Serializable]
        private struct MetricsConfig {
            public bool EnableDetailedMetrics;
            public float ReportingInterval;
            public string[] MonitoredMetrics;
        }
    }
}