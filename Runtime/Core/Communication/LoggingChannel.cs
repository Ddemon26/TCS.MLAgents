using System.Text;
using Unity.MLAgents.SideChannels;
using TCS.MLAgents.Core;

namespace TCS.MLAgents.Communication {
    /// <summary>
    /// Custom side channel for structured logging between Unity and Python
    /// </summary>
    public class LoggingChannel : SideChannel {
        private const string k_LogChannelId = "5405af75-60ac-4399-9640-91459600d584";
        private AgentContext m_Context;
        
        public LoggingChannel() : base() {
            ChannelId = new Guid(k_LogChannelId);
        }
        
        public void SetContext(AgentContext context) {
            m_Context = context;
        }
        
        /// <summary>
        /// Log an info message
        /// </summary>
        public void LogInfo(string message) {
            SendLogMessage(LogLevel.Info, message);
        }
        
        /// <summary>
        /// Log a warning message
        /// </summary>
        public void LogWarning(string message) {
            SendLogMessage(LogLevel.Warning, message);
        }
        
        /// <summary>
        /// Log an error message
        /// </summary>
        public void LogError(string message) {
            SendLogMessage(LogLevel.Error, message);
        }
        
        /// <summary>
        /// Log a debug message
        /// </summary>
        public void LogDebug(string message) {
            SendLogMessage(LogLevel.Debug, message);
        }
        
        private void SendLogMessage(LogLevel level, string message) {
            try {
                // Create log entry with timestamp and context
                var logEntry = new LogEntry {
                    Level = level,
                    Message = message,
                    Timestamp = Time.time,
                    FrameCount = Time.frameCount,
                    Context = m_Context?.ToString() ?? "No context"
                };
                
                // Serialize and send
                var json = JsonUtility.ToJson(logEntry);
                var bytes = Encoding.UTF8.GetBytes(json);
                
                var outgoingMessage = new OutgoingMessage();
                outgoingMessage.SetRawBytes(bytes);
                QueueMessageToSend(outgoingMessage);
            }
            catch (Exception ex) {
                Debug.LogError($"LoggingChannel: Failed to send log message - {ex.Message}");
            }
        }
        
        protected override void OnMessageReceived(IncomingMessage msg) {
            // Python can send log configuration or commands back
            try {
                var data = msg.GetRawBytes();
                var json = Encoding.UTF8.GetString(data);
                Debug.Log($"LoggingChannel received: {json}");
            }
            catch (Exception ex) {
                Debug.LogError($"LoggingChannel: Error processing incoming message - {ex.Message}");
            }
        }
        
        // Log entry structure
        [Serializable]
        private struct LogEntry {
            public LogLevel Level;
            public string Message;
            public float Timestamp;
            public int FrameCount;
            public string Context;
        }
        
        // Log levels
        public enum LogLevel {
            Debug = 0,
            Info = 1,
            Warning = 2,
            Error = 3
        }
    }
}