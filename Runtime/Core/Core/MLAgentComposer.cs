using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using TCS.MLAgents.Core.Interfaces;

namespace TCS.MLAgents.Core {
    /// <summary>
    /// Core component that bridges Unity ML-Agents with the composition system.
    /// This component inherits from Agent and delegates to registered IMLAgent components.
    /// </summary>
    public class MLAgentComposer : Agent {
        [Header("Composition System")]
        [SerializeField] bool autoDiscoverComponents = true;
        [SerializeField] bool debugLogging = false;
        
        [Header("Agent Components")]
        [SerializeField] List<Component> registeredComponents = new List<Component>();
        
        // Core composition system
        private AgentContext context;
        private List<IMLAgent> agentComponents;
        private bool isInitialized = false;
        
        public AgentContext Context => context;
        public IReadOnlyList<IMLAgent> AgentComponents => agentComponents.AsReadOnly();
        
        void Awake() {
            InitializeCompositionSystem();
        }
        
        void InitializeCompositionSystem() {
            if (isInitialized) return;
            
            // Create agent context
            context = new AgentContext(gameObject);
            agentComponents = new List<IMLAgent>();
            
            // Discover and register components
            if (autoDiscoverComponents) {
                DiscoverAgentComponents();
            }
            
            RegisterExplicitComponents();
            
            if (debugLogging) {
                Debug.Log($"[MLAgentComposer] Initialized with {agentComponents.Count} agent components");
            }
            
            isInitialized = true;
        }
        
        void DiscoverAgentComponents() {
            // Find all components that implement IMLAgent on this GameObject and children
            IMLAgent[] foundComponents = GetComponentsInChildren<IMLAgent>();
            
            foreach (var component in foundComponents) {
                if (!agentComponents.Contains(component)) {
                    agentComponents.Add(component);
                    
                    if (debugLogging) {
                        Debug.Log($"[MLAgentComposer] Discovered component: {component.GetType().Name}");
                    }
                }
            }
        }
        
        void RegisterExplicitComponents() {
            // Register components that were manually added to the list
            foreach (var component in registeredComponents) {
                if (component is IMLAgent agentComponent && !agentComponents.Contains(agentComponent)) {
                    agentComponents.Add(agentComponent);
                    
                    if (debugLogging) {
                        Debug.Log($"[MLAgentComposer] Registered component: {component.GetType().Name}");
                    }
                }
            }
        }
        
        public void RegisterComponent(IMLAgent component) {
            if (component != null && !agentComponents.Contains(component)) {
                agentComponents.Add(component);
                
                // If component is already initialized, call Initialize
                if (isInitialized) {
                    try {
                        component.Initialize();
                    } catch (System.Exception e) {
                        Debug.LogError($"[MLAgentComposer] Error initializing component {component.GetType().Name}: {e.Message}");
                    }
                }
                
                if (debugLogging) {
                    Debug.Log($"[MLAgentComposer] Dynamically registered component: {component.GetType().Name}");
                }
            }
        }
        
        public void UnregisterComponent(IMLAgent component) {
            if (agentComponents.Remove(component)) {
                if (debugLogging) {
                    Debug.Log($"[MLAgentComposer] Unregistered component: {component.GetType().Name}");
                }
            }
        }
        
        public T GetAgentComponent<T>() where T : class, IMLAgent {
            foreach (var component in agentComponents) {
                if (component is T typedComponent) {
                    return typedComponent;
                }
            }
            return null;
        }
        
        public List<T> GetAgentComponents<T>() where T : class, IMLAgent {
            List<T> results = new List<T>();
            
            foreach (var component in agentComponents) {
                if (component is T typedComponent) {
                    results.Add(typedComponent);
                }
            }
            
            return results;
        }
        
        // Unity ML-Agents Agent overrides - delegate to composition system
        
