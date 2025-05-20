//* define data structure for telemetry status thresholds
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// Threshold values for telemetry parameters
namespace Thresholds {
    [System.Serializable]
    public class TelemetryThresholds {
        public string parameterName;
        public float minCritical = 0;
        public float minNominal = 0;
        public float maxNominal = 100;
        public float maxCritical = 100;

        public enum Status {
            Nominal,
            Caution,
            Critical
        }
        
        // Function to check if a value is within nominal/caution/critical ranges
        public Status CheckValue(float value)
        {
            if (value < minCritical || value > maxCritical)
                return Status.Critical;
            else if (value < minNominal || value > maxNominal)
                return Status.Caution;
            else
                return Status.Nominal;
        }
        
        // Static factory methods to create standard thresholds
        public static TelemetryThresholds BatteryTimeLeft()
        {
            return new TelemetryThresholds {
                parameterName = "batt_time_left",
                minCritical = 0,
                minNominal = 3600,
                maxNominal = 10800,
                maxCritical = float.MaxValue
            };
        }
        
        public static TelemetryThresholds OxygenPrimaryStorage()
        {
            return new TelemetryThresholds {
                parameterName = "oxy_pri_storage",
                minCritical = 0, 
                minNominal = 20,
                maxNominal = 100,
                maxCritical = float.MaxValue
            };
        }
        
        public static TelemetryThresholds OxygenSecondaryStorage()
        {
            return new TelemetryThresholds {
                parameterName = "oxy_sec_storage",
                minCritical = 0,
                minNominal = 20,
                maxNominal = 100,
                maxCritical = float.MaxValue
            };
        }
        
        public static TelemetryThresholds OxygenPrimaryPressure()
        {
            return new TelemetryThresholds {
                parameterName = "oxy_pri_pressure",
                minCritical = 0,
                minNominal = 600,
                maxNominal = 3000,
                maxCritical = float.MaxValue
            };
        }
        
        public static TelemetryThresholds OxygenSecondaryPressure()
        {
            return new TelemetryThresholds {
                parameterName = "oxy_sec_pressure",
                minCritical = 0,
                minNominal = 600,
                maxNominal = 3000,
                maxCritical = float.MaxValue
            };
        }
        
        public static TelemetryThresholds OxygenTimeLeft()
        {
            return new TelemetryThresholds {
                parameterName = "oxy_time_left",
                minCritical = 0,
                minNominal = 3600,
                maxNominal = 21600,
                maxCritical = float.MaxValue
            };
        }
        
        public static TelemetryThresholds CoolantStorage()
        {
            return new TelemetryThresholds {
                parameterName = "coolant_storage",
                minCritical = 0,
                minNominal = 80,
                maxNominal = 100,
                maxCritical = float.MaxValue
            };
        }
        
        public static TelemetryThresholds HeartRate()
        {
            return new TelemetryThresholds {
                parameterName = "heart_rate",
                minCritical = 0,
                minNominal = 50,
                maxNominal = 70,
                maxCritical = float.MaxValue
            };
        }
        
        public static TelemetryThresholds OxygenConsumption()
        {
            return new TelemetryThresholds {
                parameterName = "oxy_consumption",
                minCritical = 0,
                minNominal = 0.05f,
                maxNominal = 0.15f,
                maxCritical = float.MaxValue
            };
        }
        
        public static TelemetryThresholds CO2Production()
        {
            return new TelemetryThresholds {
                parameterName = "co2_production",
                minCritical = 0,
                minNominal = 0.05f,
                maxNominal = 0.15f,
                maxCritical = float.MaxValue
            };
        }
        
        public static TelemetryThresholds SuitPressureOxy()
        {
            return new TelemetryThresholds {
                parameterName = "suit_pressure_oxy",
                minCritical = 0,
                minNominal = 3.5f,
                maxNominal = 4.1f,
                maxCritical = float.MaxValue
            };
        }
        
        public static TelemetryThresholds SuitPressureCO2()
        {
            return new TelemetryThresholds {
                parameterName = "suit_pressure_co2",
                minCritical = 0,
                minNominal = 0.0f,
                maxNominal = 0.1f,
                maxCritical = float.MaxValue
            };
        }
        
