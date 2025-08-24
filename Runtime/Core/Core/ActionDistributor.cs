using System.Collections.Generic;
using System.Linq;
using TCS.MLAgents.Interfaces;
using UnityEngine;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using CoreActionReceiver = TCS.MLAgents.Interfaces.IActionReceiver;

namespace TCS.MLAgents.Core {
    /// <summary>
    /// Distributes actions from the ML agent to multiple action receivers.
    /// Manages action space allocation and coordinates action processing.
    /// </summary>
    public class ActionDistributor : MonoBehaviour, IMLAgent {
        [Header("Action Distribution")]
        [SerializeField] bool autoDiscoverReceivers = true;
        [SerializeField] bool debugLogging = false;
        [SerializeField] bool validateActionSpace = true;
        [SerializeField] bool sortByPriority = true;
        
        [Header("Registered Receivers")]
        [SerializeField] List<Component> explicitReceivers = new List<Component>();
        
        private List<CoreActionReceiver> actionReceivers;
        private AgentContext context;
        private bool isInitialized = false;
        
        // Action space allocation
        private int totalContinuousActions = 0;
        private int totalDiscreteActions = 0;
        private Dictionary<CoreActionReceiver, ActionAllocation> receiverAllocations;
        
        public AgentContext Context => context;
        public IReadOnlyList<CoreActionReceiver> ActionReceivers => actionReceivers?.AsReadOnly();
        public int TotalContinuousActions => totalContinuousActions;
        public int TotalDiscreteActions => totalDiscreteActions;
        
        /// <summary>
        /// Information about how actions are allocated to a specific receiver.
        /// </summary>
        public class ActionAllocation {
            public int ContinuousStartIndex { get; set; }
            public int ContinuousCount { get; set; }
            public int DiscreteStartIndex { get; set; }
            public int DiscreteCount { get; set; }
            public int[] DiscreteBranchSizes { get; set; }
            
            public override string ToString() {
                return $"Continuous: [{ContinuousStartIndex}, {ContinuousStartIndex + ContinuousCount - 1}], " +
                       $"Discrete: [{DiscreteStartIndex}, {DiscreteStartIndex + DiscreteCount - 1}]";
            }
        }
        
        public void Initialize() {
            if (isInitialized) return;
            
            actionReceivers = new List<CoreActionReceiver>();
            receiverAllocations = new Dictionary<CoreActionReceiver, ActionAllocation>();
            
            DiscoverAndRegisterReceivers();
            SortReceiversByPriority();
            InitializeReceivers();
            CalculateActionSpace();
            
            if (debugLogging) {
                Debug.Log($"[ActionDistributor] Initialized with {actionReceivers.Count} receivers. " +
                         $"Action space: {totalContinuousActions} continuous, {totalDiscreteActions} discrete");
            }
            
            isInitialized = true;
        }
        
        public void OnEpisodeBegin() {
            if (!isInitialized) {
                Initialize();
            }
            
            // Notify all receivers of episode start
            foreach (var receiver in actionReceivers) {
                if (receiver.IsActive) {
                    try {
                        receiver.OnEpisodeBegin(context);
                    } catch (System.Exception e) {
                        Debug.LogError($"[ActionDistributor] Error in OnEpisodeBegin for receiver {receiver.ReceiverName}: {e.Message}");
                    }
                }
            }
            
            if (debugLogging) {
                Debug.Log($"[ActionDistributor] Episode began, notified {actionReceivers.Count} receivers");
            }
        }
        
        public void CollectObservations(VectorSensor sensor) {
            // Action distributor doesn't provide observations
        }
        
        public void OnActionReceived(ActionBuffers actionBuffers) {
            if (!isInitialized) {
                Debug.LogWarning("[ActionDistributor] Not initialized, skipping action distribution");
                return;
            }
            
            if (validateActionSpace) {
                ValidateActionBuffers(actionBuffers);
            }
            
            // Extract action arrays
            float[] continuousActions = actionBuffers.ContinuousActions.Length > 0 ? 
                actionBuffers.ContinuousActions.ToArray() : new float[0];
            int[] discreteActions = actionBuffers.DiscreteActions.Length > 0 ? 
                actionBuffers.DiscreteActions.ToArray() : new int[0];
            
            // Distribute actions to all active receivers
            foreach (var receiver in actionReceivers) {
                if (!receiver.IsActive) continue;
                
                if (!receiverAllocations.TryGetValue(receiver, out ActionAllocation allocation)) {
                    Debug.LogError($"[ActionDistributor] No allocation found for receiver {receiver.ReceiverName}");
                    continue;
                }
                
                try {
                    // Distribute continuous actions
                    if (allocation.ContinuousCount > 0 && continuousActions.Length > 0) {
                        receiver.ReceiveContinuousActions(continuousActions, allocation.ContinuousStartIndex, context);
                    }
                    
                    // Distribute discrete actions
                    if (allocation.DiscreteCount > 0 && discreteActions.Length > 0) {
                        receiver.ReceiveDiscreteActions(discreteActions, allocation.DiscreteStartIndex, context);
                    }
                    
                    if (debugLogging) {
                        Debug.Log($"[ActionDistributor] Distributed actions to {receiver.ReceiverName}: {allocation}");
                    }
                } catch (System.Exception e) {
                    Debug.LogError($"[ActionDistributor] Error distributing actions to {receiver.ReceiverName}: {e.Message}");
                }
            }
        }
        
