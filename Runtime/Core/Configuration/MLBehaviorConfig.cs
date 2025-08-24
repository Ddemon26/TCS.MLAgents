using System;
using System.Collections.Generic;
using UnityEngine;
using TCS.MLAgents.Core;

namespace TCS.MLAgents.Configuration {
    /// <summary>
    /// ScriptableObject for complete behavior configuration that defines all aspects of an ML agent's behavior.
    /// </summary>
    [CreateAssetMenu(fileName = "MLBehaviorConfig", menuName = "TCS ML-Agents/MLBehaviorConfig", order = 1)]
    public class MLBehaviorConfig : ScriptableObject {
        [Header("Behavior Identity")]
        [SerializeField] private string m_BehaviorName = "DefaultBehavior";
        [SerializeField] private string m_Description = "Default behavior configuration";
        [SerializeField] private BehaviorType m_BehaviorType = BehaviorType.Inference;
        [SerializeField] private string m_ModelPath = "";
        
        [Header("Observation Configuration")]
        [SerializeField] private bool m_UseVectorObservations = true;
        [SerializeField] private bool m_UseVisualObservations = false;
        [SerializeField] private int m_VectorObservationSize = 0;
        [SerializeField] private int m_StackedVectorObservations = 1;
        [SerializeField] private bool m_NormalizeVectorObservations = true;
        [SerializeField] private List<string> m_ObservationProviders = new List<string>();
        
        [Header("Action Configuration")]
        [SerializeField] private int m_ContinuousActionSize = 0;
        [SerializeField] private int[] m_DiscreteActionBranches = new int[0];
        [SerializeField] private List<string> m_ActionReceivers = new List<string>();
        
        [Header("Reward Configuration")]
        [SerializeField] private float m_MaxStepReward = -0.01f;
        [SerializeField] private bool m_EnableRewardClipping = true;
        [SerializeField] private float m_RewardClippingMin = -1.0f;
        [SerializeField] private float m_RewardClippingMax = 1.0f;
        [SerializeField] private List<string> m_RewardProviders = new List<string>();
        
        [Header("Episode Configuration")]
        [SerializeField] private int m_MaxSteps = 1000;
        [SerializeField] private float m_EpisodeTimeout = 30.0f;
        [SerializeField] private List<string> m_EpisodeHandlers = new List<string>();
        
        [Header("Sensor Configuration")]
        [SerializeField] private List<string> m_SensorProviders = new List<string>();
        [SerializeField] private bool m_EnableSensorLOD = true;
        
        [Header("Decision Configuration")]
        [SerializeField] private List<string> m_DecisionProviders = new List<string>();
        [SerializeField] private string m_DefaultDecisionProvider = "inference";
        
        [Header("Training Configuration")]
        [SerializeField] private float m_LearningRate = 3e-4f;
        [SerializeField] private int m_BatchSize = 1024;
        [SerializeField] private int m_BufferSize = 10240;
        [SerializeField] private float m_Beta = 5e-3f;
        [SerializeField] private float m_Epsilon = 0.2f;
        [SerializeField] private int m_NumEpochs = 3;
        [SerializeField] private string m_TrainerType = "ppo";
        
        [Header("Performance Configuration")]
        [SerializeField] private bool m_EnablePerformanceMonitoring = false;
        [SerializeField] private float m_StatisticsUpdateInterval = 1.0f;
        [SerializeField] private List<string> m_StatisticsProviders = new List<string>();
        
        [Header("Advanced Settings")]
        [SerializeField] private bool m_UseDeterministicInference = true;
        [SerializeField] private float m_TimeScale = 1.0f;
        [SerializeField] private bool m_EnableActionMasking = false;
        [SerializeField] private List<string> m_ComponentsToExclude = new List<string>();
        
        public enum BehaviorType {
            Inference,
            Training,
            Heuristic
        }
        
        // Properties
        public string BehaviorName => m_BehaviorName;
        public string Description => m_Description;
        public BehaviorType Type => m_BehaviorType;
        public string ModelPath => m_ModelPath;
        
        public bool UseVectorObservations => m_UseVectorObservations;
        public bool UseVisualObservations => m_UseVisualObservations;
        public int VectorObservationSize => m_VectorObservationSize;
        public int StackedVectorObservations => m_StackedVectorObservations;
        public bool NormalizeVectorObservations => m_NormalizeVectorObservations;
        public List<string> ObservationProviders => new List<string>(m_ObservationProviders);
        
