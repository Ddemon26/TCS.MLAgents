using TCS.MLAgents.Core;
using TCS.MLAgents.Interfaces;
namespace TCS.MLAgents.Actions {
    /// <summary>
    /// Receives and processes rotation actions from the ML agent.
    /// Supports various rotation modes including torque-based, angular velocity, and direct rotation.
    /// </summary>
    [System.Serializable]
    public class RotationActionReceiver : ActionReceiverBase {
        [Header("Rotation Settings")]
        [SerializeField] Rigidbody targetRigidbody;
        [SerializeField] Transform targetTransform;
        [SerializeField] RotationMode rotationMode = RotationMode.Torque;
        [SerializeField] bool useLocalSpace = false;
        [SerializeField] bool constrainAxes = true;
        [SerializeField] Vector3 allowedAxes = Vector3.up; // Y-axis only by default
        
        [Header("Torque Settings")]
        [SerializeField] float torqueMultiplier = 10f;
        [SerializeField] float maxTorque = 100f;
        [SerializeField] ForceMode torqueMode = ForceMode.Force;
        
        [Header("Angular Velocity Settings")]
        [SerializeField] float angularVelocityMultiplier = 5f;
        [SerializeField] float maxAngularVelocity = 360f; // degrees per second
        [SerializeField] bool preserveUnconstrainedAxes = true;
        
        [Header("Direct Rotation Settings")]
        [SerializeField] float rotationSensitivity = 90f; // degrees per second
        [SerializeField] float maxRotationDelta = 180f; // degrees per frame
        [SerializeField] bool smoothRotation = true;
        [SerializeField] float rotationSmoothTime = 0.1f;
        
        [Header("Action Configuration")]
        [SerializeField] RotationInputMode inputMode = RotationInputMode.SingleAxis;
        [SerializeField] bool clampActions = true;
        [SerializeField] float deadzone = 0.1f;
        
        [Header("Look-At Settings")]
        [SerializeField] bool enableLookAt = false;
        [SerializeField] Transform lookAtTarget;
        [SerializeField] float lookAtSmoothTime = 0.2f;
        [SerializeField] Vector3 lookAtOffset = Vector3.zero;
        
        public enum RotationMode {
            Torque,             // Apply torque to rigidbody
            AngularVelocity,    // Set angular velocity directly
            DirectRotation,     // Rotate transform directly
            LookRotation        // Look towards a direction
        }
        
        public enum RotationInputMode {
            SingleAxis,         // Single rotation axis (1 action)
            TwoAxis,           // Two rotation axes (2 actions)
            ThreeAxis,         // All three rotation axes (3 actions)
            LookDirection      // Look towards direction vector (2-3 actions)
        }
        
        public override int ContinuousActionCount {
            get {
                return inputMode switch {
                    RotationInputMode.SingleAxis => 1,
                    RotationInputMode.TwoAxis => 2,
                    RotationInputMode.ThreeAxis => 3,
                    RotationInputMode.LookDirection => 2, // Horizontal and Vertical look
                    _ => 1
                };
            }
        }
        
        public override int DiscreteActionBranchCount => 0; // Rotation uses continuous actions
        
        private Vector3 currentTargetAngularVelocity;
        private Vector3 rotationSmoothVelocity;
        private Vector3 lastActionInput;
        private Quaternion targetRotation;
        
        protected override void OnInitialize(AgentContext context) {
            if (targetRigidbody == null && targetTransform == null) {
                targetRigidbody = context.GetComponent<Rigidbody>();
                targetTransform = context.AgentGameObject.transform;
            }
            
            if (targetTransform == null && targetRigidbody != null) {
                targetTransform = targetRigidbody.transform;
            }
            
            currentTargetAngularVelocity = Vector3.zero;
            rotationSmoothVelocity = Vector3.zero;
            lastActionInput = Vector3.zero;
            
            if (targetTransform != null) {
                targetRotation = targetTransform.rotation;
            }
        }
        