        public void Heuristic(in ActionBuffers actionsOut) {
            if (!isInitialized) {
                return;
            }
            
            // Create working arrays
            float[] continuousActions = new float[totalContinuousActions];
            int[] discreteActions = new int[totalDiscreteActions];
            
            // Let all receivers provide heuristic actions
            foreach (var receiver in actionReceivers) {
                if (!receiver.IsActive) continue;
                
                if (!receiverAllocations.TryGetValue(receiver, out ActionAllocation allocation)) {
                    continue;
                }
                
                try {
                    receiver.ProvideHeuristicActions(continuousActions, discreteActions, 
                        allocation.ContinuousStartIndex, allocation.DiscreteStartIndex, context);
                } catch (System.Exception e) {
                    Debug.LogError($"[ActionDistributor] Error getting heuristic from {receiver.ReceiverName}: {e.Message}");
                }
            }
            
            // Copy to output buffers
            var continuousActionsOut = actionsOut.ContinuousActions;
            for (int i = 0; i < continuousActions.Length && i < continuousActionsOut.Length; i++) {
                continuousActionsOut[i] = continuousActions[i];
            }
            
            var discreteActionsOut = actionsOut.DiscreteActions;
            for (int i = 0; i < discreteActions.Length && i < discreteActionsOut.Length; i++) {
                discreteActionsOut[i] = discreteActions[i];
            }
        }
        
        public void FixedUpdate() {
            if (!isInitialized) return;
            
            // Let all receivers handle fixed update
            foreach (var receiver in actionReceivers) {
                if (receiver.IsActive) {
                    try {
                        receiver.FixedUpdate(context);
                    } catch (System.Exception e) {
                        Debug.LogError($"[ActionDistributor] Error in FixedUpdate for receiver {receiver.ReceiverName}: {e.Message}");
                    }
                }
            }
        }
        
        public void OnDestroy() {
            actionReceivers?.Clear();
            receiverAllocations?.Clear();
        }
        
        public void EndEpisode() {
            // Action distributor doesn't control episode end
        }
        
        public void AddReward(float reward) {
            // Action distributor doesn't handle rewards
        }
        
        public void SetReward(float reward) {
            // Action distributor doesn't handle rewards
        }
        
        private void DiscoverAndRegisterReceivers() {
            if (autoDiscoverReceivers) {
                // Find all action receivers on this GameObject and children
                CoreActionReceiver[] foundReceivers = GetComponentsInChildren<CoreActionReceiver>();
                
                foreach (var receiver in foundReceivers) {
                    if (receiver != this && !actionReceivers.Contains(receiver)) {
                        actionReceivers.Add(receiver);
                        
                        if (debugLogging) {
                            Debug.Log($"[ActionDistributor] Discovered receiver: {receiver.ReceiverName}");
                        }
                    }
                }
            }
            
            // Register explicitly assigned receivers
            foreach (var component in explicitReceivers) {
                if (component is CoreActionReceiver receiver && !actionReceivers.Contains(receiver)) {
                    actionReceivers.Add(receiver);
                    
                    if (debugLogging) {
                        Debug.Log($"[ActionDistributor] Registered explicit receiver: {receiver.ReceiverName}");
                    }
                }
            }
        }
        
        private void SortReceiversByPriority() {
            if (sortByPriority) {
                actionReceivers = actionReceivers.OrderByDescending(r => r.Priority).ThenBy(r => r.ReceiverName).ToList();
            }
        }
        
        private void InitializeReceivers() {
            foreach (var receiver in actionReceivers) {
                try {
                    receiver.Initialize(context);
                    
                    if (!receiver.ValidateReceiver(context)) {
                        Debug.LogWarning($"[ActionDistributor] Receiver {receiver.ReceiverName} failed validation");
                    }
                } catch (System.Exception e) {
                    Debug.LogError($"[ActionDistributor] Error initializing receiver {receiver.ReceiverName}: {e.Message}");
                }
            }
        }
        
