# TCS ML-Agents API Documentation

## Overview

The TCS ML-Agents library is a composition-based framework for creating machine learning agents in Unity. Unlike traditional inheritance-based approaches, this system uses component composition and interfaces to provide maximum flexibility and modularity.

## Core Concepts

### 1. Composition Over Inheritance
Instead of extending base classes, agents are composed of multiple specialized components that work together through well-defined interfaces.

### 2. Single Responsibility Principle
Each component has one specific job, making the system easier to understand, test, and extend.

### 3. Dependency Injection
Components communicate through interfaces rather than direct references, promoting loose coupling and testability.

### 4. Configuration-Driven
Most components can be configured through ScriptableObjects, allowing for easy tweaking without code changes.

## Architecture Layers

### 1. Agent Composition Layer
- **MLAgentComposer**: Bridges Unity ML-Agents with the composition system
- **AgentContext**: Shared state container for all agent components
- **IMLAgent**: Core interface defining agent lifecycle methods

### 2. Observation Layer
- **VectorObservationCollector**: Collects and manages vector observations
- **IObservationProvider**: Interface for components that provide observations
- **Built-in Providers**: Transform, Velocity, Vision, etc.

### 3. Action Layer
- **ActionDistributor**: Routes actions to appropriate receivers
- **IActionReceiver**: Interface for components that handle actions
- **Built-in Receivers**: Movement, Rotation, Discrete actions

### 4. Reward Layer
- **RewardCalculator**: Aggregates and manages reward calculations
- **IRewardProvider**: Interface for components that calculate rewards
- **Built-in Providers**: Proximity, Time, Boundary, Task Completion

### 5. Episode Management Layer
- **EpisodeManager**: Manages episode state and transitions
- **IEpisodeHandler**: Interface for episode lifecycle handling
- **Built-in Handlers**: Position Reset, State Reset, Environment Reset

### 6. Sensor Layer
- **SensorManager**: Manages sensor registration and lifecycle
- **ISensorProvider**: Interface for custom sensors
- **Built-in Providers**: Raycast, Camera, Collider sensors

### 7. Decision Layer
- **DecisionRouter**: Routes between inference/heuristic modes
- **IDecisionProvider**: Interface for heuristic/decision logic
- **HeuristicController**: Manual/scripted control for testing

### 8. Communication Layer
- **SideChannelManager**: Manages custom side channels
- **IMLCommunicator**: Interface for ML communication
- **Built-in Channels**: Logging, Metrics, Command

### 9. Statistics & Monitoring Layer
- **StatisticsCollector**: Aggregates and reports statistics
- **IStatisticsProvider**: Interface for statistics collection
- **PerformanceMonitor**: Training performance monitoring

### 10. Configuration Layer
- **MLBehaviorConfig**: Complete behavior configuration
- **BehaviorApplicator**: Applies configuration to agent components
- **ConfigurationValidator**: Validates configurations

## Core Interfaces

### IMLAgent
```csharp
public interface IMLAgent
{
    AgentContext Context { get; }
    void Initialize();
    void OnEpisodeBegin();
    void CollectObservations(VectorSensor sensor);
    void OnActionReceived(ActionBuffers actionBuffers);
    void Heuristic(in ActionBuffers actionsOut);
    void FixedUpdate();
    void OnDestroy();
    void EndEpisode();
    void AddReward(float reward);
    void SetReward(float reward);
}
```

### IObservationProvider
```csharp
public interface IObservationProvider
{
    string ProviderName { get; }
    int Priority { get; }
    bool IsActive { get; }
    int ObservationSize { get; }
    
    void Initialize(AgentContext context);
    bool ValidateProvider(AgentContext context);
    void CollectObservations(VectorSensor sensor, AgentContext context);
    void OnEpisodeBegin(AgentContext context);
    void OnEpisodeEnd(AgentContext context);
    void OnUpdate(AgentContext context, float deltaTime);
    void SetActive(bool active);
    string GetDebugInfo();
}
```

### IActionReceiver
```csharp
public interface IActionReceiver
{
    string ReceiverName { get; }
    int Priority { get; }
    bool IsActive { get; }
    int ContinuousActionCount { get; }
    int DiscreteActionBranchCount { get; }
    int[] DiscreteActionBranchSizes { get; }
    
    void Initialize(AgentContext context);
    bool ValidateReceiver(AgentContext context);
    void ReceiveContinuousActions(float[] actions, int startIndex, AgentContext context);
    void ReceiveDiscreteActions(int[] actions, int startIndex, AgentContext context);
    void ProvideHeuristicActions(float[] continuousOut, int[] discreteOut, 
        int continuousStartIndex, int discreteStartIndex, AgentContext context);
    void OnEpisodeBegin(AgentContext context);
    void FixedUpdate(AgentContext context);
    void SetActive(bool active);
    string GetDebugInfo();
}
```

