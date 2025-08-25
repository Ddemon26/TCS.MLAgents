using UnityEngine;
using TCS.MLAgents.Core;
using TCS.MLAgents.Observations;
using TCS.MLAgents.Actions;
using TCS.MLAgents.Rewards;
using TCS.MLAgents.Episodes;
using TCS.MLAgents.Sensors;
using TCS.MLAgents.Decision;
using TCS.MLAgents.Utilities;

namespace TCS.MLAgents.Examples
{
    /// <summary>
    /// Example scenario demonstrating the composition-based ML-Agents system
    /// </summary>
    public class ExampleScenario : MonoBehaviour
    {
        [Header("Scenario Configuration")]
        [SerializeField] private bool enableLogging = true;
        [SerializeField] private float scenarioDuration = 30.0f;
        
        private AgentContext agentContext;
        private MLAgentComposer agentComposer;
        private VectorObservationCollector observationCollector;
        private ActionDistributor actionDistributor;
        private RewardCalculator rewardCalculator;
        private EpisodeManager episodeManager;
        private SensorManager sensorManager;
        private DecisionRouter decisionRouter;
        private StatisticsCollector statisticsCollector;
        
        private float scenarioStartTime;
        private bool scenarioActive = false;
        
        void Start()
        {
            InitializeScenario();
            StartScenario();
        }
        
        void Update()
        {
            if (!scenarioActive) return;
            
            // Update scenario time
            float elapsedTime = Time.time - scenarioStartTime;
            
            // Update all systems
            if (statisticsCollector != null)
            {
                statisticsCollector.CollectStatistics(Time.deltaTime);
            }
            
            if (decisionRouter != null)
            {
                decisionRouter.OnUpdate(Time.deltaTime);
            }
            
            // Check if scenario should end
            if (elapsedTime >= scenarioDuration)
            {
                EndScenario();
            }
        }
        
        void InitializeScenario()
        {
            LogMessage("Initializing example scenario...");
            
            // Create agent context
            agentContext = new AgentContext(gameObject);
            
            // Get or create agent composer
            agentComposer = GetComponent<MLAgentComposer>();
            if (agentComposer == null)
            {
                agentComposer = gameObject.AddComponent<MLAgentComposer>();
            }
            
            // Setup core systems
            SetupObservationSystem();
            SetupActionSystem();
            SetupRewardSystem();
            SetupEpisodeSystem();
            SetupSensorSystem();
            SetupDecisionSystem();
            SetupStatisticsSystem();
            
            LogMessage("Scenario initialization complete");
        }
        
        void SetupObservationSystem()
        {
            observationCollector = GetComponent<VectorObservationCollector>();
            if (observationCollector == null)
            {
                observationCollector = gameObject.AddComponent<VectorObservationCollector>();
            }
            
            observationCollector.Initialize();
            
            // Register with composer
            // Note: In a real implementation, this would be handled by the composer's component discovery
        }
        
        void SetupActionSystem()
        {
            actionDistributor = GetComponent<ActionDistributor>();
            if (actionDistributor == null)
            {
                actionDistributor = gameObject.AddComponent<ActionDistributor>();
            }
            
            actionDistributor.Initialize();
        }
        
        void SetupRewardSystem()
        {
            rewardCalculator = GetComponent<RewardCalculator>();
            if (rewardCalculator == null)
            {
                rewardCalculator = gameObject.AddComponent<RewardCalculator>();
            }
            
            rewardCalculator.Initialize();
        }
        
        void SetupEpisodeSystem()
        {
            episodeManager = GetComponent<EpisodeManager>();
            if (episodeManager == null)
            {
                episodeManager = gameObject.AddComponent<EpisodeManager>();
            }
            
            episodeManager.Initialize(agentContext);
        }
        
        void SetupSensorSystem()
        {
            sensorManager = GetComponent<SensorManager>();
            if (sensorManager == null)
            {
                sensorManager = gameObject.AddComponent<SensorManager>();
            }
            
            sensorManager.Initialize(agentContext);
        }
        
        void SetupDecisionSystem()
        {
            decisionRouter = new DecisionRouter();
            decisionRouter.Initialize(agentContext);
            
            // Add heuristic controller for manual control
            var heuristicController = gameObject.AddComponent<HeuristicController>();
            heuristicController.Initialize(agentContext);
            decisionRouter.AddDecisionProvider(heuristicController);
        }
        
