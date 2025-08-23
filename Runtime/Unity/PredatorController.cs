using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.SideChannels;

namespace TCS.MLAgents._Damon.TCS.MLAgents.Runtime.Unity {
    [RequireComponent(typeof(Movement))]
    [RequireComponent(typeof(BoundarySystem))]
    [RequireComponent(typeof(RewardSystem))]
    [RequireComponent(typeof(ConeVision))]
    public class PredatorController : Agent {
        [SerializeField] Transform targetPrey;
        [SerializeField] SimulationConfig config;
        
        Movement movement;
        BoundarySystem boundarySystem;
        RewardSystem rewardSystem;
        ConeVision coneVision;
        StringLogSideChannel logChannel;
        
        float episodeStartTime;
        
        public override void Initialize() {
            movement = GetComponent<Movement>();
            boundarySystem = GetComponent<BoundarySystem>();
            rewardSystem = GetComponent<RewardSystem>();
            coneVision = GetComponent<ConeVision>();
            
            SetupFromConfig();
            
            logChannel = new StringLogSideChannel();
            SideChannelManager.RegisterSideChannel(logChannel);
            
            base.Initialize();
        }
        
        void SetupFromConfig() {
            if (config == null) return;
            
            movement.Speed = config.predatorSpeed;
        }
        
        public override void OnEpisodeBegin() {
            episodeStartTime = Time.time;
            
            movement.StopMovement();
            
            if (config != null && config.randomizeStartPositions) {
                movement.SetPosition(config.GetRandomArenaPosition());
            }
            
            ResetPrey();
            logChannel?.Log("Episode started");
        }
        
        void ResetPrey() {
            if (targetPrey == null) return;
            
            if (targetPrey.TryGetComponent<PreyController>(out var preyController)) {
                preyController.Respawn();
            }
        }
        
        public override void CollectObservations(VectorSensor sensor) {
            if (targetPrey == null || config == null) return;
            
            sensor.AddObservation(movement.Velocity);
            sensor.AddObservation(movement.Forward);
            sensor.AddObservation(Time.time - episodeStartTime);
            
            float[] visionDistances = coneVision.GetDistanceObservations();
            sensor.AddObservation(visionDistances);
            
            if (config.includeVelocityObservations) {
                if (targetPrey.TryGetComponent<Movement>(out var preyMovement)) {
                    sensor.AddObservation(preyMovement.Velocity);
                } else {
                    sensor.AddObservation(Vector3.zero);
                }
            }
            
            if (config.includeDistanceObservations) {
                Vector3 relativePosition = targetPrey.localPosition - transform.localPosition;
                sensor.AddObservation(relativePosition.normalized);
                
                float distance = Vector3.Distance(transform.localPosition, targetPrey.localPosition);
                sensor.AddObservation(distance / 20f);
            }
        }
        
        public override void OnActionReceived(ActionBuffers actionBuffers) {
            if (config == null) return;
            
            var continuousActions = actionBuffers.ContinuousActions;
            if (continuousActions.Length >= 2) {
                float forwardForce = continuousActions[0];
                float rotationForce = continuousActions[1];
                
                movement.MoveForward(forwardForce);
                movement.Rotate(rotationForce);
            }
            
            rewardSystem.ApplyTimePenalty();
            
            CheckForCatch();
            CheckBoundaries();
            CheckEpisodeTimeout();
            CheckVisionReward();
            
            LogStatistics();
        }
        
        void CheckForCatch() {
            if (targetPrey == null || config == null) return;
            
            if (config.IsWithinCatchDistance(transform.localPosition, targetPrey.localPosition)) {
                rewardSystem.RewardCatch();
                logChannel?.Log("Caught the prey!");
                rewardSystem.EndEpisode();
            }
        }
        
        void CheckBoundaries() {
            if (config == null) return;
            
            Vector3 pos = movement.Position;
            bool outOfBounds = pos.x < config.arenaMinBounds.x || pos.x > config.arenaMaxBounds.x ||
                              pos.z < config.arenaMinBounds.z || pos.z > config.arenaMaxBounds.z;
            
            if (outOfBounds) {
                rewardSystem.PenalizeBoundaryViolation();
                logChannel?.Log("Went out of bounds");
                rewardSystem.EndEpisode();
            }
        }
        
        void CheckEpisodeTimeout() {
            if (config == null) return;
            
            if (Time.time - episodeStartTime >= config.maxEpisodeLength) {
                logChannel?.Log("Episode timeout");
                rewardSystem.EndEpisode();
            }
        }
        
        void CheckVisionReward() {
            if (targetPrey == null || coneVision == null) return;
            
            if (coneVision.CanSeeTarget(targetPrey)) {
                rewardSystem.GiveReward(0.001f);
            }
        }
        
        void LogStatistics() {
            if (targetPrey == null) return;
            
            float distance = Vector3.Distance(transform.localPosition, targetPrey.localPosition);
            Academy.Instance.StatsRecorder.Add("Predator/DistanceToPrey", distance);
            
            if (coneVision != null) {
                Academy.Instance.StatsRecorder.Add("Predator/CanSeePrey", coneVision.CanSeeTarget(targetPrey) ? 1f : 0f);
            }
        }
        
        public override void Heuristic(in ActionBuffers actionsOut) {
            var continuousActionsOut = actionsOut.ContinuousActions;
            if (continuousActionsOut.Length >= 2) {
                continuousActionsOut[0] = Input.GetAxis("Vertical");
                continuousActionsOut[1] = Input.GetAxis("Horizontal");
            }
        }
        
        void OnCollisionEnter(Collision collision) {
            if (collision.transform == targetPrey) {
                rewardSystem.RewardCatch();
                logChannel?.Log("Caught the prey via collision");
                rewardSystem.EndEpisode();
            }
        }
        
        void OnDestroy() {
            if (logChannel != null) {
                SideChannelManager.UnregisterSideChannel(logChannel);
            }
        }
    }
}