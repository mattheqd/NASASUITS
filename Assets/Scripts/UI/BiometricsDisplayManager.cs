using UnityEngine;
using TMPro; // If you need to display EVA ID or other general info

public class BiometricsDisplayManager : MonoBehaviour
{
    [Header("EVA 1 Biometrics Gauges (Assign in Inspector)")]
    public BiometricGauge eva1_HeartRateGauge;
    public BiometricGauge eva1_TemperatureGauge;
    public BiometricGauge eva1_OxygenConsumptionGauge;
    public BiometricGauge eva1_CO2ProductionGauge;

    [Header("EVA 1 EMU Gauges (Assign in Inspector)")]
    public BiometricGauge eva1_OxygenPrimaryStorageGauge;
    public BiometricGauge eva1_OxygenSecondaryStorageGauge;
    public BiometricGauge eva1_SuitCO2PressureGauge;
    public BiometricGauge eva1_TotalSuitPressureGauge;

    [Header("EVA 2 Biometrics Gauges (Assign in Inspector)")]
    public BiometricGauge eva2_HeartRateGauge;
    public BiometricGauge eva2_TemperatureGauge;
    public BiometricGauge eva2_OxygenConsumptionGauge;
    public BiometricGauge eva2_CO2ProductionGauge;

    [Header("EVA 2 EMU Gauges (Assign in Inspector)")]
    public BiometricGauge eva2_OxygenPrimaryStorageGauge;
    public BiometricGauge eva2_OxygenSecondaryStorageGauge;
    public BiometricGauge eva2_SuitCO2PressureGauge;
    public BiometricGauge eva2_TotalSuitPressureGauge;

    [Header("EVA 1 Telemetry Texts (Assign in Inspector)")]
    public TextMeshProUGUI eva1_EvaIdText;
    public TextMeshProUGUI eva1_EvaTimeText;
    public TextMeshProUGUI eva1_O2TimeLeftText;
    public TextMeshProUGUI eva1_OxygenPrimaryPressureText;
    public TextMeshProUGUI eva1_OxygenSecondaryPressureText;
    public TextMeshProUGUI eva1_SuitPressureOxygenText;
    public TextMeshProUGUI eva1_SuitPressureOtherText;
    public TextMeshProUGUI eva1_ScrubberAPressureText;
    public TextMeshProUGUI eva1_ScrubberBPressureText;
    public TextMeshProUGUI eva1_H2OGasPressureText;
    public TextMeshProUGUI eva1_H2OLiquidPressureText;
    public TextMeshProUGUI eva1_PrimaryFanRPMText;
    public TextMeshProUGUI eva1_SecondaryFanRPMText;
    public TextMeshProUGUI eva1_HelmetCO2PressureText;
    public TextMeshProUGUI eva1_CoolantLevelText;

    [Header("EVA 2 Telemetry Texts (Assign in Inspector)")]
    public TextMeshProUGUI eva2_EvaIdText;
    public TextMeshProUGUI eva2_EvaTimeText;
    public TextMeshProUGUI eva2_O2TimeLeftText;
    public TextMeshProUGUI eva2_OxygenPrimaryPressureText;
    public TextMeshProUGUI eva2_OxygenSecondaryPressureText;
    public TextMeshProUGUI eva2_SuitPressureOxygenText;
    public TextMeshProUGUI eva2_SuitPressureOtherText;
    public TextMeshProUGUI eva2_ScrubberAPressureText;
    public TextMeshProUGUI eva2_ScrubberBPressureText;
    public TextMeshProUGUI eva2_H2OGasPressureText;
    public TextMeshProUGUI eva2_H2OLiquidPressureText;
    public TextMeshProUGUI eva2_PrimaryFanRPMText;
    public TextMeshProUGUI eva2_SecondaryFanRPMText;
    public TextMeshProUGUI eva2_HelmetCO2PressureText;
    public TextMeshProUGUI eva2_CoolantLevelText;

