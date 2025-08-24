namespace TCS.MLAgents.Utilities {
    /// <summary>
    /// Efficient buffer for collecting and managing observation data.
    /// Provides optimized storage and retrieval for ML agent observations.
    /// </summary>
    public class ObservationBuffer {
        private List<float> observations;
        private Dictionary<string, int> providerOffsets;
        private int totalCapacity;
        private int currentSize;
        
        public int Count => currentSize;
        public int Capacity => totalCapacity;
        public bool IsFull => currentSize >= totalCapacity;
        
        public ObservationBuffer(int capacity = 128) {
            totalCapacity = capacity;
            observations = new List<float>(capacity);
            providerOffsets = new Dictionary<string, int>();
            currentSize = 0;
        }
        
        /// <summary>
        /// Clears all observations and resets the buffer.
        /// </summary>
        public void Clear() {
            observations.Clear();
            providerOffsets.Clear();
            currentSize = 0;
        }
        
        /// <summary>
        /// Reserves space for a provider and returns the starting offset.
        /// </summary>
        /// <param name="providerName">Name of the observation provider</param>
        /// <param name="observationCount">Number of observations to reserve</param>
        /// <returns>Starting offset for the provider's observations</returns>
        public int ReserveSpace(string providerName, int observationCount) {
            if (providerOffsets.TryGetValue( providerName, out int space )) {
                Debug.LogWarning($"[ObservationBuffer] Provider '{providerName}' already has reserved space");
                return space;
            }
            
            int startOffset = currentSize;
            providerOffsets[providerName] = startOffset;
            
            // Pre-allocate space with default values
            for (int i = 0; i < observationCount; i++) {
                observations.Add(0f);
                currentSize++;
            }
            
            if (currentSize > totalCapacity) {
                Debug.LogWarning($"[ObservationBuffer] Buffer capacity ({totalCapacity}) exceeded. Current size: {currentSize}");
            }
            
            return startOffset;
        }
        
        /// <summary>
        /// Adds a single observation for a specific provider.
        /// </summary>
        /// <param name="providerName">Name of the observation provider</param>
        /// <param name="value">Observation value</param>
        /// <param name="index">Index within the provider's observation space</param>
        public void AddObservation(string providerName, float value, int index = 0) {
            if (!providerOffsets.TryGetValue(providerName, out int startOffset)) {
                Debug.LogError($"[ObservationBuffer] Provider '{providerName}' has no reserved space");
                return;
            }
            
            int targetIndex = startOffset + index;
            if (targetIndex >= observations.Count) {
                Debug.LogError($"[ObservationBuffer] Index {targetIndex} is out of bounds for provider '{providerName}'");
                return;
            }
            
            observations[targetIndex] = value;
        }
        
        /// <summary>
        /// Adds multiple observations for a specific provider.
        /// </summary>
        /// <param name="providerName">Name of the observation provider</param>
        /// <param name="values">Array of observation values</param>
        public void AddObservations(string providerName, float[] values) {
            if (values == null) {
                Debug.LogWarning($"[ObservationBuffer] Null values provided for provider '{providerName}'");
                return;
            }
            
            if (!providerOffsets.TryGetValue(providerName, out int startOffset)) {
                Debug.LogError($"[ObservationBuffer] Provider '{providerName}' has no reserved space");
                return;
            }
            
            for (int i = 0; i < values.Length; i++) {
                int targetIndex = startOffset + i;
                if (targetIndex >= observations.Count) {
                    Debug.LogError($"[ObservationBuffer] Index {targetIndex} is out of bounds for provider '{providerName}'");
                    break;
                }
                
                observations[targetIndex] = values[i];
            }
        }
        
        /// <summary>
        /// Adds observations from a Vector3.
        /// </summary>
        /// <param name="providerName">Name of the observation provider</param>
        /// <param name="vector">Vector3 to add as observations</param>
        /// <param name="startIndex">Starting index within the provider's space</param>
        public void AddVector3(string providerName, Vector3 vector, int startIndex = 0) {
            AddObservation(providerName, vector.x, startIndex);
            AddObservation(providerName, vector.y, startIndex + 1);
            AddObservation(providerName, vector.z, startIndex + 2);
        }
        
        /// <summary>
        /// Adds observations from a Vector2.
        /// </summary>
        /// <param name="providerName">Name of the observation provider</param>
        /// <param name="vector">Vector2 to add as observations</param>
        /// <param name="startIndex">Starting index within the provider's space</param>
        public void AddVector2(string providerName, Vector2 vector, int startIndex = 0) {
            AddObservation(providerName, vector.x, startIndex);
            AddObservation(providerName, vector.y, startIndex + 1);
        }
        
        /// <summary>
        /// Gets all observations for a specific provider.
        /// </summary>
        /// <param name="providerName">Name of the observation provider</param>
        /// <param name="observationCount">Number of observations expected</param>
        /// <returns>Array of observations for the provider</returns>
        public float[] GetObservations(string providerName, int observationCount) {
            if (!providerOffsets.TryGetValue(providerName, out int startOffset)) {
                Debug.LogError($"[ObservationBuffer] Provider '{providerName}' has no reserved space");
                return new float[observationCount];
            }
            
            float[] result = new float[observationCount];
            for (int i = 0; i < observationCount; i++) {
                int index = startOffset + i;
                if (index < observations.Count) {
                    result[i] = observations[index];
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Gets a single observation value for a provider.
        /// </summary>
        /// <param name="providerName">Name of the observation provider</param>
        /// <param name="index">Index within the provider's observation space</param>
        /// <returns>The observation value</returns>
        public float GetObservation(string providerName, int index = 0) {
            if (!providerOffsets.TryGetValue(providerName, out int startOffset)) {
                Debug.LogError($"[ObservationBuffer] Provider '{providerName}' has no reserved space");
                return 0f;
            }
            
            int targetIndex = startOffset + index;
            if (targetIndex >= observations.Count) {
                Debug.LogError($"[ObservationBuffer] Index {targetIndex} is out of bounds for provider '{providerName}'");
                return 0f;
            }
            
            return observations[targetIndex];
        }
        
        /// <summary>
        /// Gets all observations as a single array.
        /// </summary>
        /// <returns>Array containing all observations</returns>
        public float[] ToArray() {
            return observations.ToArray();
        }
        
        /// <summary>
        /// Gets a read-only list of all observations.
        /// </summary>
        /// <returns>Read-only list of observations</returns>
        public IReadOnlyList<float> GetObservations() {
            return observations.AsReadOnly();
        }
        
        /// <summary>
        /// Resizes the buffer capacity.
        /// </summary>
        /// <param name="newCapacity">New capacity for the buffer</param>
        public void Resize(int newCapacity) {
            if (newCapacity < currentSize) {
                Debug.LogWarning($"[ObservationBuffer] Cannot resize to {newCapacity}, current size is {currentSize}");
                return;
            }
            
            totalCapacity = newCapacity;
            
            if (observations.Capacity < newCapacity) {
                observations.Capacity = newCapacity;
            }
        }
        
        /// <summary>
        /// Gets information about all registered providers.
        /// </summary>
        /// <returns>Dictionary mapping provider names to their starting offsets</returns>
        public Dictionary<string, int> GetProviderInfo() {
            return new Dictionary<string, int>(providerOffsets);
        }
        
        /// <summary>
        /// Validates the buffer state and logs any issues.
        /// </summary>
        /// <returns>True if the buffer state is valid</returns>
        public bool ValidateBuffer() {
            bool isValid = true;
            
            if (observations.Count != currentSize) {
                Debug.LogError($"[ObservationBuffer] Inconsistent state: observations.Count={observations.Count}, currentSize={currentSize}");
                isValid = false;
            }
            
            if (currentSize > totalCapacity) {
                Debug.LogWarning($"[ObservationBuffer] Buffer over capacity: {currentSize}/{totalCapacity}");
            }
            
            // Check for NaN or Infinity values
            for (int i = 0; i < observations.Count; i++) {
                if (float.IsNaN(observations[i]) || float.IsInfinity(observations[i])) {
                    Debug.LogWarning($"[ObservationBuffer] Invalid value at index {i}: {observations[i]}");
                }
            }
            
            return isValid;
        }
        
        public override string ToString() {
            return $"ObservationBuffer[Size: {currentSize}/{totalCapacity}, Providers: {providerOffsets.Count}]";
        }
    }
}