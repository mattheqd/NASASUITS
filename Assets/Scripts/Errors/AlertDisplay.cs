//* Display alerts on the UI
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Thresholds;
using System;

public class AlertDisplay : MonoBehaviour {
    [Header ("UI References")]
    [SerializeField] private Transform AlertContainer;
    [SerializeField] private GameObject AlertPrefab; // contains the alert message and icon
    [SerializeField] private TextMeshProUGUI AlertStatusText; // contains the alert message
    
    [Header("Test Controls")]
    [SerializeField] private bool triggerHeartRateTest = false;
    [SerializeField] private float testHeartRate = 180f; // Dangerous heart rate
    
    private TelemetryMonitor telemetryMonitor;
    private Dictionary<string, GameObject> activeAlerts = new Dictionary<string, GameObject>();
    
    // Data from last update
    private Dictionary<string, float> highFreqData = new Dictionary<string, float>();

    //* ---- Functions ----
    void Start()
    {
        // Get reference to TelemetryMonitor
        telemetryMonitor = FindObjectOfType<TelemetryMonitor>();
        if (telemetryMonitor == null)
        {
            Debug.LogError("TelemetryMonitor not found in scene!");
            return;
        }
        
        // Subscribe to telemetry events
        telemetryMonitor.onCautionDetected.AddListener(HandleCautionAlert);
        telemetryMonitor.onCriticalDetected.AddListener(HandleCriticalAlert);
        telemetryMonitor.onReturnToNominal.AddListener(HandleNominalAlert);
        
        // Initialize test data
        highFreqData["eva1_heart_rate"] = 75f;
        
        // Add initial status text
        if (statusText != null)
            statusText.text = "Telemetry Monitoring Active - All Systems Nominal";
    }
    
    void Update()
    {
        // Test trigger for heart rate error
        if (triggerHeartRateTest)
        {
            triggerHeartRateTest = false;
            SimulateHeartRateError();
        }
    }
    
    public void SimulateHeartRateError()
    {
        Debug.Log($"Simulating heart rate error: {testHeartRate} BPM");
        
        // Create data structures for heart rate test
        HighFrequencyData highFreq = new HighFrequencyData();
        highFreq.data = new Dictionary<string, float>();
        highFreq.data["eva1_heart_rate"] = testHeartRate;
        highFreq.data["eva1_batt"] = 95f;
        highFreq.data["eva1_oxy"] = 98f;
        
        // Send to telemetry monitor
        telemetryMonitor.UpdateTelemetry(highFreq, null);
    }
    
    // Handle caution alerts
    private void HandleCautionAlert(TelemetryMonitor.TelemetryAlert alert)
    {
        Debug.Log($"CAUTION: {alert.astronautId} {alert.parameterName} - {alert.value} - {alert.message}");
        CreateOrUpdateAlertUI(alert, Color.yellow);
    }
    
    // Handle critical alerts
    private void HandleCriticalAlert(TelemetryMonitor.TelemetryAlert alert)
    {
        Debug.LogError($"CRITICAL: {alert.astronautId} {alert.parameterName} - {alert.value} - {alert.message}");
        CreateOrUpdateAlertUI(alert, Color.red);
    }
    
    // Handle return to nominal
    private void HandleNominalAlert(TelemetryMonitor.TelemetryAlert alert)
    {
        Debug.Log($"NOMINAL: {alert.astronautId} {alert.parameterName} is now normal");
        
        // Remove from active alerts
        string alertKey = $"{alert.astronautId}_{alert.parameterName}";
        if (activeAlerts.TryGetValue(alertKey, out GameObject alertObj))
        {
            Destroy(alertObj);
            activeAlerts.Remove(alertKey);
        }
    }
    
    // Create or update alert UI element
    private void CreateOrUpdateAlertUI(TelemetryMonitor.TelemetryAlert alert, Color color)
    {
        string alertKey = $"{alert.astronautId}_{alert.parameterName}";
        GameObject alertObj;
        
        // Create new alert UI if it doesn't exist
        if (!activeAlerts.TryGetValue(alertKey, out alertObj))
        {
            if (alertPrefab == null || alertContainer == null)
            {
                Debug.LogError("Alert prefab or container not assigned!");
                return;
            }
            
            alertObj = Instantiate(alertPrefab, alertContainer);
            activeAlerts[alertKey] = alertObj;
        }
        
        // Update alert UI
        TMP_Text alertText = alertObj.GetComponentInChildren<TMP_Text>();
        if (alertText != null)
        {
            alertText.text = $"{alert.message}";
            alertText.color = color;
        }
        
        // Update background color
        Image bgImage = alertObj.GetComponent<Image>();
        if (bgImage != null)
        {
            Color bgColor = color;
            bgColor.a = 0.3f; // Make it semi-transparent
            bgImage.color = bgColor;
        }
        
        // Update status text
        if (statusText != null)
        {
            if (alert.status == TelemetryThresholds.Status.Critical)
            {
                statusText.text = "CRITICAL ALERT: System requires immediate attention!";
                statusText.color = Color.red;
            }
            else if (alert.status == TelemetryThresholds.Status.Caution)
            {
                statusText.text = "CAUTION: System parameters outside nominal range";
                statusText.color = Color.yellow;
            }
        }
    }
    
    // Public button function to test heart rate error
    public void TestHeartRateError()
    {
        SimulateHeartRateError();
    }
    
    // Public button function to reset heart rate to normal
    public void ResetHeartRate()
    {
        HighFrequencyData highFreq = new HighFrequencyData();
        highFreq.data = new Dictionary<string, float>();
        highFreq.data["eva1_heart_rate"] = 75f;
        highFreq.data["eva1_batt"] = 95f;
        highFreq.data["eva1_oxy"] = 98f;
        
        telemetryMonitor.UpdateTelemetry(highFreq, null);
        
        if (statusText != null)
        {
            statusText.text = "Telemetry Monitoring Active - All Systems Nominal";
            statusText.color = Color.white;
        }
    }
}