    [Header("LTV Gauges (Assign in Inspector)")]
    public BiometricGauge ltv_BatteryLevelGauge;
    public BiometricGauge ltv_OxygenTankGauge;
    public BiometricGauge ltv_SpeedGauge;
    public BiometricGauge ltv_CabinTemperatureGauge;
    public BiometricGauge ltv_DistanceFromBaseGauge;
    public BiometricGauge ltv_DistanceTraveledGauge;

    [Header("EVA Display Panels (Assign in Inspector)")]
    public GameObject eva1DisplayPanel;
    public GameObject eva2DisplayPanel;
    public GameObject ltvDisplayPanel;

    [Header("Icons (Assign in Inspector)")]
    public Sprite o2ConsumptionIcon;
    public Sprite co2ProductionIcon;
    public Sprite heartRateIcon;
    public Sprite temperatureIcon;
    public Sprite oxygenStorageIcon; // Used for both Primary and Secondary O2 Storage
    public Sprite suitCO2PressureIcon;
    public Sprite suitPressureTotalIcon;
    // Add other icons as needed

    // Define Min/Max scales for each biometric. These can be constants or configurable.
    // Values below are examples and should be set based on NASA SUITS specific data ranges and alert levels.
    private const float O2_CONSUMPTION_MIN = 0f;     // kg/hr or other unit
    private const float O2_CONSUMPTION_MAX = 0.15f;  // kg/hr (example)
    private const float CO2_PRODUCTION_MIN = 0f;     // kg/hr
    private const float CO2_PRODUCTION_MAX = 0.12f;  // kg/hr (example)
    private const float HEART_RATE_MIN = 40f;        // BPM
    private const float HEART_RATE_MAX = 180f;       // BPM
    private const float TEMPERATURE_MIN = 35f;       // Celsius
    private const float TEMPERATURE_MAX = 40f;       // Celsius

    // Telemetry Data Min/Max (Used for EMU Gauges)
    private const float OXYGEN_STORAGE_MIN = 0f;         // Percent or other unit
    private const float OXYGEN_STORAGE_MAX = 100f;       // Percent (example)
    private const float SUIT_CO2_PRESSURE_MIN = 0f;      // mmHg or PSI
    private const float SUIT_CO2_PRESSURE_MAX = 8f;      // mmHg (example)
    private const float TOTAL_SUIT_PRESSURE_MIN = 0f;    // PSIa
    private const float TOTAL_SUIT_PRESSURE_MAX = 16f;   // PSIa (example)

    // LTV Data Min/Max values
    private const float BATTERY_LEVEL_MIN = 0f;
    private const float BATTERY_LEVEL_MAX = 100f;
    private const float OXYGEN_TANK_MIN = 0f;
    private const float OXYGEN_TANK_MAX = 100f;
    private const float SPEED_MIN = 0f;
    private const float SPEED_MAX = 20f;  // 20 m/s max speed
    private const float CABIN_TEMP_MIN = 15f;  // 15°C minimum
    private const float CABIN_TEMP_MAX = 35f;  // 35°C maximum
    private const float DISTANCE_MIN = 0f;
    private const float DISTANCE_MAX = 1000f;  // 1000m max distance

    private float timeSinceLastUpdate = 0f;
    private const float updateInterval = 1.0f; // Update once per second

    private string FormatTimeFromSeconds(int totalSeconds)
    {
        int hours = totalSeconds / 3600;
        int minutes = (totalSeconds % 3600) / 60;
        int seconds = totalSeconds % 60;
        return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
    }

    void Start()
    {
        if (eva1DisplayPanel != null)
        {
            eva1DisplayPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("EVA1 Display Panel is not assigned in the inspector.");
        }

        if (eva2DisplayPanel != null)
        {
            eva2DisplayPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("EVA2 Display Panel is not assigned in the inspector.");
        }

        if (ltvDisplayPanel != null)
        {
            ltvDisplayPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("LTV Display Panel is not assigned in the inspector.");
        }
    }

