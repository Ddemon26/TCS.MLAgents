# TCS.MLAgents Core Library - Development Tasks

## Overview
Create a composition-based MLAgent library that eliminates unnecessary inheritance and abstractions. Focus on modular, reusable components that can be composed together for different ML scenarios.

## Architecture Principles
- **Composition over Inheritance**: Use interfaces and component composition instead of base classes
- **Single Responsibility**: Each component handles one specific concern
- **Dependency Injection**: Components communicate through interfaces, not direct references
- **Configuration-Driven**: Use ScriptableObjects for runtime configuration
- **No Unnecessary Abstractions**: Direct ML-Agents API usage where appropriate

## Core Library Components

### 1. Agent Composition System
**Files to Create:**
- `IMLAgent.cs` - Core agent interface defining lifecycle methods
- `MLAgentComposer.cs` - Component that wires ML-Agent lifecycle to composition system
- `AgentContext.cs` - Shared context/state container for agent components

**Responsibilities:**
- Bridge Unity ML-Agents Agent class with composition system
- Manage component lifecycle (Initialize, OnEpisodeBegin, etc.)
- Provide shared context for all agent components

### 2. Observation System
**Files to Create:**
- `IObservationProvider.cs` - Interface for components that provide observations
- `VectorObservationCollector.cs` - Collects and manages vector observations
- `ObservationBuffer.cs` - Efficient buffer for observation data
- `ObservationNormalizer.cs` - Handles observation normalization

**Built-in Observation Providers:**
- `TransformObservationProvider.cs` - Position, rotation, scale observations
- `VelocityObservationProvider.cs` - Rigidbody velocity observations
- `RelativePositionObservationProvider.cs` - Distance/direction to targets
- `VisionObservationProvider.cs` - Ray-cast based vision system
- `TimeObservationProvider.cs` - Episode time and timing observations

### 3. Action System
**Files to Create:**
- `IActionReceiver.cs` - Interface for components that handle actions
- `ActionDistributor.cs` - Routes actions to appropriate receivers
- `ActionBuffer.cs` - Manages action buffering and validation

**Built-in Action Receivers:**
- `MovementActionReceiver.cs` - Handles movement actions (force, velocity)
- `RotationActionReceiver.cs` - Handles rotation actions
- `DiscreteActionReceiver.cs` - Handles discrete action choices

### 4. Reward System
**Files to Create:**
- `IRewardProvider.cs` - Interface for components that calculate rewards
- `RewardCalculator.cs` - Aggregates and manages reward calculations
- `RewardConfig.cs` - ScriptableObject for reward configuration

**Built-in Reward Providers:**
- `ProximityRewardProvider.cs` - Distance-based rewards
- `TimeRewardProvider.cs` - Time-based penalties/bonuses
- `BoundaryRewardProvider.cs` - Boundary violation penalties
- `TaskCompletionRewardProvider.cs` - Goal achievement rewards
- `EfficiencyRewardProvider.cs` - Action efficiency rewards

### 5. Episode Management
**Files to Create:**
- `IEpisodeHandler.cs` - Interface for episode lifecycle handling
- `EpisodeManager.cs` - Manages episode state and transitions
- `EpisodeConfig.cs` - ScriptableObject for episode configuration

**Built-in Episode Handlers:**
- `PositionResetHandler.cs` - Resets positions on episode start
- `StateResetHandler.cs` - Resets component states
- `EnvironmentResetHandler.cs` - Resets environment elements

### 6. Behavior Configuration
**Files to Create:**
- `MLBehaviorConfig.cs` - ScriptableObject for complete behavior configuration
- `BehaviorApplicator.cs` - Applies configuration to agent components
- `TrainingProfile.cs` - Complete training scenario configuration

### 7. Communication System
**Files to Create:**
- `IMLCommunicator.cs` - Interface for ML communication
- `SideChannelManager.cs` - Manages custom side channels
- `LoggingChannel.cs` - Structured logging to Python
- `MetricsChannel.cs` - Performance metrics communication
- `CommandChannel.cs` - Bidirectional command system

### 8. Sensor System
**Files to Create:**
- `ISensorProvider.cs` - Interface for custom sensors
- `SensorManager.cs` - Manages sensor registration and lifecycle
- `RaycastSensorProvider.cs` - Configurable raycast sensors
- `CameraSensorProvider.cs` - Camera-based visual sensors
- `ColliderSensorProvider.cs` - Collision detection sensors

### 9. Decision Making
**Files to Create:**
- `IDecisionProvider.cs` - Interface for heuristic/decision logic
- `DecisionRouter.cs` - Routes between inference/heuristic modes
- `HeuristicController.cs` - Manual/scripted control for testing

### 10. Statistics & Monitoring
**Files to Create:**
- `IStatisticsProvider.cs` - Interface for statistics collection
- `StatisticsCollector.cs` - Aggregates and reports statistics
- `PerformanceMonitor.cs` - Training performance monitoring

## Implementation Tasks

### Phase 1: Core Foundation
1. **Agent Composition System**
   - [x] Create `IMLAgent.cs` interface
   - [x] Implement `MLAgentComposer.cs` 
   - [x] Create `AgentContext.cs` shared state container
   - [x] Unit tests for core composition system

2. **Basic Observation System**
   - [x] Create `IObservationProvider.cs` interface
   - [x] Implement `VectorObservationCollector.cs`
   - [x] Create `ObservationBuffer.cs` for efficient data handling
   - [x] Implement `TransformObservationProvider.cs`
   - [x] Implement `VelocityObservationProvider.cs`

3. **Basic Action System**
   - [x] Create `IActionReceiver.cs` interface
   - [x] Implement `ActionDistributor.cs`
   - [x] Create `MovementActionReceiver.cs`
   - [x] Create `RotationActionReceiver.cs`

