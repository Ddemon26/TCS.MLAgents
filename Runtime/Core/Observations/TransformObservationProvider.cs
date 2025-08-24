using UnityEngine;
using Unity.MLAgents.Sensors;
using TCS.MLAgents.Core.Interfaces;

namespace TCS.MLAgents.Core.Observations {
    /// <summary>
    /// Provides observations based on Transform position, rotation, and scale.
    /// Configurable to include absolute or relative positions, local or world coordinates.
    /// </summary>
    [System.Serializable]
    public class TransformObservationProvider : ObservationProviderBase {
        [Header("Transform Settings")]
        [SerializeField] Transform targetTransform;
        [SerializeField] bool useLocalPosition = true;
        [SerializeField] bool useLocalRotation = true;
        [SerializeField] bool includeScale = false;
        
        [Header("Position Observations")]
        [SerializeField] bool includePosition = true;
        [SerializeField] bool normalizePosition = false;
        [SerializeField] Vector3 positionNormalizationBounds = Vector3.one * 10f;
        
        [Header("Rotation Observations")]
        [SerializeField] bool includeRotation = true;
        [SerializeField] RotationFormat rotationFormat = RotationFormat.EulerAngles;
        [SerializeField] bool normalizeRotation = true;
        
        [Header("Relative Observations")]
        [SerializeField] bool useRelativeToReference = false;
        [SerializeField] Transform referenceTransform;
        
        public enum RotationFormat {
            EulerAngles,    // 3 values (x, y, z angles)
            Quaternion,     // 4 values (x, y, z, w)
            ForwardVector   // 3 values (forward direction)
        }
        
        public override int ObservationSize {
            get {
                int size = 0;
                if (includePosition) size += 3;
                if (includeRotation) {
                    size += rotationFormat switch {
                        RotationFormat.EulerAngles => 3,
                        RotationFormat.Quaternion => 4,
                        RotationFormat.ForwardVector => 3,
                        _ => 3
                    };
                }
                if (includeScale) size += 3;
                return size;
            }
        }
        
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private Vector3 initialScale;
        
        protected override void OnInitialize(AgentContext context) {
            if (targetTransform == null) {
                targetTransform = context.AgentGameObject.transform;
            }
            
            if (useRelativeToReference && referenceTransform == null) {
                referenceTransform = context.AgentGameObject.transform.parent;
            }
            
            // Store initial values for relative calculations
            initialPosition = GetPositionValue();
            initialRotation = GetRotationValue();
            initialScale = targetTransform.localScale;
        }
        
        protected override bool OnValidate(AgentContext context) {
            if (targetTransform == null) {
                Debug.LogError($"[{ProviderName}] Target transform is null");
                return false;
            }
            
            if (useRelativeToReference && referenceTransform == null) {
                Debug.LogWarning($"[{ProviderName}] Reference transform is null, using world coordinates");
            }
            
            return true;
        }
        
        public override void OnEpisodeBegin(AgentContext context) {
            // Update initial values for new episode
            initialPosition = GetPositionValue();
            initialRotation = GetRotationValue();
            initialScale = targetTransform.localScale;
        }
        
        public override void CollectObservations(VectorSensor sensor, AgentContext context) {
            if (targetTransform == null) {
                AddZeroObservations(sensor);
                return;
            }
            
            // Position observations
            if (includePosition) {
                Vector3 position = GetObservablePosition();
                SafeAddObservation(sensor, position.x);
                SafeAddObservation(sensor, position.y);
                SafeAddObservation(sensor, position.z);
            }
            
            // Rotation observations
            if (includeRotation) {
                AddRotationObservations(sensor);
            }
            
            // Scale observations
            if (includeScale) {
                Vector3 scale = targetTransform.localScale;
                SafeAddObservation(sensor, scale.x);
                SafeAddObservation(sensor, scale.y);
                SafeAddObservation(sensor, scale.z);
            }
        }
        
        private Vector3 GetObservablePosition() {
            Vector3 position = GetPositionValue();
            
            // Apply relative to reference if configured
            if (useRelativeToReference && referenceTransform != null) {
                Vector3 referencePos = useLocalPosition ? referenceTransform.localPosition : referenceTransform.position;
                position = position - referencePos;
            }
            
            // Apply normalization if configured
            if (normalizePosition) {
                position = NormalizePosition(position);
            }
            
            return position;
        }
        
