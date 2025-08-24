namespace TCS.MLAgents.Interfaces {
    /// <summary>
    /// Interface for ML communication systems that handle data exchange between Unity and Python
    /// </summary>
    public interface IMLCommunicator : IDisposable {
        /// <summary>
        /// Unique identifier for this communicator
        /// </summary>
        string Id { get; }
        
        /// <summary>
        /// Initialize the communicator
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Send data to Python
        /// </summary>
        /// <param name="data">Data to send</param>
        void SendData<T>(T data);
        
        /// <summary>
        /// Receive data from Python
        /// </summary>
        /// <returns>Received data</returns>
        T ReceiveData<T>();
        
        /// <summary>
        /// Check if there is data available to receive
        /// </summary>
        bool HasData { get; }
        
        /// <summary>
        /// Event triggered when data is received
        /// </summary>
        Action<object> OnDataReceived { get; set; }
    }
}