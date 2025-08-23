using Unity.Barracuda;
using UnityEngine;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Actuators;

namespace TCS.MLAgents._Damon.TCS.MLAgents.Runtime.Unity {
    [System.Serializable]
    public class MLAgentSetupConfig {
        [Header( "Agent Configuration" )]
        public string behaviorName = "PredatorAgent";
        public BehaviorType behaviorType = BehaviorType.HeuristicOnly;
        public NNModel trainedModel;

        [Header( "Vision Settings" )]
        public float viewDistance = 10f;
        public float viewAngle = 45f;
        public int rayCount = 8;

        [Header( "Movement Settings" )]
        public float rotationSpeed = 180f;
        public bool useGravity = false;
    }

    public class MLAgentSetup : MonoBehaviour {
        [SerializeField] MLAgentSetupConfig setupConfig;
        [SerializeField] bool setupOnAwake = true;

        void Awake() {
            if ( setupOnAwake ) {
                SetupAgent();
            }
        }

        [ContextMenu( "Setup Agent" )]
        public void SetupAgent() {
            SetupBehaviorParameters();
            SetupVisionSystem();
            SetupMovementSystem();
        }

        void SetupBehaviorParameters() {
            if ( !TryGetComponent<BehaviorParameters>( out var behaviorParams ) ) {
                behaviorParams = gameObject.AddComponent<BehaviorParameters>();
            }

            behaviorParams.BehaviorName = setupConfig.behaviorName;
            behaviorParams.BehaviorType = setupConfig.behaviorType;
            behaviorParams.UseChildSensors = true;

            behaviorParams.BrainParameters.ActionSpec = ActionSpec.MakeContinuous( 2 );

            if ( setupConfig.trainedModel != null ) {
                behaviorParams.Model = setupConfig.trainedModel;
                behaviorParams.BehaviorType = BehaviorType.InferenceOnly;
            }
        }

        void SetupVisionSystem() {
            if ( TryGetComponent<ConeVision>( out var vision ) ) {
                #if UNITY_EDITOR
                var serializedVision = new UnityEditor.SerializedObject( vision );
                serializedVision.FindProperty( "viewDistance" ).floatValue = setupConfig.viewDistance;
                serializedVision.FindProperty( "viewAngle" ).floatValue = setupConfig.viewAngle;
                serializedVision.FindProperty( "rayCount" ).intValue = setupConfig.rayCount;
                serializedVision.ApplyModifiedProperties();
                #endif
            }
        }

        void SetupMovementSystem() {
            if ( TryGetComponent<Movement>( out var movement ) ) {
                #if UNITY_EDITOR
                var serializedMovement = new UnityEditor.SerializedObject( movement );
                serializedMovement.FindProperty( "rotationSpeed" ).floatValue = setupConfig.rotationSpeed;
                serializedMovement.FindProperty( "useGravity" ).boolValue = setupConfig.useGravity;
                serializedMovement.ApplyModifiedProperties();
                #endif
                movement.RotationSpeed = setupConfig.rotationSpeed;
            }
        }

        public void SetBehaviorType(BehaviorType type) {
            setupConfig.behaviorType = type;
            SetupBehaviorParameters();
        }

        public void SetTrainedModel(NNModel model) {
            setupConfig.trainedModel = model;
            SetupBehaviorParameters();
        }
    }
}