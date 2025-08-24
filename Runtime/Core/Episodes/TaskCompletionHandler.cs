using TCS.MLAgents.Core;
using TCS.MLAgents.Interfaces;

namespace TCS.MLAgents.Episodes {
    /// <summary>
    /// Episode handler that monitors task completion status and manages episode lifecycle based on task outcomes.
    /// Works in conjunction with TaskCompletionRewardProvider.
    /// </summary>
    [Serializable]
    public class TaskCompletionHandler : EpisodeHandlerBase {
        [Header("Task Completion Settings")]
        [SerializeField] private bool endOnTaskSuccess = true;
        [SerializeField] private bool endOnTaskFailure = true;
        [SerializeField] private bool autoStartNewEpisode = true;
        [SerializeField] private float restartDelay = 0.5f;
        
        [Header("Success Conditions")]
        [SerializeField] private List<string> successDataKeys = new List<string> { "TaskCompleted" };
        [SerializeField] private bool requireAllSuccessConditions = false;
        
        [Header("Failure Conditions")]
        [SerializeField] private List<string> failureDataKeys = new List<string> { "TaskFailed" };
        [SerializeField] private bool requireAllFailureConditions = false;
        
        [Header("Task Monitoring")]
        [SerializeField] private bool trackTaskProgress = true;
        [SerializeField] private string progressDataKey = "TaskProgress";
        [SerializeField] private float minProgressForSuccess = 1f;
        
        private bool taskCompleted = false;
        private bool taskFailed = false;
        private float lastProgressValue = 0f;
        private Dictionary<string, bool> conditionStates = new Dictionary<string, bool>();
        
        protected override void OnInitialize(AgentContext context) {
            ResetTaskState();
            InitializeConditionStates();
        }
        
        public override bool ShouldStartEpisode(AgentContext context) {
            // Task handler can auto-start episodes if configured
            return autoStartNewEpisode && !context.IsEpisodeActive;
        }
        
        public override bool ShouldEndEpisode(AgentContext context) {
            if (!isActive) return false;
            
            UpdateTaskStates(context);
            
            // Check for task completion
            if (endOnTaskSuccess && IsTaskSuccessful(context)) {
                return true;
            }
            
            // Check for task failure
            if (endOnTaskFailure && IsTaskFailed(context)) {
                return true;
            }
            
            return false;
        }
        
        protected override void OnEpisodeStart(AgentContext context) {
            ResetTaskState();
            
            // Clear any previous completion/failure flags
            foreach (string key in successDataKeys) {
                context.SetSharedData(key, false);
            }
            foreach (string key in failureDataKeys) {
                context.SetSharedData(key, false);
            }
            
            if (trackTaskProgress) {
                context.SetSharedData(progressDataKey, 0f);
            }
        }
        
        protected override void OnEpisodeComplete(AgentContext context, EpisodeEndReason reason) {
            if (reason == EpisodeEndReason.Success) {
                Debug.Log($"[{HandlerName}] Task completed successfully! Progress: {lastProgressValue:P1}");
            } else if (reason == EpisodeEndReason.Failure) {
                Debug.Log($"[{HandlerName}] Task failed. Progress: {lastProgressValue:P1}");
            }
        }
        
        protected override void OnUpdate(AgentContext context, float deltaTime) {
            if (!isActive) return;
            
            UpdateTaskStates(context);
            UpdateProgressTracking(context);
            
            // Set shared data for other systems
            context.SetSharedData("TaskCompletionHandlerActive", true);
            context.SetSharedData("TaskCompleted", taskCompleted);
            context.SetSharedData("TaskFailed", taskFailed);
        }
        
        private void UpdateTaskStates(AgentContext context) {
            // Update success state
            if (!taskCompleted) {
                taskCompleted = CheckSuccessConditions(context);
                if (taskCompleted) {
                    context.SetSharedData("TaskCompleted", true);
                }
            }
            
            // Update failure state
            if (!taskFailed) {
                taskFailed = CheckFailureConditions(context);
                if (taskFailed) {
                    context.SetSharedData("TaskFailed", true);
                }
            }
        }
        