    void Update()
    {
        timeSinceLastUpdate += Time.deltaTime;

        if (timeSinceLastUpdate >= updateInterval)
        {
            timeSinceLastUpdate = 0f; // Reset timer

            if (WebSocketClient.Instance == null)
            {
                Debug.LogWarning("BiometricsDisplayManager: WebSocketClient.Instance is not available.");
                return;
            }

            // Update EVA 1 Gauges using only EvaTelemetryData
            SingleEvaTelemetryData eva1TelemetryData = WebSocketClient.LatestEva1TelemetryData;
            SingleEvaTelemetryData eva2TelemetryData = WebSocketClient.LatestEva2TelemetryData;
            LtvData ltvData = WebSocketClient.LatestLtvData;
            
            Debug.Log("EVA 1 Telemetry Data: " + (eva1TelemetryData != null ? 
                $"HR: {eva1TelemetryData.heartRate}, " +
                $"Temp: {eva1TelemetryData.temperature}, " +
                $"O2C: {eva1TelemetryData.oxygenConsumption}, " +
                $"CO2P: {eva1TelemetryData.co2Production}, " +
                $"O2PriSto: {eva1TelemetryData.oxygenPrimaryStorage}, " +
                $"O2SecSto: {eva1TelemetryData.oxygenSecondaryStorage}, " +
                $"SuitCO2: {eva1TelemetryData.suitPressureCO2}, " +
                $"SuitPrTot: {eva1TelemetryData.suitPressureTotal}" 
                : "null"));

            Debug.Log("EVA 2 Telemetry Data: " + (eva2TelemetryData != null ? 
                $"HR: {eva2TelemetryData.heartRate}, " +
                $"Temp: {eva2TelemetryData.temperature}, " +
                $"O2C: {eva2TelemetryData.oxygenConsumption}, " +
                $"CO2P: {eva2TelemetryData.co2Production}, " +
                $"O2PriSto: {eva2TelemetryData.oxygenPrimaryStorage}, " +
                $"O2SecSto: {eva2TelemetryData.oxygenSecondaryStorage}, " +
                $"SuitCO2: {eva2TelemetryData.suitPressureCO2}, " +
                $"SuitPrTot: {eva2TelemetryData.suitPressureTotal}" 
                : "null"));
            
            if (eva1TelemetryData != null)
            {
                // EVA 1 Biometrics Gauges
                if (eva1_HeartRateGauge != null) 
                    eva1_HeartRateGauge.SetData("Heart Rate", heartRateIcon, "BPM", eva1TelemetryData.heartRate, HEART_RATE_MIN, HEART_RATE_MAX);

                if (eva1_TemperatureGauge != null) 
                    eva1_TemperatureGauge.SetData("Temperature", temperatureIcon, "°C", eva1TelemetryData.temperature, TEMPERATURE_MIN, TEMPERATURE_MAX);

                if (eva1_OxygenConsumptionGauge != null) 
                    eva1_OxygenConsumptionGauge.SetData("O2 Consumption", o2ConsumptionIcon, "kg/hr", eva1TelemetryData.oxygenConsumption, O2_CONSUMPTION_MIN, O2_CONSUMPTION_MAX);
                
                if (eva1_CO2ProductionGauge != null) 
                    eva1_CO2ProductionGauge.SetData("CO2 Production", co2ProductionIcon, "kg/hr", eva1TelemetryData.co2Production, CO2_PRODUCTION_MIN, CO2_PRODUCTION_MAX);

                // EVA 1 EMU Gauges
                if (eva1_OxygenPrimaryStorageGauge != null)
                    eva1_OxygenPrimaryStorageGauge.SetData("O2 Pri Storage", oxygenStorageIcon, "%", eva1TelemetryData.oxygenPrimaryStorage, OXYGEN_STORAGE_MIN, OXYGEN_STORAGE_MAX);

                if (eva1_OxygenSecondaryStorageGauge != null)
                    eva1_OxygenSecondaryStorageGauge.SetData("O2 Sec Storage", oxygenStorageIcon, "%", eva1TelemetryData.oxygenSecondaryStorage, OXYGEN_STORAGE_MIN, OXYGEN_STORAGE_MAX);

                if (eva1_SuitCO2PressureGauge != null)
                    eva1_SuitCO2PressureGauge.SetData("Suit CO2", suitCO2PressureIcon, "mmHg", eva1TelemetryData.suitPressureCO2, SUIT_CO2_PRESSURE_MIN, SUIT_CO2_PRESSURE_MAX);

                if (eva1_TotalSuitPressureGauge != null)
                    eva1_TotalSuitPressureGauge.SetData("Suit Pressure", suitPressureTotalIcon, "PSIa", eva1TelemetryData.suitPressureTotal, TOTAL_SUIT_PRESSURE_MIN, TOTAL_SUIT_PRESSURE_MAX);

                // Update TextMeshPro Fields
                if (eva1_EvaIdText != null) eva1_EvaIdText.text = $"EVA ID: 1";
                if (eva1_O2TimeLeftText != null) eva1_O2TimeLeftText.text = $"{FormatTimeFromSeconds(eva1TelemetryData.o2TimeLeft)}";
                if (eva1_OxygenPrimaryPressureText != null) eva1_OxygenPrimaryPressureText.text = $"O2 Pri Press:\n{eva1TelemetryData.oxygenPrimaryPressure:F2} PSI";
                if (eva1_OxygenSecondaryPressureText != null) eva1_OxygenSecondaryPressureText.text = $"O2 Sec Press:\n{eva1TelemetryData.oxygenSecondaryPressure:F2} PSI";
                if (eva1_SuitPressureOxygenText != null) eva1_SuitPressureOxygenText.text = $"Suit O2 Press:\n{eva1TelemetryData.suitPressureOxygen:F2} PSIa";
                if (eva1_SuitPressureOtherText != null) eva1_SuitPressureOtherText.text = $"Suit Press:\n{eva1TelemetryData.suitPressureOther:F2} PSIa";
                if (eva1_ScrubberAPressureText != null) eva1_ScrubberAPressureText.text = $"Scrubber A:\n{eva1TelemetryData.scrubberAPressure:F2} PSI";
                if (eva1_ScrubberBPressureText != null) eva1_ScrubberBPressureText.text = $"Scrubber B:\n{eva1TelemetryData.scrubberBPressure:F2} PSI";
                if (eva1_H2OGasPressureText != null) eva1_H2OGasPressureText.text = $"H2O Gas:\n{eva1TelemetryData.h2oGasPressure:F2} PSI";
                if (eva1_H2OLiquidPressureText != null) eva1_H2OLiquidPressureText.text = $"H2O Liquid:\n{eva1TelemetryData.h2oLiquidPressure:F2} PSI";
                if (eva1_PrimaryFanRPMText != null) eva1_PrimaryFanRPMText.text = $"Pri Fan:\n{eva1TelemetryData.primaryFanRPM} RPM";
                if (eva1_SecondaryFanRPMText != null) eva1_SecondaryFanRPMText.text = $"Sec Fan:\n{eva1TelemetryData.secondaryFanRPM} RPM";
                if (eva1_HelmetCO2PressureText != null) eva1_HelmetCO2PressureText.text = $"Helmet CO2:\n{eva1TelemetryData.helmetCO2Pressure:F2} mmHg";
                if (eva1_CoolantLevelText != null) eva1_CoolantLevelText.text = $"Coolant:\n{eva1TelemetryData.coolantLevel:F1}%";
            }

            if (eva2TelemetryData != null)
            {
                // EVA 2 Biometrics Gauges
                if (eva2_HeartRateGauge != null) 
                    eva2_HeartRateGauge.SetData("Heart Rate", heartRateIcon, "BPM", eva2TelemetryData.heartRate, HEART_RATE_MIN, HEART_RATE_MAX);
                if (eva2_TemperatureGauge != null) 
                    eva2_TemperatureGauge.SetData("Temperature", temperatureIcon, "°C", eva2TelemetryData.temperature, TEMPERATURE_MIN, TEMPERATURE_MAX);
                if (eva2_OxygenConsumptionGauge != null) 
                    eva2_OxygenConsumptionGauge.SetData("O2 Consumption", o2ConsumptionIcon, "kg/hr", eva2TelemetryData.oxygenConsumption, O2_CONSUMPTION_MIN, O2_CONSUMPTION_MAX);
                if (eva2_CO2ProductionGauge != null) 
                    eva2_CO2ProductionGauge.SetData("CO2 Production", co2ProductionIcon, "kg/hr", eva2TelemetryData.co2Production, CO2_PRODUCTION_MIN, CO2_PRODUCTION_MAX);

                // EVA 2 EMU Gauges
                if (eva2_OxygenPrimaryStorageGauge != null)
                    eva2_OxygenPrimaryStorageGauge.SetData("O2 Pri Storage", oxygenStorageIcon, "%", eva2TelemetryData.oxygenPrimaryStorage, OXYGEN_STORAGE_MIN, OXYGEN_STORAGE_MAX);
                if (eva2_OxygenSecondaryStorageGauge != null)
                    eva2_OxygenSecondaryStorageGauge.SetData("O2 Sec Storage", oxygenStorageIcon, "%", eva2TelemetryData.oxygenSecondaryStorage, OXYGEN_STORAGE_MIN, OXYGEN_STORAGE_MAX);
                if (eva2_SuitCO2PressureGauge != null)
                    eva2_SuitCO2PressureGauge.SetData("Suit CO2", suitCO2PressureIcon, "mmHg", eva2TelemetryData.suitPressureCO2, SUIT_CO2_PRESSURE_MIN, SUIT_CO2_PRESSURE_MAX);
                if (eva2_TotalSuitPressureGauge != null)
                    eva2_TotalSuitPressureGauge.SetData("Suit Pressure", suitPressureTotalIcon, "PSIa", eva2TelemetryData.suitPressureTotal, TOTAL_SUIT_PRESSURE_MIN, TOTAL_SUIT_PRESSURE_MAX);

                // Update EVA2 TextMeshPro Fields
                if (eva2_EvaIdText != null) eva2_EvaIdText.text = $"EVA ID: 2";
                if (eva2_O2TimeLeftText != null) eva2_O2TimeLeftText.text = $"{FormatTimeFromSeconds(eva2TelemetryData.o2TimeLeft)}";
                if (eva2_OxygenPrimaryPressureText != null) eva2_OxygenPrimaryPressureText.text = $"O2 Pri Press:\n{eva2TelemetryData.oxygenPrimaryPressure:F2} PSI";
                if (eva2_OxygenSecondaryPressureText != null) eva2_OxygenSecondaryPressureText.text = $"O2 Sec Press:\n{eva2TelemetryData.oxygenSecondaryPressure:F2} PSI";
                if (eva2_SuitPressureOxygenText != null) eva2_SuitPressureOxygenText.text = $"Suit O2 Press:\n{eva2TelemetryData.suitPressureOxygen:F2} PSIa";
                if (eva2_SuitPressureOtherText != null) eva2_SuitPressureOtherText.text = $"Suit Press:\n{eva2TelemetryData.suitPressureOther:F2} PSIa";
                if (eva2_ScrubberAPressureText != null) eva2_ScrubberAPressureText.text = $"Scrubber A:\n{eva2TelemetryData.scrubberAPressure:F2} PSI";
                if (eva2_ScrubberBPressureText != null) eva2_ScrubberBPressureText.text = $"Scrubber B:\n{eva2TelemetryData.scrubberBPressure:F2} PSI";
                if (eva2_H2OGasPressureText != null) eva2_H2OGasPressureText.text = $"H2O Gas:\n{eva2TelemetryData.h2oGasPressure:F2} PSI";
                if (eva2_H2OLiquidPressureText != null) eva2_H2OLiquidPressureText.text = $"H2O Liquid:\n{eva2TelemetryData.h2oLiquidPressure:F2} PSI";
                if (eva2_PrimaryFanRPMText != null) eva2_PrimaryFanRPMText.text = $"Pri Fan:\n{eva2TelemetryData.primaryFanRPM} RPM";
                if (eva2_SecondaryFanRPMText != null) eva2_SecondaryFanRPMText.text = $"Sec Fan:\n{eva2TelemetryData.secondaryFanRPM} RPM";
                if (eva2_HelmetCO2PressureText != null) eva2_HelmetCO2PressureText.text = $"Helmet CO2:\n{eva2TelemetryData.helmetCO2Pressure:F2} mmHg";
                if (eva2_CoolantLevelText != null) eva2_CoolantLevelText.text = $"Coolant:\n{eva2TelemetryData.coolantLevel:F1}%";
            }

            if (ltvData != null)
            {
                if (ltv_BatteryLevelGauge != null)
                    ltv_BatteryLevelGauge.SetData("Battery", null, "%", ltvData.battery_level, BATTERY_LEVEL_MIN, BATTERY_LEVEL_MAX);
                
                if (ltv_OxygenTankGauge != null)
                    ltv_OxygenTankGauge.SetData("O2 Tank", null, "%", ltvData.oxygen_tank, OXYGEN_TANK_MIN, OXYGEN_TANK_MAX);
                
                if (ltv_SpeedGauge != null)
                    ltv_SpeedGauge.SetData("Speed", null, "m/s", ltvData.speed, SPEED_MIN, SPEED_MAX);
                
                if (ltv_CabinTemperatureGauge != null)
                    ltv_CabinTemperatureGauge.SetData("Cabin Temp", null, "°C", ltvData.cabin_temperature, CABIN_TEMP_MIN, CABIN_TEMP_MAX);
                
                if (ltv_DistanceFromBaseGauge != null)
                    ltv_DistanceFromBaseGauge.SetData("Dist from Base", null, "m", ltvData.distance_from_base, DISTANCE_MIN, DISTANCE_MAX);
                
                if (ltv_DistanceTraveledGauge != null)
                    ltv_DistanceTraveledGauge.SetData("Dist Traveled", null, "m", ltvData.distance_traveled, DISTANCE_MIN, DISTANCE_MAX);
            }
        }
    }

    // Public methods to be called by UI Buttons
    public void ToggleEva1Display()
    {
        if (eva1DisplayPanel != null)
        {
            eva1DisplayPanel.SetActive(true);
        }

        if (eva2DisplayPanel != null)
        {
            eva2DisplayPanel.SetActive(false);
        }

        if (ltvDisplayPanel != null)
        {
            ltvDisplayPanel.SetActive(false);
        }
    }

    public void ToggleEva2Display()
    {
        if (eva1DisplayPanel != null)
        {
            eva1DisplayPanel.SetActive(false);
        }

        if (eva2DisplayPanel != null)
        {
            eva2DisplayPanel.SetActive(true);
        }

        if (ltvDisplayPanel != null)
        {
            ltvDisplayPanel.SetActive(false);
        }
    }

    public void ToggleLtvDisplay()
    {
        if (eva1DisplayPanel != null)
        {
            eva1DisplayPanel.SetActive(false);
        }

        if (eva2DisplayPanel != null)
        {
            eva2DisplayPanel.SetActive(false);
        }

        if (ltvDisplayPanel != null)
        {
            ltvDisplayPanel.SetActive(true);
        }
    }
} 