        protected override bool OnValidate(AgentContext context) {
            if (targetRigidbody == null && targetTransform == null) {
                Debug.LogError($"[{ReceiverName}] Both target rigidbody and transform are null");
                return false;
            }
            
            if (rotationMode == RotationMode.Torque || rotationMode == RotationMode.AngularVelocity) {
                if (targetRigidbody == null) {
                    Debug.LogError($"[{ReceiverName}] Rigidbody required for {rotationMode} mode");
                    return false;
                }
            }
            
            if (torqueMultiplier < 0 || angularVelocityMultiplier < 0 || rotationSensitivity < 0) {
                Debug.LogWarning($"[{ReceiverName}] Multipliers should be non-negative");
            }
            
            return true;
        }
        
        public override void OnEpisodeBegin(AgentContext context) {
            currentTargetAngularVelocity = Vector3.zero;
            rotationSmoothVelocity = Vector3.zero;
            lastActionInput = Vector3.zero;
            
            if (targetRigidbody != null) {
                targetRigidbody.angularVelocity = Vector3.zero;
            }
            
            if (targetTransform != null) {
                targetRotation = targetTransform.rotation;
            }
        }
        
        public override void ReceiveContinuousActions(float[] actions, int startIndex, AgentContext context) {
            if (targetTransform == null) return;
            
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
            
            // Apply rotation based on mode
            switch (rotationMode) {
                case RotationMode.Torque:
                    ApplyTorqueRotation(actionVector);
                    break;
                case RotationMode.AngularVelocity:
                    ApplyAngularVelocityRotation(actionVector);
                    break;
                case RotationMode.DirectRotation:
                    ApplyDirectRotation(actionVector);
                    break;
                case RotationMode.LookRotation:
                    ApplyLookRotation(actionVector);
                    break;
            }
        }
        
        public override void ProvideHeuristicActions(float[] continuousActions, int[] discreteActions, 
            int continuousStartIndex, int discreteStartIndex, AgentContext context) {
            
            // Provide mouse and keyboard input as heuristic
            Vector3 input = Vector3.zero;
            
            if (inputMode == RotationInputMode.SingleAxis) {
                // Use A/D keys or mouse X for single axis rotation
                input.y = Input.GetAxis("Horizontal");
                SafeSetContinuousAction(continuousActions, continuousStartIndex, input.y);
            }
            else if (inputMode == RotationInputMode.TwoAxis) {
                // Use mouse for two-axis rotation
                input.x = Input.GetAxis("Mouse Y") * -1f; // Invert for natural feel
                input.y = Input.GetAxis("Mouse X");
                
                SafeSetContinuousAction(continuousActions, continuousStartIndex, input.x);
                SafeSetContinuousAction(continuousActions, continuousStartIndex + 1, input.y);
            }
            else if (inputMode == RotationInputMode.ThreeAxis) {
                // Use mouse + Q/E keys for three-axis rotation
                input.x = Input.GetAxis("Mouse Y") * -1f;
                input.y = Input.GetAxis("Mouse X");
                input.z = Input.GetKey(KeyCode.Q) ? -1f : (Input.GetKey(KeyCode.E) ? 1f : 0f);
                
                SafeSetContinuousAction(continuousActions, continuousStartIndex, input.x);
                SafeSetContinuousAction(continuousActions, continuousStartIndex + 1, input.y);
                SafeSetContinuousAction(continuousActions, continuousStartIndex + 2, input.z);
            }
            else if (inputMode == RotationInputMode.LookDirection) {
                // Use mouse for look direction
                float mouseX = Input.GetAxis("Mouse X");
                float mouseY = Input.GetAxis("Mouse Y");
                
                SafeSetContinuousAction(continuousActions, continuousStartIndex, mouseX);
                SafeSetContinuousAction(continuousActions, continuousStartIndex + 1, mouseY);
            }
        }
        