        private bool CheckSuccessConditions(AgentContext context) {
            var results = new List<bool>();
            
            foreach (string key in successDataKeys) {
                bool conditionMet = context.GetSharedData<bool>(key, false);
                results.Add(conditionMet);
                conditionStates[key] = conditionMet;
            }
            
            // Check progress-based success
            if (trackTaskProgress) {
                float progress = context.GetSharedData<float>(progressDataKey, 0f);
                bool progressMet = progress >= minProgressForSuccess;
                results.Add(progressMet);
                conditionStates[progressDataKey] = progressMet;
            }
            
            // Return based on requirement mode
            return requireAllSuccessConditions ? results.TrueForAll(r => r) : results.Exists(r => r);
        }
        
        private bool CheckFailureConditions(AgentContext context) {
            var results = new List<bool>();
            
            foreach (string key in failureDataKeys) {
                bool conditionMet = context.GetSharedData<bool>(key, false);
                results.Add(conditionMet);
                conditionStates[key] = conditionMet;
            }
            
            // Return based on requirement mode
            return requireAllFailureConditions ? results.TrueForAll(r => r) : results.Exists(r => r);
        }
        
        private void UpdateProgressTracking(AgentContext context) {
            if (!trackTaskProgress) return;
            
            float currentProgress = context.GetSharedData<float>(progressDataKey, 0f);
            lastProgressValue = currentProgress;
        }
        
        private bool IsTaskSuccessful(AgentContext context) {
            return taskCompleted || CheckSuccessConditions(context);
        }
        
        private bool IsTaskFailed(AgentContext context) {
            return taskFailed || CheckFailureConditions(context);
        }
        
        private void ResetTaskState() {
            taskCompleted = false;
            taskFailed = false;
            lastProgressValue = 0f;
        }
        
        private void InitializeConditionStates() {
            conditionStates.Clear();
            
            foreach (string key in successDataKeys) {
                conditionStates[key] = false;
            }
            
            foreach (string key in failureDataKeys) {
                conditionStates[key] = false;
            }
            
            if (trackTaskProgress) {
                conditionStates[progressDataKey] = false;
            }
        }
        
        protected override void OnReset() {
            ResetTaskState();
            InitializeConditionStates();
        }
        
        public override string GetDebugInfo() {
            return base.GetDebugInfo() + 
                   $", Success={taskCompleted}, Failed={taskFailed}, Progress={lastProgressValue:P1}";
        }
        
        // Public methods for runtime configuration
        public void SetEndOnSuccess(bool endOnSuccess) {
            endOnTaskSuccess = endOnSuccess;
        }
        
        public void SetEndOnFailure(bool endOnFailure) {
            endOnTaskFailure = endOnFailure;
        }
        
        public void SetAutoStartNewEpisode(bool autoStart) {
            autoStartNewEpisode = autoStart;
        }
        
        public void SetRestartDelay(float delay) {
            restartDelay = Mathf.Max(0f, delay);
        }
        
        public void AddSuccessCondition(string dataKey) {
            if (!successDataKeys.Contains(dataKey)) {
                successDataKeys.Add(dataKey);
                conditionStates[dataKey] = false;
            }
        }
        
        public void RemoveSuccessCondition(string dataKey) {
            successDataKeys.Remove(dataKey);
            conditionStates.Remove(dataKey);
        }
        
        public void AddFailureCondition(string dataKey) {
            if (!failureDataKeys.Contains(dataKey)) {
                failureDataKeys.Add(dataKey);
                conditionStates[dataKey] = false;
            }
        }
        
        public void RemoveFailureCondition(string dataKey) {
            failureDataKeys.Remove(dataKey);
            conditionStates.Remove(dataKey);
        }
        
        public void SetMinProgressForSuccess(float minProgress) {
            minProgressForSuccess = Mathf.Clamp01(minProgress);
        }
        
        // Utility methods
        public bool IsTaskCompleted() {
            return taskCompleted;
        }
        
        public bool IsTaskFailed() {
            return taskFailed;
        }
        
        public float GetCurrentProgress() {
            return lastProgressValue;
        }
        
        public bool GetConditionState(string dataKey) {
            return conditionStates.GetValueOrDefault(dataKey, false);
        }
        
        public Dictionary<string, bool> GetAllConditionStates() {
            return new Dictionary<string, bool>(conditionStates);
        }
        
        public void ForceTaskSuccess() {
            taskCompleted = true;
            if (agentContext != null) {
                agentContext.SetSharedData("TaskCompleted", true);
            }
        }
        
        public void ForceTaskFailure() {
            taskFailed = true;
            if (agentContext != null) {
                agentContext.SetSharedData("TaskFailed", true);
            }
        }
    }
}