        public override void Initialize() {
            if (!isInitialized) {
                InitializeCompositionSystem();
            }
            
            // Initialize all agent components
            foreach (var component in agentComponents) {
                try {
                    component.Initialize();
                } catch (System.Exception e) {
                    Debug.LogError($"[MLAgentComposer] Error initializing component {component.GetType().Name}: {e.Message}");
                }
            }
            
            if (debugLogging) {
                Debug.Log($"[MLAgentComposer] Initialized {agentComponents.Count} components");
            }
        }
        
        public override void OnEpisodeBegin() {
            context.StartEpisode();
            
            // Notify all agent components
            foreach (var component in agentComponents) {
                try {
                    component.OnEpisodeBegin();
                } catch (System.Exception e) {
                    Debug.LogError($"[MLAgentComposer] Error in OnEpisodeBegin for component {component.GetType().Name}: {e.Message}");
                }
            }
            
            if (debugLogging) {
                Debug.Log($"[MLAgentComposer] Started episode {context.EpisodeCount}");
            }
        }
        
        public override void CollectObservations(VectorSensor sensor) {
            // Let all agent components add their observations
            foreach (var component in agentComponents) {
                try {
                    component.CollectObservations(sensor);
                } catch (System.Exception e) {
                    Debug.LogError($"[MLAgentComposer] Error collecting observations from component {component.GetType().Name}: {e.Message}");
                }
            }
        }
        
        public override void OnActionReceived(ActionBuffers actionBuffers) {
            context.UpdateStep();
            
            // Let all agent components handle actions
            foreach (var component in agentComponents) {
                try {
                    component.OnActionReceived(actionBuffers);
                } catch (System.Exception e) {
                    Debug.LogError($"[MLAgentComposer] Error handling actions in component {component.GetType().Name}: {e.Message}");
                }
            }
        }
        
        public override void Heuristic(in ActionBuffers actionsOut) {
            // Let agent components provide heuristic actions
            // Note: Components should coordinate to avoid conflicts
            foreach (var component in agentComponents) {
                try {
                    component.Heuristic(in actionsOut);
                } catch (System.Exception e) {
                    Debug.LogError($"[MLAgentComposer] Error in heuristic for component {component.GetType().Name}: {e.Message}");
                }
            }
        }
        
        void FixedUpdate() {
            if (!isInitialized) return;
            
            // Let agent components handle fixed update
            foreach (var component in agentComponents) {
                try {
                    component.FixedUpdate();
                } catch (System.Exception e) {
                    Debug.LogError($"[MLAgentComposer] Error in FixedUpdate for component {component.GetType().Name}: {e.Message}");
                }
            }
        }
        
        void OnDestroy() {
            // Notify all agent components
            foreach (var component in agentComponents) {
                try {
                    component.OnDestroy();
                } catch (System.Exception e) {
                    Debug.LogError($"[MLAgentComposer] Error in OnDestroy for component {component.GetType().Name}: {e.Message}");
                }
            }
            
            context?.EndEpisode();
            agentComponents?.Clear();
        }
        
        // Reward system delegation
        public new void AddReward(float reward) {
            base.AddReward(reward);
            context?.AddReward(reward);
        }
        
        public new void SetReward(float reward) {
            base.SetReward(reward);
            context?.SetReward(reward);
        }
        
        public new void EndEpisode() {
            context?.EndEpisode();
            base.EndEpisode();
        }
        
        // Editor and debugging utilities
        [ContextMenu("Refresh Components")]
        void RefreshComponents() {
            if (Application.isPlaying) {
                agentComponents.Clear();
                DiscoverAgentComponents();
                RegisterExplicitComponents();
                Debug.Log($"[MLAgentComposer] Refreshed - found {agentComponents.Count} components");
            }
        }
        
        [ContextMenu("Debug Component Info")]
        void DebugComponentInfo() {
            Debug.Log($"[MLAgentComposer] Agent Components ({agentComponents.Count}):");
            foreach (var component in agentComponents) {
                Debug.Log($"  - {component.GetType().Name} on {((MonoBehaviour)component).gameObject.name}");
            }
            
            if (context != null) {
                Debug.Log($"[MLAgentComposer] Context: {context}");
            }
        }
    }
}