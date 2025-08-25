using System;
using System.Collections.Generic;
using UnityEngine;
using TCS.MLAgents.Configuration;

namespace TCS.MLAgents.Validation
{
    /// <summary>
    /// Utility class for validating MLBehaviorConfig configurations
    /// </summary>
    public static class ConfigurationValidator
    {
        /// <summary>
        /// Validates an MLBehaviorConfig
        /// </summary>
        public static ValidationResult ValidateConfiguration(MLBehaviorConfig config)
        {
            if (config == null)
            {
                return new ValidationResult(false, "Configuration is null", new List<string> { "Configuration is null" });
            }
            
            var result = new ValidationResult(true, "Configuration validation in progress");
            
            // Validate basic properties
            if (string.IsNullOrEmpty(config.BehaviorName))
            {
                result.Errors.Add("Behavior name is empty");
                result.IsValid = false;
            }
            
            // Validate observation configuration
            ValidateObservationConfig(config, result);
            
            // Validate action configuration
            ValidateActionConfig(config, result);
            
            // Validate reward configuration
            ValidateRewardConfig(config, result);
            
            // Validate episode configuration
            ValidateEpisodeConfig(config, result);
            
            // Validate training configuration
            ValidateTrainingConfig(config, result);
            
            // Generate warnings for potential issues
            GenerateWarnings(config, result);
            
            // Generate summary
            string summary = result.IsValid ? "Configuration is valid" : 
                $"Configuration validation failed with {result.Errors.Count} errors";
            if (result.Warnings.Count > 0)
            {
                summary += $" and {result.Warnings.Count} warnings";
            }
            result.Summary = summary;
            
            return result;
        }
        
        private static void ValidateObservationConfig(MLBehaviorConfig config, ValidationResult result)
        {
            // Validate observation size
            if (config.VectorObservationSize < 0)
            {
                result.Errors.Add("Vector observation size must be non-negative");
                result.IsValid = false;
            }
            
            // Validate stacked observations
            if (config.StackedVectorObservations < 1)
            {
                result.Errors.Add("Stacked vector observations must be at least 1");
                result.IsValid = false;
            }
            
            if (config.StackedVectorObservations > 50)
            {
                result.Warnings.Add("High number of stacked observations may impact performance");
            }
            
            // Check for observation providers
            if (!config.UseVectorObservations && !config.UseVisualObservations)
            {
                result.Warnings.Add("No observation types enabled - agent will have no observations");
            }
            
            // Validate observation providers list
            if (config.ObservationProviders == null)
            {
                result.Warnings.Add("Observation providers list is null");
            }
        }
        
        private static void ValidateActionConfig(MLBehaviorConfig config, ValidationResult result)
        {
            // Validate continuous action size
            if (config.ContinuousActionSize < 0)
            {
                result.Errors.Add("Continuous action size must be non-negative");
                result.IsValid = false;
            }
            
            // Validate discrete action branches
            if (config.DiscreteActionBranches != null)
            {
                foreach (int branchSize in config.DiscreteActionBranches)
                {
                    if (branchSize <= 0)
                    {
                        result.Errors.Add("Discrete action branch sizes must be positive");
                        result.IsValid = false;
                    }
                    
                    if (branchSize > 100)
                    {
                        result.Warnings.Add("Large discrete action branch size may impact training");
                    }
                }
            }
            
            // Check for action receivers
            if (config.ContinuousActionSize == 0 && 
                (config.DiscreteActionBranches == null || config.DiscreteActionBranches.Length == 0))
            {
                result.Warnings.Add("No action space defined - agent will not be able to take actions");
            }
            
            // Validate action receivers list
            if (config.ActionReceivers == null)
            {
                result.Warnings.Add("Action receivers list is null");
            }
        }
        
        private static void ValidateRewardConfig(MLBehaviorConfig config, ValidationResult result)
        {
            // Validate reward clipping values
            if (config.EnableRewardClipping)
            {
                if (config.RewardClippingMin >= config.RewardClippingMax)
                {
                    result.Errors.Add("Reward clipping min must be less than max");
                    result.IsValid = false;
                }
                
                if (config.RewardClippingMax - config.RewardClippingMin > 100f)
                {
                    result.Warnings.Add("Large reward clipping range may impact training stability");
                }
            }
            
            // Validate max step reward
            if (config.MaxStepReward > 0)
            {
                result.Warnings.Add("Positive max step reward may encourage longer episodes");
            }
            
            // Validate reward providers list
            if (config.RewardProviders == null)
            {
                result.Warnings.Add("Reward providers list is null");
            }
        }
        
