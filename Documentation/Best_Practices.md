# TCS ML-Agents Best Practices Guide

This guide provides recommendations and best practices for developing effective ML-Agents using the TCS ML-Agents composition system.

## Table of Contents
1. [Architecture Design](#architecture-design)
2. [Component Development](#component-development)
3. [Observation Design](#observation-design)
4. [Action Design](#action-design)
5. [Reward Engineering](#reward-engineering)
6. [Performance Optimization](#performance-optimization)
7. [Testing Strategies](#testing-strategies)
8. [Debugging Techniques](#debugging-techniques)
9. [Training Workflow](#training-workflow)

## Architecture Design

### Composition Over Inheritance
Always favor composition over inheritance when designing your agent systems:

```csharp
// GOOD: Composition-based approach
public class MyAgent : MonoBehaviour
{
    private VectorObservationCollector m_ObservationCollector;
    private ActionDistributor m_ActionDistributor;
    private RewardCalculator m_RewardCalculator;
    
    void Start()
    {
        m_ObservationCollector = gameObject.AddComponent<VectorObservationCollector>();
        m_ActionDistributor = gameObject.AddComponent<ActionDistributor>();
        m_RewardCalculator = gameObject.AddComponent<RewardCalculator>();
    }
}

// AVOID: Inheritance-based approach
public class MyAgent : Agent
{
    // Complex inheritance hierarchy with tight coupling
}
```

### Single Responsibility Principle
Each component should have exactly one reason to change:

```csharp
// GOOD: Separate components for separate concerns
public class MovementActionReceiver : IActionReceiver { /* Handles movement */ }
public class RotationActionReceiver : IActionReceiver { /* Handles rotation */ }
public class CombatActionReceiver : IActionReceiver { /* Handles combat */ }

// AVOID: Monolithic component
public class MonsterActionReceiver : IActionReceiver 
{ 
    /* Handles movement, rotation, combat, AI, etc. - too many responsibilities */
}
```

### Interface Segregation
Use specific interfaces for specific functionality:

```csharp
// GOOD: Specific interfaces for specific needs
public interface IObservationProvider { /* Observation-specific methods */ }
public interface IActionReceiver { /* Action-specific methods */ }
public interface IRewardProvider { /* Reward-specific methods */ }

// AVOID: Generic interface trying to do everything
public interface IAgentComponent 
{
    void Initialize();
    void CollectObservations();
    void HandleActions();
    void CalculateRewards();
    void OnEpisodeBegin();
    void OnEpisodeEnd();
    // ... many more methods
}
```

## Component Development

### Interface Implementation
Always implement the full interface contract:

```csharp
// GOOD: Complete interface implementation
public class MyObservationProvider : IObservationProvider
{
    public string ProviderName => "MyProvider";
    public int Priority => 50;
    public bool IsActive => true;
    public int ObservationSize => 3;
    
    public void Initialize(AgentContext context) { /* Implementation */ }
    public bool ValidateProvider(AgentContext context) { /* Implementation */ }
    public void CollectObservations(VectorSensor sensor, AgentContext context) { /* Implementation */ }
    public void OnEpisodeBegin(AgentContext context) { /* Implementation */ }
    public void OnEpisodeEnd(AgentContext context) { /* Implementation */ }
    public void OnUpdate(AgentContext context, float deltaTime) { /* Implementation */ }
    public void SetActive(bool active) { /* Implementation */ }
    public string GetDebugInfo() { /* Implementation */ }
}

// AVOID: Partial implementation
public class MyIncompleteProvider : IObservationProvider
{
    // Missing several required methods
    public void CollectObservations(VectorSensor sensor, AgentContext context) { }
    // ... only implements some methods
}
```

### Component Lifecycle Management
Properly manage component initialization and cleanup:

```csharp
// GOOD: Proper lifecycle management
public class MyComponent : IMLAgent
{
    private bool m_IsInitialized = false;
    private List<IDisposable> m_Disposables = new List<IDisposable>();
    
    public void Initialize()
    {
        if (m_IsInitialized) return;
        
        try
        {
            // Initialize resources
            var resource = new ExpensiveResource();
            m_Disposables.Add(resource);
            
            // Subscribe to events
            SomeEventManager.OnEvent += HandleEvent;
            
            m_IsInitialized = true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to initialize MyComponent: {ex.Message}");
            throw;
        }
    }
    
    public void OnDestroy()
    {
        // Clean up resources
        foreach (var disposable in m_Disposables)
        {
            disposable?.Dispose();
        }
        m_Disposables.Clear();
        
        // Unsubscribe from events
        SomeEventManager.OnEvent -= HandleEvent;
        
        m_IsInitialized = false;
    }
    
    private void HandleEvent(object sender, EventArgs e)
    {
        // Check if still initialized to avoid issues during cleanup
        if (!m_IsInitialized) return;
        
        // Handle event
    }
}
```

### Error Handling and Validation
Implement robust error handling and validation:

```csharp
// GOOD: Comprehensive validation and error handling
public class MyRewardProvider : IRewardProvider
{
    [SerializeField] private float m_RewardWeight = 1.0f;
    [SerializeField] private string m_TargetTag = "Target";
    
    public bool ValidateProvider(AgentContext context)
    {
        if (context == null)
        {
            Debug.LogError("Context is null");
            return false;
        }
        
        if (string.IsNullOrEmpty(m_TargetTag))
        {
            Debug.LogWarning("Target tag is not set");
            return false;
        }
        
        if (m_RewardWeight == 0f)
        {
            Debug.LogWarning("Reward weight is zero - provider will have no effect");
        }
        
        // Validate that target objects exist
        var targets = GameObject.FindGameObjectsWithTag(m_TargetTag);
        if (targets.Length == 0)
        {
            Debug.LogWarning($"No objects found with tag '{m_TargetTag}'");
        }
        
        return true;
    }
    
    public float CalculateReward(AgentContext context, float deltaTime)
    {
        try
        {
            // Validate inputs
            if (context == null || deltaTime <= 0f)
            {
                return 0f;
            }
            
            // Calculate reward logic
            return CalculateRewardInternal(context, deltaTime);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error calculating reward: {ex.Message}");
            return 0f; // Return neutral reward on error
        }
    }
    
    private float CalculateRewardInternal(AgentContext context, float deltaTime)
    {
        // Actual reward calculation logic
        return 0f;
    }
}
```

## Observation Design

### Observation Space Design
Design your observation space carefully for optimal learning:

```csharp
// GOOD: Thoughtful observation design
public class SmartObservationProvider : IObservationProvider
{
    public int ObservationSize => 12; // Keep it reasonable
    
    public void CollectObservations(VectorSensor sensor, AgentContext context)
    {
        // 1. Normalize values to [-1, 1] or [0, 1]
        float normalizedDistance = Mathf.Clamp01(distanceToTarget / maxDistance);
        sensor.AddObservation(normalizedDistance);
        
        // 2. Use relative coordinates when possible
        Vector3 relativePosition = targetPosition - agentPosition;
        sensor.AddObservation(relativePosition.normalized); // 3 values
        
        // 3. Include velocity information
        sensor.AddObservation(agentVelocity.magnitude / maxVelocity); // 1 value
        
        // 4. Encode categorical information efficiently
        int targetType = (int)GetCurrentTargetType(); // 0, 1, 2, etc.
        // One-hot encode if few categories, otherwise use embedding-like approach
        sensor.AddObservation(OneHotEncode(targetType, maxTargetTypes)); // N values
        
        // 5. Include time information for temporal tasks
        float normalizedEpisodeTime = context.StepCount / (float)context.MaxSteps;
        sensor.AddObservation(normalizedEpisodeTime);
    }
    
    private float[] OneHotEncode(int value, int maxCategories)
    {
        var result = new float[maxCategories];
        if (value >= 0 && value < maxCategories)
        {
            result[value] = 1f;
        }
        return result;
    }
}
```

### Observation Frequency and Granularity
Balance observation frequency with computational cost:

```csharp
// GOOD: Efficient observation collection
public class EfficientObservationProvider : IObservationProvider
{
    private float m_LastUpdateTime = 0f;
    private float m_UpdateInterval = 0.1f; // Update every 0.1 seconds
    private Vector3 m_CachedPosition = Vector3.zero;
    private bool m_NeedsUpdate = true;
    
    public void OnUpdate(AgentContext context, float deltaTime)
    {
        // Only update when needed
        if (Time.time - m_LastUpdateTime >= m_UpdateInterval)
        {
            m_LastUpdateTime = Time.time;
            m_NeedsUpdate = true;
        }
    }
    
    public void CollectObservations(VectorSensor sensor, AgentContext context)
    {
        if (m_NeedsUpdate)
        {
            // Expensive calculations only when needed
            m_CachedPosition = CalculateExpensivePosition();
            m_NeedsUpdate = false;
        }
        
        sensor.AddObservation(m_CachedPosition);
    }
}
```

## Action Design

### Action Space Design
Design action spaces that are intuitive and effective:

```csharp
// GOOD: Well-designed action space
public class IntuitiveActionReceiver : IActionReceiver
{
    public int ContinuousActionCount => 4; // Forward/Back, Strafe Left/Right, Turn Left/Right, Jump
    public int DiscreteActionBranchCount => 1;
    public int[] DiscreteActionBranchSizes => new int[] { 3 }; // None, Light Attack, Heavy Attack
    
    public void ReceiveContinuousActions(float[] actions, int startIndex, AgentContext context)
    {
        float forwardBack = Mathf.Clamp(actions[startIndex], -1f, 1f);
        float strafe = Mathf.Clamp(actions[startIndex + 1], -1f, 1f);
        float turn = Mathf.Clamp(actions[startIndex + 2], -1f, 1f);
        float jump = Mathf.Clamp01(actions[startIndex + 3]); // Binary action (0 or 1)
        
        // Apply forces/torques based on actions
        ApplyMovement(forwardBack, strafe);
        ApplyTurning(turn);
        
        if (jump > 0.5f)
        {
            ApplyJump();
        }
    }
    
    public void ReceiveDiscreteActions(int[] actions, int startIndex, AgentContext context)
    {
        int attackType = actions[startIndex];
        
        switch (attackType)
        {
            case 1: // Light attack
                PerformLightAttack();
                break;
            case 2: // Heavy attack
                PerformHeavyAttack();
                break;
        }
    }
}
```

### Action Smoothing and Filtering
Implement action smoothing for better control:

```csharp
// GOOD: Action smoothing
public class SmoothActionReceiver : IActionReceiver
{
    private float[] m_PreviousActions;
    private float m_SmoothingFactor = 0.1f;
    
    public void ReceiveContinuousActions(float[] actions, int startIndex, AgentContext context)
    {
        if (m_PreviousActions == null)
        {
            m_PreviousActions = new float[ContinuousActionCount];
            Array.Copy(actions, startIndex, m_PreviousActions, 0, ContinuousActionCount);
        }
        
        // Smooth actions
        for (int i = 0; i < ContinuousActionCount; i++)
        {
            float currentAction = actions[startIndex + i];
            m_PreviousActions[i] = Mathf.Lerp(m_PreviousActions[i], currentAction, m_SmoothingFactor);
        }
        
        // Apply smoothed actions
        ApplySmoothedActions(m_PreviousActions);
    }
}
```

## Reward Engineering

### Reward Design Principles
Follow these principles for effective reward design:

```csharp
// GOOD: Well-designed reward system
public class BalancedRewardProvider : IRewardProvider
{
    [Header("Reward Settings")]
    [SerializeField] private float m_ProgressWeight = 1.0f;
    [SerializeField] private float m_EfficiencyWeight = 0.1f;
    [SerializeField] private float m_TimePenalty = -0.001f;
    [SerializeField] private float m_CompletionBonus = 10.0f;
    
    // Track progress for differential rewards
    private float m_PreviousProgress = 0f;
    private float m_EnergyConsumed = 0f;
    
    public float CalculateReward(AgentContext context, float deltaTime)
    {
        float totalReward = 0f;
        
        // 1. Progress-based rewards (differential)
        float currentProgress = CalculateProgress(context);
        float progressReward = (currentProgress - m_PreviousProgress) * m_ProgressWeight;
        totalReward += progressReward;
        m_PreviousProgress = currentProgress;
        
        // 2. Efficiency rewards (inverse of resource consumption)
        float energyCost = CalculateEnergyConsumption(context, deltaTime);
        m_EnergyConsumed += energyCost;
        float efficiencyReward = -energyCost * m_EfficiencyWeight;
        totalReward += efficiencyReward;
        
        // 3. Time penalties (encourage completion)
        totalReward += m_TimePenalty * deltaTime;
        
        // 4. Sparse rewards for major milestones
        if (CheckCompletionCondition(context))
        {
            totalReward += m_CompletionBonus;
        }
        
        // 5. Reward shaping (ensure rewards are well-scaled)
        totalReward = Mathf.Clamp(totalReward, -1f, 1f);
        
        return totalReward;
    }
    
    private float CalculateProgress(AgentContext context)
    {
        // Example: distance to goal
        float distanceToGoal = Vector3.Distance(context.AgentGameObject.transform.position, goalPosition);
        return 1f - Mathf.Clamp01(distanceToGoal / maxDistance);
    }
    
    private float CalculateEnergyConsumption(AgentContext context, float deltaTime)
    {
        // Example: energy based on movement magnitude
        float movementMagnitude = context.AgentRigidbody.velocity.magnitude;
        return movementMagnitude * deltaTime;
    }
}
```

### Reward Shaping Techniques
Use proper reward shaping techniques:

```csharp
// GOOD: Potential-based reward shaping
public class PotentialBasedRewardProvider : IRewardProvider
{
    private Vector3 m_PreviousPosition = Vector3.zero;
    private float m_PreviousPotential = 0f;
    
    public float CalculateReward(AgentContext context, float deltaTime)
    {
        Vector3 currentPosition = context.AgentGameObject.transform.position;
        
        // Define potential function (example: negative distance to goal)
        float currentPotential = -Vector3.Distance(currentPosition, goalPosition);
        
        // Potential-based reward shaping: ΔΦ = Φ(s') - Φ(s)
        float potentialDifference = currentPotential - m_PreviousPotential;
        
        // Store current state
        m_PreviousPosition = currentPosition;
        m_PreviousPotential = currentPotential;
        
        return potentialDifference;
    }
}
```

## Performance Optimization

### Efficient Memory Management
Minimize allocations and manage memory efficiently:

```csharp
// GOOD: Efficient memory usage
public class EfficientComponent : IMLAgent
{
    // Reuse arrays instead of creating new ones
    private float[] m_TemporaryArray = new float[10];
    private Vector3[] m_PositionCache = new Vector3[5];
    
    // Use object pooling for frequently created objects
    private ObjectPool<MyExpensiveObject> m_ObjectPool;
    
    public void OnUpdate(AgentContext context, float deltaTime)
    {
        // Reuse existing arrays
        for (int i = 0; i < m_TemporaryArray.Length; i++)
        {
            m_TemporaryArray[i] = 0f;
        }
        
        // Use pooled objects
        var obj = m_ObjectPool.Get();
        try
        {
            // Use object
        }
        finally
        {
            // Return to pool
            m_ObjectPool.Return(obj);
        }
    }
}
```

### Computational Efficiency
Optimize expensive calculations:

```csharp
// GOOD: Computational efficiency
public class OptimizedObservationProvider : IObservationProvider
{
    private float m_LastCalculationTime = 0f;
    private float m_CalculationInterval = 0.05f; // 20 FPS
    private float[] m_CachedObservations;
    private bool m_CacheValid = false;
    
    public void OnUpdate(AgentContext context, float deltaTime)
    {
        // Only recalculate when needed
        if (Time.time - m_LastCalculationTime >= m_CalculationInterval)
        {
            m_LastCalculationTime = Time.time;
            m_CacheValid = false;
        }
    }
    
    public void CollectObservations(VectorSensor sensor, AgentContext context)
    {
        if (!m_CacheValid)
        {
            CalculateObservations();
            m_CacheValid = true;
        }
        
        // Add cached observations
        for (int i = 0; i < m_CachedObservations.Length; i++)
        {
            sensor.AddObservation(m_CachedObservations[i]);
        }
    }
    
    private void CalculateObservations()
    {
        // Expensive calculations here
        // Store results in m_CachedObservations
    }
}
```

## Testing Strategies

### Unit Testing
Write comprehensive unit tests for individual components:

```csharp
// GOOD: Comprehensive unit testing
[TestFixture]
public class RewardProviderTests
{
    private MockAgentContext m_MockContext;
    private TestRewardProvider m_RewardProvider;
    
    [SetUp]
    public void Setup()
    {
        m_MockContext = new MockAgentContext();
        m_RewardProvider = new TestRewardProvider();
        m_RewardProvider.Initialize(m_MockContext);
    }
    
    [Test]
    public void CalculateReward_ShouldReturnZero_WhenContextIsNull()
    {
        float reward = m_RewardProvider.CalculateReward(null, 0.016f);
        Assert.AreEqual(0f, reward, 0.001f);
    }
    
    [Test]
    public void CalculateReward_ShouldReturnExpectedValue_WhenConditionsMet()
    {
        // Arrange
        m_MockContext.SetTestData("distance", 5f);
        
        // Act
        float reward = m_RewardProvider.CalculateReward(m_MockContext, 0.016f);
        
        // Assert
        Assert.AreEqual(0.5f, reward, 0.001f); // Expected reward calculation
    }
    
    [Test]
    public void OnEpisodeBegin_ShouldResetState()
    {
        // Arrange
        m_RewardProvider.SomeInternalState = 100f;
        
        // Act
        m_RewardProvider.OnEpisodeBegin(m_MockContext);
        
        // Assert
        Assert.AreEqual(0f, m_RewardProvider.SomeInternalState);
    }
}
```

### Integration Testing
Test component interactions and complete scenarios:

```csharp
// GOOD: Integration testing
[TestFixture]
public class AgentIntegrationTests
{
    private GameObject m_TestAgent;
    private MLAgentComposer m_Composer;
    
    [SetUp]
    public void Setup()
    {
        m_TestAgent = new GameObject("TestAgent");
        m_Composer = m_TestAgent.AddComponent<MLAgentComposer>();
    }
    
    [TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(m_TestAgent);
    }
    
    [Test]
    public void FullEpisodeCycle_ShouldWorkCorrectly()
    {
        // Arrange
        SetupTestComponents();
        
        // Act
        m_Composer.OnEpisodeBegin();
        SimulateSteps(100);
        m_Composer.EndEpisode();
        
        // Assert
        Assert.AreEqual(100, m_Composer.Context.StepCount);
        Assert.IsFalse(m_Composer.Context.IsEpisodeActive);
    }
    
    private void SetupTestComponents()
    {
        // Add test components
        var observationCollector = m_TestAgent.AddComponent<VectorObservationCollector>();
        var actionDistributor = m_TestAgent.AddComponent<ActionDistributor>();
        var rewardCalculator = m_TestAgent.AddComponent<RewardCalculator>();
        
        // Add mock providers
        var mockObserver = new MockObservationProvider();
        observationCollector.RegisterProvider(mockObserver);
        
        var mockReceiver = new MockActionReceiver();
        actionDistributor.RegisterReceiver(mockReceiver);
        
        var mockProvider = new MockRewardProvider();
        rewardCalculator.RegisterProvider(mockProvider);
    }
    
    private void SimulateSteps(int stepCount)
    {
        for (int i = 0; i < stepCount; i++)
        {
            var actionBuffers = new ActionBuffers(
                new float[2], // continuous actions
                new int[1]    // discrete actions
            );
            
            m_Composer.OnActionReceived(actionBuffers);
        }
    }
}
```

## Debugging Techniques

### Debugging Information
Provide useful debugging information:

```csharp
// GOOD: Helpful debugging
public class DebuggableComponent : IMLAgent
{
    [SerializeField] private bool m_EnableDebugLogging = false;
    
    public string GetDebugInfo()
    {
        return $"DebuggableComponent[" +
               $"State: {m_CurrentState}, " +
               $"Progress: {m_Progress:F2}, " +
               $"LastAction: {m_LastActionTime:F2}s ago, " +
               $"Rewards: {m_CumulativeReward:F3}" +
               $"]";
    }
    
    private void LogDebug(string message)
    {
        if (m_EnableDebugLogging)
        {
            Debug.Log($"[DebuggableComponent] {message}");
        }
    }
    
    public void OnEpisodeBegin(AgentContext context)
    {
        ResetState();
        LogDebug($"Episode began - Initial state: {m_CurrentState}");
    }
    
    public void OnActionReceived(ActionBuffers actionBuffers)
    {
        LogDebug($"Received actions - Continuous: {actionBuffers.ContinuousActions.Length}, " +
                 $"Discrete: {actionBuffers.DiscreteActions.Length}");
    }
}
```

### Visualization Tools
Use visualization for better understanding:

```csharp
// GOOD: Visualization support
public class VisualizableObservationProvider : IObservationProvider
{
    [SerializeField] private bool m_ShowDebugVisualization = false;
    
    public void OnUpdate(AgentContext context, float deltaTime)
    {
        if (m_ShowDebugVisualization)
        {
            // Visualize raycasts, positions, etc.
            VisualizeObservations(context);
        }
    }
    
    private void VisualizeObservations(AgentContext context)
    {
        // Draw gizmos for raycasts
        foreach (var ray in m_RaycastDirections)
        {
            Vector3 origin = context.AgentGameObject.transform.position;
            Vector3 direction = context.AgentGameObject.transform.TransformDirection(ray);
            Debug.DrawRay(origin, direction * m_RayLength, Color.red, 0.1f);
        }
        
        // Draw target positions
        foreach (var target in m_DetectedTargets)
        {
            Debug.DrawLine(context.AgentGameObject.transform.position, target.position, Color.green, 0.1f);
        }
    }
}
```

## Training Workflow

### Training Setup
Establish a consistent training workflow:

```csharp
// GOOD: Training workflow setup
public class TrainingWorkflowHelper
{
    public static void SetupTrainingEnvironment()
    {
        // 1. Configure behavior for training
        var config = Resources.Load<MLBehaviorConfig>("TrainingConfig");
        if (config != null)
        {
            config.SetBehaviorType(MLBehaviorConfig.BehaviorType.Training);
            config.SetLearningRate(3e-4f);
            config.SetBatchSize(1024);
            config.SetBufferSize(10240);
        }
        
        // 2. Enable performance monitoring
        var performanceMonitor = GameObject.FindObjectOfType<PerformanceMonitor>();
        if (performanceMonitor != null)
        {
            performanceMonitor.SetActive(true);
            performanceMonitor.SetUpdateInterval(1.0f);
        }
        
        // 3. Disable unnecessary debug visualization
        DisableDebugVisualization();
        
        // 4. Set appropriate time scale
        Time.timeScale = 20f; // Fast training
        
        Debug.Log("Training environment configured");
    }
    
    public static void SetupInferenceEnvironment()
    {
        // 1. Configure behavior for inference
        var config = Resources.Load<MLBehaviorConfig>("InferenceConfig");
        if (config != null)
        {
            config.SetBehaviorType(MLBehaviorConfig.BehaviorType.Inference);
            config.SetModelPath("models/latest_model.onnx");
        }
        
        // 2. Disable performance monitoring
        var performanceMonitor = GameObject.FindObjectOfType<PerformanceMonitor>();
        if (performanceMonitor != null)
        {
            performanceMonitor.SetActive(false);
        }
        
        // 3. Enable debug visualization if needed
        EnableDebugVisualization();
        
        // 4. Set normal time scale
        Time.timeScale = 1f;
        
        Debug.Log("Inference environment configured");
    }
    
    private static void DisableDebugVisualization()
    {
        var visualizationComponents = GameObject.FindObjectsOfType<MonoBehaviour>()
            .Where(c => c.GetType().Name.Contains("Visualizer") || c.GetType().Name.Contains("Debug"))
            .ToArray();
            
        foreach (var component in visualizationComponents)
        {
            component.enabled = false;
        }
    }
    
    private static void EnableDebugVisualization()
    {
        var visualizationComponents = GameObject.FindObjectsOfType<MonoBehaviour>()
            .Where(c => c.GetType().Name.Contains("Visualizer") || c.GetType().Name.Contains("Debug"))
            .ToArray();
            
        foreach (var component in visualizationComponents)
        {
            component.enabled = true;
        }
    }
}
```

### Monitoring and Evaluation
Monitor training progress effectively:

```csharp
// GOOD: Training monitoring
public class TrainingMonitor
{
    private StatisticsCollector m_StatisticsCollector;
    private Dictionary<string, float> m_BaselinePerformance;
    
    public void StartMonitoring()
    {
        // Capture baseline performance
        CaptureBaselinePerformance();
        
        // Start collecting statistics
        m_StatisticsCollector = new StatisticsCollector();
        m_StatisticsCollector.Initialize(AgentContext);
        m_StatisticsCollector.SetUpdateInterval(5.0f);
    }
    
    public TrainingStatus EvaluateProgress()
    {
        var currentStats = m_StatisticsCollector.CurrentStatistics;
        
        // Check for improvement
        bool improved = CheckImprovement(currentStats, m_BaselinePerformance);
        
        // Check for convergence
        bool converged = CheckConvergence(currentStats);
        
        // Check for instability
        bool unstable = CheckInstability(currentStats);
        
        return new TrainingStatus
        {
            Improved = improved,
            Converged = converged,
            Unstable = unstable,
            CurrentPerformance = CalculatePerformanceScore(currentStats)
        };
    }
    
    private bool CheckImprovement(Dictionary<string, float> current, Dictionary<string, float> baseline)
    {
        float currentReward = current.GetValueOrDefault("AverageEpisodeReward", 0f);
        float baselineReward = baseline.GetValueOrDefault("AverageEpisodeReward", 0f);
        
        return currentReward > baselineReward + 0.1f; // 10% improvement threshold
    }
    
    private bool CheckConvergence(Dictionary<string, float> current)
    {
        float rewardVariance = current.GetValueOrDefault("RewardVariance", 0f);
        return rewardVariance < 0.01f; // Low variance indicates convergence
    }
    
    private bool CheckInstability(Dictionary<string, float> current)
    {
        float lossVariance = current.GetValueOrDefault("LossVariance", 0f);
        return lossVariance > 1.0f; // High variance indicates instability
    }
}

public class TrainingStatus
{
    public bool Improved { get; set; }
    public bool Converged { get; set; }
    public bool Unstable { get; set; }
    public float CurrentPerformance { get; set; }
}
```

This best practices guide provides a comprehensive foundation for developing effective ML-Agents with the TCS ML-Agents system. Following these guidelines will help you create robust, efficient, and maintainable agent systems.