        public int ContinuousActionSize => m_ContinuousActionSize;
        public int[] DiscreteActionBranches => (int[])m_DiscreteActionBranches.Clone();
        public List<string> ActionReceivers => new List<string>(m_ActionReceivers);
        
        public float MaxStepReward => m_MaxStepReward;
        public bool EnableRewardClipping => m_EnableRewardClipping;
        public float RewardClippingMin => m_RewardClippingMin;
        public float RewardClippingMax => m_RewardClippingMax;
        public List<string> RewardProviders => new List<string>(m_RewardProviders);
        
        public int MaxSteps => m_MaxSteps;
        public float EpisodeTimeout => m_EpisodeTimeout;
        public List<string> EpisodeHandlers => new List<string>(m_EpisodeHandlers);
        
        public List<string> SensorProviders => new List<string>(m_SensorProviders);
        public bool EnableSensorLOD => m_EnableSensorLOD;
        
        public List<string> DecisionProviders => new List<string>(m_DecisionProviders);
        public string DefaultDecisionProvider => m_DefaultDecisionProvider;
        
        public float LearningRate => m_LearningRate;
        public int BatchSize => m_BatchSize;
        public int BufferSize => m_BufferSize;
        public float Beta => m_Beta;
        public float Epsilon => m_Epsilon;
        public int NumEpochs => m_NumEpochs;
        public string TrainerType => m_TrainerType;
        
        public bool EnablePerformanceMonitoring => m_EnablePerformanceMonitoring;
        public float StatisticsUpdateInterval => m_StatisticsUpdateInterval;
        public List<string> StatisticsProviders => new List<string>(m_StatisticsProviders);
        
        public bool UseDeterministicInference => m_UseDeterministicInference;
        public float TimeScale => m_TimeScale;
        public bool EnableActionMasking => m_EnableActionMasking;
        public List<string> ComponentsToExclude => new List<string>(m_ComponentsToExclude);
        
        // Methods to modify configuration
        public void SetBehaviorName(string name) {
            m_BehaviorName = name;
        }
        
        public void SetBehaviorType(BehaviorType type) {
            m_BehaviorType = type;
        }
        
        public void SetModelPath(string path) {
            m_ModelPath = path;
        }
        
        public void AddObservationProvider(string provider) {
            if (!m_ObservationProviders.Contains(provider)) {
                m_ObservationProviders.Add(provider);
            }
        }
        
        public void RemoveObservationProvider(string provider) {
            m_ObservationProviders.Remove(provider);
        }
        
        public void AddActionReceiver(string receiver) {
            if (!m_ActionReceivers.Contains(receiver)) {
                m_ActionReceivers.Add(receiver);
            }
        }
        
        public void RemoveActionReceiver(string receiver) {
            m_ActionReceivers.Remove(receiver);
        }
        
        public void AddRewardProvider(string provider) {
            if (!m_RewardProviders.Contains(provider)) {
                m_RewardProviders.Add(provider);
            }
        }
        
        public void RemoveRewardProvider(string provider) {
            m_RewardProviders.Remove(provider);
        }
        
        public void AddEpisodeHandler(string handler) {
            if (!m_EpisodeHandlers.Contains(handler)) {
                m_EpisodeHandlers.Add(handler);
            }
        }
        
        public void RemoveEpisodeHandler(string handler) {
            m_EpisodeHandlers.Remove(handler);
        }
        
        public void AddSensorProvider(string provider) {
            if (!m_SensorProviders.Contains(provider)) {
                m_SensorProviders.Add(provider);
            }
        }
        
        public void RemoveSensorProvider(string provider) {
            m_SensorProviders.Remove(provider);
        }
        
        public void AddDecisionProvider(string provider) {
            if (!m_DecisionProviders.Contains(provider)) {
                m_DecisionProviders.Add(provider);
            }
        }
        
        public void RemoveDecisionProvider(string provider) {
            m_DecisionProviders.Remove(provider);
        }
        
        public void AddStatisticsProvider(string provider) {
            if (!m_StatisticsProviders.Contains(provider)) {
                m_StatisticsProviders.Add(provider);
            }
        }
        
        public void RemoveStatisticsProvider(string provider) {
            m_StatisticsProviders.Remove(provider);
        }
        
        public void AddComponentToExclude(string component) {
            if (!m_ComponentsToExclude.Contains(component)) {
                m_ComponentsToExclude.Add(component);
            }
        }
        
        public void RemoveComponentToExclude(string component) {
            m_ComponentsToExclude.Remove(component);
        }
        