        private static void ValidateEpisodeConfig(MLBehaviorConfig config, ValidationResult result)
        {
            // Validate max steps
            if (config.MaxSteps <= 0)
            {
                result.Errors.Add("Max steps must be positive");
                result.IsValid = false;
            }
            
            if (config.MaxSteps > 100000)
            {
                result.Warnings.Add("Very high max steps may lead to long episodes");
            }
            
            // Validate episode timeout
            if (config.EpisodeTimeout <= 0)
            {
                result.Errors.Add("Episode timeout must be positive");
                result.IsValid = false;
            }
            
            // Validate episode handlers list
            if (config.EpisodeHandlers == null)
            {
                result.Warnings.Add("Episode handlers list is null");
            }
        }
        
        private static void ValidateTrainingConfig(MLBehaviorConfig config, ValidationResult result)
        {
            // Validate learning rate
            if (config.LearningRate <= 0)
            {
                result.Errors.Add("Learning rate must be positive");
                result.IsValid = false;
            }
            
            if (config.LearningRate > 1.0f)
            {
                result.Warnings.Add("Very high learning rate may cause training instability");
            }
            
            // Validate batch size
            if (config.BatchSize <= 0)
            {
                result.Errors.Add("Batch size must be positive");
                result.IsValid = false;
            }
            
            // Validate buffer size
            if (config.BufferSize <= 0)
            {
                result.Errors.Add("Buffer size must be positive");
                result.IsValid = false;
            }
            
            if (config.BufferSize < config.BatchSize)
            {
                result.Errors.Add("Buffer size must be greater than or equal to batch size");
                result.IsValid = false;
            }
            
            // Validate beta and epsilon
            if (config.Beta < 0)
            {
                result.Errors.Add("Beta must be non-negative");
                result.IsValid = false;
            }
            
            if (config.Epsilon <= 0)
            {
                result.Errors.Add("Epsilon must be positive");
                result.IsValid = false;
            }
            
            // Validate epochs
            if (config.NumEpochs <= 0)
            {
                result.Errors.Add("Number of epochs must be positive");
                result.IsValid = false;
            }
        }
        
        private static void GenerateWarnings(MLBehaviorConfig config, ValidationResult result)
        {
            // Warn about potential performance issues
            if (config.VectorObservationSize > 1000)
            {
                result.Warnings.Add("Large observation space (>1000) may impact training performance");
            }
            
            if (config.ContinuousActionSize > 50)
            {
                result.Warnings.Add("Large continuous action space (>50) may impact training performance");
            }
            
            if (config.DiscreteActionBranches != null && config.DiscreteActionBranches.Length > 20)
            {
                result.Warnings.Add("Large discrete action space (>20 branches) may impact training performance");
            }
            
            // Warn about model path for inference
            if (config.Type == MLBehaviorConfig.BehaviorType.Inference && 
                string.IsNullOrEmpty(config.ModelPath))
            {
                result.Warnings.Add("Model path is empty for inference behavior");
            }
            
            // Warn about training-specific settings for inference
            if (config.Type == MLBehaviorConfig.BehaviorType.Inference)
            {
                if (config.LearningRate > 0 || config.BatchSize > 0 || config.BufferSize > 0)
                {
                    result.Notes.Add("Training settings present for inference behavior (will be ignored)");
                }
            }
        }
        
