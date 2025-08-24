using UnityEngine;
using TCS.MLAgents.Core.Interfaces;

namespace TCS.MLAgents.Core.Actions {
    /// <summary>
    /// Receives and processes movement actions from the ML agent.
    /// Supports various movement modes including force-based, velocity-based, and position-based movement.
    /// </summary>
    [System.Serializable]
    public class MovementActionReceiver : ActionReceiverBase {
        [Header("Movement Settings")]
        [SerializeField] Rigidbody targetRigidbody;
        [SerializeField] MovementMode movementMode = MovementMode.Force;
        [SerializeField] bool constrainToXZ = true;
        [SerializeField] bool useLocalSpace = false;
        
        [Header("Force Settings")]
        [SerializeField] float forceMultiplier = 10f;
        [SerializeField] ForceMode forceMode = ForceMode.Force;
        [SerializeField] float maxForce = 100f;
        
        [Header("Velocity Settings")]
        [SerializeField] float velocityMultiplier = 5f;
        [SerializeField] float maxVelocity = 20f;
        [SerializeField] bool preserveYVelocity = true;
        
        [Header("Position Settings")]
        [SerializeField] float positionSensitivity = 1f;
        [SerializeField] float maxPositionDelta = 1f;
        [SerializeField] bool smoothPositionMovement = true;
        [SerializeField] float positionSmoothTime = 0.1f;
        
        [Header("Action Configuration")]
        [SerializeField] ActionInputMode inputMode = ActionInputMode.TwoAxis;
        [SerializeField] bool clampActions = true;
        [SerializeField] float deadzone = 0.1f;
        
        public enum MovementMode {
            Force,          // Apply forces to rigidbody
            Velocity,       // Set velocity directly
            Position,       // Move to target position
            Acceleration    // Apply acceleration
        }
        
        public enum ActionInputMode {
            TwoAxis,        // X and Z movement (2 actions)
            ThreeAxis,      // X, Y, and Z movement (3 actions)
            DirectionMagnitude  // Direction vector + magnitude (4 actions)
        }
        
        public override int ContinuousActionCount {
            get {
                return inputMode switch {
                    ActionInputMode.TwoAxis => 2,
                    ActionInputMode.ThreeAxis => 3,
                    ActionInputMode.DirectionMagnitude => 4,
                    _ => 2
                };
            }
        }
        
        public override int DiscreteActionBranchCount => 0; // Movement uses continuous actions
        
        private Vector3 currentTargetVelocity;
        private Vector3 positionSmoothVelocity;
        private Vector3 lastActionInput;
        
        protected override void OnInitialize(AgentContext context) {
            if (targetRigidbody == null) {
                targetRigidbody = context.GetComponent<Rigidbody>();
                if (targetRigidbody == null) {
                    Debug.LogWarning($"[{ReceiverName}] No Rigidbody found on agent GameObject");
                }
            }
            
            currentTargetVelocity = Vector3.zero;
            positionSmoothVelocity = Vector3.zero;
            lastActionInput = Vector3.zero;
        }
        
        protected override bool OnValidate(AgentContext context) {
            if (targetRigidbody == null) {
                Debug.LogError($"[{ReceiverName}] Target rigidbody is null");
                return false;
            }
            
            if (forceMultiplier < 0 || velocityMultiplier < 0 || positionSensitivity < 0) {
                Debug.LogWarning($"[{ReceiverName}] Multipliers should be non-negative");
            }
            
            return true;
        }
        
        public override void OnEpisodeBegin(AgentContext context) {
            currentTargetVelocity = Vector3.zero;
            positionSmoothVelocity = Vector3.zero;
            lastActionInput = Vector3.zero;
            
            if (targetRigidbody != null) {
                targetRigidbody.linearVelocity = Vector3.zero;
                targetRigidbody.angularVelocity = Vector3.zero;
            }
        }
        
