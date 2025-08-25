using UnityEngine;
using UnityEditor;
using TCS.MLAgents.Configuration;
using TCS.MLAgents.Validation;

namespace TCS.MLAgents.Editor
{
    /// <summary>
    /// Simple editor window to demonstrate configuration validation
    /// </summary>
    public class ConfigurationValidationWindow : EditorWindow
    {
        private MLBehaviorConfig m_Configuration;
        private Vector2 m_ScrollPosition;

        [MenuItem("TCS ML-Agents/Configuration Validation")]
        public static void ShowWindow()
        {
            GetWindow<ConfigurationValidationWindow>("Config Validation");
        }

        void OnGUI()
        {
            GUILayout.Label("ML-Agents Configuration Validation", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Configuration selection
            m_Configuration = (MLBehaviorConfig)EditorGUILayout.ObjectField(
                "Configuration", m_Configuration, typeof(MLBehaviorConfig), false
            );

            EditorGUILayout.Space();

            // Validation button
            if (GUILayout.Button("Validate Configuration", GUILayout.Height(30)))
            {
                ValidateConfiguration();
            }

            EditorGUILayout.Space();

            // Quick validation buttons
            GUILayout.Label("Quick Validations", EditorStyles.boldLabel);

            if (GUILayout.Button("Validate Basic Properties"))
            {
                ValidateBasicProperties();
            }

            if (GUILayout.Button("Validate Observation Space"))
            {
                ValidateObservationSpace();
            }

            if (GUILayout.Button("Validate Action Space"))
            {
                ValidateActionSpace();
            }

            if (GUILayout.Button("Validate Training Settings"))
            {
                ValidateTrainingSettings();
            }

            EditorGUILayout.Space();

            // Create new configuration
            if (GUILayout.Button("Create New Configuration"))
            {
                CreateNewConfiguration();
            }
        }

        private void ValidateConfiguration()
        {
            if (m_Configuration == null)
            {
                EditorUtility.DisplayDialog("Validation Error", "Please select a configuration to validate.", "OK");
                return;
            }

            var result = ConfigurationValidator.ValidateConfiguration(m_Configuration);

            if (result.IsValid)
            {
                string message = $"Configuration is valid!\n\nWarnings: {result.Warnings.Count}\nNotes: {result.Notes.Count}";
                EditorUtility.DisplayDialog("Validation Result", message, "OK");
            }
            else
            {
                var message = $"Configuration has {result.Errors.Count} errors:\n";
                foreach (var error in result.Errors)
                {
                    message += $"• {error}\n";
                }

                if (result.Warnings.Count > 0)
                {
                    message += $"\nAnd {result.Warnings.Count} warnings:\n";
                    foreach (var warning in result.Warnings)
                    {
                        message += $"• {warning}\n";
                    }
                }

                if (result.Notes.Count > 0)
                {
                    message += $"\nNotes:\n";
                    foreach (var note in result.Notes)
                    {
                        message += $"• {note}\n";
                    }
                }

                EditorUtility.DisplayDialog("Validation Result", message, "OK");
            }
        }

        private void ValidateBasicProperties()
        {
            if (m_Configuration == null)
            {
                EditorUtility.DisplayDialog("Validation Error", "Please select a configuration to validate.", "OK");
                return;
            }

            var errors = new System.Text.StringBuilder();

            if (string.IsNullOrEmpty(m_Configuration.BehaviorName))
            {
                errors.AppendLine("• Behavior name is empty");
            }

            if (m_Configuration.Type == MLBehaviorConfig.BehaviorType.Inference && string.IsNullOrEmpty(m_Configuration.ModelPath))
            {
                errors.AppendLine("• Model path must be specified for inference behavior");
            }

            if (errors.Length == 0)
            {
                EditorUtility.DisplayDialog("Basic Properties Validation", "All basic properties are valid!", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Basic Properties Validation", $"Issues found:\n{errors}", "OK");
            }
        }

        private void ValidateObservationSpace()
        {
            if (m_Configuration == null)
            {
                EditorUtility.DisplayDialog("Validation Error", "Please select a configuration to validate.", "OK");
                return;
            }

            var errors = new System.Text.StringBuilder();
            var warnings = new System.Text.StringBuilder();

            if (m_Configuration.VectorObservationSize < 0)
            {
                errors.AppendLine("• Vector observation size must be non-negative");
            }

            if (m_Configuration.StackedVectorObservations < 1)
            {
                errors.AppendLine("• Stacked vector observations must be at least 1");
            }

            if (m_Configuration.StackedVectorObservations > 50)
            {
                warnings.AppendLine("• High number of stacked observations may impact performance");
            }

            if (!m_Configuration.UseVectorObservations && !m_Configuration.UseVisualObservations)
            {
                warnings.AppendLine("• No observation types enabled - agent will have no observations");
            }

            var message = "";
            if (errors.Length > 0)
            {
                message += $"Errors:\n{errors}";
            }
            if (warnings.Length > 0)
            {
                message += $"Warnings:\n{warnings}";
            }
            if (message == "")
            {
                message = "Observation space is valid!";
            }

            EditorUtility.DisplayDialog("Observation Space Validation", message, "OK");
        }

        private void ValidateActionSpace()
        {
            if (m_Configuration == null)
            {
                EditorUtility.DisplayDialog("Validation Error", "Please select a configuration to validate.", "OK");
                return;
            }

            var errors = new System.Text.StringBuilder();
            var warnings = new System.Text.StringBuilder();

            if (m_Configuration.ContinuousActionSize < 0)
            {
                errors.AppendLine("• Continuous action size must be non-negative");
            }

            if (m_Configuration.DiscreteActionBranches != null)
            {
                foreach (int branchSize in m_Configuration.DiscreteActionBranches)
                {
                    if (branchSize <= 0)
                    {
                        errors.AppendLine("• Discrete action branch sizes must be positive");
                        break;
                    }
                }
            }

            if (m_Configuration.ContinuousActionSize == 0 &&
                (m_Configuration.DiscreteActionBranches == null || m_Configuration.DiscreteActionBranches.Length == 0))
            {
                warnings.AppendLine("• No action space defined - agent will not be able to take actions");
            }

            var message = "";
            if (errors.Length > 0)
            {
                message += $"Errors:\n{errors}";
            }
            if (warnings.Length > 0)
            {
                message += $"Warnings:\n{warnings}";
            }
            if (message == "")
            {
                message = "Action space is valid!";
            }

            EditorUtility.DisplayDialog("Action Space Validation", message, "OK");
        }

        private void ValidateTrainingSettings()
        {
            if (m_Configuration == null)
            {
                EditorUtility.DisplayDialog("Validation Error", "Please select a configuration to validate.", "OK");
                return;
            }

            var errors = new System.Text.StringBuilder();
            var warnings = new System.Text.StringBuilder();

            if (m_Configuration.LearningRate <= 0)
            {
                errors.AppendLine("• Learning rate must be positive");
            }

            if (m_Configuration.LearningRate > 1.0f)
            {
                warnings.AppendLine("• Very high learning rate may cause training instability");
            }

            if (m_Configuration.BatchSize <= 0)
            {
                errors.AppendLine("• Batch size must be positive");
            }

            if (m_Configuration.BufferSize <= 0)
            {
                errors.AppendLine("• Buffer size must be positive");
            }

            if (m_Configuration.BufferSize < m_Configuration.BatchSize)
            {
                errors.AppendLine("• Buffer size must be greater than or equal to batch size");
            }

            var message = "";
            if (errors.Length > 0)
            {
                message += $"Errors:\n{errors}";
            }
            if (warnings.Length > 0)
            {
                message += $"Warnings:\n{warnings}";
            }
            if (message == "")
            {
                message = "Training settings are valid!";
            }

            EditorUtility.DisplayDialog("Training Settings Validation", message, "OK");
        }

        private void CreateNewConfiguration()
        {
            var config = CreateInstance<MLBehaviorConfig>();
            config.SetBehaviorName("New ML Behavior");
            config.SetBehaviorType(MLBehaviorConfig.BehaviorType.Training);

            var path = EditorUtility.SaveFilePanelInProject(
                "Save Configuration",
                "NewMLBehaviorConfig",
                "asset",
                "Choose location to save the configuration"
            );

            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(config, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                m_Configuration = config;
                EditorUtility.DisplayDialog("Configuration Created", $"New configuration created at: {path}", "OK");
            }
        }
    }
}