        /// <summary>
        /// Compares two configurations and reports differences
        /// </summary>
        public static ConfigurationComparison CompareConfigurations(MLBehaviorConfig config1, MLBehaviorConfig config2)
        {
            if (config1 == null || config2 == null)
            {
                return new ConfigurationComparison(false, "One or both configurations are null", 
                    new List<string> { "One or both configurations are null" });
            }
            
            var comparison = new ConfigurationComparison(true, "Configuration comparison in progress");
            
            // Compare basic properties
            CompareProperty("BehaviorName", config1.BehaviorName, config2.BehaviorName, comparison);
            CompareProperty("BehaviorType", config1.Type, config2.Type, comparison);
            CompareProperty("ModelPath", config1.ModelPath, config2.ModelPath, comparison);
            
            // Compare observation config
            CompareProperty("VectorObservationSize", config1.VectorObservationSize, config2.VectorObservationSize, comparison);
            CompareProperty("StackedVectorObservations", config1.StackedVectorObservations, config2.StackedVectorObservations, comparison);
            CompareProperty("UseVectorObservations", config1.UseVectorObservations, config2.UseVectorObservations, comparison);
            CompareProperty("UseVisualObservations", config1.UseVisualObservations, config2.UseVisualObservations, comparison);
            CompareProperty("NormalizeVectorObservations", config1.NormalizeVectorObservations, config2.NormalizeVectorObservations, comparison);
            
            // Compare action config
            CompareProperty("ContinuousActionSize", config1.ContinuousActionSize, config2.ContinuousActionSize, comparison);
            
            // Compare reward config
            CompareProperty("MaxStepReward", config1.MaxStepReward, config2.MaxStepReward, comparison);
            CompareProperty("EnableRewardClipping", config1.EnableRewardClipping, config2.EnableRewardClipping, comparison);
            
            // Compare episode config
            CompareProperty("MaxSteps", config1.MaxSteps, config2.MaxSteps, comparison);
            CompareProperty("EpisodeTimeout", config1.EpisodeTimeout, config2.EpisodeTimeout, comparison);
            
            // Compare training config
            CompareProperty("LearningRate", config1.LearningRate, config2.LearningRate, comparison);
            CompareProperty("BatchSize", config1.BatchSize, config2.BatchSize, comparison);
            CompareProperty("BufferSize", config1.BufferSize, config2.BufferSize, comparison);
            
            // Generate summary
            string summary = comparison.AreIdentical ? "Configurations are identical" : 
                $"Configurations differ in {comparison.Differences.Count} properties";
            comparison.Summary = summary;
            
            return comparison;
        }
        
        private static void CompareProperty<T>(string propertyName, T value1, T value2, ConfigurationComparison comparison) where T : IEquatable<T>
        {
            if (!EqualityComparer<T>.Default.Equals(value1, value2))
            {
                comparison.AreIdentical = false;
                comparison.Differences.Add($"{propertyName}: {value1} -> {value2}");
            }
        }
        
        private static void CompareProperty(string propertyName, string value1, string value2, ConfigurationComparison comparison)
        {
            if (value1 != value2)
            {
                comparison.AreIdentical = false;
                comparison.Differences.Add($"{propertyName}: \"{value1}\" -> \"{value2}\"");
            }
        }
        
        private static void CompareProperty(string propertyName, bool value1, bool value2, ConfigurationComparison comparison)
        {
            if (value1 != value2)
            {
                comparison.AreIdentical = false;
                comparison.Differences.Add($"{propertyName}: {value1} -> {value2}");
            }
        }
        
        private static void CompareProperty(string propertyName, int value1, int value2, ConfigurationComparison comparison)
        {
            if (value1 != value2)
            {
                comparison.AreIdentical = false;
                comparison.Differences.Add($"{propertyName}: {value1} -> {value2}");
            }
        }
        
        private static void CompareProperty(string propertyName, float value1, float value2, ConfigurationComparison comparison)
        {
            if (Math.Abs(value1 - value2) > 0.0001f)
            {
                comparison.AreIdentical = false;
                comparison.Differences.Add($"{propertyName}: {value1:F4} -> {value2:F4}");
            }
        }
        
        private static void CompareProperty(string propertyName, MLBehaviorConfig.BehaviorType value1, MLBehaviorConfig.BehaviorType value2, ConfigurationComparison comparison)
        {
            if (value1 != value2)
            {
                comparison.AreIdentical = false;
                comparison.Differences.Add($"{propertyName}: {value1} -> {value2}");
            }
        }
    }
    
    /// <summary>
    /// Result of configuration validation
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string Summary { get; set; }
        public List<string> Errors { get; } = new List<string>();
        public List<string> Warnings { get; } = new List<string>();
        public List<string> Notes { get; } = new List<string>();
        
        public ValidationResult(bool isValid = true, string summary = "", List<string> errors = null, List<string> warnings = null)
        {
            IsValid = isValid;
            Summary = summary;
            if (errors != null) Errors.AddRange(errors);
            if (warnings != null) Warnings.AddRange(warnings);
        }
    }
    
    /// <summary>
    /// Result of configuration comparison
    /// </summary>
    public class ConfigurationComparison
    {
        public bool AreIdentical { get; set; }
        public bool HasDifferences => !AreIdentical;
        public string Summary { get; set; }
        public List<string> Differences { get; } = new List<string>();
        
        public ConfigurationComparison(bool areIdentical = true, string summary = "", List<string> differences = null)
        {
            AreIdentical = areIdentical;
            Summary = summary;
            if (differences != null) Differences.AddRange(differences);
        }
    }
}