//* Monitors when telemetry values reach an off-nominal threshold
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Events;
using Thresholds;
// using TelemetryData; // from websocket client
public class TelemetryMonitor : MonoBehaviour
{
    // ------- Variables -------
    // creates a new alert when a telemetry value is off-nominal
    [Serializable]
    public class TelemetryAlert {
        public string astronautId; // EV1 or EV2
        public string parameterName; // Name of the telemetry parameter
        public float value; // Value of the telemetry parameter
        public TelemetryThresholds.Status status; // nominal, caution, critical
        public string message; // Message to display to the user
        public DateTime timestamp; // Time the alert was created    
    }

    // events systems can subscribe to to be notified when a telemetry value falls under that range
    // create a new unity event that is notified when a telemetry value falls under the caution range
    // creates a list of all alerts which is processed by the telemetry monitor
    public UnityEvent<TelemetryAlert> onCautionDetected = new UnityEvent<TelemetryAlert>();
    public UnityEvent<TelemetryAlert> onCriticalDetected = new UnityEvent<TelemetryAlert>();
    public UnityEvent<TelemetryAlert> onReturnToNominal = new UnityEvent<TelemetryAlert>();

    // creates a list of all thresholds which is compared to the telemetry values ( based on TelemetryThreshold)    
    [SerializeField] 
    private List<TelemetryThresholds> thresholds = new List<TelemetryThresholds>();

    // dictionary tracks current alert status for each parameter
    // alerts stored in a dict with parameter name as key and alert as the value
    // the alert will persist on the screen until the status reaches "nominal" by which "onReturnToNominal" is called
    private Dictionary<string, Dictionary<string, TelemetryAlert>> alerts = new Dictionary<string, Dictionary<string, TelemetryAlert>>();

    // Current telemetry data for each astronaut
    private HighFrequencyData highFreqData; // High frequency data
    private LowFrequencyData lowFreqData;   // Low frequency data
    private ErrorData errorData; // Error data (pump, o2, fan)

    // error state tracking
    private float fanError = 0;
    private float o2Error = 0;
    private float pumpError = 0;

    //* --------- FUNCTIONS ---------
    //------- Initialization -------
    private void Awake() {
        // initialize status tracking for each astronaut
        // A dictionary stores the status (TelemetryThreshold.Status) of each parameter (string)
        alerts["EVA1"] = new Dictionary<string, TelemetryAlert>();
        alerts["EVA2"] = new Dictionary<string, TelemetryAlert>();

        // initialize default status (nominal) for each parameter
        // changes value based on telemetrythresholds.cs
        foreach (var threshold in thresholds) {
            alerts["EVA1"][threshold.parameterName] = new TelemetryAlert();
            alerts["EVA2"][threshold.parameterName] = new TelemetryAlert();
        }
    }

