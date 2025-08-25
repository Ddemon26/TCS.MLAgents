# TCS ML-Agents Usage Examples

This document provides practical examples for common ML-Agents scenarios using the TCS ML-Agents composition system.

## Table of Contents
1. [Basic Agent Setup](#basic-agent-setup)
2. [Movement-Based Agent](#movement-based-agent)
3. [Vision-Based Agent](#vision-based-agent)
4. [Multi-Agent Scenario](#multi-agent-scenario)
5. [Custom Reward System](#custom-reward-system)
6. [Heuristic Control](#heuristic-control)
7. [Performance Monitoring](#performance-monitoring)

## Basic Agent Setup

### Creating a Simple Agent

```csharp
using UnityEngine;
using TCS.MLAgents.Core;
using TCS.MLAgents.Observations;
using TCS.MLAgents.Actions;
using TCS.MLAgents.Rewards;

public class BasicAgentSetup : MonoBehaviour
{
    void Start()
    {
        // The MLAgentComposer is automatically added when you add other components
        // Just add the components you need and the composer will wire them up
        
        // Add observation collector
        gameObject.AddComponent<VectorObservationCollector>();
        
        // Add action distributor
        gameObject.AddComponent<ActionDistributor>();
        
        // Add reward calculator
        gameObject.AddComponent<RewardCalculator>();
        
        // Add episode manager
        gameObject.AddComponent<EpisodeManager>();
        
        // The system will automatically discover and register components
    }
}
```

### Configuring Behavior

Create a `MLBehaviorConfig` ScriptableObject to define your agent's behavior:

```csharp
// In an editor script or initialization code
var config = ScriptableObject.CreateInstance<MLBehaviorConfig>();
config.SetBehaviorName("BasicAgent");
config.SetBehaviorType(MLBehaviorConfig.BehaviorType.Training);
config.SetVectorObservationSize(8); // 8 observation values
config.SetContinuousActionSize(2);  // 2 continuous actions (e.g., X,Z movement)
config.SetMaxSteps(1000);
config.SetLearningRate(3e-4f);
config.SetBatchSize(1024);

// Save the configuration
#if UNITY_EDITOR
string path = "Assets/Configurations/BasicAgentConfig.asset";
UnityEditor.AssetDatabase.CreateAsset(config, path);
UnityEditor.AssetDatabase.SaveAssets();
#endif
```

## Movement-Based Agent

### Setting Up Movement Components

```csharp
using UnityEngine;
using TCS.MLAgents.Observations;
using TCS.MLAgents.Actions;
using TCS.MLAgents.Rewards;
using TCS.MLAgents.Episodes;

public class MovementAgentSetup : MonoBehaviour
{
    [Header("Movement Settings")]
    public float movementForce = 10f;
    public float rotationTorque = 5f;
    
    void Start()
    {
        SetupObservationSystem();
        SetupActionSystem();
        SetupRewardSystem();
        SetupEpisodeSystem();
    }
    
    void SetupObservationSystem()
    {
        // Add observation collector
        var observationCollector = gameObject.AddComponent<VectorObservationCollector>();
        
        // Add transform observation provider (position, rotation)
        var transformProvider = gameObject.AddComponent<TransformObservationProvider>();
        transformProvider.providerName = "TransformObservations";
        transformProvider.isActive = true;
        transformProvider.includePosition = true;
        transformProvider.includeRotation = true;
        transformProvider.includeScale = false;
        transformProvider.normalizePosition = true;
        transformProvider.normalizeRotation = true;
        
        // Add velocity observation provider (if Rigidbody exists)
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            var velocityProvider = gameObject.AddComponent<VelocityObservationProvider>();
            velocityProvider.providerName = "VelocityObservations";
            velocityProvider.isActive = true;
            velocityProvider.includeLinearVelocity = true;
            velocityProvider.includeAngularVelocity = true;
            velocityProvider.normalizeVelocity = true;
        }
        
        // Add relative position provider (to target objects)
        var relativePositionProvider = gameObject.AddComponent<RelativePositionObservationProvider>();
        relativePositionProvider.providerName = "RelativePositionObservations";
        relativePositionProvider.isActive = true;
        relativePositionProvider.targetTags = new string[] { "Target", "Food", "Enemy" };
        relativePositionProvider.includeDistance = true;
        relativePositionProvider.includeDirection = true;
        relativePositionProvider.normalizeDistance = true;
    }
    
    void SetupActionSystem()
    {
        // Add action distributor
        var actionDistributor = gameObject.AddComponent<ActionDistributor>();
        
        // Add movement action receiver
        var movementReceiver = gameObject.AddComponent<MovementActionReceiver>();
        movementReceiver.receiverName = "MovementActions";
        movementReceiver.isActive = true;
        movementReceiver.actionMode = MovementActionReceiver.ActionMode.Force;
        movementReceiver.forceMultiplier = movementForce;
        movementReceiver.applyForceMode = ForceMode.Force;
        
        // Add rotation action receiver
        var rotationReceiver = gameObject.AddComponent<RotationActionReceiver>();
        rotationReceiver.receiverName = "RotationActions";
        rotationReceiver.isActive = true;
        rotationReceiver.rotationMode = RotationActionReceiver.RotationMode.Torque;
        rotationReceiver.torqueMultiplier = rotationTorque;
        rotationReceiver.applyTorqueMode = ForceMode.Force;
    }
    
    void SetupRewardSystem()
    {
        // Add reward calculator
        var rewardCalculator = gameObject.AddComponent<RewardCalculator>();
        
        // Add proximity reward (closer to targets = better reward)
        var proximityReward = gameObject.AddComponent<ProximityRewardProvider>();
        proximityReward.providerName = "ProximityRewards";
        proximityReward.isActive = true;
        proximityReward.rewardWeight = 1.0f;
        proximityReward.targetTag = "Target";
        proximityReward.rewardCurve = AnimationCurve.Linear(0, 1, 1, 0); // Closer = more reward
        proximityReward.distanceThreshold = 10f;
        
        // Add time penalty (encourage faster completion)
        var timeReward = gameObject.AddComponent<TimeRewardProvider>();
        timeReward.providerName = "TimePenalties";
        timeReward.isActive = true;
        timeReward.rewardWeight = -0.001f; // Small penalty per step
        timeReward.applyMode = TimeRewardProvider.ApplyMode.PerStep;
        
        // Add boundary violation penalty
        var boundaryReward = gameObject.AddComponent<BoundaryRewardProvider>();
        boundaryReward.providerName = "BoundaryPenalties";
        boundaryReward.isActive = true;
        boundaryReward.rewardWeight = -1.0f; // Large penalty for leaving bounds
        boundaryReward.boundarySize = new Vector3(20, 20, 20); // 20x20x20 unit boundary
        boundaryReward.checkMode = BoundaryRewardProvider.CheckMode.Box;
    }
    
    void SetupEpisodeSystem()
    {
        // Add episode manager
        var episodeManager = gameObject.AddComponent<EpisodeManager>();
        
        // Add position reset handler
        var positionReset = gameObject.AddComponent<PositionResetHandler>();
        positionReset.handlerName = "PositionReset";
        positionReset.isActive = true;
        positionReset.resetPosition = true;
        positionReset.resetRotation = true;
        positionReset.resetVelocity = true;
        positionReset.randomizeSpawnPosition = true;
        positionReset.spawnAreaCenter = Vector3.zero;
        positionReset.spawnAreaSize = new Vector3(10, 1, 10);
        
        // Add boundary handler
        var boundaryHandler = gameObject.AddComponent<BoundaryHandler>();
        boundaryHandler.handlerName = "BoundaryCheck";
        boundaryHandler.isActive = true;
        boundaryHandler.boundarySize = new Vector3(20, 20, 20);
        boundaryHandler.endEpisodeOnViolation = true;
    }
}
```

## Vision-Based Agent

### Setting Up Raycast Vision

```csharp
using UnityEngine;
using TCS.MLAgents.Sensors;
using TCS.MLAgents.Observations;

public class VisionAgentSetup : MonoBehaviour
{
    [Header("Vision Settings")]
    public int rayCount = 8;
    public float rayLength = 20f;
    public LayerMask detectionLayers = Physics.AllLayers;
    
    void Start()
    {
        SetupVisionSystem();
    }
    
    void SetupVisionSystem()
    {
        // Add sensor manager
        var sensorManager = gameObject.AddComponent<SensorManager>();
        
        // Add raycast sensor provider
        var raycastProvider = gameObject.AddComponent<RaycastSensorProvider>();
        raycastProvider.providerName = "RaycastVision";
        raycastProvider.isActive = true;
        raycastProvider.rayCount = rayCount;
        raycastProvider.rayLength = rayLength;
        raycastProvider.castLayerMask = detectionLayers;
        raycastProvider.detectableTags = new string[] { "Wall", "Obstacle", "Target", "Enemy" };
        raycastProvider.castSource = RaycastSensorProvider.CastSource.TransformForward;
        raycastProvider.rayPattern = RaycastSensorProvider.RayPattern.Radial;
        raycastProvider.rayAngles = new float[] { -45, -30, -15, 0, 15, 30, 45, 60 }; // Custom angles
        
        // Add vision observation provider to convert raycast data to observations
        var visionProvider = gameObject.AddComponent<VisionObservationProvider>();
        visionProvider.providerName = "VisionObservations";
        visionProvider.isActive = true;
        visionProvider.normalizeDistances = true;
        visionProvider.includeHitData = true;
        visionProvider.includeDistanceData = true;
    }
}
```

### Camera-Based Vision

```csharp
using UnityEngine;
using TCS.MLAgents.Sensors;

public class CameraVisionAgentSetup : MonoBehaviour
{
    [Header("Camera Settings")]
    public Camera observationCamera;
    public int cameraWidth = 84;
    public int cameraHeight = 84;
    public bool grayscale = true;
    
    void Start()
    {
        SetupCameraVision();
    }
    
    void SetupCameraVision()
    {
        // Add sensor manager
        var sensorManager = gameObject.AddComponent<SensorManager>();
        
        // Ensure we have a camera
        if (observationCamera == null)
        {
            observationCamera = GetComponent<Camera>();
            if (observationCamera == null)
            {
                observationCamera = gameObject.AddComponent<Camera>();
            }
        }
        
        // Add camera sensor provider
        var cameraProvider = gameObject.AddComponent<CameraSensorProvider>();
        cameraProvider.providerName = "CameraVision";
        cameraProvider.isActive = true;
        cameraProvider.observationCamera = observationCamera;
        cameraProvider.cameraWidth = cameraWidth;
        cameraProvider.cameraHeight = cameraHeight;
        cameraProvider.grayscale = grayscale;
        cameraProvider.useSegmentation = false; // Set to true for segmentation masks
        cameraProvider.captureInterval = 1; // Capture every frame
    }
}
```

## Multi-Agent Scenario

### Setting Up Multiple Agents

```csharp
using UnityEngine;
using TCS.MLAgents.Core;

public class MultiAgentScenario : MonoBehaviour
{
    [Header("Agent Prefabs")]
    public GameObject predatorPrefab;
    public GameObject preyPrefab;
    
    [Header("Scenario Settings")]
    public int predatorCount = 3;
    public int preyCount = 5;
    public Vector3 spawnArea = new Vector3(20, 1, 20);
    
    void Start()
    {
        SpawnAgents();
    }
    
    void SpawnAgents()
    {
        // Spawn predators
        for (int i = 0; i < predatorCount; i++)
        {
            Vector3 spawnPosition = GetRandomPosition();
            GameObject predator = Instantiate(predatorPrefab, spawnPosition, Quaternion.identity);
            predator.name = $"Predator_{i}";
            
            // Configure predator-specific settings
            ConfigurePredator(predator);
        }
        
        // Spawn prey
        for (int i = 0; i < preyCount; i++)
        {
            Vector3 spawnPosition = GetRandomPosition();
            GameObject prey = Instantiate(preyPrefab, spawnPosition, Quaternion.identity);
            prey.name = $"Prey_{i}";
            
            // Configure prey-specific settings
            ConfigurePrey(prey);
        }
    }
    
    void ConfigurePredator(GameObject predator)
    {
        // Add team identifier
        var context = predator.GetComponent<MLAgentComposer>()?.Context;
        if (context != null)
        {
            context.SetSharedData("Team", "Predator");
            context.SetSharedData("PreyCount", preyCount);
        }
        
        // Configure observation to detect prey
        var relativePositionProvider = predator.GetComponent<RelativePositionObservationProvider>();
        if (relativePositionProvider != null)
        {
            relativePositionProvider.targetTags = new string[] { "Prey" };
        }
        
        // Configure rewards for catching prey
        var taskCompletionProvider = predator.GetComponent<TaskCompletionRewardProvider>();
        if (taskCompletionProvider != null)
        {
            taskCompletionProvider.eventName = "CaughtPrey";
            taskCompletionProvider.rewardWeight = 1.0f;
        }
    }
    
    void ConfigurePrey(GameObject prey)
    {
        // Add team identifier
        var context = prey.GetComponent<MLAgentComposer>()?.Context;
        if (context != null)
        {
            context.SetSharedData("Team", "Prey");
            context.SetSharedData("PredatorCount", predatorCount);
        }
        
        // Configure observation to detect predators
        var relativePositionProvider = prey.GetComponent<RelativePositionObservationProvider>();
        if (relativePositionProvider != null)
        {
            relativePositionProvider.targetTags = new string[] { "Predator" };
        }
        
        // Configure rewards for survival
        var timeRewardProvider = prey.GetComponent<TimeRewardProvider>();
        if (timeRewardProvider != null)
        {
            timeRewardProvider.rewardWeight = 0.01f; // Positive reward for surviving
        }
    }
    
    Vector3 GetRandomPosition()
    {
        return new Vector3(
            Random.Range(-spawnArea.x/2, spawnArea.x/2),
            Random.Range(-spawnArea.y/2, spawnArea.y/2),
            Random.Range(-spawnArea.z/2, spawnArea.z/2)
        );
    }
}
```

## Custom Reward System

### Creating Custom Reward Providers

```csharp
using UnityEngine;
using TCS.MLAgents.Core;
using TCS.MLAgents.Interfaces;

public class CustomEnergyRewardProvider : IRewardProvider
{
    [Header("Energy Settings")]
    public string providerName = "EnergyEfficiency";
    public int priority = 50;
    public bool isActive = true;
    public float rewardWeight = 0.1f;
    
    [Header("Energy Configuration")]
    public float maxEnergy = 100f;
    public float energyConsumptionRate = 1f;
    public float energyRegenerationRate = 0.5f;
    
    private AgentContext m_Context;
    private float m_CurrentEnergy;
    private float m_PreviousEnergy;
    
    public string ProviderName => providerName;
    public int Priority => priority;
    public bool IsActive => isActive;
    public float RewardWeight => rewardWeight;
    
    public void Initialize(AgentContext context)
    {
        m_Context = context;
        m_CurrentEnergy = maxEnergy;
        m_PreviousEnergy = maxEnergy;
        
        // Store initial energy in context for other components
        m_Context.SetSharedData("CurrentEnergy", m_CurrentEnergy);
        m_Context.SetSharedData("MaxEnergy", maxEnergy);
    }
    
    public bool ValidateProvider(AgentContext context)
    {
        return context != null && context.AgentGameObject != null;
    }
    
    public float CalculateReward(AgentContext context, float deltaTime)
    {
        if (!isActive) return 0f;
        
        m_PreviousEnergy = m_CurrentEnergy;
        
        // Calculate energy change based on actions
        float energyChange = CalculateEnergyChange(context, deltaTime);
        m_CurrentEnergy += energyChange;
        
        // Clamp energy between 0 and max
        m_CurrentEnergy = Mathf.Clamp(m_CurrentEnergy, 0, maxEnergy);
        
        // Update context
        m_Context.SetSharedData("CurrentEnergy", m_CurrentEnergy);
        m_Context.SetSharedData("EnergyPercentage", m_CurrentEnergy / maxEnergy);
        
        // Reward based on energy efficiency
        float efficiencyReward = CalculateEfficiencyReward();
        
        return efficiencyReward;
    }
    
    float CalculateEnergyChange(AgentContext context, float deltaTime)
    {
        float energyChange = 0f;
        
        // Energy regeneration over time
        energyChange += energyRegenerationRate * deltaTime;
        
        // Energy consumption based on actions (example: movement costs energy)
        var actionDistributor = context.GetComponent<ActionDistributor>();
        if (actionDistributor != null)
        {
            // Get recent actions and calculate energy cost
            // This is a simplified example - in practice, you'd need to track actions
            energyChange -= energyConsumptionRate * deltaTime;
        }
        
        // Bonus for maintaining high energy
        if (m_CurrentEnergy > maxEnergy * 0.8f)
        {
            energyChange += 0.1f * deltaTime; // Small bonus for good energy management
        }
        
        return energyChange;
    }
    
    float CalculateEfficiencyReward()
    {
        // Reward for maintaining energy
        float energyRatio = m_CurrentEnergy / maxEnergy;
        
        // Higher reward for maintaining 80-100% energy
        if (energyRatio > 0.8f)
        {
            return 0.1f * rewardWeight;
        }
        // Lower reward for 50-80% energy
        else if (energyRatio > 0.5f)
        {
            return 0.05f * rewardWeight;
        }
        // Penalty for low energy (<50%)
        else if (energyRatio < 0.5f)
        {
            return -0.1f * rewardWeight;
        }
        
        return 0f;
    }
    
    public void OnEpisodeBegin(AgentContext context)
    {
        m_CurrentEnergy = maxEnergy;
        m_PreviousEnergy = maxEnergy;
        m_Context.SetSharedData("CurrentEnergy", m_CurrentEnergy);
        m_Context.SetSharedData("EnergyPercentage", 1.0f);
    }
    
    public void OnEpisodeEnd(AgentContext context)
    {
        // No special cleanup needed
    }
    
    public void OnUpdate(AgentContext context, float deltaTime)
    {
        // Energy changes handled in CalculateReward
    }
    
    public void OnRewardEvent(string eventName, AgentContext context, object eventData = null)
    {
        // Handle specific events that affect energy
        switch (eventName)
        {
            case "Jump":
                m_CurrentEnergy -= 10f; // Jump costs 10 energy
                break;
            case "Attack":
                m_CurrentEnergy -= 15f; // Attack costs 15 energy
                break;
            case "PowerUp":
                m_CurrentEnergy = Mathf.Min(m_CurrentEnergy + 25f, maxEnergy); // Power-up gives 25 energy
                break;
        }
    }
    
    public void SetActive(bool active)
    {
        isActive = active;
    }
    
    public string GetDebugInfo()
    {
        return $"EnergyProvider[Energy: {m_CurrentEnergy:F1}/{maxEnergy:F1}, " +
               $"Efficiency: {CalculateEfficiencyReward():F3}]";
    }
}
```

### Registering Custom Reward Provider

```csharp
using UnityEngine;
using TCS.MLAgents.Rewards;

public class AgentWithCustomReward : MonoBehaviour
{
    void Start()
    {
        // Add the custom reward provider
        var customRewardProvider = gameObject.AddComponent<CustomEnergyRewardProvider>();
        customRewardProvider.providerName = "EnergyEfficiency";
        customRewardProvider.rewardWeight = 0.1f;
        customRewardProvider.isActive = true;
        
        // The RewardCalculator will automatically discover and register it
    }
}
```

## Heuristic Control

### Setting Up Heuristic Controller

```csharp
using UnityEngine;
using TCS.MLAgents.Decision;

public class HeuristicAgentSetup : MonoBehaviour
{
    private HeuristicController m_HeuristicController;
    
    void Start()
    {
        SetupHeuristicControl();
    }
    
    void SetupHeuristicControl()
    {
        // Add decision router
        var decisionRouter = gameObject.AddComponent<DecisionRouter>();
        
        // Add heuristic controller
        m_HeuristicController = gameObject.AddComponent<HeuristicController>();
        m_HeuristicController.controllerName = "ManualControl";
        m_HeuristicController.isActive = true;
        m_HeuristicController.controlMode = HeuristicController.ControlMode.Keyboard;
        m_HeuristicController.enableSmoothing = true;
        m_HeuristicController.smoothingFactor = 0.1f;
        
        // Register with decision router
        decisionRouter.AddDecisionProvider(m_HeuristicController);
        decisionRouter.SetDecisionMode(DecisionRouter.DecisionMode.Heuristic, "ManualControl");
    }
    
    void Update()
    {
        // Toggle between heuristic and inference control
        if (Input.GetKeyDown(KeyCode.H))
        {
            var decisionRouter = GetComponent<DecisionRouter>();
            if (decisionRouter != null)
            {
                var currentMode = decisionRouter.CurrentMode;
                if (currentMode == DecisionRouter.DecisionMode.Heuristic)
                {
                    decisionRouter.SetDecisionMode(DecisionRouter.DecisionMode.Inference);
                    Debug.Log("Switched to inference mode");
                }
                else
                {
                    decisionRouter.SetDecisionMode(DecisionRouter.DecisionMode.Heuristic, "ManualControl");
                    Debug.Log("Switched to heuristic mode");
                }
            }
        }
    }
}
```

### Custom Heuristic Logic

```csharp
using UnityEngine;
using TCS.MLAgents.Core;
using TCS.MLAgents.Interfaces;

public class CustomHeuristicController : IDecisionProvider
{
    public string Id => "CustomHeuristic";
    public int Priority => 100; // High priority to override ML decisions
    public bool IsActive { get; private set; } = true;
    
    private AgentContext m_Context;
    private Transform m_AgentTransform;
    private Transform m_TargetTransform;
    
    public void Initialize(AgentContext context)
    {
        m_Context = context;
        m_AgentTransform = context.AgentGameObject.transform;
        
        // Find target object
        var targets = GameObject.FindGameObjectsWithTag("Target");
        if (targets.Length > 0)
        {
            m_TargetTransform = targets[0].transform;
        }
    }
    
    public bool ShouldDecide(AgentContext context, List<ISensor> sensors)
    {
        return IsActive && m_TargetTransform != null;
    }
    
    public void DecideAction(AgentContext context, List<ISensor> sensors, ActionBuffers actions)
    {
        if (m_TargetTransform == null || m_AgentTransform == null) return;
        
        // Calculate direction to target
        Vector3 directionToTarget = m_TargetTransform.position - m_AgentTransform.position;
        directionToTarget.y = 0f; // Ignore Y axis for 2D movement
        directionToTarget.Normalize();
        
        // Get continuous actions buffer
        var continuousActions = actions.ContinuousActions;
        
        // Simple proportional controller for movement
        float forwardMovement = directionToTarget.z; // Forward/backward
        float rightMovement = directionToTarget.x;   // Left/right
        
        // Clamp actions to valid range [-1, 1]
        continuousActions[0] = Mathf.Clamp(forwardMovement, -1f, 1f);
        continuousActions[1] = Mathf.Clamp(rightMovement, -1f, 1f);
        
        // Add some randomness to make it more interesting
        continuousActions[0] += Random.Range(-0.1f, 0.1f);
        continuousActions[1] += Random.Range(-0.1f, 0.1f);
        
        // Example discrete action (if applicable)
        if (actions.DiscreteActions.Length > 0)
        {
            // Simple decision: jump if close to target
            float distanceToTarget = Vector3.Distance(m_AgentTransform.position, m_TargetTransform.position);
            actions.DiscreteActions[0] = distanceToTarget < 2f ? 1 : 0; // Jump if close
        }
    }
    
    public void OnEpisodeBegin(AgentContext context)
    {
        // Reset any episode-specific state
    }
    
    public void OnEpisodeEnd(AgentContext context)
    {
        // Clean up episode-specific resources
    }
    
    public void OnUpdate(AgentContext context, float deltaTime)
    {
        // Update any continuous logic
    }
    
    public void SetActive(bool active)
    {
        IsActive = active;
    }
    
    public string GetDebugInfo()
    {
        return $"CustomHeuristicController[Active: {IsActive}]";
    }
}
```

## Performance Monitoring

### Setting Up Statistics Collection

```csharp
using UnityEngine;
using TCS.MLAgents.Utilities;
using TCS.MLAgents.Core;

public class PerformanceMonitoringSetup : MonoBehaviour
{
    private StatisticsCollector m_StatisticsCollector;
    private PerformanceMonitor m_PerformanceMonitor;
    
    void Start()
    {
        SetupStatisticsSystem();
    }
    
    void SetupStatisticsSystem()
    {
        // Add statistics collector
        m_StatisticsCollector = gameObject.AddComponent<StatisticsCollector>();
        
        // Add performance monitor
        m_PerformanceMonitor = gameObject.AddComponent<PerformanceMonitor>();
        m_PerformanceMonitor.monitorName = "AgentPerformance";
        m_PerformanceMonitor.priority = 50;
        m_PerformanceMonitor.isActive = true;
        m_PerformanceMonitor.samplingInterval = 0.5f;
        
        // Register performance monitor with statistics collector
        m_StatisticsCollector.AddStatisticsProvider(m_PerformanceMonitor);
        
        // Add custom statistics provider
        var customStats = new CustomAgentStatisticsProvider();
        customStats.Initialize(GetComponent<MLAgentComposer>()?.Context);
        m_StatisticsCollector.AddStatisticsProvider(customStats);
    }
    
    void Update()
    {
        // Collect statistics periodically
        if (Input.GetKeyDown(KeyCode.S))
        {
            var stats = m_StatisticsCollector.CurrentStatistics;
            Debug.Log("=== Agent Statistics ===");
            foreach (var kvp in stats)
            {
                Debug.Log($"{kvp.Key}: {kvp.Value:F3}");
            }
        }
        
        // Export statistics to JSON
        if (Input.GetKeyDown(KeyCode.E))
        {
            string jsonStats = m_StatisticsCollector.ExportToJSON();
            Debug.Log($"Exported Statistics:\n{jsonStats}");
        }
    }
}

public class CustomAgentStatisticsProvider : IStatisticsProvider
{
    public string Id => "CustomAgentStats";
    public int Priority => 25;
    public bool IsActive { get; private set; } = true;
    
    private AgentContext m_Context;
    private int m_StepCount = 0;
    private float m_TotalReward = 0f;
    private float m_AverageRewardPerStep = 0f;
    
    public void Initialize(AgentContext context)
    {
        m_Context = context;
    }
    
    public void CollectStatistics(AgentContext context, float deltaTime)
    {
        if (!IsActive) return;
        
        m_StepCount++;
        m_TotalReward = context.CumulativeReward;
        m_AverageRewardPerStep = m_StepCount > 0 ? m_TotalReward / m_StepCount : 0f;
    }
    
    public void OnEpisodeBegin(AgentContext context)
    {
        m_StepCount = 0;
        m_TotalReward = 0f;
        m_AverageRewardPerStep = 0f;
    }
    
    public void OnEpisodeEnd(AgentContext context)
    {
        // No special cleanup needed
    }
    
    public Dictionary<string, float> GetStatistics()
    {
        return new Dictionary<string, float>
        {
            {"Custom.Steps", m_StepCount},
            {"Custom.TotalReward", m_TotalReward},
            {"Custom.AvgRewardPerStep", m_AverageRewardPerStep}
        };
    }
    
    public Dictionary<string, float> GetChangedStatistics()
    {
        // Return all statistics since they change every step
        return GetStatistics();
    }
    
    public void ResetStatistics()
    {
        m_StepCount = 0;
        m_TotalReward = 0f;
        m_AverageRewardPerStep = 0f;
    }
    
    public void SetActive(bool active)
    {
        IsActive = active;
    }
    
    public string GetDebugInfo()
    {
        return $"CustomAgentStats[Steps: {m_StepCount}, AvgReward: {m_AverageRewardPerStep:F3}]";
    }
}
```