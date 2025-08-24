using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TCS.MLAgents.Utilities;
using TCS.MLAgents.Core;

namespace TCS.MLAgents.Utilities {
    /// <summary>
    /// Simple UI visualization tool for displaying statistics in real-time.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class StatisticsVisualizer : MonoBehaviour {
        [Header("Visualization Configuration")]
        [SerializeField] private StatisticsCollector m_StatisticsCollector;
        [SerializeField] private float m_UpdateInterval = 0.5f;
        [SerializeField] private int m_MaxDisplayItems = 20;
        [SerializeField] private bool m_ShowOnlyChanged = true;
        [SerializeField] private bool m_AutoScroll = true;
        
        // UI components
        [Header("UI References")]
        [SerializeField] private Text m_StatsText;
        [SerializeField] private ScrollRect m_ScrollRect;
        [SerializeField] private RectTransform m_ContentContainer;
        
        // Display data
        private float m_LastUpdate_time = 0f;
        private Dictionary<string, float> m_DisplayedStats;
        private StringBuilder m_StringBuilder;
        
        void Awake() {
            m_DisplayedStats = new Dictionary<string, float>();
            m_StringBuilder = new StringBuilder();
            
            // Create default UI if not provided
            if (m_StatsText == null) {
                SetupDefaultUI();
            }
        }
        
        void Update() {
            if (m_StatisticsCollector == null) return;
            
            // Update at specified interval
            if (Time.time - m_LastUpdate_time >= m_UpdateInterval) {
                UpdateDisplay();
                m_LastUpdate_time = Time.time;
            }
        }
        
        private void SetupDefaultUI() {
            // Create a basic canvas setup for statistics display
            var canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null) {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            
            // Create a scroll view for the statistics
            var scrollViewObj = new GameObject("ScrollView");
            scrollViewObj.transform.SetParent(transform, false);
            
            var scrollView = scrollViewObj.AddComponent<ScrollRect>();
            var scrollViewRect = scrollViewObj.AddComponent<RectTransform>();
            scrollViewRect.anchorMin = new Vector2(0, 0);
            scrollViewRect.anchorMax = new Vector2(1, 1);
            scrollViewRect.offsetMin = new Vector2(10, 10);
            scrollViewRect.offsetMax = new Vector2(-10, -10);
            
            // Create content container
            var contentObj = new GameObject("Content");
            contentObj.transform.SetParent(scrollViewObj.transform, false);
            m_ContentContainer = contentObj.AddComponent<RectTransform>();
            m_ContentContainer.anchorMin = new Vector2(0, 1);
            m_ContentContainer.anchorMax = new Vector2(1, 1);
            m_ContentContainer.pivot = new Vector2(0.5f, 1);
            m_ContentContainer.sizeDelta = new Vector2(0, 0);
            
            scrollView.content = m_ContentContainer;
            m_ScrollRect = scrollView;
            
            // Create text component
            var textObj = new GameObject("StatsText");
            textObj.transform.SetParent(contentObj.transform, false);
            m_StatsText = textObj.AddComponent<Text>();
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 1);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.pivot = new Vector2(0, 1);
            textRect.offsetMin = new Vector2(0, -1000);
            textRect.offsetMax = new Vector2(0, 0);
            
            m_StatsText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            m_StatsText.fontSize = 14;
            m_StatsText.alignment = TextAnchor.UpperLeft;
        }
        
        private void UpdateDisplay() {
            if (m_StatsText == null || m_StatisticsCollector == null) return;
            
            m_StringBuilder.Clear();
            
            // Get statistics to display
            Dictionary<string, float> statsToDisplay;
            if (m_ShowOnlyChanged) {
                statsToDisplay = m_StatisticsCollector.GetChangedStatistics();
            } else {
                statsToDisplay = m_StatisticsCollector.CurrentStatistics;
            }
            
            // Update displayed stats
            foreach (var kvp in statsToDisplay) {
                m_DisplayedStats[kvp.Key] = kvp.Value;
            }
            
            // Sort and limit display items
            var sortedStats = new List<KeyValuePair<string, float>>(m_DisplayedStats);
            sortedStats.Sort((a, b) => string.Compare(a.Key, b.Key));
            
            // Limit to max display items
            int displayCount = Mathf.Min(sortedStats.Count, m_MaxDisplayItems);
            
            // Build display string
            m_StringBuilder.AppendLine("<b>Statistics Visualizer</b>");
            m_StringBuilder.AppendLine($"Updated: {DateTime.Now:HH:mm:ss}");
            m_StringBuilder.AppendLine($"Total Stats: {m_DisplayedStats.Count}");
            m_StringBuilder.AppendLine($"Displaying: {displayCount}/{sortedStats.Count}");
            m_StringBuilder.AppendLine("------------------------");
            
            for (int i = 0; i < displayCount; i++) {
                var kvp = sortedStats[i];
                m_StringBuilder.AppendLine($"{kvp.Key}: {kvp.Value:F3}");
            }
            
            if (sortedStats.Count > displayCount) {
                m_StringBuilder.AppendLine($"... and {sortedStats.Count - displayCount} more");
            }
            
            // Update text
            m_StatsText.text = m_StringBuilder.ToString();
            
            // Update content height based on text
            UpdateContentHeight();
            
            // Auto-scroll to bottom if enabled
            if (m_AutoScroll && m_ScrollRect != null) {
                Canvas.ForceUpdateCanvases();
                m_ScrollRect.verticalNormalizedPosition = 0f;
            }
        }
        
        private void UpdateContentHeight() {
            if (m_StatsText == null || m_ContentContainer == null) return;
            
            // Calculate approximate height based on line count
            int lineCount = m_StringBuilder.ToString().Split('\n').Length;
            float lineHeight = m_StatsText.fontSize * 1.2f; // Approximate line height
            float contentHeight = lineCount * lineHeight;
            
            // Ensure minimum height
            contentHeight = Mathf.Max(contentHeight, 100);
            
            var rect = m_ContentContainer.rect;
            m_ContentContainer.sizeDelta = new Vector2(rect.width, contentHeight);
        }
        
        public void SetStatisticsCollector(StatisticsCollector collector) {
            m_StatisticsCollector = collector;
        }
        
        public void SetUpdateInterval(float interval) {
            m_UpdateInterval = Mathf.Max(0.1f, interval);
        }
        
        public void SetMaxDisplayItems(int maxItems) {
            m_MaxDisplayItems = Mathf.Max(1, maxItems);
        }
        
        public void SetShowOnlyChanged(bool showOnlyChanged) {
            m_ShowOnlyChanged = showOnlyChanged;
        }
        
        public void ClearDisplayedStats() {
            m_DisplayedStats.Clear();
            if (m_StatsText != null) {
                m_StatsText.text = string.Empty;
            }
        }
        
        public void ForceUpdate() {
            UpdateDisplay();
        }
    }
}