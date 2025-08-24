using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using TCS.MLAgents.Core;
using TCS.MLAgents.Interfaces;

namespace TCS.MLAgents.Decision {
    /// <summary>
    /// Heuristic decision provider that allows manual or scripted control of agents.
    /// Useful for testing, demonstrations, or providing baseline behavior.
    /// </summary>
    public class HeuristicController : MonoBehaviour, IDecisionProvider {
        [Header("Controller Configuration")]
        [SerializeField] private string m_Id = "heuristic";
        [SerializeField] private int m_Priority = 100; // High priority to override ML decisions
        [SerializeField] private bool m_IsActive = false;
        [SerializeField] private ControlMode m_ControlMode = ControlMode.Keyboard;
        [SerializeField] private bool m_EnableSmoothing = true;
        [SerializeField] private float m_SmoothingFactor = 0.1f;
        
        // Control state
        private Vector2 m_TargetDirection = Vector2.zero;
        private float m_TargetSpeed = 0f;
        private bool m_ActionRequested = false;
        
        // Input mapping
        private Dictionary<KeyCode, Action> m_KeyActions;
        private Dictionary<string, float> m_AxisValues;
        
        // Agent context
        private AgentContext m_Context;
        private Transform m_AgentTransform;
        
        // Performance tracking
        private float m_TotalActionTime = 0f;
        private int m_ActionCount = 0;
        
        public enum ControlMode {
            Keyboard,       // Keyboard input (WASD/arrows)
            Mouse,          // Mouse position/targeting
            Gamepad,        // Gamepad input
            Scripted,       // Programmatic control
            External        // External input (e.g., network commands)
        }
        
        public string Id => m_Id;
        public int Priority => m_Priority;
        public bool IsActive => m_IsActive;
        public ControlMode Mode => m_ControlMode;
        public float AverageActionTime => m_ActionCount > 0 ? m_TotalActionTime / m_ActionCount : 0f;
        
        void Awake() {
            m_KeyActions = new Dictionary<KeyCode, Action>();
            m_AxisValues = new Dictionary<string, float>();
            
            SetupDefaultKeybindings();
        }
        
        private void SetupDefaultKeybindings() {
            m_KeyActions[KeyCode.W] = () => m_TargetDirection = Vector2.up;
            m_KeyActions[KeyCode.A] = () => m_TargetDirection = Vector2.left;
            m_KeyActions[KeyCode.S] = () => m_TargetDirection = Vector2.down;
            m_KeyActions[KeyCode.D] = () => m_TargetDirection = Vector2.right;
            m_KeyActions[KeyCode.UpArrow] = () => m_TargetDirection = Vector2.up;
            m_KeyActions[KeyCode.LeftArrow] = () => m_TargetDirection = Vector2.left;
            m_KeyActions[KeyCode.DownArrow] = () => m_TargetDirection = Vector2.down;
            m_KeyActions[KeyCode.RightArrow] = () => m_TargetDirection = Vector2.right;
            m_KeyActions[KeyCode.Space] = () => m_TargetSpeed = 1f;
        }
        
        public void Initialize(AgentContext context) {
            m_Context = context;
            m_AgentTransform = context?.AgentGameObject?.transform;
            
            LogDebug($"HeuristicController {m_Id} initialized");
        }
        
        public bool ShouldDecide(AgentContext context, List<ISensor> sensors) {
            // Always decide if active (high priority)
            return m_IsActive;
        }
        
        public void DecideAction(AgentContext context, List<ISensor> sensors, ActionBuffers actions) {
            if (!m_IsActive) return;
            
            float startTime = Time.realtimeSinceStartup;
            
            try {
                switch (m_ControlMode) {
                    case ControlMode.Keyboard:
                        HandleKeyboardInput(actions);
                        break;
                    case ControlMode.Mouse:
                        HandleMouseInput(actions);
                        break;
                    case ControlMode.Gamepad:
                        HandleGamepadInput(actions);
                        break;
                    case ControlMode.Scripted:
                        HandleScriptedInput(actions);
                        break;
                    case ControlMode.External:
                        HandleExternalInput(actions);
                        break;
                }
                
                // Apply smoothing if enabled
                if (m_EnableSmoothing) {
                    ApplySmoothing(actions);
                }
            }
            catch (Exception ex) {
                Debug.LogError($"HeuristicController: Error in DecideAction - {ex.Message}");
            }
            
            // Track performance
            float actionTime = Time.realtimeSinceStartup - startTime;
            m_TotalActionTime += actionTime;
            m_ActionCount++;
        }
        
        private void HandleKeyboardInput(ActionBuffers actions) {
            // Reset movement if no keys are pressed
            m_TargetDirection = Vector2.zero;
            m_TargetSpeed = 0f;
            
            // Check for key presses
            foreach (var kvp in m_KeyActions) {
                if (Input.GetKey(kvp.Key)) {
                    kvp.Value?.Invoke();
                }
            }
            
            // Normalize direction
            if (m_TargetDirection != Vector2.zero) {
                m_TargetDirection.Normalize();
                m_TargetSpeed = 1f;
            }
            
            // Map to actions
            MapMovementToActions(actions, m_TargetDirection, m_TargetSpeed);
        }
        
