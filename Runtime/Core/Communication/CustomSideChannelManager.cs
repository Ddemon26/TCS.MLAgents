using TCS.MLAgents.Communication;
using TCS.MLAgents.Core;
using TCS.MLAgents.Interfaces;

/// <summary>
/// Manages custom side channels for communication between Unity and Python
/// </summary>
public class CustomSideChannelManager : MonoBehaviour {
    [Header( "Channel Configuration" )]
    [SerializeField] private bool enableLogging = true;
    [SerializeField] private bool enableMetrics = true;
    [SerializeField] private bool enableCommands = true;

    // Channel instances
    private LoggingChannel m_LoggingChannel;
    private MetricsChannel m_MetricsChannel;
    private CommandChannel m_CommandChannel;

    // Registered communicators
    private Dictionary<string, IMLCommunicator> m_Communicators;

    // Agent context reference
    private AgentContext m_Context;

    public LoggingChannel LoggingChannel => m_LoggingChannel;
    public MetricsChannel MetricsChannel => m_MetricsChannel;
    public CommandChannel CommandChannel => m_CommandChannel;

    private void Awake() {
        m_Communicators = new Dictionary<string, IMLCommunicator>();
        InitializeChannels();
    }

    private void InitializeChannels() {
        try {
            if ( enableLogging ) {
                m_LoggingChannel = new LoggingChannel();
                Unity.MLAgents.SideChannels.SideChannelManager.RegisterSideChannel( m_LoggingChannel );
            }

            if ( enableMetrics ) {
                m_MetricsChannel = new MetricsChannel();
                Unity.MLAgents.SideChannels.SideChannelManager.RegisterSideChannel( m_MetricsChannel );
            }

            if ( enableCommands ) {
                m_CommandChannel = new CommandChannel();
                Unity.MLAgents.SideChannels.SideChannelManager.RegisterSideChannel( m_CommandChannel );
            }

            Debug.Log( "CustomSideChannelManager: Channels initialized successfully" );
        }
        catch (Exception ex) {
            Debug.LogError( $"CustomSideChannelManager: Failed to initialize channels - {ex.Message}" );
        }
    }

    public void SetContext(AgentContext context) {
        m_Context = context;

        // Pass context to channels that need it
        if ( m_LoggingChannel != null ) {
            m_LoggingChannel.SetContext( context );
        }

        if ( m_MetricsChannel != null ) {
            m_MetricsChannel.SetContext( context );
        }

        if ( m_CommandChannel != null ) {
            m_CommandChannel.SetContext( context );
        }
    }

    /// <summary>
    /// Register a communicator with the manager
    /// </summary>
    public void RegisterCommunicator(IMLCommunicator communicator) {
        if ( communicator == null ) {
            Debug.LogWarning( "CustomSideChannelManager: Attempted to register null communicator" );
            return;
        }

        if ( m_Communicators.ContainsKey( communicator.Id ) ) {
            Debug.LogWarning( $"CustomSideChannelManager: Communicator with ID {communicator.Id} already registered" );
            return;
        }

        m_Communicators[communicator.Id] = communicator;
        communicator.Initialize();

        if ( enableLogging && m_LoggingChannel != null ) {
            m_LoggingChannel.LogInfo( $"Registered communicator: {communicator.Id}" );
        }
    }

    /// <summary>
    /// Unregister a communicator from the manager
    /// </summary>
    public void UnregisterCommunicator(string id) {
        if ( string.IsNullOrEmpty( id ) ) {
            Debug.LogWarning( "CustomSideChannelManager: Attempted to unregister communicator with null/empty ID" );
            return;
        }

        if ( m_Communicators.TryGetValue( id, out IMLCommunicator communicator ) ) {
            communicator.Dispose();
            m_Communicators.Remove( id );

            if ( enableLogging && m_LoggingChannel != null ) {
                m_LoggingChannel.LogInfo( $"Unregistered communicator: {id}" );
            }
        }
    }

    /// <summary>
    /// Get a registered communicator by ID
    /// </summary>
    public T GetCommunicator<T>(string id) where T : class, IMLCommunicator {
        if ( m_Communicators.TryGetValue( id, out IMLCommunicator communicator ) ) {
            return communicator as T;
        }

        return null;
    }

    /// <summary>
    /// Send data through a specific communicator
    /// </summary>
    public void SendData<T>(string communicatorId, T data) {
        var communicator = GetCommunicator<IMLCommunicator>( communicatorId );
        if ( communicator != null ) {
            communicator.SendData( data );
        }
        else {
            Debug.LogWarning( $"CustomSideChannelManager: Communicator {communicatorId} not found" );
        }
    }

    /// <summary>
    /// Broadcast data to all registered communicators
    /// </summary>
    public void BroadcastData<T>(T data) {
        foreach (var kvp in m_Communicators) {
            try {
                kvp.Value.SendData( data );
            }
            catch (Exception ex) {
                Debug.LogError( $"CustomSideChannelManager: Error broadcasting to {kvp.Key} - {ex.Message}" );
            }
        }
    }

    private void Update() {
        // Process incoming data from all communicators
        foreach (var kvp in m_Communicators) {
            try {
                if ( kvp.Value.HasData ) {
                    var data = kvp.Value.ReceiveData<object>();
                    // Fix: Use the proper event invocation
                    var handler = kvp.Value.OnDataReceived;
                    if ( handler != null ) {
                        handler( data );
                    }
                }
            }
            catch (Exception ex) {
                Debug.LogError( $"CustomSideChannelManager: Error updating {kvp.Key} - {ex.Message}" );
            }
        }
    }

    private void OnDestroy() {
        Cleanup();
    }

    private void OnApplicationQuit() {
        Cleanup();
    }

    private void Cleanup() {
        // Unregister all side channels
        if ( m_LoggingChannel != null ) {
            try {
                Unity.MLAgents.SideChannels.SideChannelManager.UnregisterSideChannel( m_LoggingChannel );
            }
            catch (Exception ex) {
                Debug.LogError( $"CustomSideChannelManager: Error unregistering LoggingChannel - {ex.Message}" );
            }
        }

        if ( m_MetricsChannel != null ) {
            try {
                Unity.MLAgents.SideChannels.SideChannelManager.UnregisterSideChannel( m_MetricsChannel );
            }
            catch (Exception ex) {
                Debug.LogError( $"CustomSideChannelManager: Error unregistering MetricsChannel - {ex.Message}" );
            }
        }

        if ( m_CommandChannel != null ) {
            try {
                Unity.MLAgents.SideChannels.SideChannelManager.UnregisterSideChannel( m_CommandChannel );
            }
            catch (Exception ex) {
                Debug.LogError( $"CustomSideChannelManager: Error unregistering CommandChannel - {ex.Message}" );
            }
        }

        // Dispose all communicators
        foreach (var kvp in m_Communicators) {
            try {
                kvp.Value.Dispose();
            }
            catch (Exception ex) {
                Debug.LogError( $"CustomSideChannelManager: Error disposing {kvp.Key} - {ex.Message}" );
            }
        }

        m_Communicators.Clear();
    }
}