        private Vector3 GetPositionValue() {
            return useLocalPosition ? targetTransform.localPosition : targetTransform.position;
        }
        
        private Quaternion GetRotationValue() {
            return useLocalRotation ? targetTransform.localRotation : targetTransform.rotation;
        }
        
        private Vector3 NormalizePosition(Vector3 position) {
            return new Vector3(
                Mathf.Clamp(position.x / positionNormalizationBounds.x, -1f, 1f),
                Mathf.Clamp(position.y / positionNormalizationBounds.y, -1f, 1f),
                Mathf.Clamp(position.z / positionNormalizationBounds.z, -1f, 1f)
            );
        }
        
        private void AddRotationObservations(VectorSensor sensor) {
            Quaternion rotation = GetRotationValue();
            
            // Apply relative to reference if configured
            if (useRelativeToReference && referenceTransform != null) {
                Quaternion referenceRot = useLocalRotation ? referenceTransform.localRotation : referenceTransform.rotation;
                rotation = Quaternion.Inverse(referenceRot) * rotation;
            }
            
            switch (rotationFormat) {
                case RotationFormat.EulerAngles:
                    AddEulerAngles(sensor, rotation);
                    break;
                case RotationFormat.Quaternion:
                    AddQuaternion(sensor, rotation);
                    break;
                case RotationFormat.ForwardVector:
                    AddForwardVector(sensor, rotation);
                    break;
            }
        }
        
        private void AddEulerAngles(VectorSensor sensor, Quaternion rotation) {
            Vector3 euler = rotation.eulerAngles;
            
            if (normalizeRotation) {
                // Normalize angles to [-1, 1] range
                euler.x = NormalizeAngle(euler.x);
                euler.y = NormalizeAngle(euler.y);
                euler.z = NormalizeAngle(euler.z);
            }
            
            SafeAddObservation(sensor, euler.x);
            SafeAddObservation(sensor, euler.y);
            SafeAddObservation(sensor, euler.z);
        }
        
        private void AddQuaternion(VectorSensor sensor, Quaternion rotation) {
            SafeAddObservation(sensor, rotation.x);
            SafeAddObservation(sensor, rotation.y);
            SafeAddObservation(sensor, rotation.z);
            SafeAddObservation(sensor, rotation.w);
        }
        
        private void AddForwardVector(VectorSensor sensor, Quaternion rotation) {
            Vector3 forward = rotation * Vector3.forward;
            SafeAddObservation(sensor, forward.x);
            SafeAddObservation(sensor, forward.y);
            SafeAddObservation(sensor, forward.z);
        }
        
        private float NormalizeAngle(float angle) {
            // Convert angle from [0, 360] to [-1, 1]
            if (angle > 180f) angle -= 360f;
            return angle / 180f;
        }
        
        private void AddZeroObservations(VectorSensor sensor) {
            for (int i = 0; i < ObservationSize; i++) {
                sensor.AddObservation(0f);
            }
        }
        
        // Public methods for runtime configuration
        public void SetTargetTransform(Transform transform) {
            targetTransform = transform;
        }
        
        public void SetReferenceTransform(Transform transform) {
            referenceTransform = transform;
            useRelativeToReference = transform != null;
        }
        
        public void SetPositionNormalization(bool normalize, Vector3 bounds) {
            normalizePosition = normalize;
            positionNormalizationBounds = bounds;
        }
        
        public void SetRotationFormat(RotationFormat format) {
            rotationFormat = format;
        }
        
        // Utility methods for getting current values
        public Vector3 GetCurrentPosition() {
            return targetTransform != null ? GetObservablePosition() : Vector3.zero;
        }
        
        public Quaternion GetCurrentRotation() {
            return targetTransform != null ? GetRotationValue() : Quaternion.identity;
        }
        
        public Vector3 GetCurrentScale() {
            return targetTransform != null ? targetTransform.localScale : Vector3.one;
        }
    }
}