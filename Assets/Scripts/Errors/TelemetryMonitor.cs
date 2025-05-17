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
    private List<Thresholds.TelemetryThresholds> thresholds = new List<Thresholds.TelemetryThresholds>();

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
    public void UpdateTelemetry(HighFrequencyData highFreqData, LowFrequencyData lowFreqData) {
        this.highFreqData = highFreqData;
        this.lowFreqData = lowFreqData;
        this.errorData = errorData;

        // process immediately after updating
        ProcessTelemetry();
    }

    public void ProcessTelemetry() {
        if (highFreqData == null && lowFreqData == null && errorData == null) return;

        // High frequency data (from dictionary)
        if (highFreqData != null && highFreqData.data != null) {
            // EVA1 Data
            if (highFreqData.data.TryGetValue("eva1_batt", out float eva1_batt))
                CheckParameter("EVA1", "battery", eva1_batt);
            
            if (highFreqData.data.TryGetValue("eva1_oxy", out float eva1_oxy))
                CheckParameter("EVA1", "oxygen", eva1_oxy);
            
            if (highFreqData.data.TryGetValue("eva1_co2", out float eva1_co2))
                CheckParameter("EVA1", "co2", eva1_co2);
        
            // EVA2 Data
            if (highFreqData.data.TryGetValue("eva2_batt", out float eva2_batt))
                CheckParameter("EVA2", "battery", eva2_batt);
            
            if (highFreqData.data.TryGetValue("eva2_oxy", out float eva2_oxy))
                CheckParameter("EVA2", "oxygen", eva2_oxy);
            
            if (highFreqData.data.TryGetValue("eva2_co2", out float eva2_co2))
                CheckParameter("EVA2", "co2", eva2_co2);
        }

        // Low frequency data (direct properties)
        if (lowFreqData != null) {
            // Only check properties that actually exist in LowFrequencyData
            // Based on WebSocketClient.cs structure
            if (highFreqData != null && highFreqData.data != null) {
                // Check if heart rate data exists in the dictionary
                if (highFreqData.data.TryGetValue("eva1_heart_rate", out float eva1_heart_rate))
                    CheckParameter("EVA1", "heart_rate", eva1_heart_rate);
                
                if (highFreqData.data.TryGetValue("eva1_temperature", out float eva1_temp))
                    CheckParameter("EVA1", "temperature", eva1_temp);
                
                if (highFreqData.data.TryGetValue("eva2_heart_rate", out float eva2_heart_rate))
                    CheckParameter("EVA2", "heart_rate", eva2_heart_rate);
                
                if (highFreqData.data.TryGetValue("eva2_temperature", out float eva2_temp))
                    CheckParameter("EVA2", "temperature", eva2_temp);
            }
        }

        // Error data (if provided)
        if (errorData != null) {
            ProcessErrorState("EVA1", "fan_error", errorData.eva1_fan_error > 0);
            ProcessErrorState("EVA1", "o2_error", errorData.eva1_o2_error > 0);  
            ProcessErrorState("EVA1", "pump_error", errorData.eva1_pump_error > 0);
        }
    }
    //----------- Helper functions -----------
    // checks if a parameter (paramName) value (value) of an astronaut (astronautId) is off-nominal
    private void CheckParameter(string astronautId, string paramName, float value) {
        // find the current threshold ranges for the parameter based on the name
        // ex: (oxygen errors: 0 < oxygen < 100)
        TelemetryThresholds threshold = thresholds.Find(t => t.parameterName == paramName);
        if (threshold == null) return;

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
                message = GetAlertMessage(astronautId, paramName, value, newStatus), 
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
    private void ProcessErrorState(string astronautId, string paramName, bool errorState) {
       
    }
}