        private void HandleMouseInput(ActionBuffers actions) {
            if (Camera.main == null) return;
            
            // Get mouse position in world space
            Vector3 mousePos = Input.mousePosition;
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, Camera.main.nearClipPlane));
            
            // Calculate direction to mouse position
            if (m_AgentTransform != null) {
                Vector3 direction = worldPos - m_AgentTransform.position;
                m_TargetDirection = new Vector2(direction.x, direction.z).normalized;
                m_TargetSpeed = 1f;
                
                MapMovementToActions(actions, m_TargetDirection, m_TargetSpeed);
            }
        }
        
        private void HandleGamepadInput(ActionBuffers actions) {
            // Get axis values
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            
            m_TargetDirection = new Vector2(horizontal, vertical);
            if (m_TargetDirection != Vector2.zero) {
                m_TargetDirection.Normalize();
                m_TargetSpeed = m_TargetDirection.magnitude;
            } else {
                m_TargetSpeed = 0f;
            }
            
            MapMovementToActions(actions, m_TargetDirection, m_TargetSpeed);
        }
        
        private void HandleScriptedInput(ActionBuffers actions) {
            // Use the currently set target values
            MapMovementToActions(actions, m_TargetDirection, m_TargetSpeed);
        }
        
        private void HandleExternalInput(ActionBuffers actions) {
            // For external input, we would typically receive commands from another system
            // For now, just use the current target values
            MapMovementToActions(actions, m_TargetDirection, m_TargetSpeed);
        }
        
        private void MapMovementToActions(ActionBuffers actions, Vector2 direction, float speed) {
            var continuousActions = actions.ContinuousActions;
            
            // Assuming a 2D movement action space (X, Z)
            if (continuousActions.Length >= 2) {
                continuousActions[0] = direction.x * speed; // X movement
                continuousActions[1] = direction.y * speed; // Z movement
            }
            
            // If there's a 3rd action, use it for speed control
            if (continuousActions.Length >= 3) {
                continuousActions[2] = speed;
            }
        }
        
        private void ApplySmoothing(ActionBuffers actions) {
            // Apply simple exponential smoothing to continuous actions
            var continuousActions = actions.ContinuousActions;
            for (int i = 0; i < continuousActions.Length; i++) {
                continuousActions[i] = Mathf.Lerp(continuousActions[i], continuousActions[i], m_SmoothingFactor);
            }
        }
        
        public void OnEpisodeBegin(AgentContext context) {
            // Reset control state
            m_TargetDirection = Vector2.zero;
            m_TargetSpeed = 0f;
            m_ActionRequested = false;
            
            LogDebug($"HeuristicController {m_Id} episode begin");
        }
        
        public void OnEpisodeEnd(AgentContext context) {
            LogDebug($"HeuristicController {m_Id} episode end - Avg action time: {AverageActionTime:F4}s");
        }
        
        public void OnUpdate(AgentContext context, float deltaTime) {
            // Update any time-based control logic here
        }
        
        public void SetActive(bool active) {
            m_IsActive = active;
            LogDebug($"HeuristicController {m_Id} set active: {active}");
        }
        
        // Public control methods for scripted/external control
        public void SetTargetDirection(Vector2 direction) {
            m_TargetDirection = direction.normalized;
        }
        
        public void SetTargetSpeed(float speed) {
            m_TargetSpeed = Mathf.Clamp01(speed);
        }
        
        public void SetTarget(Vector2 direction, float speed) {
            m_TargetDirection = direction.normalized;
            m_TargetSpeed = Mathf.Clamp01(speed);
        }
        
        public void RequestAction() {
            m_ActionRequested = true;
        }
        
        public void SetAxisValue(string axisName, float value) {
            m_AxisValues[axisName] = value;
        }
        
        public float GetAxisValue(string axisName, float defaultValue = 0f) {
            return m_AxisValues.TryGetValue(axisName, out float value) ? value : defaultValue;
        }
        
        // Key binding methods
        public void BindKey(KeyCode key, Action action) {
            m_KeyActions[key] = action;
        }
        
        public void UnbindKey(KeyCode key) {
            m_KeyActions.Remove(key);
        }
        
        public void SetControlMode(ControlMode mode) {
            m_ControlMode = mode;
        }
        
        public string GetDebugInfo() {
            return $"HeuristicController[{m_Id}] - Active: {m_IsActive}, Mode: {m_ControlMode}, " +
                   $"Direction: {m_TargetDirection}, Speed: {m_TargetSpeed:F2}, " +
                   $"AvgTime: {AverageActionTime:F4}s";
        }
        
        private void LogDebug(string message) {
            Debug.Log($"[HeuristicController({m_Id})] {message}");
        }
    }
}