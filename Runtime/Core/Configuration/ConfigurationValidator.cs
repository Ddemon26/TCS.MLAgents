using System;
using System.Collections.Generic;
using UnityEngine;
using TCS.MLAgents.Configuration;

namespace TCS.MLAgents.Validation {
    /// <summary>
    /// Validates ML behavior configurations and provides detailed error reporting.
    /// </summary>
    public static class ConfigurationValidator {
        
        /// <summary>
        /// Comprehensive validation of ML behavior configuration
        /// </summary>
        public static ValidationResult ValidateConfiguration(MLBehaviorConfig config) {
            if (config == null) {
                return new ValidationResult(false, "Configuration is null");
            }
            
            var errors = new List<string>();
            var warnings = new List<string>();
            
            // Validate basic properties
            ValidateBasicProperties(config, errors, warnings);
            
            // Validate observation configuration
            ValidateObservationConfig(config, errors, warnings);
            
            // Validate action configuration
            ValidateActionConfig(config, errors, warnings);
            
            // Validate reward configuration
            ValidateRewardConfig(config, errors, warnings);
            
            // Validate episode configuration
            ValidateEpisodeConfig(config, errors, warnings);
            
            // Validate training configuration
            ValidateTrainingConfig(config, errors, warnings);
            
            // Validate advanced settings
            ValidateAdvancedSettings(config, errors, warnings);
            
            bool isValid = errors.Count == 0;
            string summary = isValid ? "Configuration is valid" : $"Configuration has {errors.Count} errors and {warnings.Count} warnings";
            
            return new ValidationResult(isValid, summary, errors, warnings);
        }
        
        private static void ValidateBasicProperties(MLBehaviorConfig config, List<string> errors, List<string> warnings) {
            if (string.IsNullOrEmpty(config.BehaviorName)) {
                errors.Add("Behavior name cannot be empty");
            }
            
            if (config.Type == MLBehaviorConfig.BehaviorType.Inference && string.IsNullOrEmpty(config.ModelPath)) {
                errors.Add("Model path must be specified for inference behavior");
            }
            
            if (config.Type == MLBehaviorConfig.BehaviorType.Training && !string.IsNullOrEmpty(config.ModelPath)) {
                warnings.Add("Model path is specified for training behavior (will be ignored)");
            }
        }
        
        private static void ValidateObservationConfig(MLBehaviorConfig config, List<string> errors, List<string> warnings) {
            if (config.VectorObservationSize < 0) {
                errors.Add("Vector observation size must be non-negative");
            }
            
            if (config.StackedVectorObservations < 1) {
                errors.Add("Stacked vector observations must be at least 1");
            }
            
            if (config.StackedVectorObservations > 50) {
                warnings.Add("High number of stacked observations may impact performance");
            }
            
            if (!config.UseVectorObservations && !config.UseVisualObservations) {
                warnings.Add("No observation types enabled - agent will have no observations");
            }
        }
        
        private static void ValidateActionConfig(MLBehaviorConfig config, List<string> errors, List<string> warnings) {
            if (config.ContinuousActionSize < 0) {
                errors.Add("Continuous action size must be non-negative");
            }
            
            if (config.DiscreteActionBranches != null) {
                foreach (int branch in config.DiscreteActionBranches) {
                    if (branch <= 0) {
                        errors.Add("Discrete action branches must be positive");
                    }
                }
            }
            
            if (config.ContinuousActionSize == 0 && (config.DiscreteActionBranches == null || config.DiscreteActionBranches.Length == 0)) {
                warnings.Add("No action types defined - agent will not be able to take actions");
            }
        }
        
        private static void ValidateRewardConfig(MLBehaviorConfig config, List<string> errors, List<string> warnings) {
            if (config.RewardClippingMin >= config.RewardClippingMax) {
                errors.Add("Reward clipping min must be less than max");
            }
            
            if (config.MaxStepReward > 0) {
                warnings.Add("Positive max step reward may encourage longer episodes");
            }
        }
        
        private static void ValidateEpisodeConfig(MLBehaviorConfig config, List<string> errors, List<string> warnings) {
            if (config.MaxSteps <= 0) {
                errors.Add("Max steps must be positive");
            }
            
            if (config.EpisodeTimeout <= 0) {
                errors.Add("Episode timeout must be positive");
            }
            
            if (config.MaxSteps > 100000) {
                warnings.Add("Very high max steps may lead to long episodes");
            }
        }
        
        private static void ValidateTrainingConfig(MLBehaviorConfig config, List<string> errors, List<string> warnings) {
            if (config.LearningRate <= 0) {
                errors.Add("Learning rate must be positive");
            }
            
            if (config.LearningRate > 1.0f) {
                warnings.Add("Very high learning rate may cause training instability");
            }
            
            if (config.BatchSize <= 0) {
                errors.Add("Batch size must be positive");
            }
            
            if (config.BufferSize <= 0) {
                errors.Add("Buffer size must be positive");
            }
            
            if (config.BufferSize < config.BatchSize) {
                errors.Add("Buffer size must be greater than or equal to batch size");
            }
            
            if (config.Beta < 0) {
                errors.Add("Beta must be non-negative");
            }
            
            if (config.Epsilon <= 0) {
                errors.Add("Epsilon must be positive");
            }
            
            if (config.NumEpochs <= 0) {
                errors.Add("Number of epochs must be positive");
            }
        }
        
