//* define data structure for telemetry status thresholds
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// Threshold values for telemetry parameters
namespace Thresholds {
    [Serializable]
    public class TelemetryThresholds {
        public string parameterName;       // Name of the telemetry parameter
        public float minNominal;           // Minimum value for nominal range
        public float maxNominal;           // Maximum value for nominal range
        public float minCaution;           // Minimum value for caution range
        public float maxCaution;           // Maximum value for caution range

        public enum Status { Nominal, Caution, Critical }

        public Status CheckValue(float value)
        {
            if (value >= minNominal && value <= maxNominal)
                return Status.Nominal;
            else if (value >= minCaution && value <= maxCaution)
                return Status.Caution;
            else
                return Status.Critical;
        }
    }
}