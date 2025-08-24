using TCS.MLAgents.Core;
using TCS.MLAgents.Interfaces;
using Unity.MLAgents.Sensors;
namespace TCS.MLAgents.Observations {
    /// <summary>
    /// Provides observations based on Rigidbody velocity and angular velocity.
    /// Supports both linear and angular velocity with optional normalization and smoothing.
    /// </summary>
    [Serializable]
    public class VelocityObservationProvider : ObservationProviderBase {
        [Header("Velocity Settings")]
        [SerializeField] Rigidbody targetRigidbody;
        [SerializeField] bool includeLinearVelocity = true;
        [SerializeField] bool includeAngularVelocity = true;
        [SerializeField] bool useLocalSpace = false;
        
        [Header("Normalization")]
        [SerializeField] bool normalizeLinearVelocity = true;
        [SerializeField] float maxLinearVelocity = 10f;
        [SerializeField] bool normalizeAngularVelocity = true;
        [SerializeField] float maxAngularVelocity = 360f; // degrees per second
        
        [Header("Smoothing")]
        [SerializeField] bool smoothVelocity = false;
        [SerializeField] float smoothingFactor = 0.1f;
        [SerializeField] int smoothingBufferSize = 5;
        
        [Header("Additional Observations")]
        [SerializeField] bool includeMagnitudes = false;
        [SerializeField] bool includeDirection = false;
        [SerializeField] bool includeAcceleration = false;
        
        public override int ObservationSize {
            get {
                int size = 0;
                if (includeLinearVelocity) size += 3;
                if (includeAngularVelocity) size += 3;
                if (includeMagnitudes) {
                    if (includeLinearVelocity) size += 1;
                    if (includeAngularVelocity) size += 1;
                }
                if (includeDirection && includeLinearVelocity) size += 3; // normalized direction
                if (includeAcceleration) size += 3; // linear acceleration
                return size;
            }
        }
        
        // Smoothing and acceleration tracking
        private Vector3[] linearVelocityBuffer;
        private Vector3[] angularVelocityBuffer;
        private int bufferIndex = 0;
        private bool bufferFilled = false;
        
        private Vector3 previousLinearVelocity;
        private Vector3 currentAcceleration;
        private float lastUpdateTime;
        
        protected override void OnInitialize(AgentContext context) {
            if (targetRigidbody == null) {
                targetRigidbody = context.GetComponent<Rigidbody>();
                if (targetRigidbody == null) {
                    Debug.LogWarning($"[{ProviderName}] No Rigidbody found on agent GameObject");
                }
            }
            
            if (smoothVelocity) {
                InitializeSmoothing();
            }
            
            previousLinearVelocity = GetLinearVelocity();
            lastUpdateTime = Time.time;
        }
        
        protected override bool OnValidate(AgentContext context) {
            if (targetRigidbody == null) {
                Debug.LogError($"[{ProviderName}] Target rigidbody is null");
                return false;
            }
            
            if (maxLinearVelocity <= 0) {
                Debug.LogWarning($"[{ProviderName}] Max linear velocity should be positive");
                maxLinearVelocity = 1f;
            }
            
            if (maxAngularVelocity <= 0) {
                Debug.LogWarning($"[{ProviderName}] Max angular velocity should be positive");
                maxAngularVelocity = 1f;
            }
            
            return true;
        }
        
        public override void OnEpisodeBegin(AgentContext context) {
            if (smoothVelocity) {
                ClearSmoothing();
            }
            
            previousLinearVelocity = GetLinearVelocity();
            currentAcceleration = Vector3.zero;
            lastUpdateTime = Time.time;
        }
        
