using System.Collections.Generic;
using TCS.MLAgents.Interfaces;
using TCS.MLAgents.Utilities;
using UnityEngine;
using Unity.MLAgents.Sensors;

namespace TCS.MLAgents.Core {
    /// <summary>
    /// Collects and manages observations from multiple observation providers.
    /// Coordinates observation collection and provides them to the ML agent.
    /// </summary>
    public class VectorObservationCollector : MonoBehaviour, IMLAgent {
        [Header("Observation Collection")]
        [SerializeField] bool autoDiscoverProviders = true;
        [SerializeField] bool debugLogging = false;
        [SerializeField] bool validateObservations = true;
        
        [Header("Registered Providers")]
        [SerializeField] List<Component> explicitProviders = new List<Component>();
        
        private List<IObservationProvider> observationProviders;
        private ObservationBuffer observationBuffer;
        private AgentContext context;
        private bool isInitialized = false;
        private int totalObservationSize = 0;
        
        public AgentContext Context => context;
        public IReadOnlyList<IObservationProvider> ObservationProviders => observationProviders?.AsReadOnly();
        public int TotalObservationSize => totalObservationSize;
        public ObservationBuffer Buffer => observationBuffer;
        
        public void Initialize() {
            if (isInitialized) return;
            
            observationProviders = new List<IObservationProvider>();
            observationBuffer = new ObservationBuffer();
            
            DiscoverAndRegisterProviders();
            InitializeProviders();
            SetupObservationBuffer();
            
            if (debugLogging) {
                Debug.Log($"[VectorObservationCollector] Initialized with {observationProviders.Count} providers, total size: {totalObservationSize}");
            }
            
            isInitialized = true;
        }
        
        public void OnEpisodeBegin() {
            if (!isInitialized) {
                Initialize();
            }
            
            observationBuffer.Clear();
            SetupObservationBuffer();
            
            // Notify all providers of episode start
            foreach (var provider in observationProviders) {
                if (provider.IsActive) {
                    try {
                        provider.OnEpisodeBegin(context);
                    } catch (System.Exception e) {
                        Debug.LogError($"[VectorObservationCollector] Error in OnEpisodeBegin for provider {provider.ProviderName}: {e.Message}");
                    }
                }
            }
            
            if (debugLogging) {
                Debug.Log($"[VectorObservationCollector] Episode began, reset {observationProviders.Count} providers");
            }
        }
        
        public void CollectObservations(VectorSensor sensor) {
            if (!isInitialized) {
                Debug.LogWarning("[VectorObservationCollector] Not initialized, skipping observation collection");
                return;
            }
            
            observationBuffer.Clear();
            SetupObservationBuffer();
            
            int totalObservationsAdded = 0;
            
            // Collect observations from all active providers
            foreach (var provider in observationProviders) {
                if (!provider.IsActive) continue;
                
                try {
                    int beforeCount = observationBuffer.Count;
                    provider.CollectObservations(sensor, context);
                    totalObservationsAdded += provider.ObservationSize;
                    
                    if (debugLogging) {
                        Debug.Log($"[VectorObservationCollector] {provider.ProviderName} added {provider.ObservationSize} observations");
                    }
                } catch (System.Exception e) {
                    Debug.LogError($"[VectorObservationCollector] Error collecting observations from {provider.ProviderName}: {e.Message}");
                    
                    // Add zeros for failed provider to maintain observation space consistency
                    for (int i = 0; i < provider.ObservationSize; i++) {
                        sensor.AddObservation(0f);
                    }
                    totalObservationsAdded += provider.ObservationSize;
                }
            }
            
            if (validateObservations) {
                ValidateCollectedObservations(totalObservationsAdded);
            }
        }
        
        public void OnActionReceived(Unity.MLAgents.Actuators.ActionBuffers actionBuffers) {
            // Observation collector doesn't need to handle actions
        }
        
        public void Heuristic(in Unity.MLAgents.Actuators.ActionBuffers actionsOut) {
            // Observation collector doesn't provide heuristic actions
        }
        
        public void FixedUpdate() {
            // Observation collector doesn't need fixed update logic
        }
        
        public void OnDestroy() {
            observationProviders?.Clear();
            observationBuffer = null;
        }
        
        public void EndEpisode() {
            // Observation collector doesn't control episode end
        }
        
        public void AddReward(float reward) {
            // Observation collector doesn't handle rewards
        }
        
        public void SetReward(float reward) {
            // Observation collector doesn't handle rewards
        }
        
