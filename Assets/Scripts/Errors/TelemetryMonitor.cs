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
        // First ensure all thresholds are loaded
        InitializeThresholds();
        
        // THEN initialize status tracking for each astronaut
        alerts = new Dictionary<string, Dictionary<string, TelemetryAlert>>();
        alerts["EVA1"] = new Dictionary<string, TelemetryAlert>();
        alerts["EVA2"] = new Dictionary<string, TelemetryAlert>();

        // initialize default status (nominal) for each parameter
        foreach (var threshold in thresholds) {
            alerts["EVA1"][threshold.parameterName] = null;
            alerts["EVA2"][threshold.parameterName] = null;
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
        
        // Check LTV critical data
        var ltvCriticalData = WebSocketClient.LatestLtvCriticalData;
        if (ltvCriticalData != null && ltvCriticalData.alerts != null)
        {
            foreach (var kvp in ltvCriticalData.alerts)
            {
                string alertName = kvp.Key;
                int alertValue = kvp.Value;
                string parameterName = $"ltv_{alertName.ToLower()}";
                
                // Create alert for LTV critical data
                TelemetryAlert alert = new TelemetryAlert
                {
                    astronautId = "LTV",
                    parameterName = parameterName,
                    value = alertValue,
                    status = alertValue == 1 ? TelemetryThresholds.Status.Critical : TelemetryThresholds.Status.Nominal,
                    message = GetLtvAlertMessage(alertName, alertValue),
                    timestamp = DateTime.Now
                };

                // Fire appropriate event based on status
                if (alertValue == 1)
                {
                    onCriticalDetected.Invoke(alert);
                }
                else
                {
                    onReturnToNominal.Invoke(alert);
                }
            }
        }
        
        // Process high frequency data (DCU data)
        if (highFreqData != null && highFreqData.data != null)
        {
            // Process EVA1 telemetry
            if (highFreqData.data.TryGetValue("eva1_batt", out float battery))
                CheckParameter("EVA1", "batt_time_left", battery);
            
            if (highFreqData.data.TryGetValue("eva1_oxy", out float oxygen))
                CheckParameter("EVA1", "oxy_pri_storage", oxygen);
            
            // if (highFreqData.data.TryGetValue("eva1_co2", out float co2))
            //     CheckParameter("EVA1", "helmet_pressure_co2", co2);
            
            // Check fan value - maps to an error state if it's 0
            if (highFreqData.data.TryGetValue("eva1_fan", out float fan))
            {
                CheckParameter("EVA1", "fan_pri_rpm", fan * 30000); // Convert 0-1 to RPM
                bool fanErrorState = fan < 0.5f; // Consider it an error if below 0.5
                ProcessErrorState("EVA1", "fan_error", fanErrorState);
            }
            
            // Check pump value - maps to an error state if it's 0
            if (highFreqData.data.TryGetValue("eva1_pump", out float pump))
            {
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
            
            // Check O2 consumption
            CheckParameter("EVA1", "oxy_consumption", eva1Bio.o2Consumption);
            
            // Check CO2 production
            CheckParameter("EVA1", "co2_production", eva1Bio.co2Production);
            
            // Check suit pressure
            if (eva1Bio.suitPressureTotal > 0)
                CheckParameter("EVA1", "suit_pressure_total", eva1Bio.suitPressureTotal);
            
            // Check helmet CO2
            // if (eva1Bio.helmetCO2 > 0)
            //     CheckParameter("EVA1", "helmet_pressure_co2", eva1Bio.helmetCO2);
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
    // ex: "Checking parameter EVA2 suit_pressure_total: 3.9"
    private void CheckParameter(string astronautId, string paramName, float value) {
        if (debugMode)
            Debug.Log($"Checking parameter {astronautId} {paramName}: {value}");
        
        // Find the threshold for this parameter
        TelemetryThresholds threshold = thresholds.Find(t => t.parameterName == paramName);
        
        // Check if a matching threshold was found
        if (threshold == null) {
            Debug.LogWarning($"No threshold defined for parameter: {paramName}");
            
            // Add debug info to help identify the issue
            if (thresholds.Count == 0) {
                Debug.LogError("Thresholds list is empty! Please populate it in the Inspector.");
            }
            return;
        }
        
        // Continue with normal processing
        TelemetryThresholds.Status status = threshold.CheckValue(value);
        
        if (debugMode) {
            Debug.Log($"Threshold values for {paramName}: min={threshold.minCritical}/{threshold.minNominal}, max={threshold.maxNominal}/{threshold.maxCritical}");
            Debug.Log($"The status of parameter {astronautId} {paramName} is {status}");
        }
        
        // Get the previous status if it exists
        TelemetryThresholds.Status previousStatus = TelemetryThresholds.Status.Nominal;
        if (alerts[astronautId].ContainsKey(paramName) && alerts[astronautId][paramName] != null) {
            previousStatus = alerts[astronautId][paramName].status;
        }
        
        // Create a new telemetry alert
        TelemetryAlert alert = new TelemetryAlert {
            astronautId = astronautId,
            parameterName = paramName,
            value = value,
            status = status,
            message = GetAlertMessage(astronautId, paramName, value, status),
            timestamp = DateTime.Now
        };
            
        // Store the alert for future reference
        alerts[astronautId][paramName] = alert;
        
        // Fire the appropriate event based on status
        switch (status) {
            case TelemetryThresholds.Status.Nominal:
                if (previousStatus != TelemetryThresholds.Status.Nominal)
                    onReturnToNominal.Invoke(alert);
                break;
            case TelemetryThresholds.Status.Caution:
                if (previousStatus != TelemetryThresholds.Status.Caution)
                    onCautionDetected.Invoke(alert);
                break;
            case TelemetryThresholds.Status.Critical:
                if (previousStatus != TelemetryThresholds.Status.Critical)
                    onCriticalDetected.Invoke(alert);
                break;
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
    private string FormatParameter(string paramName, float value)
    {
        switch (paramName)
        {
            case "batt_time_left":
            case "oxy_time_left":
                return $"{value:F0} minutes";
            
            case "oxy_pri_storage":
            case "oxy_sec_storage":
            case "coolant_storage":
            case "scrubber_a_co2_storage":
            case "scrubber_b_co2_storage":
                return $"{value:F1}%";
            
            case "oxy_pri_pressure":
            case "oxy_sec_pressure":
            case "suit_pressure_oxy":
            case "suit_pressure_co2":
            case "suit_pressure_other":
            case "suit_pressure_total":
            case "coolant_liquid_pressure":
            case "coolant_gas_pressure":
                return $"{value:F1} psi";
            
            case "heart_rate":
                return $"{value:F0} BPM";
            
            case "oxy_consumption":
            case "co2_production":
                return $"{value:F2} psi/min";
            
            case "fan_pri_rpm":
            case "fan_sec_rpm":
                return $"{value:F0} RPM";
            
            case "temperature":
                return $"{value:F1}°F";
            
            default:
                return $"{value:F1}";
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

    private string GetAlertMessage(string astronautId, string paramName, float value, TelemetryThresholds.Status status)
    {
        // Convert value to display units
        float displayValue = ConvertToDisplayUnits(paramName, value);
        
        // Format parameter with appropriate units
        string valueWithUnits = FormatParameter(paramName, displayValue);
        
        if (status == TelemetryThresholds.Status.Nominal)
        {
            return $"RESOLVED: {astronautId} {paramName.Replace("_", " ")} returned to nominal ({valueWithUnits})";
        }
        
        switch (paramName)
        {
            case "heart_rate":
                return status == TelemetryThresholds.Status.Critical ? 
                    $"CRITICAL: {astronautId} heart rate at dangerous level ({valueWithUnits})" :
                    $"CAUTION: {astronautId} heart rate outside nominal range ({valueWithUnits})";
                
            case "temperature":
                return status == TelemetryThresholds.Status.Critical ? 
                    $"CRITICAL: {astronautId} body temperature at dangerous level ({valueWithUnits})" :
                    $"CAUTION: {astronautId} body temperature outside nominal range ({valueWithUnits})";
                
            case "oxy_pri_storage":
            case "oxy_sec_storage":
                return status == TelemetryThresholds.Status.Critical ? 
                    $"CRITICAL: {astronautId} oxygen storage critically low ({valueWithUnits})" :
                    $"CAUTION: {astronautId} oxygen storage below nominal ({valueWithUnits})";
                
            case "batt_time_left":
                return status == TelemetryThresholds.Status.Critical ? 
                    $"CRITICAL: {astronautId} battery time critically low ({valueWithUnits})" :
                    $"CAUTION: {astronautId} battery time below nominal ({valueWithUnits})";
                
            case "suit_pressure_total":
                return status == TelemetryThresholds.Status.Critical ? 
                    $"CRITICAL: {astronautId} suit pressure at dangerous level ({valueWithUnits})" :
                    $"CAUTION: {astronautId} suit pressure outside nominal range ({valueWithUnits})";
                
            default:
                return status == TelemetryThresholds.Status.Critical ? 
                    $"CRITICAL: {astronautId} {paramName.Replace("_", " ")} at dangerous level ({valueWithUnits})" :
                    $"CAUTION: {astronautId} {paramName.Replace("_", " ")} outside nominal range ({valueWithUnits})";
        }
    }

    private float ConvertToDisplayUnits(string paramName, float value)
    {
        switch (paramName)
        {
            case "temperature":
                // Convert from Celsius to Fahrenheit for display
                return (value * 9/5) + 32;
            
            case "batt_time_left":
            case "oxy_time_left":
                // Convert seconds to minutes for display
                return value / 60;
            
            default:
                return value;
        }
    }

    private void InitializeThresholds() {
        // Clear existing thresholds to avoid duplicates
        thresholds.Clear();
        Debug.Log("Initializing default telemetry thresholds");
        
        // Add all the required thresholds
        thresholds.Add(TelemetryThresholds.BatteryTimeLeft());
        thresholds.Add(TelemetryThresholds.OxygenPrimaryStorage());
        thresholds.Add(TelemetryThresholds.OxygenSecondaryStorage());
        thresholds.Add(TelemetryThresholds.OxygenPrimaryPressure());
        thresholds.Add(TelemetryThresholds.OxygenSecondaryPressure());
        thresholds.Add(TelemetryThresholds.OxygenTimeLeft());
        thresholds.Add(TelemetryThresholds.CoolantStorage());
        thresholds.Add(TelemetryThresholds.HeartRate());
        thresholds.Add(TelemetryThresholds.OxygenConsumption());
        thresholds.Add(TelemetryThresholds.CO2Production());
        thresholds.Add(TelemetryThresholds.SuitPressureOxy());
        thresholds.Add(TelemetryThresholds.SuitPressureCO2());
        thresholds.Add(TelemetryThresholds.SuitPressureOther());
        thresholds.Add(TelemetryThresholds.SuitPressureTotal());
        thresholds.Add(TelemetryThresholds.FanPrimaryRPM());
        thresholds.Add(TelemetryThresholds.FanSecondaryRPM());
        thresholds.Add(TelemetryThresholds.ScrubberACO2Storage());
        thresholds.Add(TelemetryThresholds.ScrubberBCO2Storage());
        thresholds.Add(TelemetryThresholds.Temperature());
        thresholds.Add(TelemetryThresholds.CoolantLiquidPressure());
        thresholds.Add(TelemetryThresholds.CoolantGasPressure());
        
        // Add LTV thresholds
        thresholds.Add(TelemetryThresholds.LtvBattery());
        thresholds.Add(TelemetryThresholds.LtvCO2());
        thresholds.Add(TelemetryThresholds.LtvCoolant());
        thresholds.Add(TelemetryThresholds.LtvOxygen());
        thresholds.Add(TelemetryThresholds.LtvTemperature());
        
        // Log all available thresholds for debugging
        Debug.Log($"Total thresholds initialized: {thresholds.Count}");
        foreach (var t in thresholds) {
            Debug.Log($"Loaded threshold: {t.parameterName}");
        }
    }

    private string GetLtvAlertMessage(string alertName, int value)
    {
        string baseMessage;
        switch (alertName.ToLower())
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
                baseMessage = $"LTV {alertName.ToUpper()} System";
                break;
        }

        if (value == 1)
        {
            return $"CRITICAL: {baseMessage} is critical!";
        }
        else
        {
            return $"RESOLVED: {baseMessage} is now nominal.";
        }
    }
}
