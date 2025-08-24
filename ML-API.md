# ML-API Documentation

This document outlines the Unity ML-Agents API calls currently in use within the TCS.MLAgents system.

## Core ML-Agents API Usage

### Agent Class APIs

#### PredatorAgent.cs:16-113
```csharp
// Core Agent inheritance
public class PredatorAgent : Agent

// Agent lifecycle methods
public override void Initialize()
public override void OnEpisodeBegin()
public override void CollectObservations(VectorSensor sensor)
public override void OnActionReceived(ActionBuffers actionBuffers)
public override void Heuristic(in ActionBuffers actionsOut)

// Agent reward/episode control
AddReward(-0.001f);
SetReward(1.0f);
EndEpisode();
```

### VectorSensor APIs
```csharp
// Observation collection
sensor.AddObservation(preyTransform.localPosition - transform.localPosition);
sensor.AddObservation(rBody.linearVelocity);
sensor.AddObservation(preyRb.linearVelocity);
```

### ActionBuffers APIs
```csharp
// Action retrieval
var continuousActions = actionBuffers.ContinuousActions;
Vector3 force = new Vector3(continuousActions[0], 0, continuousActions[1]);

// Heuristic input
var continuousActionsOut = actionsOut.ContinuousActions;
continuousActionsOut[0] = Input.GetAxis("Horizontal");
continuousActionsOut[1] = Input.GetAxis("Vertical");
```

### BehaviorParameters APIs

#### BehaviorSetup.cs:19-40 & MLAgentSetup.cs:41-56
```csharp
// Behavior configuration
behaviorParameters.BehaviorName = behaviorName;
behaviorParameters.BehaviorType = behaviorType;
behaviorParameters.UseChildSensors = useChildSensors;

// Action space configuration
behaviorParameters.BrainParameters.ActionSpec = ActionSpec.MakeDiscrete(branchSizes);
behaviorParameters.BrainParameters.ActionSpec = ActionSpec.MakeContinuous(continuousActionCount);

// Model assignment
behaviorParameters.Model = customBrain;
```

### ActionSpec APIs
```csharp
// Continuous action space
ActionSpec.MakeContinuous(2)

// Discrete action space
ActionSpec.MakeDiscrete(branchSizes)
```

### Academy APIs

#### PredatorAgent.cs:86
```csharp
// Statistics recording
Academy.Instance.StatsRecorder.Add("Predator/DistanceToPrey", distance);

// Agent step callbacks
Academy.Instance.AgentPreStep += OnAgentPreStep;
```

### SideChannel APIs

#### StringLogSideChannel.cs:8-22
```csharp
// Side channel creation and registration
logChannel = new StringLogSideChannel();
SideChannelManager.RegisterSideChannel(logChannel);
SideChannelManager.UnregisterSideChannel(logChannel);

// Message sending
using (var msgOut = new OutgoingMessage()) {
    msgOut.WriteString(message);
    QueueMessageToSend(msgOut);
}

// Message receiving
protected override void OnMessageReceived(IncomingMessage msg) {
    string received = msg.ReadString();
}
```

### Barracuda (Neural Network) APIs

#### MLBrain.cs:7-34
```csharp
// Model inheritance
public class MlBrain : NNModel

// Model data assignment
modelData = m_modelData;
```

## Enums and Types Used

### BehaviorType
- `BehaviorType.HeuristicOnly`
- `BehaviorType.InferenceOnly` 
- `BehaviorType.Default`

### Action Types
- `ActionBuffers.ContinuousActions`
- `ActionSpec` for defining action spaces

### Component Requirements
- `[RequireComponent(typeof(BehaviorParameters))]`
- `[RequireComponent(typeof(Rigidbody))]`

## Key API Patterns

1. **Agent Lifecycle**: Initialize → OnEpisodeBegin → CollectObservations → OnActionReceived → (repeat or EndEpisode)

2. **Observation Pattern**: Use `VectorSensor.AddObservation()` to feed data to the neural network

3. **Action Pattern**: Read from `ActionBuffers.ContinuousActions` or discrete equivalents

4. **Reward Pattern**: Use `AddReward()` for incremental rewards, `SetReward()` for absolute values

5. **Episode Management**: `EndEpisode()` to terminate and restart training episodes

6. **Model Assignment**: Configure `BehaviorParameters.Model` with trained `.onnx` models via `NNModel`