        public override void ReceiveContinuousActions(float[] actions, int startIndex, AgentContext context) {
            if (targetRigidbody == null) return;
            
            Vector3 actionVector = ParseActionInput(actions, startIndex);
            
            // Apply deadzone
            if (deadzone > 0f && actionVector.magnitude < deadzone) {
                actionVector = Vector3.zero;
            }
            
            // Clamp actions if enabled
            if (clampActions) {
                actionVector = Vector3.ClampMagnitude(actionVector, 1f);
            }
            
            lastActionInput = actionVector;
            
            // Apply movement based on mode
            switch (movementMode) {
                case MovementMode.Force:
                    ApplyForceMovement(actionVector);
                    break;
                case MovementMode.Velocity:
                    ApplyVelocityMovement(actionVector);
                    break;
                case MovementMode.Position:
                    ApplyPositionMovement(actionVector);
                    break;
                case MovementMode.Acceleration:
                    ApplyAccelerationMovement(actionVector);
                    break;
            }
        }
        
        public override void ProvideHeuristicActions(float[] continuousActions, int[] discreteActions, 
            int continuousStartIndex, int discreteStartIndex, AgentContext context) {
            
            // Provide keyboard input as heuristic
            Vector3 input = Vector3.zero;
            
            if (inputMode == ActionInputMode.TwoAxis) {
                input.x = Input.GetAxis("Horizontal");
                input.z = Input.GetAxis("Vertical");
                
                SafeSetContinuousAction(continuousActions, continuousStartIndex, input.x);
                SafeSetContinuousAction(continuousActions, continuousStartIndex + 1, input.z);
            }
            else if (inputMode == ActionInputMode.ThreeAxis) {
                input.x = Input.GetAxis("Horizontal");
                input.y = Input.GetKey(KeyCode.Space) ? 1f : (Input.GetKey(KeyCode.LeftShift) ? -1f : 0f);
                input.z = Input.GetAxis("Vertical");
                
                SafeSetContinuousAction(continuousActions, continuousStartIndex, input.x);
                SafeSetContinuousAction(continuousActions, continuousStartIndex + 1, input.y);
                SafeSetContinuousAction(continuousActions, continuousStartIndex + 2, input.z);
            }
            else if (inputMode == ActionInputMode.DirectionMagnitude) {
                Vector2 moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
                float magnitude = moveInput.magnitude;
                Vector2 direction = magnitude > 0.1f ? moveInput.normalized : Vector2.zero;
                
                SafeSetContinuousAction(continuousActions, continuousStartIndex, direction.x);
                SafeSetContinuousAction(continuousActions, continuousStartIndex + 1, direction.y);
                SafeSetContinuousAction(continuousActions, continuousStartIndex + 2, 0f); // Z direction
                SafeSetContinuousAction(continuousActions, continuousStartIndex + 3, magnitude);
            }
        }
        
        private Vector3 ParseActionInput(float[] actions, int startIndex) {
            Vector3 actionVector = Vector3.zero;
            
            switch (inputMode) {
                case ActionInputMode.TwoAxis:
                    actionVector.x = SafeGetContinuousAction(actions, startIndex);
                    actionVector.z = SafeGetContinuousAction(actions, startIndex + 1);
                    break;
                    
                case ActionInputMode.ThreeAxis:
                    actionVector.x = SafeGetContinuousAction(actions, startIndex);
                    actionVector.y = SafeGetContinuousAction(actions, startIndex + 1);
                    actionVector.z = SafeGetContinuousAction(actions, startIndex + 2);
                    break;
                    
                case ActionInputMode.DirectionMagnitude:
                    float dirX = SafeGetContinuousAction(actions, startIndex);
                    float dirY = SafeGetContinuousAction(actions, startIndex + 1);
                    float dirZ = SafeGetContinuousAction(actions, startIndex + 2);
                    float magnitude = SafeGetContinuousAction(actions, startIndex + 3);
                    
                    Vector3 direction = new Vector3(dirX, dirY, dirZ);
                    if (direction.magnitude > 0.001f) {
                        direction = direction.normalized;
                    }
                    actionVector = direction * Mathf.Clamp01(magnitude);
                    break;
            }
            
            // Constrain to XZ plane if enabled
            if (constrainToXZ) {
                actionVector.y = 0f;
            }
            
            return actionVector;
        }
        