        // Validation methods
        public bool Validate(out string errorMessage) {
            errorMessage = "";
            
            if (string.IsNullOrEmpty(m_BehaviorName)) {
                errorMessage = "Behavior name cannot be empty";
                return false;
            }
            
            if (m_BehaviorType == BehaviorType.Inference && string.IsNullOrEmpty(m_ModelPath)) {
                errorMessage = "Model path must be specified for inference behavior";
                return false;
            }
            
            if (m_VectorObservationSize < 0) {
                errorMessage = "Vector observation size must be non-negative";
                return false;
            }
            
            if (m_ContinuousActionSize < 0) {
                errorMessage = "Continuous action size must be non-negative";
                return false;
            }
            
            if (m_DiscreteActionBranches != null) {
                foreach (int branch in m_DiscreteActionBranches) {
                    if (branch <= 0) {
                        errorMessage = "Discrete action branches must be positive";
                        return false;
                    }
                }
            }
            
            if (m_MaxSteps <= 0) {
                errorMessage = "Max steps must be positive";
                return false;
            }
            
            return true;
        }
        
        // Clone method
        public MLBehaviorConfig Clone() {
            var clone = CreateInstance<MLBehaviorConfig>();
            
            clone.m_BehaviorName = m_BehaviorName;
            clone.m_Description = m_Description;
            clone.m_BehaviorType = m_BehaviorType;
            clone.m_ModelPath = m_ModelPath;
            
            clone.m_UseVectorObservations = m_UseVectorObservations;
            clone.m_UseVisualObservations = m_UseVisualObservations;
            clone.m_VectorObservationSize = m_VectorObservationSize;
            clone.m_StackedVectorObservations = m_StackedVectorObservations;
            clone.m_NormalizeVectorObservations = m_NormalizeVectorObservations;
            clone.m_ObservationProviders = new List<string>(m_ObservationProviders);
            
            clone.m_ContinuousActionSize = m_ContinuousActionSize;
            clone.m_DiscreteActionBranches = (int[])m_DiscreteActionBranches.Clone();
            clone.m_ActionReceivers = new List<string>(m_ActionReceivers);
            
            clone.m_MaxStepReward = m_MaxStepReward;
            clone.m_EnableRewardClipping = m_EnableRewardClipping;
            clone.m_RewardClippingMin = m_RewardClippingMin;
            clone.m_RewardClippingMax = m_RewardClippingMax;
            clone.m_RewardProviders = new List<string>(m_RewardProviders);
            
            clone.m_MaxSteps = m_MaxSteps;
            clone.m_EpisodeTimeout = m_EpisodeTimeout;
            clone.m_EpisodeHandlers = new List<string>(m_EpisodeHandlers);
            
            clone.m_SensorProviders = new List<string>(m_SensorProviders);
            clone.m_EnableSensorLOD = m_EnableSensorLOD;
            
            clone.m_DecisionProviders = new List<string>(m_DecisionProviders);
            clone.m_DefaultDecisionProvider = m_DefaultDecisionProvider;
            
            clone.m_LearningRate = m_LearningRate;
            clone.m_BatchSize = m_BatchSize;
            clone.m_BufferSize = m_BufferSize;
            clone.m_Beta = m_Beta;
            clone.m_Epsilon = m_Epsilon;
            clone.m_NumEpochs = m_NumEpochs;
            clone.m_TrainerType = m_TrainerType;
            
            clone.m_EnablePerformanceMonitoring = m_EnablePerformanceMonitoring;
            clone.m_StatisticsUpdateInterval = m_StatisticsUpdateInterval;
            clone.m_StatisticsProviders = new List<string>(m_StatisticsProviders);
            
            clone.m_UseDeterministicInference = m_UseDeterministicInference;
            clone.m_TimeScale = m_TimeScale;
            clone.m_EnableActionMasking = m_EnableActionMasking;
            clone.m_ComponentsToExclude = new List<string>(m_ComponentsToExclude);
            
            return clone;
        }
        
        // Get configuration summary
        public string GetSummary() {
            return $"MLBehaviorConfig: {m_BehaviorName} ({m_BehaviorType})\n" +
                   $"Observations: Vector={m_UseVectorObservations}, Visual={m_UseVisualObservations}\n" +
                   $"Actions: Continuous={m_ContinuousActionSize}, Discrete={m_DiscreteActionBranches?.Length ?? 0}\n" +
                   $"Max Steps: {m_MaxSteps}, Reward Providers: {m_RewardProviders.Count}";
        }
    }
}