        private void CalculateActionSpace() {
            totalContinuousActions = 0;
            totalDiscreteActions = 0;
            receiverAllocations.Clear();
            
            foreach (var receiver in actionReceivers) {
                if (!receiver.IsActive) continue;
                
                var allocation = new ActionAllocation {
                    ContinuousStartIndex = totalContinuousActions,
                    ContinuousCount = receiver.ContinuousActionCount,
                    DiscreteStartIndex = totalDiscreteActions,
                    DiscreteCount = receiver.DiscreteActionBranchCount,
                    DiscreteBranchSizes = receiver.DiscreteActionBranchSizes
                };
                
                receiverAllocations[receiver] = allocation;
                
                totalContinuousActions += receiver.ContinuousActionCount;
                totalDiscreteActions += receiver.DiscreteActionBranchCount;
                
                if (debugLogging) {
                    Debug.Log($"[ActionDistributor] {receiver.ReceiverName} allocated: {allocation}");
                }
            }
        }
        
        private void ValidateActionBuffers(ActionBuffers actionBuffers) {
            if (actionBuffers.ContinuousActions.Length != totalContinuousActions) {
                Debug.LogWarning($"[ActionDistributor] Continuous action count mismatch. " +
                               $"Expected: {totalContinuousActions}, Received: {actionBuffers.ContinuousActions.Length}");
            }
            
            if (actionBuffers.DiscreteActions.Length != totalDiscreteActions) {
                Debug.LogWarning($"[ActionDistributor] Discrete action count mismatch. " +
                               $"Expected: {totalDiscreteActions}, Received: {actionBuffers.DiscreteActions.Length}");
            }
        }
        
        public void RegisterReceiver(CoreActionReceiver receiver) {
            if (receiver != null && !actionReceivers.Contains(receiver)) {
                actionReceivers.Add(receiver);
                
                if (isInitialized) {
                    receiver.Initialize(context);
                    SortReceiversByPriority();
                    CalculateActionSpace();
                }
                
                if (debugLogging) {
                    Debug.Log($"[ActionDistributor] Dynamically registered receiver: {receiver.ReceiverName}");
                }
            }
        }
        
        public void UnregisterReceiver(CoreActionReceiver receiver) {
            if (actionReceivers.Remove(receiver)) {
                receiverAllocations.Remove(receiver);
                CalculateActionSpace();
                
                if (debugLogging) {
                    Debug.Log($"[ActionDistributor] Unregistered receiver: {receiver.ReceiverName}");
                }
            }
        }
        
        public T GetReceiver<T>() where T : class, CoreActionReceiver {
            foreach (var receiver in actionReceivers) {
                if (receiver is T typedReceiver) {
                    return typedReceiver;
                }
            }
            return null;
        }
        
        public List<T> GetReceivers<T>() where T : class, CoreActionReceiver {
            List<T> results = new List<T>();
            
            foreach (var receiver in actionReceivers) {
                if (receiver is T typedReceiver) {
                    results.Add(typedReceiver);
                }
            }
            
            return results;
        }
        
        public ActionAllocation GetAllocation(CoreActionReceiver receiver) {
            return receiverAllocations.TryGetValue(receiver, out ActionAllocation allocation) ? allocation : null;
        }
        
        [ContextMenu("Refresh Receivers")]
        private void RefreshReceivers() {
            if (Application.isPlaying) {
                actionReceivers.Clear();
                DiscoverAndRegisterReceivers();
                SortReceiversByPriority();
                InitializeReceivers();
                CalculateActionSpace();
                Debug.Log($"[ActionDistributor] Refreshed - found {actionReceivers.Count} receivers");
            }
        }
        
        [ContextMenu("Debug Action Space")]
        private void DebugActionSpace() {
            Debug.Log($"[ActionDistributor] Action Space Summary:");
            Debug.Log($"  Total Continuous Actions: {totalContinuousActions}");
            Debug.Log($"  Total Discrete Actions: {totalDiscreteActions}");
            Debug.Log($"[ActionDistributor] Receivers ({actionReceivers.Count}):");
            
            foreach (var receiver in actionReceivers) {
                var allocation = GetAllocation(receiver);
                Debug.Log($"  - {receiver.ReceiverName} (Priority: {receiver.Priority}, Active: {receiver.IsActive})");
                if (allocation != null) {
                    Debug.Log($"    {allocation}");
                }
            }
        }
    }
}