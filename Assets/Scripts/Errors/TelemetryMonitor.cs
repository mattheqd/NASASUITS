//* Monitors when telemetry values reach an off-nominal threshold
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class TelemetryMonitor : MonoBehaviour
{
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