        private void ApplyForceMovement(Vector3 actionVector) {
            Vector3 force = actionVector * forceMultiplier;
            
            // Transform to world space if using local space
            if (useLocalSpace) {
                force = targetRigidbody.transform.TransformDirection(force);
            }
            
            // Clamp force magnitude
            if (maxForce > 0) {
                force = Vector3.ClampMagnitude(force, maxForce);
            }
            
            targetRigidbody.AddForce(force, forceMode);
        }
        
        private void ApplyVelocityMovement(Vector3 actionVector) {
            Vector3 targetVelocity = actionVector * velocityMultiplier;
            
            // Transform to world space if using local space
            if (useLocalSpace) {
                targetVelocity = targetRigidbody.transform.TransformDirection(targetVelocity);
            }
            
            // Clamp velocity magnitude
            if (maxVelocity > 0) {
                targetVelocity = Vector3.ClampMagnitude(targetVelocity, maxVelocity);
            }
            
            // Preserve Y velocity if enabled
            if (preserveYVelocity && constrainToXZ) {
                targetVelocity.y = targetRigidbody.linearVelocity.y;
            }
            
            targetRigidbody.linearVelocity = targetVelocity;
            currentTargetVelocity = targetVelocity;
        }
        
        private void ApplyPositionMovement(Vector3 actionVector) {
            Vector3 deltaPosition = actionVector * positionSensitivity * Time.fixedDeltaTime;
            
            // Transform to world space if using local space
            if (useLocalSpace) {
                deltaPosition = targetRigidbody.transform.TransformDirection(deltaPosition);
            }
            
            // Clamp position delta
            if (maxPositionDelta > 0) {
                deltaPosition = Vector3.ClampMagnitude(deltaPosition, maxPositionDelta * Time.fixedDeltaTime);
            }
            
            Vector3 targetPosition = targetRigidbody.position + deltaPosition;
            
            if (smoothPositionMovement) {
                targetPosition = Vector3.SmoothDamp(targetRigidbody.position, targetPosition, 
                    ref positionSmoothVelocity, positionSmoothTime);
            }
            
            targetRigidbody.MovePosition(targetPosition);
        }
        
        private void ApplyAccelerationMovement(Vector3 actionVector) {
            Vector3 acceleration = actionVector * forceMultiplier;
            
            // Transform to world space if using local space
            if (useLocalSpace) {
                acceleration = targetRigidbody.transform.TransformDirection(acceleration);
            }
            
            Vector3 force = acceleration * targetRigidbody.mass;
            
            // Clamp force magnitude
            if (maxForce > 0) {
                force = Vector3.ClampMagnitude(force, maxForce);
            }
            
            targetRigidbody.AddForce(force, ForceMode.Force);
        }
        
        // Public methods for runtime configuration
        public void SetMovementMode(MovementMode mode) {
            movementMode = mode;
        }
        
        public void SetTargetRigidbody(Rigidbody rb) {
            targetRigidbody = rb;
        }
        
        public void SetForceMultiplier(float multiplier) {
            forceMultiplier = Mathf.Max(0f, multiplier);
        }
        
        public void SetVelocityMultiplier(float multiplier) {
            velocityMultiplier = Mathf.Max(0f, multiplier);
        }
        
        public void SetConstrainToXZ(bool constrain) {
            constrainToXZ = constrain;
        }
        
        // Utility methods for getting current state
        public Vector3 GetLastActionInput() {
            return lastActionInput;
        }
        
        public Vector3 GetCurrentVelocity() {
            return targetRigidbody != null ? targetRigidbody.linearVelocity : Vector3.zero;
        }
        
        public Vector3 GetCurrentTargetVelocity() {
            return currentTargetVelocity;
        }
        
        public float GetCurrentSpeed() {
            return GetCurrentVelocity().magnitude;
        }
        
        public bool IsMoving() {
            return GetCurrentSpeed() > 0.1f;
        }
    }
}