    //----------- Main Telemetry processing functions -----------
    public void UpdateTelemetry()
    {
        Debug.Log("TelemetryMonitor: Checking for telemetry updates");
        
        // Directly access WebSocketClient data
        HighFrequencyData highFreqData = WebSocketClient.LatestHighFrequencyData;
        LowFrequencyData lowFreqData = WebSocketClient.LatestLowFrequencyData;
        ErrorData errorData = WebSocketClient.LatestErrorData;
        BiometricsData eva1Bio = WebSocketClient.LatestEva1BiometricsData;
        BiometricsData eva2Bio = WebSocketClient.LatestEva2BiometricsData;

        // Log the data we found for debugging
        if (highFreqData != null) 
            Debug.Log("Processing high frequency data from WebSocketClient");
        if (lowFreqData != null)
            Debug.Log("Processing low frequency data from WebSocketClient");
        if (errorData != null)
            Debug.Log("Processing error data from WebSocketClient");
        if (eva1Bio != null)
            Debug.Log("Processing EVA1 biometrics data from WebSocketClient");
        
        // Process high frequency data (DCU data)
        if (highFreqData != null && highFreqData.data != null)
        {
            // Process EVA1 telemetry
            if (highFreqData.data.TryGetValue("eva1_batt", out float battery))
                CheckParameter("EVA1", "battery", battery);
            
            if (highFreqData.data.TryGetValue("eva1_oxy", out float oxygen))
                CheckParameter("EVA1", "oxygen", oxygen);
            
            if (highFreqData.data.TryGetValue("eva1_co2", out float co2))
                CheckParameter("EVA1", "co2", co2);
            
            // Check fan value - maps to an error state if it's 0
            if (highFreqData.data.TryGetValue("eva1_fan", out float fan))
            {
                CheckParameter("EVA1", "fan", fan);
                bool fanErrorState = fan < 0.5f; // Consider it an error if below 0.5
                ProcessErrorState("EVA1", "fan_error", fanErrorState);
            }
            
            // Check pump value - maps to an error state if it's 0
            if (highFreqData.data.TryGetValue("eva1_pump", out float pump))
            {
                CheckParameter("EVA1", "pump", pump);
                bool pumpErrorState = pump < 0.5f; // Consider it an error if below 0.5
                ProcessErrorState("EVA1", "pump_error", pumpErrorState);
            }
        }
        
        // Process biometrics data directly from WebSocketClient
        if (eva1Bio != null)
        {
            // Check heart rate
            CheckParameter("EVA1", "heart_rate", eva1Bio.heartRate);
            
            // Check temperature
            CheckParameter("EVA1", "temperature", eva1Bio.temperature);
            
            // Check suit pressure if available
            if (eva1Bio.suitPressureTotal > 0)
                CheckParameter("EVA1", "suit_pressure", eva1Bio.suitPressureTotal);
        }
        
        // Process error data if available
        if (errorData != null)
        {
            ProcessErrorState("EVA1", "fan_error", errorData.eva1_fan_error);
            ProcessErrorState("EVA1", "o2_error", errorData.eva1_o2_error);
            ProcessErrorState("EVA1", "pump_error", errorData.eva1_pump_error);
        }
        
        // Process low frequency data
        if (lowFreqData != null)
        {
            // Process additional low frequency data
            // Example: CheckParameter("EVA1", "radiation", lowFreqData.radiation);
        }
    }

    //----------- Helper functions -----------
    // checks if a parameter (paramName) value (value) of an astronaut (astronautId) is off-nominal
    private void CheckParameter(string astronautId, string paramName, float value) {
        if (debugMode)
            Debug.Log($"Checking parameter {astronautId} {paramName}: {value}");
        
        // find the current threshold ranges for the parameter based on the name
        // ex: (oxygen errors: 0 < oxygen < 100)
        TelemetryThresholds threshold = thresholds.Find(t => t.parameterName == paramName);
        if (threshold == null) {
            if (debugMode)
                Debug.LogWarning($"No threshold defined for parameter: {paramName}");
            return;
        }

        // get the prev alert status
        // this will be used to determine if the alert should be updated (i.e. if its the same or different)
        // if it is different, then update it
        // set to nominal automatically if not found;
        TelemetryThresholds.Status prevStatus = TelemetryThresholds.Status.Nominal;
        string alertKey = $"{astronautId}_{paramName}"; // ex: eva1_o2
        if (alerts[astronautId].ContainsKey(paramName)) {
            TelemetryAlert prevAlert = alerts[astronautId][paramName];
            if (prevAlert != null) prevStatus = prevAlert.status; 
        }

        // convert float to bool
        bool errorState = value > 0 ? true : false;

        // call the checker which sees if the telemetry value is within nominal range
        TelemetryThresholds.Status newStatus = threshold.CheckValue(value);

        // if the status changes, trigger events to store the new alert data, store the alert, 
        if (newStatus != prevStatus) {
            // create a new alert data struct
            TelemetryAlert alert = new TelemetryAlert {
                astronautId = astronautId,
                parameterName = paramName,
                value = value,
                status = newStatus,
                message = GetErrorMessage(astronautId, paramName, errorState),
                timestamp = DateTime.Now
            };

            // store the alert into the dictionary
            alerts[astronautId][paramName] = alert;

            // trigger event based on new status
            switch (newStatus) {
                case TelemetryThresholds.Status.Nominal:
                    onReturnToNominal.Invoke(alert);
                    break;
                    
                case TelemetryThresholds.Status.Caution:
                    onCautionDetected.Invoke(alert);
                    break;
                    
                case TelemetryThresholds.Status.Critical:
                    onCriticalDetected.Invoke(alert);
                    break;
            }
        }
    }