        private static void ValidateAdvancedSettings(MLBehaviorConfig config, List<string> errors, List<string> warnings) {
            if (config.StatisticsUpdateInterval <= 0) {
                errors.Add("Statistics update interval must be positive");
            }
            
            if (config.StatisticsUpdateInterval < 0.1f) {
                warnings.Add("Very frequent statistics updates may impact performance");
            }
            
            if (config.TimeScale <= 0) {
                errors.Add("Time scale must be positive");
            }
        }
        
        /// <summary>
        /// Compare two configurations and report differences
        /// </summary>
        public static ConfigurationComparison CompareConfigurations(MLBehaviorConfig config1, MLBehaviorConfig config2) {
            if (config1 == null || config2 == null) {
                return new ConfigurationComparison(false, "One or both configurations are null");
            }
            
            var differences = new List<string>();
            
            // Compare basic properties
            CompareProperty("BehaviorName", config1.BehaviorName, config2.BehaviorName, differences);
            if (config1.Type != config2.Type) {
                differences.Add($"BehaviorType: {config1.Type} -> {config2.Type}");
            }
            CompareProperty("ModelPath", config1.ModelPath, config2.ModelPath, differences);
            
            // Compare observation config
            CompareProperty("VectorObservationSize", config1.VectorObservationSize, config2.VectorObservationSize, differences);
            CompareProperty("StackedVectorObservations", config1.StackedVectorObservations, config2.StackedVectorObservations, differences);
            CompareProperty("UseVectorObservations", config1.UseVectorObservations, config2.UseVectorObservations, differences);
            CompareProperty("UseVisualObservations", config1.UseVisualObservations, config2.UseVisualObservations, differences);
            
            // Compare action config
            CompareProperty("ContinuousActionSize", config1.ContinuousActionSize, config2.ContinuousActionSize, differences);
            
            // Compare reward config
            CompareProperty("MaxStepReward", config1.MaxStepReward, config2.MaxStepReward, differences);
            
            // Compare episode config
            CompareProperty("MaxSteps", config1.MaxSteps, config2.MaxSteps, differences);
            CompareProperty("EpisodeTimeout", config1.EpisodeTimeout, config2.EpisodeTimeout, differences);
            
            // Compare training config
            CompareProperty("LearningRate", config1.LearningRate, config2.LearningRate, differences);
            CompareProperty("BatchSize", config1.BatchSize, config2.BatchSize, differences);
            
            bool hasDifferences = differences.Count > 0;
            string summary = hasDifferences ? $"Configurations differ in {differences.Count} aspects" : "Configurations are identical";
            
            return new ConfigurationComparison(hasDifferences, summary, differences);
        }
        
        private static void CompareProperty<T>(string propertyName, T value1, T value2, List<string> differences) where T : IEquatable<T> {
            if (!EqualityComparer<T>.Default.Equals(value1, value2)) {
                differences.Add($"{propertyName}: {value1} -> {value2}");
            }
        }
        
        /// <summary>
        /// Generate a report for the configuration
        /// </summary>
        public static string GenerateReport(MLBehaviorConfig config) {
            if (config == null) {
                return "Configuration is null";
            }
            
            var result = ValidateConfiguration(config);
            
            var report = new System.Text.StringBuilder();
            report.AppendLine("=== ML Behavior Configuration Report ===");
            report.AppendLine($"Behavior Name: {config.BehaviorName}");
            report.AppendLine($"Type: {config.Type}");
            report.AppendLine($"Validation: {result.Summary}");
            report.AppendLine();
            
            if (result.Errors.Count > 0) {
                report.AppendLine("Errors:");
                foreach (string error in result.Errors) {
                    report.AppendLine($"  - {error}");
                }
                report.AppendLine();
            }
            
            if (result.Warnings.Count > 0) {
                report.AppendLine("Warnings:");
                foreach (string warning in result.Warnings) {
                    report.AppendLine($"  - {warning}");
                }
                report.AppendLine();
            }
            
            report.AppendLine("Configuration Details:");
            report.AppendLine($"  Observations: Vector={config.UseVectorObservations} ({config.VectorObservationSize}), Visual={config.UseVisualObservations}");
            report.AppendLine($"  Actions: Continuous={config.ContinuousActionSize}, Discrete={config.DiscreteActionBranches?.Length ?? 0}");
            report.AppendLine($"  Episode: Max Steps={config.MaxSteps}, Timeout={config.EpisodeTimeout}s");
            report.AppendLine($"  Training: LR={config.LearningRate}, Batch={config.BatchSize}, Buffer={config.BufferSize}");
            
            return report.ToString();
        }
    }
    
    /// <summary>
    /// Result of configuration validation
    /// </summary>
    public class ValidationResult {
        public bool IsValid { get; }
        public string Summary { get; }
        public List<string> Errors { get; }
        public List<string> Warnings { get; }
        
        public ValidationResult(bool isValid, string summary, List<string> errors = null, List<string> warnings = null) {
            IsValid = isValid;
            Summary = summary;
            Errors = errors ?? new List<string>();
            Warnings = warnings ?? new List<string>();
        }
    }
    
    /// <summary>
    /// Result of configuration comparison
    /// </summary>
    public class ConfigurationComparison {
        public bool HasDifferences { get; }
        public string Summary { get; }
        public List<string> Differences { get; }
        
        public ConfigurationComparison(bool hasDifferences, string summary, List<string> differences = null) {
            HasDifferences = hasDifferences;
            Summary = summary;
            Differences = differences ?? new List<string>();
        }
    }
}