using UnityEngine;
using UnityEditor;
using TCS.MLAgents.Configuration;

namespace TCS.MLAgents.Editor {
    /// <summary>
    /// Wizard for creating new MLBehaviorConfig assets
    /// </summary>
    public class MLBehaviorConfigWizard : ScriptableWizard {
        public string behaviorName = "NewBehavior";
        public MLBehaviorConfig.BehaviorType behaviorType = MLBehaviorConfig.BehaviorType.Inference;
        public string modelPath = "";
        
        public int vectorObservationSize = 0;
        public int continuousActionSize = 0;
        public int[] discreteActionBranches = new int[0];
        
        public int maxSteps = 1000;
        public float episodeTimeout = 30.0f;
        
        [MenuItem("Assets/Create/TCS ML-Agents/MLBehaviorConfig Wizard")]
        static void CreateWizard() {
            DisplayWizard<MLBehaviorConfigWizard>("Create MLBehaviorConfig", "Create");
        }
        
        void OnWizardCreate() {
            // Create new MLBehaviorConfig asset
            var config = CreateInstance<MLBehaviorConfig>();
            config.SetBehaviorName(behaviorName);
            config.SetBehaviorType(behaviorType);
            config.SetModelPath(modelPath);
            
            // This is a simplified approach - in a real implementation, you would
            // need to properly set all the serialized fields through reflection
            // or create helper methods in MLBehaviorConfig
            
            // Save the asset
            string path = EditorUtility.SaveFilePanelInProject(
                "Save MLBehaviorConfig",
                behaviorName + ".asset",
                "asset",
                "Enter a file name for the MLBehaviorConfig asset"
            );
            
            if (!string.IsNullOrEmpty(path)) {
                AssetDatabase.CreateAsset(config, path);
                AssetDatabase.SaveAssets();
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = config;
            }
        }
        
        void OnWizardUpdate() {
            helpString = "Create a new MLBehaviorConfig asset";
            errorString = "";
            
            if (string.IsNullOrEmpty(behaviorName)) {
                errorString = "Behavior name cannot be empty";
            }
            
            if (behaviorType == MLBehaviorConfig.BehaviorType.Inference && string.IsNullOrEmpty(modelPath)) {
                errorString = "Model path must be specified for inference behavior";
            }
        }
    }
}