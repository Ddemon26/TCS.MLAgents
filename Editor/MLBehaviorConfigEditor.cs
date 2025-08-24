using UnityEngine;
using UnityEditor;
using TCS.MLAgents.Configuration;

namespace TCS.MLAgents.Editor {
    /// <summary>
    /// Custom editor for MLBehaviorConfig ScriptableObject
    /// </summary>
    [CustomEditor(typeof(MLBehaviorConfig))]
    public class MLBehaviorConfigEditor : UnityEditor.Editor {
        private SerializedProperty m_BehaviorName;
        private SerializedProperty m_Description;
        private SerializedProperty m_BehaviorType;
        private SerializedProperty m_ModelPath;
        
        private SerializedProperty m_UseVectorObservations;
        private SerializedProperty m_UseVisualObservations;
        private SerializedProperty m_VectorObservationSize;
        private SerializedProperty m_StackedVectorObservations;
        private SerializedProperty m_NormalizeVectorObservations;
        private SerializedProperty m_ObservationProviders;
        
        private SerializedProperty m_ContinuousActionSize;
        private SerializedProperty m_DiscreteActionBranches;
        private SerializedProperty m_ActionReceivers;
        
        private SerializedProperty m_MaxStepReward;
        private SerializedProperty m_EnableRewardClipping;
        private SerializedProperty m_RewardClippingMin;
        private SerializedProperty m_RewardClippingMax;
        private SerializedProperty m_RewardProviders;
        
        private SerializedProperty m_MaxSteps;
        private SerializedProperty m_EpisodeTimeout;
        private SerializedProperty m_EpisodeHandlers;
        
        private SerializedProperty m_SensorProviders;
        private SerializedProperty m_EnableSensorLOD;
        
        private SerializedProperty m_DecisionProviders;
        private SerializedProperty m_DefaultDecisionProvider;
        
        private SerializedProperty m_LearningRate;
        private SerializedProperty m_BatchSize;
        private SerializedProperty m_BufferSize;
        private SerializedProperty m_Beta;
        private SerializedProperty m_Epsilon;
        private SerializedProperty m_NumEpochs;
        private SerializedProperty m_TrainerType;
        
        private SerializedProperty m_EnablePerformanceMonitoring;
        private SerializedProperty m_StatisticsUpdateInterval;
        private SerializedProperty m_StatisticsProviders;
        
        private SerializedProperty m_UseDeterministicInference;
        private SerializedProperty m_TimeScale;
        private SerializedProperty m_EnableActionMasking;
        private SerializedProperty m_ComponentsToExclude;
        
        private void OnEnable() {
            m_BehaviorName = serializedObject.FindProperty("m_BehaviorName");
            m_Description = serializedObject.FindProperty("m_Description");
            m_BehaviorType = serializedObject.FindProperty("m_BehaviorType");
            m_ModelPath = serializedObject.FindProperty("m_ModelPath");
            
            m_UseVectorObservations = serializedObject.FindProperty("m_UseVectorObservations");
            m_UseVisualObservations = serializedObject.FindProperty("m_UseVisualObservations");
            m_VectorObservationSize = serializedObject.FindProperty("m_VectorObservationSize");
            m_StackedVectorObservations = serializedObject.FindProperty("m_StackedVectorObservations");
            m_NormalizeVectorObservations = serializedObject.FindProperty("m_NormalizeVectorObservations");
            m_ObservationProviders = serializedObject.FindProperty("m_ObservationProviders");
            
            m_ContinuousActionSize = serializedObject.FindProperty("m_ContinuousActionSize");
            m_DiscreteActionBranches = serializedObject.FindProperty("m_DiscreteActionBranches");
            m_ActionReceivers = serializedObject.FindProperty("m_ActionReceivers");
            
            m_MaxStepReward = serializedObject.FindProperty("m_MaxStepReward");
            m_EnableRewardClipping = serializedObject.FindProperty("m_EnableRewardClipping");
            m_RewardClippingMin = serializedObject.FindProperty("m_RewardClippingMin");
            m_RewardClippingMax = serializedObject.FindProperty("m_RewardClippingMax");
            m_RewardProviders = serializedObject.FindProperty("m_RewardProviders");
            
            m_MaxSteps = serializedObject.FindProperty("m_MaxSteps");
            m_EpisodeTimeout = serializedObject.FindProperty("m_EpisodeTimeout");
            m_EpisodeHandlers = serializedObject.FindProperty("m_EpisodeHandlers");
            
            m_SensorProviders = serializedObject.FindProperty("m_SensorProviders");
            m_EnableSensorLOD = serializedObject.FindProperty("m_EnableSensorLOD");
            
            m_DecisionProviders = serializedObject.FindProperty("m_DecisionProviders");
            m_DefaultDecisionProvider = serializedObject.FindProperty("m_DefaultDecisionProvider");
            
            m_LearningRate = serializedObject.FindProperty("m_LearningRate");
            m_BatchSize = serializedObject.FindProperty("m_BatchSize");
            m_BufferSize = serializedObject.FindProperty("m_BufferSize");
            m_Beta = serializedObject.FindProperty("m_Beta");
            m_Epsilon = serializedObject.FindProperty("m_Epsilon");
            m_NumEpochs = serializedObject.FindProperty("m_NumEpochs");
            m_TrainerType = serializedObject.FindProperty("m_TrainerType");
            
            m_EnablePerformanceMonitoring = serializedObject.FindProperty("m_EnablePerformanceMonitoring");
            m_StatisticsUpdateInterval = serializedObject.FindProperty("m_StatisticsUpdateInterval");
            m_StatisticsProviders = serializedObject.FindProperty("m_StatisticsProviders");
            
            m_UseDeterministicInference = serializedObject.FindProperty("m_UseDeterministicInference");
            m_TimeScale = serializedObject.FindProperty("m_TimeScale");
            m_EnableActionMasking = serializedObject.FindProperty("m_EnableActionMasking");
            m_ComponentsToExclude = serializedObject.FindProperty("m_ComponentsToExclude");
        }
        