        public static TelemetryThresholds SuitPressureOther()
        {
            return new TelemetryThresholds {
                parameterName = "suit_pressure_other",
                minCritical = 0,
                minNominal = 0.0f,
                maxNominal = 0.5f,
                maxCritical = float.MaxValue
            };
        }
        
        public static TelemetryThresholds SuitPressureTotal()
        {
            return new TelemetryThresholds {
                parameterName = "suit_pressure_total",
                minCritical = 0,
                minNominal = 3.5f,
                maxNominal = 4.5f,
                maxCritical = 5.0f
            };
        }
        
        // public static TelemetryThresholds HelmetPressureCO2()
        // {
        //     return new TelemetryThresholds {
        //         parameterName = "helmet_pressure_co2",
        //         minCritical = 0,
        //         minNominal = 0.0f,
        //         maxNominal = 0.15f,
        //         maxCritical = float.MaxValue
        //     };
        // }
        
        public static TelemetryThresholds FanPrimaryRPM()
        {
            return new TelemetryThresholds {
                parameterName = "fan_pri_rpm",
                minCritical = 0,
                minNominal = 20000,
                maxNominal = 30000,
                maxCritical = float.MaxValue
            };
        }
        
        public static TelemetryThresholds FanSecondaryRPM()
        {
            return new TelemetryThresholds {
                parameterName = "fan_sec_rpm",
                minCritical = 0,
                minNominal = 20000,
                maxNominal = 30000,
                maxCritical = float.MaxValue
            };
        }
        
        public static TelemetryThresholds ScrubberACO2Storage()
        {
            return new TelemetryThresholds {
                parameterName = "scrubber_a_co2_storage",
                minCritical = 0,
                minNominal = 0,
                maxNominal = 60,
                maxCritical = float.MaxValue
            };
        }
        
        public static TelemetryThresholds ScrubberBCO2Storage()
        {
            return new TelemetryThresholds {
                parameterName = "scrubber_b_co2_storage",
                minCritical = 0,
                minNominal = 0,
                maxNominal = 60,
                maxCritical = float.MaxValue
            };
        }
        
        public static TelemetryThresholds Temperature()
        {
            return new TelemetryThresholds {
                parameterName = "temperature",
                minCritical = 0,
                minNominal = 50,
                maxNominal = 90,
                maxCritical = 100
            };
        }
        
        public static TelemetryThresholds CoolantLiquidPressure()
        {
            return new TelemetryThresholds {
                parameterName = "coolant_liquid_pressure",
                minCritical = 0,
                minNominal = 100,
                maxNominal = 700,
                maxCritical = float.MaxValue
            };
        }
        
        public static TelemetryThresholds CoolantGasPressure()
        {
            return new TelemetryThresholds {
                parameterName = "coolant_gas_pressure",
                minCritical = 0,
                minNominal = 0,
                maxNominal = 700,
                maxCritical = float.MaxValue
            };
        }

        public static TelemetryThresholds LtvBattery()
        {
            return new TelemetryThresholds {
                parameterName = "ltv_battery",
                minCritical = 0,
                minNominal = 0,
                maxNominal = 0,
                maxCritical = 1
            };
        }

        public static TelemetryThresholds LtvCO2()
        {
            return new TelemetryThresholds {
                parameterName = "ltv_co2",
                minCritical = 0,
                minNominal = 0,
                maxNominal = 0,
                maxCritical = 1
            };
        }

        public static TelemetryThresholds LtvCoolant()
        {
            return new TelemetryThresholds {
                parameterName = "ltv_coolant",
                minCritical = 0,
                minNominal = 0,
                maxNominal = 0,
                maxCritical = 1
            };
        }

        public static TelemetryThresholds LtvOxygen()
        {
            return new TelemetryThresholds {
                parameterName = "ltv_oxygen",
                minCritical = 0,
                minNominal = 0,
                maxNominal = 0,
                maxCritical = 1
            };
        }

        public static TelemetryThresholds LtvTemperature()
        {
            return new TelemetryThresholds {
                parameterName = "ltv_temperature",
                minCritical = 0,
                minNominal = 0,
                maxNominal = 0,
                maxCritical = 1
            };
        }
    }
}