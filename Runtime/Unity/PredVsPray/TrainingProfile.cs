using Unity.Barracuda;
using Unity.MLAgents.Policies;
namespace TCS.MLAgents.PredVsPray {
    [CreateAssetMenu(menuName = "ML Agents/Training Profile", fileName = "TrainingProfile")]
    public class TrainingProfile : ScriptableObject {
        [Header("Training Settings")]
        public string profileName = "PredatorTraining";
        public BehaviorType behaviorType = BehaviorType.Default;
        public NNModel trainedModel;
        
        [Header("Environment Settings")]
        public SimulationConfig simulationConfig;
        
        [Header("Agent Configuration")]
        [Range(1, 100)]
        public int numberOfAgents = 1;
        public bool randomizeStartPositions = true;
        
        [Header("Vision Settings")]
        [Range(1f, 50f)]
        public float viewDistance = 10f;
        [Range(10f, 180f)]
        public float viewAngle = 45f;
        [Range(4, 32)]
        public int visionRayCount = 8;
        
        [Header("Movement Settings")]
        [Range(1f, 500f)]
        public float rotationSpeed = 180f;
        [Range(1f, 20f)]
        public float movementSpeed = 5f;
        
        [Header("Training Parameters")]
        [Range(10f, 300f)]
        public float maxEpisodeLength = 30f;
        public bool useTimePenalty = true;
        public bool useVisionReward = true;
        
        public void ApplyToAgent(PredatorController agent) {
            if (agent == null) return;
            
            // Apply behavior settings
            if (agent.TryGetComponent<BehaviorParameters>(out var behaviorParams)) {
                behaviorParams.BehaviorType = behaviorType;
                if (trainedModel != null) {
                    behaviorParams.Model = trainedModel;
                }
            }
            
            // Apply movement settings
            if (agent.TryGetComponent<Movement>(out var movement)) {
                movement.Speed = movementSpeed;
                movement.RotationSpeed = rotationSpeed;
            }
            
            // Apply vision settings
            if (agent.TryGetComponent<ConeVision>(out var vision)) {
                #if UNITY_EDITOR
                var serializedVision = new UnityEditor.SerializedObject(vision);
                serializedVision.FindProperty("viewDistance").floatValue = viewDistance;
                serializedVision.FindProperty("viewAngle").floatValue = viewAngle;
                serializedVision.FindProperty("rayCount").intValue = visionRayCount;
                serializedVision.ApplyModifiedProperties();
                #endif
            }
        }
        
        public void ApplyToSimulation(SimulationManager manager) {
            if (manager == null || simulationConfig == null) return;
            
            // Apply simulation config
            var field = typeof(SimulationManager).GetField("config", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(manager, simulationConfig);
            
            // Update simulation config with training profile settings
            simulationConfig.maxEpisodeLength = maxEpisodeLength;
            simulationConfig.randomizeStartPositions = randomizeStartPositions;
            simulationConfig.predatorSpeed = movementSpeed;
        }
        
        [ContextMenu("Create Training Scene")]
        public void CreateTrainingScene() {
            Debug.Log($"Training Profile: {profileName}");
            Debug.Log($"- Agents: {numberOfAgents}");
            Debug.Log($"- Episode Length: {maxEpisodeLength}s");
            Debug.Log($"- Vision: {viewAngle}° angle, {viewDistance} distance, {visionRayCount} rays");
            Debug.Log($"- Movement: {movementSpeed} speed, {rotationSpeed} rotation");
        }
    }
}