        private void DiscoverAndRegisterProviders() {
            if (autoDiscoverProviders) {
                // Find all observation providers on this GameObject and children
                IObservationProvider[] foundProviders = GetComponentsInChildren<IObservationProvider>();
                
                foreach (var provider in foundProviders) {
                    if (provider != this && !observationProviders.Contains(provider)) {
                        observationProviders.Add(provider);
                        
                        if (debugLogging) {
                            Debug.Log($"[VectorObservationCollector] Discovered provider: {provider.ProviderName}");
                        }
                    }
                }
            }
            
            // Register explicitly assigned providers
            foreach (var component in explicitProviders) {
                if (component is IObservationProvider provider && !observationProviders.Contains(provider)) {
                    observationProviders.Add(provider);
                    
                    if (debugLogging) {
                        Debug.Log($"[VectorObservationCollector] Registered explicit provider: {provider.ProviderName}");
                    }
                }
            }
        }
        
        private void InitializeProviders() {
            totalObservationSize = 0;
            
            foreach (var provider in observationProviders) {
                try {
                    provider.Initialize(context);
                    
                    if (provider.ValidateProvider(context)) {
                        totalObservationSize += provider.ObservationSize;
                    } else {
                        Debug.LogWarning($"[VectorObservationCollector] Provider {provider.ProviderName} failed validation");
                    }
                } catch (System.Exception e) {
                    Debug.LogError($"[VectorObservationCollector] Error initializing provider {provider.ProviderName}: {e.Message}");
                }
            }
        }
        
        private void SetupObservationBuffer() {
            if (observationBuffer.Capacity < totalObservationSize) {
                observationBuffer.Resize(totalObservationSize + 32); // Extra buffer for safety
            }
            
            // Reserve space for each provider
            foreach (var provider in observationProviders) {
                if (provider.IsActive) {
                    observationBuffer.ReserveSpace(provider.ProviderName, provider.ObservationSize);
                }
            }
        }
        
        private void ValidateCollectedObservations(int expectedCount) {
            if (expectedCount != totalObservationSize) {
                Debug.LogWarning($"[VectorObservationCollector] Observation count mismatch. Expected: {totalObservationSize}, Actual: {expectedCount}");
            }
            
            if (!observationBuffer.ValidateBuffer()) {
                Debug.LogWarning("[VectorObservationCollector] Observation buffer validation failed");
            }
        }
        
        public void RegisterProvider(IObservationProvider provider) {
            if (provider != null && !observationProviders.Contains(provider)) {
                observationProviders.Add(provider);
                
                if (isInitialized) {
                    provider.Initialize(context);
                    totalObservationSize += provider.ObservationSize;
                    SetupObservationBuffer();
                }
                
                if (debugLogging) {
                    Debug.Log($"[VectorObservationCollector] Dynamically registered provider: {provider.ProviderName}");
                }
            }
        }
        
        public void UnregisterProvider(IObservationProvider provider) {
            if (observationProviders.Remove(provider)) {
                totalObservationSize -= provider.ObservationSize;
                SetupObservationBuffer();
                
                if (debugLogging) {
                    Debug.Log($"[VectorObservationCollector] Unregistered provider: {provider.ProviderName}");
                }
            }
        }
        
        public T GetProvider<T>() where T : class, IObservationProvider {
            foreach (var provider in observationProviders) {
                if (provider is T typedProvider) {
                    return typedProvider;
                }
            }
            return null;
        }
        
        public List<T> GetProviders<T>() where T : class, IObservationProvider {
            List<T> results = new List<T>();
            
            foreach (var provider in observationProviders) {
                if (provider is T typedProvider) {
                    results.Add(typedProvider);
                }
            }
            
            return results;
        }
        
        [ContextMenu("Refresh Providers")]
        private void RefreshProviders() {
            if (Application.isPlaying) {
                observationProviders.Clear();
                DiscoverAndRegisterProviders();
                InitializeProviders();
                Debug.Log($"[VectorObservationCollector] Refreshed - found {observationProviders.Count} providers");
            }
        }
        
        [ContextMenu("Debug Provider Info")]
        private void DebugProviderInfo() {
            Debug.Log($"[VectorObservationCollector] Observation Providers ({observationProviders.Count}):");
            foreach (var provider in observationProviders) {
                Debug.Log($"  - {provider.ProviderName}: {provider.ObservationSize} observations, Active: {provider.IsActive}");
            }
            Debug.Log($"[VectorObservationCollector] Total observation size: {totalObservationSize}");
        }
    }
}