        void SetupStatisticsSystem()
        {
            statisticsCollector = new StatisticsCollector();
            statisticsCollector.Initialize(agentContext);
            
            // Add performance monitor
            var performanceMonitor = gameObject.AddComponent<PerformanceMonitor>();
            performanceMonitor.Initialize(agentContext);
            statisticsCollector.AddStatisticsProvider(performanceMonitor);
        }
        
        public void StartScenario()
        {
            if (scenarioActive) return;
            
            scenarioActive = true;
            scenarioStartTime = Time.time;
            
            // Start episode
            if (episodeManager != null)
            {
                episodeManager.RequestEpisodeStart();
            }
            
            // Activate heuristic controller
            if (decisionRouter != null)
            {
                var heuristicController = decisionRouter.GetDecisionProvider<HeuristicController>("heuristic");
                if (heuristicController != null)
                {
                    heuristicController.SetActive(true);
                }
            }
            
            LogMessage("Scenario started");
        }
        
        public void EndScenario()
        {
            if (!scenarioActive) return;
            
            scenarioActive = false;
            
            // End episode
            if (episodeManager != null)
            {
                episodeManager.EndEpisode();
            }
            
            // Deactivate heuristic controller
            if (decisionRouter != null)
            {
                var heuristicController = decisionRouter.GetDecisionProvider<HeuristicController>("heuristic");
                if (heuristicController != null)
                {
                    heuristicController.SetActive(false);
                }
            }
            
            LogMessage("Scenario ended");
            
            // Print statistics
            if (statisticsCollector != null)
            {
                var stats = statisticsCollector.CurrentStatistics;
                LogMessage($"Final statistics - Count: {stats.Count}");
            }
        }
        
        public void ResetScenario()
        {
            if (scenarioActive)
            {
                EndScenario();
            }
            
            // Reset all systems
            if (agentContext != null)
            {
                agentContext.Reset();
            }
            
            if (episodeManager != null)
            {
                episodeManager.ResetAllHandlers();
            }
            
            if (statisticsCollector != null)
            {
                statisticsCollector.ResetStatistics();
            }
            
            LogMessage("Scenario reset");
        }
        
        public void ToggleHeuristicControl()
        {
            if (decisionRouter == null) return;
            
            var heuristicController = decisionRouter.GetDecisionProvider<HeuristicController>("heuristic");
            if (heuristicController != null)
            {
                bool newState = !heuristicController.IsActive;
                heuristicController.SetActive(newState);
                LogMessage($"Heuristic control: {newState}");
            }
        }
        
        public string GetScenarioStatus()
        {
            if (!scenarioActive)
            {
                return "Inactive";
            }
            
            float elapsedTime = Time.time - scenarioStartTime;
            float remainingTime = scenarioDuration - elapsedTime;
            
            return $"Active - Time: {elapsedTime:F1}s / {scenarioDuration:F1}s (Remaining: {remainingTime:F1}s)";
        }
        
        public string GetStatisticsSummary()
        {
            if (statisticsCollector == null)
            {
                return "No statistics collector";
            }
            
            var stats = statisticsCollector.CurrentStatistics;
            return $"Statistics - Items: {stats.Count}, Updates: {statisticsCollector.UpdateCount}";
        }
        
        void OnGUI()
        {
            if (!scenarioActive) return;
            
            // Display scenario status
            GUI.Label(new Rect(10, 10, 300, 20), $"Status: {GetScenarioStatus()}");
            GUI.Label(new Rect(10, 30, 300, 20), $"Statistics: {GetStatisticsSummary()}");
            
            // Display controls
            if (GUI.Button(new Rect(10, 60, 150, 30), "Reset Scenario"))
            {
                ResetScenario();
            }
            
            if (GUI.Button(new Rect(10, 100, 150, 30), "Toggle Control"))
            {
                ToggleHeuristicControl();
            }
        }
        
        void LogMessage(string message)
        {
            if (enableLogging)
            {
                Debug.Log($"[ExampleScenario] {message}");
            }
        }
        
        void OnDestroy()
        {
            // Cleanup
            if (statisticsCollector != null)
            {
                statisticsCollector.ResetStatistics();
            }
            
            if (decisionRouter != null)
            {
                // DecisionRouter cleanup would happen here
            }
        }
    }
}