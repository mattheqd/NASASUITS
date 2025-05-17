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

    //----------- Telemetry processing -----------
    public void UpdateTelemetry(HighFrequencyData highFreqData, LowFrequencyData lowFreqData) {
        this.highFreqData = highFreqData;
        this.lowFreqData = lowFreqData;
    }

    public void ProcessTelemetry() {
        if (highFreqData == null && lowFreqData == null) return;

        // High frequency data
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

        // low frequency data
        if (lowFreqData != null) {
            // Critical biometric parameters
            CheckParameter("EVA1", "heart_rate", lowFreqData.eva1_heart_rate);
            CheckParameter("EVA1", "temperature", lowFreqData.eva1_temperature);
            CheckParameter("EVA2", "heart_rate", lowFreqData.eva2_heart_rate);
            CheckParameter("EVA2", "temperature", lowFreqData.eva2_temperature);

            // get error states of fan, o2, and pump from the tss CapCom
            try {
                fanError = lowFreqData.fan_error;
                o2Error = lowFreqData.o2_error;
                pumpError = lowFreqData.pump_error;
            } catch (Exception e) {
                Debug.LogError("Error getting error states from tss CapCom: " + e.Message);
            }
            // based on the tss data structure
            if (fanError == "true")
                ProcessErrorState("EVA", "fan_error", true);
            if (o2Error == "true")
                ProcessErrorState("EVA", "o2_error", true);
            if (pumpError == "true")
                ProcessErrorState("EVA", "pump_error", true);
        }
    }
}