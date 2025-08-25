using UnityEngine;
using Unity.MLAgents;
using TCS.MLAgents.Core;
using TCS.MLAgents.Configuration;

namespace TCS.MLAgents.Examples
{
    /// <summary>
    /// Example script demonstrating how to use migration utilities
    /// </summary>
    public class MigrationExample : MonoBehaviour
    {
        [Header("Migration Settings")]
        [SerializeField] private Agent legacyAgent;
        [SerializeField] private GameObject targetGameObject;
        [SerializeField] private bool autoMigrate = false;
        [SerializeField] private bool validateAfterMigration = true;
        [SerializeField] private bool generateReport = true;
        
        void Start()
        {
            if (autoMigrate)
            {
                MigrateAgent();
            }
        }
        
        /// <summary>
        /// Sets up a composition-based agent manually (migration utilities are editor-only)
        /// </summary>
        public void MigrateAgent()
        {
            if (legacyAgent == null)
            {
                Debug.LogError("[MigrationExample] No legacy agent assigned");
                return;
            }
            
            if (targetGameObject == null)
            {
                targetGameObject = gameObject;
            }
            
            Debug.Log($"[MigrationExample] Starting manual setup of composition agent on {targetGameObject.name}");
            Debug.LogWarning("[MigrationExample] Note: Automatic migration utilities are only available in Editor. This example shows manual composition setup.");
            
            // Manual composition setup
            bool success = SetupCompositionAgent();
            
            if (success)
            {
                Debug.Log("[MigrationExample] Composition agent setup completed successfully");
                
                // Basic validation
                if (validateAfterMigration)
                {
                    bool isValid = ValidateCompositionSetup();
                    if (isValid)
                    {
                        Debug.Log("[MigrationExample] Basic validation passed");
                    }
                    else
                    {
                        Debug.LogWarning("[MigrationExample] Basic validation found issues - check component setup");
                    }
                }
                
                // Generate report
                if (generateReport)
                {
                    string report = GenerateBasicReport();
                    Debug.Log($"[MigrationExample] Setup Report:\n{report}");
                }
            }
            else
            {
                Debug.LogError("[MigrationExample] Composition agent setup failed");
            }
        }
        
        /// <summary>
        /// Manually sets up composition components
        /// </summary>
        private bool SetupCompositionAgent()
        {
            try
            {
                // Add core components if not present
                if (targetGameObject.GetComponent<MLAgentComposer>() == null)
                {
                    targetGameObject.AddComponent<MLAgentComposer>();
                    Debug.Log("[MigrationExample] Added MLAgentComposer");
                }
                
                if (targetGameObject.GetComponent<VectorObservationCollector>() == null)
                {
                    targetGameObject.AddComponent<VectorObservationCollector>();
                    Debug.Log("[MigrationExample] Added VectorObservationCollector");
                }
                
                if (targetGameObject.GetComponent<ActionDistributor>() == null)
                {
                    targetGameObject.AddComponent<ActionDistributor>();
                    Debug.Log("[MigrationExample] Added ActionDistributor");
                }
                
                if (targetGameObject.GetComponent<RewardCalculator>() == null)
                {
                    targetGameObject.AddComponent<RewardCalculator>();
                    Debug.Log("[MigrationExample] Added RewardCalculator");
                }
                
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MigrationExample] Error setting up composition agent: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Basic validation of composition setup
        /// </summary>
        private bool ValidateCompositionSetup()
        {
            bool isValid = true;
            
            if (targetGameObject.GetComponent<MLAgentComposer>() == null)
            {
                Debug.LogWarning("[MigrationExample] Missing MLAgentComposer");
                isValid = false;
            }
            
            if (targetGameObject.GetComponent<VectorObservationCollector>() == null)
            {
                Debug.LogWarning("[MigrationExample] Missing VectorObservationCollector");
                isValid = false;
            }
            
            if (targetGameObject.GetComponent<ActionDistributor>() == null)
            {
                Debug.LogWarning("[MigrationExample] Missing ActionDistributor");
                isValid = false;
            }
            
            if (targetGameObject.GetComponent<RewardCalculator>() == null)
            {
                Debug.LogWarning("[MigrationExample] Missing RewardCalculator");
                isValid = false;
            }
            
            return isValid;
        }
        
        /// <summary>
        /// Generates a basic setup report
        /// </summary>
        private string GenerateBasicReport()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine($"=== Composition Agent Setup Report ===");
            report.AppendLine($"Target: {targetGameObject.name}");
            report.AppendLine($"Generated: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine();
            
            var components = targetGameObject.GetComponents<Component>();
            report.AppendLine($"Total Components: {components.Length}");
            
            var composer = targetGameObject.GetComponent<MLAgentComposer>();
            report.AppendLine($"MLAgentComposer: {(composer != null ? "✓" : "✗")}");
            
            var obsCollector = targetGameObject.GetComponent<VectorObservationCollector>();
            report.AppendLine($"VectorObservationCollector: {(obsCollector != null ? "✓" : "✗")}");
            
            var actionDistributor = targetGameObject.GetComponent<ActionDistributor>();
            report.AppendLine($"ActionDistributor: {(actionDistributor != null ? "✓" : "✗")}");
            
            var rewardCalculator = targetGameObject.GetComponent<RewardCalculator>();
            report.AppendLine($"RewardCalculator: {(rewardCalculator != null ? "✓" : "✗")}");
            
            return report.ToString();
        }
        
        /// <summary>
        /// Validates the current setup
        /// </summary>
        public void ValidateSetup()
        {
            if (targetGameObject == null)
            {
                targetGameObject = gameObject;
            }
            
            bool isValid = ValidateCompositionSetup();
            
            Debug.Log($"[MigrationExample] Validation Results:");
            Debug.Log($"  Valid: {isValid}");
            
            if (isValid)
            {
                Debug.Log("[MigrationExample] All core components are present");
            }
            else
            {
                Debug.LogWarning("[MigrationExample] Some core components are missing - check console for details");
            }
        }
        
        /// <summary>
        /// Generates a setup report
        /// </summary>
        public void GenerateReport()
        {
            if (targetGameObject == null)
            {
                targetGameObject = gameObject;
            }
            
            string report = GenerateBasicReport();
            Debug.Log($"[MigrationExample] Setup Report:\n{report}");
        }
    }
}