        public override void CollectObservations(VectorSensor sensor, AgentContext context) {
            if (targetRigidbody == null) {
                AddZeroObservations(sensor);
                return;
            }
            
            Vector3 linearVel = GetLinearVelocity();
            Vector3 angularVel = GetAngularVelocity();
            
            // Update acceleration
            if (includeAcceleration) {
                UpdateAcceleration(linearVel);
            }
            
            // Apply smoothing if enabled
            if (smoothVelocity) {
                linearVel = GetSmoothedLinearVelocity(linearVel);
                angularVel = GetSmoothedAngularVelocity(angularVel);
            }
            
            // Add linear velocity observations
            if (includeLinearVelocity) {
                Vector3 normalizedLinearVel = normalizeLinearVelocity ? 
                    NormalizeVelocity(linearVel, maxLinearVelocity) : linearVel;
                
                SafeAddObservation(sensor, normalizedLinearVel.x);
                SafeAddObservation(sensor, normalizedLinearVel.y);
                SafeAddObservation(sensor, normalizedLinearVel.z);
            }
            
            // Add angular velocity observations
            if (includeAngularVelocity) {
                Vector3 normalizedAngularVel = normalizeAngularVelocity ? 
                    NormalizeVelocity(angularVel, maxAngularVelocity * Mathf.Deg2Rad) : angularVel;
                
                SafeAddObservation(sensor, normalizedAngularVel.x);
                SafeAddObservation(sensor, normalizedAngularVel.y);
                SafeAddObservation(sensor, normalizedAngularVel.z);
            }
            
            // Add magnitude observations
            if (includeMagnitudes) {
                if (includeLinearVelocity) {
                    float linearMagnitude = linearVel.magnitude;
                    if (normalizeLinearVelocity) {
                        linearMagnitude = Mathf.Clamp01(linearMagnitude / maxLinearVelocity);
                    }
                    SafeAddObservation(sensor, linearMagnitude);
                }
                
                if (includeAngularVelocity) {
                    float angularMagnitude = angularVel.magnitude;
                    if (normalizeAngularVelocity) {
                        angularMagnitude = Mathf.Clamp01(angularMagnitude / (maxAngularVelocity * Mathf.Deg2Rad));
                    }
                    SafeAddObservation(sensor, angularMagnitude);
                }
            }
            
            // Add direction observations
            if (includeDirection && includeLinearVelocity) {
                Vector3 direction = linearVel.magnitude > 0.001f ? linearVel.normalized : Vector3.zero;
                SafeAddObservation(sensor, direction.x);
                SafeAddObservation(sensor, direction.y);
                SafeAddObservation(sensor, direction.z);
            }
            
            // Add acceleration observations
            if (includeAcceleration) {
                Vector3 normalizedAcceleration = normalizeLinearVelocity ? 
                    NormalizeVelocity(currentAcceleration, maxLinearVelocity * 10f) : currentAcceleration;
                
                SafeAddObservation(sensor, normalizedAcceleration.x);
                SafeAddObservation(sensor, normalizedAcceleration.y);
                SafeAddObservation(sensor, normalizedAcceleration.z);
            }
        }
        
        private Vector3 GetLinearVelocity() {
            if (targetRigidbody == null) return Vector3.zero;
            
            Vector3 velocity = targetRigidbody.linearVelocity;
            
            if (useLocalSpace && targetRigidbody.transform != null) {
                velocity = targetRigidbody.transform.InverseTransformDirection(velocity);
            }
            
            return velocity;
        }
        
        private Vector3 GetAngularVelocity() {
            if (targetRigidbody == null) return Vector3.zero;
            
            Vector3 angularVel = targetRigidbody.angularVelocity;
            
            if (useLocalSpace && targetRigidbody.transform != null) {
                angularVel = targetRigidbody.transform.InverseTransformDirection(angularVel);
            }
            
            return angularVel;
        }
        
        private Vector3 NormalizeVelocity(Vector3 velocity, float maxValue) {
            if (maxValue <= 0) return velocity;
            
            return new Vector3(
                Mathf.Clamp(velocity.x / maxValue, -1f, 1f),
                Mathf.Clamp(velocity.y / maxValue, -1f, 1f),
                Mathf.Clamp(velocity.z / maxValue, -1f, 1f)
            );
        }
        
