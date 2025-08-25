using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using TCS.MLAgents.Core;
using TCS.MLAgents.Observations;
using TCS.MLAgents.Actions;
using TCS.MLAgents.Rewards;
using TCS.MLAgents.Episodes;
using TCS.MLAgents.Sensors;
using TCS.MLAgents.Decision;
using TCS.MLAgents.Utilities;
using TCS.MLAgents.Configuration;
using TCS.MLAgents.Validation;
using TCS.MLAgents.Interfaces;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

namespace TCS.MLAgents.Editor
{
    /// <summary>
    /// Validation tool for migrated ML-Agents scenarios
    /// </summary>
    public class MigrationValidationTool : EditorWindow
    {
        private GameObject m_SelectedGameObject;
        private Vector2 m_ScrollPosition;
        private ValidationResults m_ValidationResults;
        private bool m_ShowDetails = true;
        private bool m_ShowComponentList = true;
        private bool m_ShowWarnings = true;
        private bool m_ShowErrors = true;
        
        [MenuItem("TCS ML-Agents/Migration Validation Tool")]
        public static void ShowWindow()
        {
            GetWindow<MigrationValidationTool>("Migration Validation");
        }
        
        void OnGUI()
        {
            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);
            
            GUILayout.Label("ML-Agents Migration Validation Tool", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Selected GameObject
            m_SelectedGameObject = (GameObject)EditorGUILayout.ObjectField(
                "Target GameObject", m_SelectedGameObject, typeof(GameObject), true);
            
            EditorGUILayout.Space();
            
            // Validation Controls
            if (GUILayout.Button("Run Validation", GUILayout.Height(30)))
            {
                RunValidation();
            }
            
            if (GUILayout.Button("Run Detailed Analysis", GUILayout.Height(30)))
            {
                RunDetailedAnalysis();
            }
            
            EditorGUILayout.Space();
            
            // Quick Validation Buttons
            GUILayout.Label("Quick Checks", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Check Component Completeness"))
            {
                CheckComponentCompleteness();
            }
            
            if (GUILayout.Button("Check Configuration Consistency"))
            {
                CheckConfigurationConsistency();
            }
            
            if (GUILayout.Button("Check Performance Indicators"))
            {
                CheckPerformanceIndicators();
            }
            
            EditorGUILayout.Space();
            
            // Results Display
            if (m_ValidationResults != null)
            {
                DisplayValidationResults();
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void RunValidation()
        {
            if (m_SelectedGameObject == null)
            {
                EditorUtility.DisplayDialog("Validation Error", "Please select a GameObject to validate.", "OK");
                return;
            }
            
            m_ValidationResults = new ValidationResults();
            m_ValidationResults.TargetName = m_SelectedGameObject.name;
            
            try
            {
                // Run all validation checks
                ValidateCoreComponents();
                ValidateComponentInteractions();
                ValidateConfiguration();
                ValidatePerformance();
                ValidateBestPractices();
                
                // Generate summary
                GenerateValidationSummary();
                
                Debug.Log($"[Migration Validation] Validation completed for {m_SelectedGameObject.name}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Migration Validation] Error during validation: {ex.Message}");
                m_ValidationResults.Errors.Add($"Validation error: {ex.Message}");
            }
        }
        
        private void RunDetailedAnalysis()
        {
            if (m_SelectedGameObject == null)
            {
                EditorUtility.DisplayDialog("Analysis Error", "Please select a GameObject to analyze.", "OK");
                return;
            }
            
            m_ValidationResults = new ValidationResults();
            m_ValidationResults.TargetName = m_SelectedGameObject.name;
            m_ValidationResults.IsDetailedAnalysis = true;
            
            try
            {
                // Run detailed analysis
                AnalyzeComponentHierarchy();
                AnalyzeObservationFlow();
                AnalyzeActionFlow();
                AnalyzeRewardFlow();
                AnalyzeEpisodeFlow();
                AnalyzeSensorFlow();
                AnalyzeDecisionFlow();
                
                // Generate detailed summary
                GenerateDetailedAnalysisSummary();
                
                Debug.Log($"[Migration Validation] Detailed analysis completed for {m_SelectedGameObject.name}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Migration Validation] Error during detailed analysis: {ex.Message}");
                m_ValidationResults.Errors.Add($"Analysis error: {ex.Message}");
            }
        }
        
        private void ValidateCoreComponents()
        {
            if (m_SelectedGameObject == null) return;
            
            // Check for required core components
            var composer = m_SelectedGameObject.GetComponent<MLAgentComposer>();
            if (composer == null)
            {
                m_ValidationResults.Warnings.Add("Missing MLAgentComposer - Agent may not function properly");
            }
            else
            {
                m_ValidationResults.Passes.Add("MLAgentComposer present");
            }
            
            var observationCollector = m_SelectedGameObject.GetComponent<VectorObservationCollector>();
            if (observationCollector == null)
            {
                m_ValidationResults.Warnings.Add("Missing VectorObservationCollector - No vector observations will be collected");
            }
            else
            {
                m_ValidationResults.Passes.Add("VectorObservationCollector present");
                
                // Check for observation providers
                var providers = m_SelectedGameObject.GetComponents<IObservationProvider>();
                if (providers.Length == 0)
                {
                    m_ValidationResults.Warnings.Add("No observation providers registered - Agent will have no observations");
                }
                else
                {
                    m_ValidationResults.Passes.Add($"Found {providers.Length} observation providers");
                }
            }
            
            var actionDistributor = m_SelectedGameObject.GetComponent<ActionDistributor>();
            if (actionDistributor == null)
            {
                m_ValidationResults.Warnings.Add("Missing ActionDistributor - No actions will be processed");
            }
            else
            {
                m_ValidationResults.Passes.Add("ActionDistributor present");
                
                // Check for action receivers
                var receivers = m_SelectedGameObject.GetComponents<TCS.MLAgents.Interfaces.IActionHandler>();
                if (receivers.Length == 0)
                {
                    m_ValidationResults.Warnings.Add("No action receivers registered - Agent will not respond to actions");
                }
                else
                {
                    m_ValidationResults.Passes.Add($"Found {receivers.Length} action receivers");
                }
            }
            
            var rewardCalculator = m_SelectedGameObject.GetComponent<RewardCalculator>();
            if (rewardCalculator == null)
            {
                m_ValidationResults.Warnings.Add("Missing RewardCalculator - No rewards will be calculated");
            }
            else
            {
                m_ValidationResults.Passes.Add("RewardCalculator present");
                
                // Check for reward providers
                var providers = m_SelectedGameObject.GetComponents<IRewardProvider>();
                if (providers.Length == 0)
                {
                    m_ValidationResults.Warnings.Add("No reward providers registered - Agent will receive no rewards");
                }
                else
                {
                    m_ValidationResults.Passes.Add($"Found {providers.Length} reward providers");
                }
            }
            
            var episodeManager = m_SelectedGameObject.GetComponent<EpisodeManager>();
            if (episodeManager == null)
            {
                m_ValidationResults.Warnings.Add("Missing EpisodeManager - Episode management may be incomplete");
            }
            else
            {
                m_ValidationResults.Passes.Add("EpisodeManager present");
                
                // Check for episode handlers
                var handlers = m_SelectedGameObject.GetComponents<IEpisodeHandler>();
                if (handlers.Length == 0)
                {
                    m_ValidationResults.Warnings.Add("No episode handlers registered - Episode events won't be handled");
                }
                else
                {
                    m_ValidationResults.Passes.Add($"Found {handlers.Length} episode handlers");
                }
            }
        }
        
        private void ValidateComponentInteractions()
        {
            if (m_SelectedGameObject == null) return;
            
            // Check for potential component conflicts
            var components = m_SelectedGameObject.GetComponents<Component>();
            
            // Check for duplicate core components
            var composers = m_SelectedGameObject.GetComponents<MLAgentComposer>();
            if (composers.Length > 1)
            {
                m_ValidationResults.Warnings.Add($"Multiple MLAgentComposers found ({composers.Length}) - May cause conflicts");
            }
            
            var observationCollectors = m_SelectedGameObject.GetComponents<VectorObservationCollector>();
            if (observationCollectors.Length > 1)
            {
                m_ValidationResults.Warnings.Add($"Multiple VectorObservationCollectors found ({observationCollectors.Length}) - May cause conflicts");
            }
            
            var actionDistributors = m_SelectedGameObject.GetComponents<ActionDistributor>();
            if (actionDistributors.Length > 1)
            {
                m_ValidationResults.Warnings.Add($"Multiple ActionDistributors found ({actionDistributors.Length}) - May cause conflicts");
            }
            
            // Check for legacy Agent component
            var legacyAgent = m_SelectedGameObject.GetComponent<Agent>();
            if (legacyAgent != null)
            {
                m_ValidationResults.Warnings.Add("Legacy Agent component detected - May conflict with composition system");
            }
            
            // Check for proper component initialization
            var composer = m_SelectedGameObject.GetComponent<MLAgentComposer>();
            if (composer != null)
            {
                // This would require runtime checking, so we'll just note it
                m_ValidationResults.Notes.Add("Verify composer initialization in play mode");
            }
        }
        
        private void ValidateConfiguration()
        {
            if (m_SelectedGameObject == null) return;
            
            // Check for configuration assets
            var configs = m_SelectedGameObject.GetComponents<MLBehaviorConfig>();
            if (configs.Length > 0)
            {
                foreach (var config in configs)
                {
                    if (config != null)
                    {
                        m_ValidationResults.Passes.Add($"Found configuration: {config.BehaviorName}");
                        
                        // Validate configuration
                        var validationResult = ConfigurationValidator.ValidateConfiguration(config);
                        if (!validationResult.IsValid)
                        {
                            foreach (var error in validationResult.Errors)
                            {
                                m_ValidationResults.Errors.Add($"Config {config.BehaviorName}: {error}");
                            }
                        }
                        
                        if (validationResult.Warnings.Count > 0)
                        {
                            foreach (var warning in validationResult.Warnings)
                            {
                                m_ValidationResults.Warnings.Add($"Config {config.BehaviorName}: {warning}");
                            }
                        }
                    }
                }
            }
            else
            {
                m_ValidationResults.Notes.Add("No MLBehaviorConfig found - Using default configuration");
            }
        }
        
        private void ValidatePerformance()
        {
            if (m_SelectedGameObject == null) return;
            
            // Check for performance considerations
            var observationProviders = m_SelectedGameObject.GetComponents<IObservationProvider>();
            if (observationProviders.Length > 50)
            {
                m_ValidationResults.Warnings.Add($"High number of observation providers ({observationProviders.Length}) - May impact performance");
            }
            
            var actionReceivers = m_SelectedGameObject.GetComponents<TCS.MLAgents.Interfaces.IActionHandler>();
            if (actionReceivers.Length > 20)
            {
                m_ValidationResults.Warnings.Add($"High number of action receivers ({actionReceivers.Length}) - May impact performance");
            }
            
            var rewardProviders = m_SelectedGameObject.GetComponents<IRewardProvider>();
            if (rewardProviders.Length > 30)
            {
                m_ValidationResults.Warnings.Add($"High number of reward providers ({rewardProviders.Length}) - May impact performance");
            }
            
            // Check for heavy components
            var cameraSensors = m_SelectedGameObject.GetComponents<CameraSensorProvider>();
            if (cameraSensors.Length > 2)
            {
                m_ValidationResults.Warnings.Add($"Multiple camera sensors ({cameraSensors.Length}) - May impact performance");
            }
            
            var raycastSensors = m_SelectedGameObject.GetComponents<RaycastSensorProvider>();
            if (raycastSensors.Length > 5)
            {
                m_ValidationResults.Warnings.Add($"Many raycast sensors ({raycastSensors.Length}) - Consider reducing ray count");
            }
        }
        
        private void ValidateBestPractices()
        {
            if (m_SelectedGameObject == null) return;
            
            // Check for best practice violations
            var observationProviders = m_SelectedGameObject.GetComponents<IObservationProvider>();
            var actionReceivers = m_SelectedGameObject.GetComponents<TCS.MLAgents.Interfaces.IActionHandler>();
            var rewardProviders = m_SelectedGameObject.GetComponents<IRewardProvider>();
            
            // Check for unused components
            foreach (var provider in observationProviders)
            {
                if (provider is MonoBehaviour mb && !mb.isActiveAndEnabled)
                {
                    m_ValidationResults.Warnings.Add($"Inactive observation provider: {provider.ProviderName}");
                }
            }
            
            foreach (var receiver in actionReceivers)
            {
                if (receiver is MonoBehaviour mb && !mb.isActiveAndEnabled)
                {
                    m_ValidationResults.Warnings.Add($"Inactive action receiver: {receiver.ReceiverName}");
                }
            }
            
            foreach (var provider in rewardProviders)
            {
                if (provider is MonoBehaviour mb && !mb.isActiveAndEnabled)
                {
                    m_ValidationResults.Warnings.Add($"Inactive reward provider: {provider.ProviderName}");
                }
            }
            
            // Check for naming conventions
            foreach (var component in m_SelectedGameObject.GetComponents<Component>())
            {
                if (component is MonoBehaviour mb && string.IsNullOrEmpty(mb.name))
                {
                    m_ValidationResults.Notes.Add($"Unnamed component: {component.GetType().Name}");
                }
            }
        }
        
        private void AnalyzeComponentHierarchy()
        {
            if (m_SelectedGameObject == null) return;
            
            m_ValidationResults.AnalysisSections.Add("Component Hierarchy Analysis");
            
            // Analyze component relationships
            var components = m_SelectedGameObject.GetComponents<Component>();
            var componentTypes = components.Select(c => c.GetType().Name).ToList();
            
            m_ValidationResults.AnalysisDetails.Add($"Total components: {components.Length}");
            m_ValidationResults.AnalysisDetails.Add($"Unique component types: {componentTypes.Distinct().Count()}");
            
            // Check for component organization
            var childObjects = m_SelectedGameObject.GetComponentsInChildren<Transform>()
                .Where(t => t != m_SelectedGameObject.transform)
                .ToArray();
                
            if (childObjects.Length > 10)
            {
                m_ValidationResults.AnalysisDetails.Add($"Warning: Many child objects ({childObjects.Length}) - Consider flattening hierarchy");
            }
        }
        
        private void AnalyzeObservationFlow()
        {
            if (m_SelectedGameObject == null) return;
            
            m_ValidationResults.AnalysisSections.Add("Observation Flow Analysis");
            
            var observationCollector = m_SelectedGameObject.GetComponent<VectorObservationCollector>();
            if (observationCollector != null)
            {
                var providers = m_SelectedGameObject.GetComponents<IObservationProvider>();
                int totalObservationSize = 0;
                
                foreach (var provider in providers)
                {
                    totalObservationSize += provider.ObservationSize;
                    m_ValidationResults.AnalysisDetails.Add($"Provider {provider.ProviderName}: {provider.ObservationSize} observations");
                }
                
                m_ValidationResults.AnalysisDetails.Add($"Total observation size: {totalObservationSize}");
                
                if (totalObservationSize > 1000)
                {
                    m_ValidationResults.AnalysisDetails.Add("Warning: Large observation space (>1000) - May impact training");
                }
            }
        }
        
        private void AnalyzeActionFlow()
        {
            if (m_SelectedGameObject == null) return;
            
            m_ValidationResults.AnalysisSections.Add("Action Flow Analysis");
            
            var actionDistributor = m_SelectedGameObject.GetComponent<ActionDistributor>();
            if (actionDistributor != null)
            {
                var receivers = m_SelectedGameObject.GetComponents<TCS.MLAgents.Interfaces.IActionHandler>();
                int totalContinuousActions = 0;
                int totalDiscreteActions = 0;
                
                foreach (var receiver in receivers)
                {
                    totalContinuousActions += receiver.ContinuousActionCount;
                    totalDiscreteActions += receiver.DiscreteActionBranchCount;
                    
                    m_ValidationResults.AnalysisDetails.Add(
                        $"Receiver {receiver.ReceiverName}: " +
                        $"{receiver.ContinuousActionCount} continuous, " +
                        $"{receiver.DiscreteActionBranchCount} discrete"
                    );
                }
                
                m_ValidationResults.AnalysisDetails.Add(
                    $"Total action space: {totalContinuousActions} continuous, {totalDiscreteActions} discrete"
                );
                
                if (totalContinuousActions > 50)
                {
                    m_ValidationResults.AnalysisDetails.Add("Warning: Large continuous action space (>50) - May impact training");
                }
                
                if (totalDiscreteActions > 20)
                {
                    m_ValidationResults.AnalysisDetails.Add("Warning: Large discrete action space (>20) - May impact training");
                }
            }
        }
        
        private void AnalyzeRewardFlow()
        {
            if (m_SelectedGameObject == null) return;
            
            m_ValidationResults.AnalysisSections.Add("Reward Flow Analysis");
            
            var rewardCalculator = m_SelectedGameObject.GetComponent<RewardCalculator>();
            if (rewardCalculator != null)
            {
                var providers = m_SelectedGameObject.GetComponents<IRewardProvider>();
                float totalWeight = 0f;
                
                foreach (var provider in providers)
                {
                    totalWeight += provider.RewardWeight;
                    m_ValidationResults.AnalysisDetails.Add(
                        $"Provider {provider.ProviderName}: Weight {provider.RewardWeight:F2}"
                    );
                }
                
                m_ValidationResults.AnalysisDetails.Add($"Total reward weight: {totalWeight:F2}");
                
                if (Math.Abs(totalWeight) > 10f)
                {
                    m_ValidationResults.AnalysisDetails.Add("Warning: High total reward weight (>10) - May cause unstable training");
                }
            }
        }
        
        private void AnalyzeEpisodeFlow()
        {
            if (m_SelectedGameObject == null) return;
            
            m_ValidationResults.AnalysisSections.Add("Episode Flow Analysis");
            
            var episodeManager = m_SelectedGameObject.GetComponent<EpisodeManager>();
            if (episodeManager != null)
            {
                var handlers = m_SelectedGameObject.GetComponents<IEpisodeHandler>();
                
                m_ValidationResults.AnalysisDetails.Add($"Total episode handlers: {handlers.Length}");
                
                foreach (var handler in handlers)
                {
                    m_ValidationResults.AnalysisDetails.Add(
                        $"Handler {handler.HandlerName}: Priority {handler.Priority}"
                    );
                }
                
                if (handlers.Length == 0)
                {
                    m_ValidationResults.AnalysisDetails.Add("Warning: No episode handlers - Episodes may not end properly");
                }
            }
        }
        
        private void AnalyzeSensorFlow()
        {
            if (m_SelectedGameObject == null) return;
            
            m_ValidationResults.AnalysisSections.Add("Sensor Flow Analysis");
            
            var sensorManager = m_SelectedGameObject.GetComponent<SensorManager>();
            if (sensorManager != null)
            {
                var providers = m_SelectedGameObject.GetComponents<ISensorProvider>();
                
                m_ValidationResults.AnalysisDetails.Add($"Total sensor providers: {providers.Length}");
                
                foreach (var provider in providers)
                {
                    m_ValidationResults.AnalysisDetails.Add(
                        $"Sensor {provider.SensorName}: Priority {provider.Priority}"
                    );
                }
                
                if (providers.Length == 0)
                {
                    m_ValidationResults.AnalysisDetails.Add("Note: No sensor providers - Agent will use default sensors only");
                }
            }
            else
            {
                m_ValidationResults.AnalysisDetails.Add("No SensorManager found - Using default Unity ML-Agents sensors");
            }
        }
        
        private void AnalyzeDecisionFlow()
        {
            if (m_SelectedGameObject == null) return;
            
            m_ValidationResults.AnalysisSections.Add("Decision Flow Analysis");
            
            var decisionRouter = m_SelectedGameObject.GetComponent<DecisionRouter>();
            if (decisionRouter != null)
            {
                m_ValidationResults.AnalysisDetails.Add("Decision routing system present");
                
                var providers = m_SelectedGameObject.GetComponents<IDecisionProvider>();
                m_ValidationResults.AnalysisDetails.Add($"Decision providers: {providers.Length}");
                
                foreach (var provider in providers)
                {
                    m_ValidationResults.AnalysisDetails.Add($"Provider: {provider.GetType().Name}");
                }
            }
            else
            {
                m_ValidationResults.AnalysisDetails.Add("Using default Unity ML-Agents decision system");
            }
        }
        
        private void CheckComponentCompleteness()
        {
            if (m_SelectedGameObject == null) return;
            
            var missingComponents = new List<string>();
            
            // Check for all core components
            if (m_SelectedGameObject.GetComponent<MLAgentComposer>() == null)
                missingComponents.Add("MLAgentComposer");
                
            if (m_SelectedGameObject.GetComponent<VectorObservationCollector>() == null)
                missingComponents.Add("VectorObservationCollector");
                
            if (m_SelectedGameObject.GetComponent<ActionDistributor>() == null)
                missingComponents.Add("ActionDistributor");
                
            if (m_SelectedGameObject.GetComponent<RewardCalculator>() == null)
                missingComponents.Add("RewardCalculator");
                
            if (m_SelectedGameObject.GetComponent<EpisodeManager>() == null)
                missingComponents.Add("EpisodeManager");
            
            if (missingComponents.Count == 0)
            {
                EditorUtility.DisplayDialog("Component Check", "All core components are present.", "OK");
            }
            else
            {
                var message = $"Missing components:\n\n{string.Join("\n", missingComponents)}";
                EditorUtility.DisplayDialog("Component Check", message, "OK");
            }
        }
        
        private void CheckConfigurationConsistency()
        {
            if (m_SelectedGameObject == null) return;
            
            var inconsistencies = new List<string>();
            
            // Check configuration consistency between components
            var observationCollector = m_SelectedGameObject.GetComponent<VectorObservationCollector>();
            var actionDistributor = m_SelectedGameObject.GetComponent<ActionDistributor>();
            var rewardCalculator = m_SelectedGameObject.GetComponent<RewardCalculator>();
            
            if (observationCollector != null && actionDistributor != null)
            {
                var obsProviders = m_SelectedGameObject.GetComponents<IObservationProvider>().Length;
                var actionReceivers = m_SelectedGameObject.GetComponents<TCS.MLAgents.Interfaces.IActionHandler>().Length;
                
                if (obsProviders == 0 && actionReceivers > 0)
                {
                    inconsistencies.Add("Action receivers present but no observation providers - Agent may not learn effectively");
                }
            }
            
            if (rewardCalculator != null)
            {
                var rewardProviders = m_SelectedGameObject.GetComponents<IRewardProvider>();
                if (rewardProviders.Length == 0)
                {
                    inconsistencies.Add("Reward calculator present but no reward providers - Agent will not learn");
                }
            }
            
            if (inconsistencies.Count == 0)
            {
                EditorUtility.DisplayDialog("Configuration Check", "No obvious configuration inconsistencies detected.", "OK");
            }
            else
            {
                var message = $"Potential inconsistencies:\n\n{string.Join("\n", inconsistencies)}";
                EditorUtility.DisplayDialog("Configuration Check", message, "OK");
            }
        }
        
        private void CheckPerformanceIndicators()
        {
            if (m_SelectedGameObject == null) return;
            
            var performanceIssues = new List<string>();
            
            // Check for potential performance issues
            var components = m_SelectedGameObject.GetComponents<Component>();
            if (components.Length > 100)
            {
                performanceIssues.Add($"Large number of components ({components.Length}) - May impact performance");
            }
            
            var raycastProviders = m_SelectedGameObject.GetComponents<RaycastSensorProvider>();
            int totalRays = 0;
            foreach (var provider in raycastProviders)
            {
                // This would require accessing the provider's ray count property
                totalRays += 16; // Default assumption
            }
            
            if (totalRays > 128)
            {
                performanceIssues.Add($"High raycast count ({totalRays}) - Consider reducing for better performance");
            }
            
            var cameraProviders = m_SelectedGameObject.GetComponents<CameraSensorProvider>();
            if (cameraProviders.Length > 2)
            {
                performanceIssues.Add($"Multiple camera sensors ({cameraProviders.Length}) - May impact frame rate");
            }
            
            if (performanceIssues.Count == 0)
            {
                EditorUtility.DisplayDialog("Performance Check", "No obvious performance issues detected.", "OK");
            }
            else
            {
                var message = $"Potential performance issues:\n\n{string.Join("\n", performanceIssues)}";
                EditorUtility.DisplayDialog("Performance Check", message, "OK");
            }
        }
        
        private void GenerateValidationSummary()
        {
            if (m_ValidationResults == null) return;
            
            m_ValidationResults.Summary = $"Validation Summary for {m_ValidationResults.TargetName}:\n" +
                $"Passes: {m_ValidationResults.Passes.Count}\n" +
                $"Warnings: {m_ValidationResults.Warnings.Count}\n" +
                $"Errors: {m_ValidationResults.Errors.Count}\n" +
                $"Notes: {m_ValidationResults.Notes.Count}";
        }
        
        private void GenerateDetailedAnalysisSummary()
        {
            if (m_ValidationResults == null) return;
            
            m_ValidationResults.Summary = $"Detailed Analysis for {m_ValidationResults.TargetName}:\n" +
                $"Analysis Sections: {m_ValidationResults.AnalysisSections.Count}\n" +
                $"Analysis Details: {m_ValidationResults.AnalysisDetails.Count}\n" +
                $"Passes: {m_ValidationResults.Passes.Count}\n" +
                $"Warnings: {m_ValidationResults.Warnings.Count}\n" +
                $"Errors: {m_ValidationResults.Errors.Count}";
        }
        
        private void DisplayValidationResults()
        {
            if (m_ValidationResults == null) return;
            
            GUILayout.Label(m_ValidationResults.Summary, EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Passes
            if (m_ValidationResults.Passes.Count > 0)
            {
                m_ShowDetails = EditorGUILayout.Foldout(m_ShowDetails, $"Passes ({m_ValidationResults.Passes.Count})", true);
                if (m_ShowDetails)
                {
                    EditorGUI.indentLevel++;
                    foreach (var pass in m_ValidationResults.Passes)
                    {
                        EditorGUILayout.LabelField($"✓ {pass}", new GUIStyle(EditorStyles.label) { normal = { textColor = Color.green } });
                    }
                    EditorGUI.indentLevel--;
                }
            }
            
            EditorGUILayout.Space();
            
            // Warnings
            if (m_ValidationResults.Warnings.Count > 0)
            {
                m_ShowWarnings = EditorGUILayout.Foldout(m_ShowWarnings, $"Warnings ({m_ValidationResults.Warnings.Count})", true);
                if (m_ShowWarnings)
                {
                    EditorGUI.indentLevel++;
                    foreach (var warning in m_ValidationResults.Warnings)
                    {
                        EditorGUILayout.LabelField($"⚠ {warning}", new GUIStyle(EditorStyles.label) { normal = { textColor = Color.yellow } });
                    }
                    EditorGUI.indentLevel--;
                }
            }
            
            EditorGUILayout.Space();
            
            // Errors
            if (m_ValidationResults.Errors.Count > 0)
            {
                m_ShowErrors = EditorGUILayout.Foldout(m_ShowErrors, $"Errors ({m_ValidationResults.Errors.Count})", true);
                if (m_ShowErrors)
                {
                    EditorGUI.indentLevel++;
                    foreach (var error in m_ValidationResults.Errors)
                    {
                        EditorGUILayout.LabelField($"✗ {error}", new GUIStyle(EditorStyles.label) { normal = { textColor = Color.red } });
                    }
                    EditorGUI.indentLevel--;
                }
            }
            
            EditorGUILayout.Space();
            
            // Notes
            if (m_ValidationResults.Notes.Count > 0)
            {
                EditorGUILayout.LabelField($"Notes ({m_ValidationResults.Notes.Count})", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                foreach (var note in m_ValidationResults.Notes)
                {
                    EditorGUILayout.LabelField($"ⓘ {note}");
                }
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // Analysis Results (for detailed analysis)
            if (m_ValidationResults.IsDetailedAnalysis)
            {
                if (m_ValidationResults.AnalysisSections.Count > 0)
                {
                    m_ShowComponentList = EditorGUILayout.Foldout(m_ShowComponentList, "Analysis Results", true);
                    if (m_ShowComponentList)
                    {
                        EditorGUI.indentLevel++;
                        foreach (var section in m_ValidationResults.AnalysisSections)
                        {
                            EditorGUILayout.LabelField(section, EditorStyles.boldLabel);
                        }
                        
                        foreach (var detail in m_ValidationResults.AnalysisDetails)
                        {
                            EditorGUILayout.LabelField(detail);
                        }
                        EditorGUI.indentLevel--;
                    }
                }
            }
            
            EditorGUILayout.Space();
            
            // Export Report
            if (GUILayout.Button("Export Validation Report"))
            {
                ExportValidationReport();
            }
        }
        
        private void ExportValidationReport()
        {
            if (m_ValidationResults == null) return;
            
            var report = GenerateValidationReport();
            var fileName = $"ValidationReport_{m_ValidationResults.TargetName}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            
            #if UNITY_EDITOR
            var path = EditorUtility.SaveFilePanel("Save Validation Report", "", fileName, "txt");
            if (!string.IsNullOrEmpty(path))
            {
                System.IO.File.WriteAllText(path, report);
                EditorUtility.RevealInFinder(path);
                Debug.Log($"[Migration Validation] Report exported to: {path}");
            }
            #endif
        }
        
        private string GenerateValidationReport()
        {
            if (m_ValidationResults == null) return "No validation results available.";
            
            var report = new System.Text.StringBuilder();
            report.AppendLine("=== ML-Agents Migration Validation Report ===");
            report.AppendLine($"Target: {m_ValidationResults.TargetName}");
            report.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"Type: {(m_ValidationResults.IsDetailedAnalysis ? "Detailed Analysis" : "Basic Validation")}");
            report.AppendLine();
            
            report.AppendLine(m_ValidationResults.Summary);
            report.AppendLine();
            
            if (m_ValidationResults.Passes.Count > 0)
            {
                report.AppendLine("=== PASSES ===");
                foreach (var pass in m_ValidationResults.Passes)
                {
                    report.AppendLine($"✓ {pass}");
                }
                report.AppendLine();
            }
            
            if (m_ValidationResults.Warnings.Count > 0)
            {
                report.AppendLine("=== WARNINGS ===");
                foreach (var warning in m_ValidationResults.Warnings)
                {
                    report.AppendLine($"⚠ {warning}");
                }
                report.AppendLine();
            }
            
            if (m_ValidationResults.Errors.Count > 0)
            {
                report.AppendLine("=== ERRORS ===");
                foreach (var error in m_ValidationResults.Errors)
                {
                    report.AppendLine($"✗ {error}");
                }
                report.AppendLine();
            }
            
            if (m_ValidationResults.Notes.Count > 0)
            {
                report.AppendLine("=== NOTES ===");
                foreach (var note in m_ValidationResults.Notes)
                {
                    report.AppendLine($"ⓘ {note}");
                }
                report.AppendLine();
            }
            
            if (m_ValidationResults.IsDetailedAnalysis)
            {
                if (m_ValidationResults.AnalysisSections.Count > 0)
                {
                    report.AppendLine("=== ANALYSIS SECTIONS ===");
                    foreach (var section in m_ValidationResults.AnalysisSections)
                    {
                        report.AppendLine(section);
                    }
                    report.AppendLine();
                }
                
                if (m_ValidationResults.AnalysisDetails.Count > 0)
                {
                    report.AppendLine("=== ANALYSIS DETAILS ===");
                    foreach (var detail in m_ValidationResults.AnalysisDetails)
                    {
                        report.AppendLine(detail);
                    }
                    report.AppendLine();
                }
            }
            
            return report.ToString();
        }
    }
    
    /// <summary>
    /// Results of validation process
    /// </summary>
    public class ValidationResults
    {
        public string TargetName { get; set; }
        public string Summary { get; set; }
        public bool IsDetailedAnalysis { get; set; }
        
        public List<string> Passes { get; } = new List<string>();
        public List<string> Warnings { get; } = new List<string>();
        public List<string> Errors { get; } = new List<string>();
        public List<string> Notes { get; } = new List<string>();
        
        public List<string> AnalysisSections { get; } = new List<string>();
        public List<string> AnalysisDetails { get; } = new List<string>();
    }
}