        public override void FixedUpdate(AgentContext context) {
            // Handle look-at behavior if enabled
            if (enableLookAt && lookAtTarget != null && targetTransform != null) {
                UpdateLookAtBehavior();
            }
        }
        
        private Vector3 ParseActionInput(float[] actions, int startIndex) {
            Vector3 actionVector = Vector3.zero;
            
            switch (inputMode) {
                case RotationInputMode.SingleAxis:
                    // Apply to the primary allowed axis (typically Y)
                    float primaryAxis = SafeGetContinuousAction(actions, startIndex);
                    if (allowedAxes.y != 0) actionVector.y = primaryAxis;
                    else if (allowedAxes.x != 0) actionVector.x = primaryAxis;
                    else if (allowedAxes.z != 0) actionVector.z = primaryAxis;
                    break;
                    
                case RotationInputMode.TwoAxis:
                    actionVector.x = SafeGetContinuousAction(actions, startIndex);
                    actionVector.y = SafeGetContinuousAction(actions, startIndex + 1);
                    break;
                    
                case RotationInputMode.ThreeAxis:
                    actionVector.x = SafeGetContinuousAction(actions, startIndex);
                    actionVector.y = SafeGetContinuousAction(actions, startIndex + 1);
                    actionVector.z = SafeGetContinuousAction(actions, startIndex + 2);
                    break;
                    
                case RotationInputMode.LookDirection:
                    // Convert look input to rotation around X and Y axes
                    float lookH = SafeGetContinuousAction(actions, startIndex);
                    float lookV = SafeGetContinuousAction(actions, startIndex + 1);
                    actionVector.x = lookV;
                    actionVector.y = lookH;
                    break;
            }
            
            // Apply axis constraints
            if (constrainAxes) {
                actionVector.x *= allowedAxes.x;
                actionVector.y *= allowedAxes.y;
                actionVector.z *= allowedAxes.z;
            }
            
            return actionVector;
        }
        
        private void ApplyTorqueRotation(Vector3 actionVector) {
            if (targetRigidbody == null) return;
            
            Vector3 torque = actionVector * torqueMultiplier;
            
            // Transform to world space if using local space
            if (useLocalSpace) {
                torque = targetTransform.TransformDirection(torque);
            }
            
            // Clamp torque magnitude
            if (maxTorque > 0) {
                torque = Vector3.ClampMagnitude(torque, maxTorque);
            }
            
            targetRigidbody.AddTorque(torque, torqueMode);
        }
        
        private void ApplyAngularVelocityRotation(Vector3 actionVector) {
            if (targetRigidbody == null) return;
            
            Vector3 targetAngularVelocity = actionVector * angularVelocityMultiplier * Mathf.Deg2Rad;
            
            // Transform to world space if using local space
            if (useLocalSpace) {
                targetAngularVelocity = targetTransform.TransformDirection(targetAngularVelocity);
            }
            
            // Clamp angular velocity magnitude
            if (maxAngularVelocity > 0) {
                float maxAngularVelRad = maxAngularVelocity * Mathf.Deg2Rad;
                targetAngularVelocity = Vector3.ClampMagnitude(targetAngularVelocity, maxAngularVelRad);
            }
            
            // Preserve unconstrained axes if enabled
            if (preserveUnconstrainedAxes && constrainAxes) {
                Vector3 currentAngularVel = targetRigidbody.angularVelocity;
                if (allowedAxes.x == 0) targetAngularVelocity.x = currentAngularVel.x;
                if (allowedAxes.y == 0) targetAngularVelocity.y = currentAngularVel.y;
                if (allowedAxes.z == 0) targetAngularVelocity.z = currentAngularVel.z;
            }
            
            targetRigidbody.angularVelocity = targetAngularVelocity;
            currentTargetAngularVelocity = targetAngularVelocity;
        }
        