        public override void OnInspectorGUI() {
            serializedObject.Update();
            
            EditorGUILayout.LabelField("ML Behavior Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Basic Properties
            EditorGUILayout.PropertyField(m_BehaviorName);
            EditorGUILayout.PropertyField(m_Description);
            EditorGUILayout.PropertyField(m_BehaviorType);
            
            if ((MLBehaviorConfig.BehaviorType)m_BehaviorType.enumValueIndex == MLBehaviorConfig.BehaviorType.Inference) {
                EditorGUILayout.PropertyField(m_ModelPath);
            }
            
            EditorGUILayout.Space();
            
            // Observation Configuration
            EditorGUILayout.LabelField("Observation Configuration", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_UseVectorObservations);
            if (m_UseVectorObservations.boolValue) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_VectorObservationSize);
                EditorGUILayout.PropertyField(m_StackedVectorObservations);
                EditorGUILayout.PropertyField(m_NormalizeVectorObservations);
                EditorGUILayout.PropertyField(m_ObservationProviders, true);
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.PropertyField(m_UseVisualObservations);
            EditorGUILayout.Space();
            
            // Action Configuration
            EditorGUILayout.LabelField("Action Configuration", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_ContinuousActionSize);
            EditorGUILayout.PropertyField(m_DiscreteActionBranches, true);
            EditorGUILayout.PropertyField(m_ActionReceivers, true);
            EditorGUILayout.Space();
            
            // Reward Configuration
            EditorGUILayout.LabelField("Reward Configuration", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_MaxStepReward);
            EditorGUILayout.PropertyField(m_EnableRewardClipping);
            if (m_EnableRewardClipping.boolValue) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_RewardClippingMin);
                EditorGUILayout.PropertyField(m_RewardClippingMax);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.PropertyField(m_RewardProviders, true);
            EditorGUILayout.Space();
            
            // Episode Configuration
            EditorGUILayout.LabelField("Episode Configuration", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_MaxSteps);
            EditorGUILayout.PropertyField(m_EpisodeTimeout);
            EditorGUILayout.PropertyField(m_EpisodeHandlers, true);
            EditorGUILayout.Space();
            
            // Sensor Configuration
            EditorGUILayout.LabelField("Sensor Configuration", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_SensorProviders, true);
            EditorGUILayout.PropertyField(m_EnableSensorLOD);
            EditorGUILayout.Space();
            
            // Decision Configuration
            EditorGUILayout.LabelField("Decision Configuration", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_DecisionProviders, true);
            EditorGUILayout.PropertyField(m_DefaultDecisionProvider);
            EditorGUILayout.Space();
            
            // Training Configuration
            EditorGUILayout.LabelField("Training Configuration", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_LearningRate);
            EditorGUILayout.PropertyField(m_BatchSize);
            EditorGUILayout.PropertyField(m_BufferSize);
            EditorGUILayout.PropertyField(m_Beta);
            EditorGUILayout.PropertyField(m_Epsilon);
            EditorGUILayout.PropertyField(m_NumEpochs);
            EditorGUILayout.PropertyField(m_TrainerType);
            EditorGUILayout.Space();
            
            // Performance Configuration
            EditorGUILayout.LabelField("Performance Configuration", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_EnablePerformanceMonitoring);
            EditorGUILayout.PropertyField(m_StatisticsUpdateInterval);
            EditorGUILayout.PropertyField(m_StatisticsProviders, true);
            EditorGUILayout.Space();
            
            // Advanced Settings
            EditorGUILayout.LabelField("Advanced Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_UseDeterministicInference);
            EditorGUILayout.PropertyField(m_TimeScale);
            EditorGUILayout.PropertyField(m_EnableActionMasking);
            EditorGUILayout.PropertyField(m_ComponentsToExclude, true);
            
            serializedObject.ApplyModifiedProperties();
            
            // Validation button
            if (GUILayout.Button("Validate Configuration")) {
                ValidateConfiguration();
            }
        }
        
        private void ValidateConfiguration() {\n            var config = target as MLBehaviorConfig;\n            if (config == null) return;\n            \n            var result = TCS.MLAgents.Validation.ConfigurationValidator.ValidateConfiguration(config);\n            \n            if (result.IsValid) {\n                EditorUtility.DisplayDialog(\"Validation Result\", \"Configuration is valid!\", \"OK\");\n            } else {\n                string message = $\"Configuration has {result.Errors.Count} errors:\\n\\n\";\n                foreach (string error in result.Errors) {\n                    message += $\"- {error}\\n\";\n                }\n                \n                if (result.Warnings.Count > 0) {\n                    message += $\"\\nAnd {result.Warnings.Count} warnings:\\n\\n\";\n                    foreach (string warning in result.Warnings) {\n                        message += $\"- {warning}\\n\";\n                    }\n                }\n                \n                EditorUtility.DisplayDialog(\"Validation Result\", message, \"OK\");\n            }\n        }
    }
}