        private void UpdateAcceleration(Vector3 currentLinearVelocity) {
            float deltaTime = Time.time - lastUpdateTime;
            if (deltaTime > 0) {
                currentAcceleration = (currentLinearVelocity - previousLinearVelocity) / deltaTime;
                previousLinearVelocity = currentLinearVelocity;
                lastUpdateTime = Time.time;
            }
        }
        
        private void InitializeSmoothing() {
            if (smoothingBufferSize <= 0) smoothingBufferSize = 5;
            
            linearVelocityBuffer = new Vector3[smoothingBufferSize];
            angularVelocityBuffer = new Vector3[smoothingBufferSize];
            bufferIndex = 0;
            bufferFilled = false;
        }
        
        private void ClearSmoothing() {
            if (linearVelocityBuffer != null) {
                Array.Clear(linearVelocityBuffer, 0, linearVelocityBuffer.Length);
            }
            if (angularVelocityBuffer != null) {
                Array.Clear(angularVelocityBuffer, 0, angularVelocityBuffer.Length);
            }
            bufferIndex = 0;
            bufferFilled = false;
        }
        
        private Vector3 GetSmoothedLinearVelocity(Vector3 currentVelocity) {
            if (linearVelocityBuffer == null) return currentVelocity;
            
            linearVelocityBuffer[bufferIndex] = currentVelocity;
            
            Vector3 smoothed = Vector3.zero;
            int count = bufferFilled ? smoothingBufferSize : bufferIndex + 1;
            
            for (int i = 0; i < count; i++) {
                smoothed += linearVelocityBuffer[i];
            }
            
            return smoothed / count;
        }
        
        private Vector3 GetSmoothedAngularVelocity(Vector3 currentVelocity) {
            if (angularVelocityBuffer == null) return currentVelocity;
            
            angularVelocityBuffer[bufferIndex] = currentVelocity;
            
            // Update buffer index
            bufferIndex = (bufferIndex + 1) % smoothingBufferSize;
            if (bufferIndex == 0) bufferFilled = true;
            
            Vector3 smoothed = Vector3.zero;
            int count = bufferFilled ? smoothingBufferSize : bufferIndex;
            if (!bufferFilled && bufferIndex == 0) count = smoothingBufferSize;
            
            for (int i = 0; i < count; i++) {
                smoothed += angularVelocityBuffer[i];
            }
            
            return smoothed / count;
        }
        
        private void AddZeroObservations(VectorSensor sensor) {
            for (int i = 0; i < ObservationSize; i++) {
                sensor.AddObservation(0f);
            }
        }
        
        // Public methods for runtime configuration
        public void SetTargetRigidbody(Rigidbody rb) {
            targetRigidbody = rb;
        }
        
        public void SetMaxVelocities(float maxLinear, float maxAngular) {
            maxLinearVelocity = maxLinear;
            maxAngularVelocity = maxAngular;
        }
        
        public void SetSmoothing(bool enable, float factor, int bufferSize) {
            smoothVelocity = enable;
            smoothingFactor = factor;
            smoothingBufferSize = bufferSize;
            
            if (enable) {
                InitializeSmoothing();
            }
        }
        
        // Utility methods for getting current values
        public Vector3 GetCurrentLinearVelocity() {
            return targetRigidbody != null ? GetLinearVelocity() : Vector3.zero;
        }
        
        public Vector3 GetCurrentAngularVelocity() {
            return targetRigidbody != null ? GetAngularVelocity() : Vector3.zero;
        }
        
        public Vector3 GetCurrentAcceleration() {
            return currentAcceleration;
        }
        
        public float GetSpeedMagnitude() {
            return GetCurrentLinearVelocity().magnitude;
        }
        
        public float GetAngularSpeedMagnitude() {
            return GetCurrentAngularVelocity().magnitude;
        }
    }
}