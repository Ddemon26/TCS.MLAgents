using System.Text;
using Unity.MLAgents.SideChannels;
using TCS.MLAgents.Core;

namespace TCS.MLAgents.Communication {
    /// <summary>
    /// Custom side channel for bidirectional command system
    /// </summary>
    public class CommandChannel : SideChannel {
        private const string k_CommandChannelId = "c1f0e3b2-4a8d-4e1c-9a2f-7b8e6d5f1c3a";
        private AgentContext m_Context;
        private Queue<Command> m_IncomingCommands;
        private Queue<CommandResponse> m_OutgoingResponses;
        
        // Event for when commands are received
        public event Action<Command> OnCommandReceived;
        
        public CommandChannel() : base() {
            ChannelId = new Guid(k_CommandChannelId);
            m_IncomingCommands = new Queue<Command>();
            m_OutgoingResponses = new Queue<CommandResponse>();
        }
        
        public void SetContext(AgentContext context) {
            m_Context = context;
        }
        
        /// <summary>
        /// Send a command to Python
        /// </summary>
        public void SendCommand(string commandName, object data = null) {
            try {
                var command = new Command {
                    Id = Guid.NewGuid().ToString(),
                    Name = commandName,
                    Data = data != null ? JsonUtility.ToJson(data) : null,
                    Timestamp = Time.time,
                    Context = m_Context?.ToString() ?? "No context"
                };
                
                var json = JsonUtility.ToJson(command);
                var bytes = Encoding.UTF8.GetBytes(json);
                
                var outgoingMessage = new OutgoingMessage();
                outgoingMessage.SetRawBytes(bytes);
                QueueMessageToSend(outgoingMessage);
            }
            catch (Exception ex) {
                Debug.LogError($"CommandChannel: Failed to send command - {ex.Message}");
            }
        }
        
        /// <summary>
        /// Send a response to a command
        /// </summary>
        public void SendResponse(string commandId, bool success, string message = null, object data = null) {
            try {
                var response = new CommandResponse {
                    CommandId = commandId,
                    Success = success,
                    Message = message,
                    Data = data != null ? JsonUtility.ToJson(data) : null,
                    Timestamp = Time.time
                };
                
                var json = JsonUtility.ToJson(response);
                var bytes = Encoding.UTF8.GetBytes(json);
                
                var outgoingMessage = new OutgoingMessage();
                outgoingMessage.SetRawBytes(bytes);
                QueueMessageToSend(outgoingMessage);
            }
            catch (Exception ex) {
                Debug.LogError($"CommandChannel: Failed to send response - {ex.Message}");
            }
        }
        
        /// <summary>
        /// Check if there are incoming commands
        /// </summary>
        public bool HasCommands => m_IncomingCommands.Count > 0;
        
        /// <summary>
        /// Get the next command in the queue
        /// </summary>
        public Command? GetNextCommand() {
            return m_IncomingCommands.Count > 0 ? m_IncomingCommands.Dequeue() : (Command?)null;
        }
        
        protected override void OnMessageReceived(IncomingMessage msg) {
            try {
                var data = msg.GetRawBytes();
                var json = Encoding.UTF8.GetString(data);
                
                // Try to deserialize as a command first
                try {
                    var command = JsonUtility.FromJson<Command>(json);
                    if (!string.IsNullOrEmpty(command.Name)) {
                        m_IncomingCommands.Enqueue(command);
                        OnCommandReceived?.Invoke(command);
                        return;
                    }
                }
                catch {
                    // Not a command, try as response
                }
                
                // Try to deserialize as a response
                try {
                    var response = JsonUtility.FromJson<CommandResponse>(json);
                    if (!string.IsNullOrEmpty(response.CommandId)) {
                        m_OutgoingResponses.Enqueue(response);
                        Debug.Log($"CommandChannel: Received response for command {response.CommandId} - Success: {response.Success}");
                        return;
                    }
                }
                catch {
                    // Not a response either
                }
                
                // If we get here, we couldn't parse the message
                Debug.LogWarning($"CommandChannel: Received unknown message format: {json}");
            }
            catch (Exception ex) {
                Debug.LogError($"CommandChannel: Error processing incoming message - {ex.Message}");
            }
        }
        
        // Command structure
        [Serializable]
        public struct Command {
            public string Id;
            public string Name;
            public string Data;
            public float Timestamp;
            public string Context;
        }
        
        // Command response structure
        [Serializable]
        public struct CommandResponse {
            public string CommandId;
            public bool Success;
            public string Message;
            public string Data;
            public float Timestamp;
        }
    }
}