    // processes the error states for pump, o2, and fan
    private void ProcessErrorState(string astronautId, string errorName, bool isError) {
        // Default to nominal if no error
        TelemetryThresholds.Status status = isError ? 
            TelemetryThresholds.Status.Critical : 
            TelemetryThresholds.Status.Nominal;
        
        // convert float to bool
        bool errorState = isError ? true : false;

        // Get previous status if it exists
        TelemetryThresholds.Status prevStatus = TelemetryThresholds.Status.Nominal;
        
        if (alerts[astronautId].ContainsKey(errorName)) {
            TelemetryAlert prevAlert = alerts[astronautId][errorName];
            if (prevAlert != null) {
                prevStatus = prevAlert.status;
            }
        }
        
        // If status changed, trigger the appropriate events
        if (status != prevStatus) {
            // Create alert
            TelemetryAlert alert = new TelemetryAlert {
                astronautId = astronautId,
                parameterName = errorName,
                value = isError ? 1 : 0,  // Convert boolean to numeric value
                status = status,
                message = GetErrorMessage(astronautId, errorName, isError),
                timestamp = DateTime.Now
            };
            
            // Store the alert
            alerts[astronautId][errorName] = alert;
            
            // Trigger appropriate event
            if (isError) {
                onCriticalDetected.Invoke(alert);
            } else {
                onReturnToNominal.Invoke(alert);
            }
        }
    }

    //* ----- Generate UI components -----
    // human readable alert message displayed in the UI
    private string GetErrorMessage(string astronautId, string errorName, bool isError)
    {
        if (!isError) {
            return $"RESOLVED: {astronautId} {errorName.Replace("_", " ")} is now normal";
        }
        
        switch (errorName) {
            case "fan_error":
                return $"CRITICAL: {astronautId} fan system failure!";
            case "o2_error":
                return $"CRITICAL: {astronautId} oxygen system failure!";
            case "pump_error":
                return $"CRITICAL: {astronautId} water pump failure!";
            default:
                return $"CRITICAL: {astronautId} {errorName.Replace("_", " ")}";
        }
    }

    // format the parameters
    private string FormatParameter(string paramName, float value) {
        switch (paramName) {
            case "battery": return $"{value:F1}%";
            case "oxygen": return $"{value:F1}%";
            case "co2": return $"{value:F2} kPa";
            case "heart_rate": return $"{value:F0} BPM";
            case "temperature": return $"{value:F1}°C";
            default: return $"{value:F1}";
        }
    }

    [Header("Debug Settings")]
    [SerializeField] private bool debugMode = true;

    // Add this method to check if data properties exist
    private void CheckDataAvailability()
    {
        Debug.Log("WebSocketClient Static Properties Status:");
        Debug.Log($"- HighFrequencyData: {(WebSocketClient.LatestHighFrequencyData != null ? "Available" : "NULL")}");
        Debug.Log($"- LowFrequencyData: {(WebSocketClient.LatestLowFrequencyData != null ? "Available" : "NULL")}");
        Debug.Log($"- ErrorData: {(WebSocketClient.LatestErrorData != null ? "Available" : "NULL")}");
        Debug.Log($"- EVA1 Biometrics: {(WebSocketClient.LatestEva1BiometricsData != null ? "Available" : "NULL")}");
    }

    // Add this method to force test alerts for debugging
    public void ForceTestData()
    {
        Debug.Log("TelemetryMonitor: Forcing test data...");
        
        // Create and check a test heart rate alert
        CheckParameter("EVA1", "heart_rate", 125f);
        
        // Create and check a test oxygen alert
        CheckParameter("EVA1", "oxygen", 15f);
        
        // Force a critical fan error
        ProcessErrorState("EVA1", "fan_error", true);
    }
}
