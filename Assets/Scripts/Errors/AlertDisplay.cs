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
    
    [Header("Data Sources")]
    [SerializeField] private TelemetryMonitor telemetryMonitor;
    
    [Header("Settings")]
    [SerializeField] private float updateInterval = 1.0f;
    
    private Dictionary<string, GameObject> activeAlerts = new Dictionary<string, GameObject>();
    private float timeSinceLastUpdate = 0f;
    private Dictionary<string, bool> ltvCriticalActive = new Dictionary<string, bool>();
    private Dictionary<string, float> ignoredAlerts = new Dictionary<string, float>();
    private float ignoreDuration = 60f; // seconds
    private Queue<TelemetryMonitor.TelemetryAlert> alertQueue = new Queue<TelemetryMonitor.TelemetryAlert>();
    private TelemetryMonitor.TelemetryAlert currentAlert = null;

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
    }
    
    void Update()
    {    
        timeSinceLastUpdate += Time.deltaTime;
        if (timeSinceLastUpdate >= updateInterval)
        {
            timeSinceLastUpdate = 0f;
            CheckTelemetry();
            CheckLtvCritical();
            ProcessAlertQueue();
        }
    }
    
    void CheckTelemetry()
    {      
        telemetryMonitor.UpdateTelemetry();
    }
    
    void CheckLtvCritical()
    {
        var ltv = WebSocketClient.LatestLtvCriticalData;
        if (ltv == null || ltv.alerts == null) return;
        foreach (var kvp in ltv.alerts)
        {
            string alertName = kvp.Key;
            int alertValue = kvp.Value;
            string errorKey = $"LTV_{alertName.ToUpper()}";
            if (alertValue == 1)
            {
                if (!ltvCriticalActive.ContainsKey(errorKey) || !ltvCriticalActive[errorKey])
                {
                    var alert = new TelemetryMonitor.TelemetryAlert
                    {
                        astronautId = "LTV",
                        parameterName = errorKey,
                        value = 1,
                        status = Thresholds.TelemetryThresholds.Status.Critical,
                        message = GetLtvCustomMessage(alertName, true),
                        timestamp = DateTime.Now
                    };
                    HandleCriticalAlert(alert);
                    ltvCriticalActive[errorKey] = true;
                }
            }
            else
            {
                if (ltvCriticalActive.ContainsKey(errorKey) && ltvCriticalActive[errorKey])
                {
                    var alert = new TelemetryMonitor.TelemetryAlert
                    {
                        astronautId = "LTV",
                        parameterName = errorKey,
                        value = 0,
                        status = Thresholds.TelemetryThresholds.Status.Nominal,
                        message = GetLtvCustomMessage(alertName, false),
                        timestamp = DateTime.Now
                    };
                    HandleNominalAlert(alert);
                    ltvCriticalActive[errorKey] = false;
                }
            }
        }
    }
    
    // Helper method to generate custom LTV alert messages
    private string GetLtvCustomMessage(string alertName, bool isCritical)
    {
        string baseMessage;
        switch (alertName.ToLower()) // Using ToLower() for case-insensitive matching
        {
            case "battery":
                baseMessage = "LTV Battery Level";
                break;
            case "co2":
                baseMessage = "LTV CO2 Scrubber";
                break;
            case "coolant":
                baseMessage = "LTV Coolant System";
                break;
            case "oxygen":
                baseMessage = "LTV Oxygen Supply";
                break;
            case "temperature":
                baseMessage = "LTV Cabin Temperature";
                break;
            default:
                baseMessage = $"LTV {alertName.ToUpper()} System"; // Fallback for unknown alerts
                break;
        }

        if (isCritical)
        {
            return $"CRITICAL: {baseMessage} is critical!";
        }
        else
        {
            return $"RESOLVED: {baseMessage} is now nominal.";
        }
    }

    void ProcessAlertQueue()
    {
        // Remove expired ignores
        var expired = new List<string>();
        foreach (var kvp in ignoredAlerts)
        {
            if (Time.time >= kvp.Value)
                expired.Add(kvp.Key);
        }
        foreach (var key in expired)
            ignoredAlerts.Remove(key);

        // If no current alert, show next in queue
        if (currentAlert == null && alertQueue.Count > 0)
        {
            var next = alertQueue.Dequeue();
            ShowAlert(next);
        }
    }

    void ShowAlert(TelemetryMonitor.TelemetryAlert alert)
    {
        string alertId = alert.astronautId + "_" + alert.parameterName;
        if (ignoredAlerts.TryGetValue(alertId, out float ignoreUntil) && Time.time < ignoreUntil)
            return;
        CreateOrUpdateAlert(alert, alert.status == Thresholds.TelemetryThresholds.Status.Critical ? Color.red : Color.yellow);
        currentAlert = alert;
    }

    void DismissAlert(string alertId)
    {
        RemoveAlert(alertId);
        ignoredAlerts[alertId] = Time.time + ignoreDuration;
        currentAlert = null;
    }
    
    //* ---- Alert Handlers ----
    void HandleCautionAlert(TelemetryMonitor.TelemetryAlert alert)
    {
        Debug.Log($"CAUTION ALERT: {alert.astronautId} {alert.parameterName}: {alert.value}");
        EnqueueAlert(alert);
    }
    
    void HandleCriticalAlert(TelemetryMonitor.TelemetryAlert alert)
    {
        Debug.Log($"CRITICAL ALERT: {alert.astronautId} {alert.parameterName}: {alert.value}");
        EnqueueAlert(alert);
    }
    
    void HandleNominalAlert(TelemetryMonitor.TelemetryAlert alert)
    {
        Debug.Log($"RESOLVED ALERT: {alert.astronautId} {alert.parameterName} returned to nominal");
        RemoveAlert(alert.astronautId + "_" + alert.parameterName);
        if (currentAlert != null && currentAlert.astronautId + "_" + currentAlert.parameterName == alert.astronautId + "_" + alert.parameterName)
        {
            currentAlert = null;
        }
    }
    
    // Create or update an alert in the UI
    void CreateOrUpdateAlert(TelemetryMonitor.TelemetryAlert alert, Color color)
    {
        string alertId = alert.astronautId + "_" + alert.parameterName;
        if (ignoredAlerts.TryGetValue(alertId, out float ignoreUntil) && Time.time < ignoreUntil)
            return;

        if (activeAlerts.TryGetValue(alertId, out GameObject alertObj))
        {
            TextMeshProUGUI AlertText = alertObj.GetComponentInChildren<TextMeshProUGUI>();
            if (AlertText != null)
            {
                AlertText.text = $"{alert.message}";
                AlertText.color = color;
            }
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
            if (AlertContainer != null && AlertPrefab != null)
            {
                GameObject newAlert = Instantiate(AlertPrefab, AlertContainer);
                activeAlerts[alertId] = newAlert;
                TextMeshProUGUI AlertText = newAlert.GetComponentInChildren<TextMeshProUGUI>();
                if (AlertText != null)
                {
                    AlertText.text = $"{alert.message}";
                    AlertText.color = color;
                }
                Image bgImage = newAlert.GetComponent<Image>();
                if (bgImage != null)
                {
                    Color bgColor = color;
                    bgColor.a = 0.3f;
                    bgImage.color = bgColor;
                }
                var button = newAlert.GetComponentInChildren<UnityEngine.UI.Button>();
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => DismissAlert(alertId));
                }
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

    void EnqueueAlert(TelemetryMonitor.TelemetryAlert alert)
    {
        string alertId = alert.astronautId + "_" + alert.parameterName;
        if (ignoredAlerts.TryGetValue(alertId, out float ignoreUntil) && Time.time < ignoreUntil)
            return;
        // If this is the only alert, show immediately
        if (currentAlert == null)
        {
            ShowAlert(alert);
        }
        else
        {
            // Otherwise, add to queue if not already present
            if (!alertQueue.Contains(alert))
                alertQueue.Enqueue(alert);
        }
    }
}