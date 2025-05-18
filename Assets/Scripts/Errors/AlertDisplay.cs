//* Display alerts on the UI
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Thresholds;
using System;

public class AlertDisplay : MonoBehaviour {
    [Header("UI References")]
    [SerializeField] private Transform AlertContainer;
    [SerializeField] private GameObject AlertPrefab; // contains the alert message and icon
    [SerializeField] private TextMeshProUGUI AlertStatusText; // contains the alert message
    
    [Header("Data Sources")]
    [SerializeField] private TelemetryMonitor telemetryMonitor;
    
    [Header("Settings")]
    [SerializeField] private float updateInterval = 1.0f;
    [SerializeField] private bool runTest = false;
    [SerializeField] private bool forceTest = false; // For testing alerts without real data
    
    private Dictionary<string, GameObject> activeAlerts = new Dictionary<string, GameObject>();
    private float timeSinceLastUpdate = 0f;

    //* ---- Functions ----
    void Start()
    {
        Debug.Log("AlertDisplay starting...");
        
        // Find references if not assigned
        if (telemetryMonitor == null)
            telemetryMonitor = FindObjectOfType<TelemetryMonitor>();
        
        if (telemetryMonitor == null) {
            Debug.LogError("TelemetryMonitor not found! Alerts will not work.");
            return;
        }
        
        // Subscribe to telemetry events
        telemetryMonitor.onCautionDetected.AddListener(HandleCautionAlert);
        telemetryMonitor.onCriticalDetected.AddListener(HandleCriticalAlert);
        telemetryMonitor.onReturnToNominal.AddListener(HandleNominalAlert);
        
        Debug.Log("Registered telemetry event listeners");
        
        // Set initial status text
        if (AlertStatusText != null)
            AlertStatusText.text = "Telemetry Monitoring Active - All Systems Nominal";
    }
    
    void Update()
    {
        // Manual test
        if (runTest)
        {
            runTest = false;
            CheckTelemetry();
        }
        
        // Force test alerts (bypass real data)
        if (forceTest)
        {
            forceTest = false;
            ForceTestAlerts();
        }
        
        // Automatic updates
        timeSinceLastUpdate += Time.deltaTime;
        if (timeSinceLastUpdate >= updateInterval)
        {
            timeSinceLastUpdate = 0f;
            CheckTelemetry();
        }
    }
    
    void CheckTelemetry()
    {
        if (telemetryMonitor == null) {
            Debug.LogError("Cannot check telemetry: TelemetryMonitor is null");
            return;
        }
        
        telemetryMonitor.UpdateTelemetry();
        Debug.Log("AlertDisplay: Called TelemetryMonitor.UpdateTelemetry()");
    }
    
    // Force test alerts for debugging
    void ForceTestAlerts()
    {
        Debug.Log("Forcing test alerts");
        
        // Create test alerts
        TelemetryMonitor.TelemetryAlert heartRateAlert = new TelemetryMonitor.TelemetryAlert {
            astronautId = "EVA1",
            parameterName = "heart_rate",
            value = 125f,
            status = TelemetryThresholds.Status.Caution,
            message = "EVA1 heart_rate: 125 BPM - above nominal range",
            timestamp = DateTime.Now
        };
        
        TelemetryMonitor.TelemetryAlert fanAlert = new TelemetryMonitor.TelemetryAlert {
            astronautId = "EVA1",
            parameterName = "fan_error",
            value = 0f,
            status = TelemetryThresholds.Status.Critical,
            message = "EVA1 fan system failure",
            timestamp = DateTime.Now
        };
        
        // Trigger alerts
        HandleCautionAlert(heartRateAlert);
        HandleCriticalAlert(fanAlert);
    }
    
    // Handle different alert types
    void HandleCautionAlert(TelemetryMonitor.TelemetryAlert alert)
    {
        Debug.Log($"CAUTION ALERT: {alert.astronautId} {alert.parameterName}: {alert.value}");
        CreateOrUpdateAlert(alert, Color.yellow);
    }
    
    void HandleCriticalAlert(TelemetryMonitor.TelemetryAlert alert)
    {
        Debug.Log($"CRITICAL ALERT: {alert.astronautId} {alert.parameterName}: {alert.value}");
        CreateOrUpdateAlert(alert, Color.red);
    }
    
    void HandleNominalAlert(TelemetryMonitor.TelemetryAlert alert)
    {
        Debug.Log($"RESOLVED ALERT: {alert.astronautId} {alert.parameterName} returned to nominal");
        RemoveAlert(alert.astronautId + "_" + alert.parameterName);
        
        // If no more alerts, set status to nominal
        if (activeAlerts.Count == 0 && AlertStatusText != null)
        {
            AlertStatusText.text = "Telemetry Monitoring Active - All Systems Nominal";
            AlertStatusText.color = Color.white;
        }
    }
    
    // Create or update an alert in the UI
    void CreateOrUpdateAlert(TelemetryMonitor.TelemetryAlert alert, Color color)
    {
        string alertId = alert.astronautId + "_" + alert.parameterName;
        
        // If alert already exists, update it
        if (activeAlerts.TryGetValue(alertId, out GameObject alertObj))
        {
            // Update existing alert
            TextMeshProUGUI alertText = alertObj.GetComponentInChildren<TextMeshProUGUI>();
            if (alertText != null)
            {
                alertText.text = $"{alert.message}";
                alertText.color = color;
            }
            
            // Update background
            Image bgImage = alertObj.GetComponent<Image>();
            if (bgImage != null)
            {
                Color bgColor = color;
                bgColor.a = 0.3f;
                bgImage.color = bgColor;
            }
        }
        else
        {
            // Create new alert if container and prefab exist
            if (AlertContainer != null && AlertPrefab != null)
            {
                GameObject newAlert = Instantiate(AlertPrefab, AlertContainer);
                activeAlerts[alertId] = newAlert;
                
                // Set alert text
                TextMeshProUGUI alertText = newAlert.GetComponentInChildren<TextMeshProUGUI>();
                if (alertText != null)
                {
                    alertText.text = $"{alert.message}";
                    alertText.color = color;
                }
                
                // Set background color
                Image bgImage = newAlert.GetComponent<Image>();
                if (bgImage != null)
                {
                    Color bgColor = color;
                    bgColor.a = 0.3f;
                    bgImage.color = bgColor;
                }
            }
        }
        
        // Update status text
        if (AlertStatusText != null)
        {
            if (alert.status == TelemetryThresholds.Status.Critical)
            {
                AlertStatusText.text = "CRITICAL ALERT: System requires immediate attention!";
                AlertStatusText.color = Color.red;
            }
            else if (alert.status == TelemetryThresholds.Status.Caution)
            {
                AlertStatusText.text = "CAUTION: System parameters outside nominal range";
                AlertStatusText.color = Color.yellow;
            }
        }
    }
    
    // Remove an alert from the UI
    void RemoveAlert(string alertId)
    {
        if (activeAlerts.TryGetValue(alertId, out GameObject alertObj))
        {
            Destroy(alertObj);
            activeAlerts.Remove(alertId);
        }
    }
}