### Phase 2: Core Systems
4. **Reward System**
   - [x] Create `IRewardProvider.cs` interface
   - [x] Implement `RewardCalculator.cs`
   - [x] Create `RewardConfig.cs` ScriptableObject
   - [x] Implement built-in reward providers
   - [x] Reward system integration tests

5. **Episode Management**
   - [x] Create `IEpisodeHandler.cs` interface
   - [x] Implement `EpisodeManager.cs`
   - [x] Create `EpisodeConfig.cs`
   - [x] Implement built-in episode handlers

6. **Vision System Enhancement**
   - [x] Create `VisionObservationProvider.cs`
   - [x] Implement configurable raycast vision
   - [x] Add vision debugging tools
   - [x] Vision system performance optimization

### Phase 3: Advanced Features
7. **Sensor System**
   - [x] Create `ISensorProvider.cs` interface
   - [x] Implement `SensorManager.cs`
   - [x] Create `RaycastSensorProvider.cs`
   - [x] Create `CameraSensorProvider.cs`
   - [x] Sensor system integration

8. **Communication System**
   - [x] Create `IMLCommunicator.cs` interface
   - [x] Implement `CustomSideChannelManager.cs`
   - [x] Create `LoggingChannel.cs`
   - [x] Create `MetricsChannel.cs`
   - [x] Create `CommandChannel.cs`
   - [x] Python-side communication handlers example

9. **Decision System**
   - [x] Create `IDecisionProvider.cs` interface
   - [x] Implement `DecisionRouter.cs`
   - [x] Create `HeuristicController.cs`
   - [x] Decision system testing framework

### Phase 4: Configuration & Tooling
10. **Behavior Configuration**
    - [ ] Create `MLBehaviorConfig.cs` ScriptableObject
    - [ ] Implement `BehaviorApplicator.cs`
    - [ ] Create configuration validation system
    - [ ] Editor tools for behavior configuration

11. **Statistics & Monitoring**
    - [ ] Create `IStatisticsProvider.cs` interface
    - [ ] Implement `StatisticsCollector.cs`
    - [ ] Create `PerformanceMonitor.cs`
    - [ ] Statistics visualization tools

12. **Testing & Validation**
    - [ ] Unit tests for all core components
    - [ ] Integration tests for complete scenarios
    - [ ] Performance benchmarking suite
    - [ ] Example scenarios using new system

### Phase 5: Migration & Documentation
13. **Migration Tools**
    - [ ] Create migration utilities from inheritance-based system
    - [ ] Automated component setup tools
    - [ ] Validation tools for migrated scenarios

14. **Documentation & Examples**
    - [ ] Complete API documentation
    - [ ] Usage examples for common scenarios
    - [ ] Best practices guide
    - [ ] Performance optimization guide

15. **Final Integration**
    - [ ] Integration with existing PredVsPray scenario
    - [ ] Performance comparison with inheritance-based system
    - [ ] Final optimization and cleanup

## Configuration Strategy
Each component will have its own ScriptableObject configuration:
- `VisionConfig.cs` - Vision system parameters
- `MovementConfig.cs` - Movement system parameters  
- `RewardConfig.cs` - Reward calculation parameters
- `EpisodeConfig.cs` - Episode management parameters
- `MLBehaviorConfig.cs` - Complete behavior composition

## Success Criteria
1. **Performance**: Equal or better performance than inheritance-based system
2. **Flexibility**: Easy to create new ML scenarios by composing components
3. **Maintainability**: Clear separation of concerns, testable components
4. **Usability**: Simple configuration through ScriptableObjects
5. **Extensibility**: Easy to add new observation/action/reward providers

## File Structure
```
Assets/_Damon/TCS.MLAgents/Runtime/Core/
├── Interfaces/
│   ├── IMLAgent.cs
│   ├── IObservationProvider.cs
│   ├── IActionReceiver.cs
│   ├── IRewardProvider.cs
│   ├── IEpisodeHandler.cs
│   ├── ISensorProvider.cs
│   └── IMLCommunicator.cs
├── Core/
│   ├── MLAgentComposer.cs
│   ├── AgentContext.cs
│   ├── VectorObservationCollector.cs
│   ├── ActionDistributor.cs
│   ├── RewardCalculator.cs
│   ├── EpisodeManager.cs
│   └── SensorManager.cs
├── Observations/
│   ├── TransformObservationProvider.cs
│   ├── VelocityObservationProvider.cs
│   ├── RelativePositionObservationProvider.cs
│   ├── VisionObservationProvider.cs
│   └── TimeObservationProvider.cs
├── Actions/
│   ├── MovementActionReceiver.cs
│   ├── RotationActionReceiver.cs
│   └── DiscreteActionReceiver.cs
├── Rewards/
│   ├── ProximityRewardProvider.cs
│   ├── TimeRewardProvider.cs
│   ├── BoundaryRewardProvider.cs
│   ├── TaskCompletionRewardProvider.cs
│   └── EfficiencyRewardProvider.cs
├── Episodes/
│   ├── PositionResetHandler.cs
│   ├── StateResetHandler.cs
│   └── EnvironmentResetHandler.cs
├── Communication/
│   ├── CustomSideChannelManager.cs
│   ├── LoggingChannel.cs
│   ├── MetricsChannel.cs
│   └── CommandChannel.cs
├── Decision/
│   ├── DecisionRouter.cs
│   └── HeuristicController.cs
└── Utilities/
    ├── ObservationBuffer.cs
    ├── ActionBuffer.cs
    ├── StatisticsCollector.cs
    ├── PerformanceMonitor.cs
    └── BehaviorApplicator.cs
```