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
    
    private Dictionary<string, GameObject> activeAlerts = new Dictionary<string, GameObject>();
    private float timeSinceLastUpdate = 0f;

    //* ---- Functions ----
    void Start()
    {
        Debug.Log("AlertDisplay starting...");
        
        // Find references if not assigned
        if (telemetryMonitor == null)
            telemetryMonitor = FindObjectOfType<TelemetryMonitor>();
        
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
        telemetryMonitor.UpdateTelemetry();
    }
    
    //* ---- Alert Handlers ----
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
            TextMeshProUGUI AlertText = alertObj.GetComponentInChildren<TextMeshProUGUI>();
            if (AlertText != null)
            {
                AlertText.text = $"{alert.message}";
                AlertText.color = color;
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
                TextMeshProUGUI AlertText = newAlert.GetComponentInChildren<TextMeshProUGUI>();
                if (AlertText != null)
                {
                    AlertText.text = $"{alert.message}";
                    AlertText.color = color;
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