        private void ApplyDirectRotation(Vector3 actionVector) {
            if (targetTransform == null) return;
            
            Vector3 rotationDelta = actionVector * rotationSensitivity * Time.fixedDeltaTime;
            
            // Clamp rotation delta
            if (maxRotationDelta > 0) {
                rotationDelta = Vector3.ClampMagnitude(rotationDelta, maxRotationDelta * Time.fixedDeltaTime);
            }
            
            Quaternion deltaRotation = Quaternion.Euler(rotationDelta);
            
            if (useLocalSpace) {
                targetRotation = targetTransform.rotation * deltaRotation;
            } else {
                targetRotation = deltaRotation * targetTransform.rotation;
            }
            
            if (smoothRotation) {
                targetTransform.rotation = Quaternion.Slerp(targetTransform.rotation, targetRotation, 
                    Time.fixedDeltaTime / rotationSmoothTime);
            } else {
                targetTransform.rotation = targetRotation;
            }
        }
        
        private void ApplyLookRotation(Vector3 actionVector) {
            if (targetTransform == null) return;
            
            // Create look direction from action input
            Vector3 lookDirection = Vector3.forward;
            
            if (inputMode == RotationInputMode.LookDirection) {
                // Convert horizontal and vertical look to direction
                float yaw = actionVector.y * rotationSensitivity;
                float pitch = actionVector.x * rotationSensitivity;
                
                Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
                lookDirection = rotation * Vector3.forward;
            } else {
                // Use action vector as direct look direction
                if (actionVector.magnitude > 0.1f) {
                    lookDirection = actionVector.normalized;
                }
            }
            
            // Calculate target rotation
            Quaternion lookRotation = Quaternion.LookRotation(lookDirection);
            
            if (smoothRotation) {
                targetTransform.rotation = Quaternion.Slerp(targetTransform.rotation, lookRotation, 
                    Time.fixedDeltaTime / rotationSmoothTime);
            } else {
                targetTransform.rotation = lookRotation;
            }
        }
        
        private void UpdateLookAtBehavior() {
            if (lookAtTarget == null || targetTransform == null) return;
            
            Vector3 lookDirection = (lookAtTarget.position + lookAtOffset - targetTransform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(lookDirection);
            
            targetTransform.rotation = Quaternion.Slerp(targetTransform.rotation, lookRotation, 
                Time.fixedDeltaTime / lookAtSmoothTime);
        }
        
        // Public methods for runtime configuration
        public void SetRotationMode(RotationMode mode) {
            rotationMode = mode;
        }
        
        public void SetTargetRigidbody(Rigidbody rb) {
            targetRigidbody = rb;
            if (rb != null && targetTransform == null) {
                targetTransform = rb.transform;
            }
        }
        
        public void SetTargetTransform(Transform transform) {
            targetTransform = transform;
        }
        
        public void SetTorqueMultiplier(float multiplier) {
            torqueMultiplier = Mathf.Max(0f, multiplier);
        }
        
        public void SetAngularVelocityMultiplier(float multiplier) {
            angularVelocityMultiplier = Mathf.Max(0f, multiplier);
        }
        
        public void SetAllowedAxes(Vector3 axes) {
            allowedAxes = axes;
        }
        
        public void SetLookAtTarget(Transform target) {
            lookAtTarget = target;
            enableLookAt = target != null;
        }
        
        // Utility methods for getting current state
        public Vector3 GetLastActionInput() {
            return lastActionInput;
        }
        
        public Vector3 GetCurrentAngularVelocity() {
            return targetRigidbody != null ? targetRigidbody.angularVelocity * Mathf.Rad2Deg : Vector3.zero;
        }
        
        public Vector3 GetCurrentTargetAngularVelocity() {
            return currentTargetAngularVelocity * Mathf.Rad2Deg;
        }
        
        public Quaternion GetCurrentRotation() {
            return targetTransform != null ? targetTransform.rotation : Quaternion.identity;
        }
        
        public Vector3 GetCurrentEulerAngles() {
            return GetCurrentRotation().eulerAngles;
        }
        
        public bool IsRotating() {
            return GetCurrentAngularVelocity().magnitude > 1f; // 1 degree per second threshold
        }
    }
}