### IRewardProvider
```csharp
public interface IRewardProvider
{
    string ProviderName { get; }
    int Priority { get; }
    bool IsActive { get; }
    float RewardWeight { get; }
    
    void Initialize(AgentContext context);
    bool ValidateProvider(AgentContext context);
    float CalculateReward(AgentContext context, float deltaTime);
    void OnEpisodeBegin(AgentContext context);
    void OnEpisodeEnd(AgentContext context);
    void OnUpdate(AgentContext context, float deltaTime);
    void OnRewardEvent(string eventName, AgentContext context, object eventData = null);
    void SetActive(bool active);
    string GetDebugInfo();
}
```

### IEpisodeHandler
```csharp
public interface IEpisodeHandler
{
    string HandlerName { get; }
    int Priority { get; }
    bool IsActive { get; }
    
    void Initialize(AgentContext context);
    bool ValidateHandler(AgentContext context);
    bool ShouldStartEpisode(AgentContext context);
    bool ShouldEndEpisode(AgentContext context);
    void OnEpisodeBegin(AgentContext context);
    void OnEpisodeEnd(AgentContext context, EpisodeEndReason reason);
    void OnEpisodeUpdate(AgentContext context, float deltaTime);
    void Reset();
    void SetActive(bool active);
    string GetDebugInfo();
}
```

### ISensorProvider
```csharp
public interface ISensorProvider
{
    string SensorName { get; }
    int Priority { get; }
    bool IsActive { get; }
    
    void Initialize(AgentContext context);
    bool ValidateProvider(AgentContext context);
    void UpdateSensor(AgentContext context, float deltaTime);
    void OnEpisodeBegin(AgentContext context);
    void OnEpisodeEnd(AgentContext context);
    void SetActive(bool active);
    string GetDebugInfo();
}
```

### IDecisionProvider
```csharp
public interface IDecisionProvider
{
    string Id { get; }
    int Priority { get; }
    bool IsActive { get; }
    
    void Initialize(AgentContext context);
    bool ShouldDecide(AgentContext context, List<ISensor> sensors);
    void DecideAction(AgentContext context, List<ISensor> sensors, ActionBuffers actions);
    void OnEpisodeBegin(AgentContext context);
    void OnEpisodeEnd(AgentContext context);
    void OnUpdate(AgentContext context, float deltaTime);
    void SetActive(bool active);
    string GetDebugInfo();
}
```

### IStatisticsProvider
```csharp
public interface IStatisticsProvider
{
    string Id { get; }
    int Priority { get; }
    bool IsActive { get; }
    
    void Initialize(AgentContext context);
    void CollectStatistics(AgentContext context, float deltaTime);
    void OnEpisodeBegin(AgentContext context);
    void OnEpisodeEnd(AgentContext context);
    Dictionary<string, float> GetStatistics();
    Dictionary<string, float> GetChangedStatistics();
    void ResetStatistics();
    void SetActive(bool active);
    string GetDebugInfo();
}
```

## Component Reference

### MLAgentComposer
The core component that bridges Unity ML-Agents with the composition system.

**Key Methods:**
- `Initialize()`: Initializes the composition system
- `RegisterComponent(IMLAgent component)`: Registers an agent component
- `GetAgentComponent<T>()`: Retrieves a specific agent component
- `OnEpisodeBegin()`: Notifies all components of episode start

### VectorObservationCollector
Manages the collection of vector observations from multiple providers.

**Key Methods:**
- `RegisterProvider(IObservationProvider provider)`: Adds an observation provider
- `UnregisterProvider(IObservationProvider provider)`: Removes an observation provider
- `CollectObservations(VectorSensor sensor, AgentContext context)`: Collects observations from all providers

### ActionDistributor
Routes actions from the neural network to appropriate receivers.

**Key Methods:**
- `RegisterReceiver(IActionReceiver receiver)`: Adds an action receiver
- `UnregisterReceiver(IActionReceiver receiver)`: Removes an action receiver
- `OnActionReceived(ActionBuffers actionBuffers)`: Distributes actions to receivers

### RewardCalculator
Aggregates rewards from multiple providers and applies them to the agent.

**Key Methods:**
- `RegisterProvider(IRewardProvider provider)`: Adds a reward provider
- `UnregisterProvider(IRewardProvider provider)`: Removes a reward provider
- `CalculateStepRewards()`: Calculates and applies rewards for the current step

### EpisodeManager
Manages episode lifecycle and coordinates multiple episode handlers.

**Key Methods:**
- `RegisterHandler(IEpisodeHandler handler)`: Adds an episode handler
- `UnregisterHandler(IEpisodeHandler handler)`: Removes an episode handler
- `StartEpisode()`: Begins a new episode
- `EndEpisode()`: Ends the current episode

### SensorManager
Manages sensor registration and lifecycle for custom sensors.

**Key Methods:**
- `RegisterProvider(ISensorProvider provider)`: Adds a sensor provider
- `UnregisterProvider(ISensorProvider provider)`: Removes a sensor provider
- `UpdateSensors(float deltaTime)`: Updates all active sensors

### DecisionRouter
Routes between inference and heuristic decision modes.

**Key Methods:**
- `AddDecisionProvider(IDecisionProvider provider)`: Adds a decision provider
- `RemoveDecisionProvider(string id)`: Removes a decision provider
- `SetDecisionMode(DecisionMode mode, string providerId)`: Sets the decision mode

### StatisticsCollector
Aggregates and reports statistics from multiple providers.

**Key Methods:**
- `AddStatisticsProvider(IStatisticsProvider provider)`: Adds a statistics provider
- `RemoveStatisticsProvider(string id)`: Removes a statistics provider
- `CollectStatistics(float deltaTime)`: Collects statistics from all providers

## Configuration

### MLBehaviorConfig
ScriptableObject that defines complete agent behavior configuration.

**Key Properties:**
- `BehaviorName`: Unique identifier for the behavior
- `BehaviorType`: Training or Inference mode
- `ModelPath`: Path to trained model (for inference)
- `VectorObservationSize`: Size of vector observation space
- `ContinuousActionSize`: Number of continuous actions
- `DiscreteActionBranches`: Sizes of discrete action branches
- `MaxSteps`: Maximum steps per episode
- `LearningRate`: Training learning rate
- `BatchSize`: Training batch size

## Best Practices

### Component Design
1. **Single Responsibility**: Each component should have one clear purpose
2. **Interface Communication**: Use interfaces for component interaction
3. **State Management**: Store state in AgentContext when needed for sharing
4. **Validation**: Implement Validate methods to check configuration
5. **Error Handling**: Gracefully handle errors and provide meaningful messages

### Performance Optimization
1. **Efficient Observation Collection**: Minimize observation space size
2. **Action Distribution**: Avoid expensive action processing in hot paths
3. **Reward Calculation**: Cache expensive calculations when possible
4. **Memory Management**: Reuse buffers and avoid allocations in Update loops
5. **Sensor Updates**: Update sensors only when necessary

### Testing
1. **Unit Tests**: Test individual components in isolation
2. **Integration Tests**: Test component interactions
3. **Performance Tests**: Benchmark critical paths
4. **Scenario Tests**: Test complete agent scenarios

## Extensibility

### Adding New Observation Providers
1. Implement `IObservationProvider` interface
2. Override required methods
3. Register with `VectorObservationCollector`
4. Configure in `MLBehaviorConfig`

### Adding New Action Receivers
1. Implement `IActionReceiver` interface
2. Override required methods
3. Register with `ActionDistributor`
4. Configure action space in `MLBehaviorConfig`

### Adding New Reward Providers
1. Implement `IRewardProvider` interface
2. Override required methods
3. Register with `RewardCalculator`
4. Configure weights in `MLBehaviorConfig`

### Adding New Episode Handlers
1. Implement `IEpisodeHandler` interface
2. Override required methods
3. Register with `EpisodeManager`
4. Configure in `EpisodeConfig`

## Common Patterns

### Observer Pattern
Components observe AgentContext for shared data changes.

### Strategy Pattern
Different implementations of interfaces provide different behaviors.

### Factory Pattern
Components are created based on configuration.

### Decorator Pattern
Components can wrap other components to add functionality.

## Troubleshooting

### Common Issues
1. **Missing Components**: Ensure all required components are added
2. **Configuration Mismatches**: Check that configuration matches implementation
3. **Performance Bottlenecks**: Profile and optimize hot paths
4. **Memory Leaks**: Check for proper cleanup in OnDestroy methods

### Debugging Tips
1. **Enable Debug Logging**: Use debug logging to trace execution
2. **Use Editor Tools**: Leverage custom editor windows for visualization
3. **Profile Regularly**: Monitor performance during development
4. **Validate Configurations**: Use validation tools to catch issues early

This documentation provides a comprehensive overview of the TCS ML-Agents API. For detailed information about specific components, refer to their individual documentation files.