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

    [Header("EVA Display Panels (Assign in Inspector)")]
    public GameObject eva1DisplayPanel;
    public GameObject eva2DisplayPanel; // Assuming you'll have a similar setup for EVA2 later

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

    private float timeSinceLastUpdate = 0f;
    private const float updateInterval = 1.0f; // Update once per second

    void Start()
    {
        // Set initial panel visibility: EVA1 active, EVA2 inactive
        if (eva1DisplayPanel != null)
        {
            eva1DisplayPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("EVA1 Display Panel is not assigned in the inspector. Initial state not set.");
        }

        if (eva2DisplayPanel != null)
        {
            eva2DisplayPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("EVA2 Display Panel is not assigned in the inspector. Initial state not set.");
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
                // Add other EVA2 fields to log if needed, mirroring EVA1's full log
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
                if (eva1_O2TimeLeftText != null) eva1_O2TimeLeftText.text = $"O2 Time Left: {eva1TelemetryData.o2TimeLeft}s";
                if (eva1_OxygenPrimaryPressureText != null) eva1_OxygenPrimaryPressureText.text = $"O2 Pri Press: {eva1TelemetryData.oxygenPrimaryPressure:F2} PSI";
                if (eva1_OxygenSecondaryPressureText != null) eva1_OxygenSecondaryPressureText.text = $"O2 Sec Press: {eva1TelemetryData.oxygenSecondaryPressure:F2} PSI";
                if (eva1_SuitPressureOxygenText != null) eva1_SuitPressureOxygenText.text = $"Suit O2 Press: {eva1TelemetryData.suitPressureOxygen:F2} PSIa";
                if (eva1_SuitPressureOtherText != null) eva1_SuitPressureOtherText.text = $"Suit Other Press: {eva1TelemetryData.suitPressureOther:F2} PSIa";
                if (eva1_ScrubberAPressureText != null) eva1_ScrubberAPressureText.text = $"Scrubber A: {eva1TelemetryData.scrubberAPressure:F2} PSI";
                if (eva1_ScrubberBPressureText != null) eva1_ScrubberBPressureText.text = $"Scrubber B: {eva1TelemetryData.scrubberBPressure:F2} PSI";
                if (eva1_H2OGasPressureText != null) eva1_H2OGasPressureText.text = $"H2O Gas: {eva1TelemetryData.h2oGasPressure:F2} PSI";
                if (eva1_H2OLiquidPressureText != null) eva1_H2OLiquidPressureText.text = $"H2O Liquid: {eva1TelemetryData.h2oLiquidPressure:F2} PSI";
                if (eva1_PrimaryFanRPMText != null) eva1_PrimaryFanRPMText.text = $"Pri Fan: {eva1TelemetryData.primaryFanRPM} RPM";
                if (eva1_SecondaryFanRPMText != null) eva1_SecondaryFanRPMText.text = $"Sec Fan: {eva1TelemetryData.secondaryFanRPM} RPM";
                if (eva1_HelmetCO2PressureText != null) eva1_HelmetCO2PressureText.text = $"Helmet CO2: {eva1TelemetryData.helmetCO2Pressure:F2} mmHg";
                if (eva1_CoolantLevelText != null) eva1_CoolantLevelText.text = $"Coolant: {eva1TelemetryData.coolantLevel:F1}%";
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
                if (eva2_O2TimeLeftText != null) eva2_O2TimeLeftText.text = $"O2 Time Left: {eva2TelemetryData.o2TimeLeft}s";
                if (eva2_OxygenPrimaryPressureText != null) eva2_OxygenPrimaryPressureText.text = $"O2 Pri Press: {eva2TelemetryData.oxygenPrimaryPressure:F2} PSI";
                if (eva2_OxygenSecondaryPressureText != null) eva2_OxygenSecondaryPressureText.text = $"O2 Sec Press: {eva2TelemetryData.oxygenSecondaryPressure:F2} PSI";
                if (eva2_SuitPressureOxygenText != null) eva2_SuitPressureOxygenText.text = $"Suit O2 Press: {eva2TelemetryData.suitPressureOxygen:F2} PSIa";
                if (eva2_SuitPressureOtherText != null) eva2_SuitPressureOtherText.text = $"Suit Other Press: {eva2TelemetryData.suitPressureOther:F2} PSIa";
                if (eva2_ScrubberAPressureText != null) eva2_ScrubberAPressureText.text = $"Scrubber A: {eva2TelemetryData.scrubberAPressure:F2} PSI";
                if (eva2_ScrubberBPressureText != null) eva2_ScrubberBPressureText.text = $"Scrubber B: {eva2TelemetryData.scrubberBPressure:F2} PSI";
                if (eva2_H2OGasPressureText != null) eva2_H2OGasPressureText.text = $"H2O Gas: {eva2TelemetryData.h2oGasPressure:F2} PSI";
                if (eva2_H2OLiquidPressureText != null) eva2_H2OLiquidPressureText.text = $"H2O Liquid: {eva2TelemetryData.h2oLiquidPressure:F2} PSI";
                if (eva2_PrimaryFanRPMText != null) eva2_PrimaryFanRPMText.text = $"Pri Fan: {eva2TelemetryData.primaryFanRPM} RPM";
                if (eva2_SecondaryFanRPMText != null) eva2_SecondaryFanRPMText.text = $"Sec Fan: {eva2TelemetryData.secondaryFanRPM} RPM";
                if (eva2_HelmetCO2PressureText != null) eva2_HelmetCO2PressureText.text = $"Helmet CO2: {eva2TelemetryData.helmetCO2Pressure:F2} mmHg";
                if (eva2_CoolantLevelText != null) eva2_CoolantLevelText.text = $"Coolant: {eva2TelemetryData.coolantLevel:F1}%";
            }
        }
    }

    // Public methods to be called by UI Buttons
    public void ToggleEva1Display()
    {
        Debug.Log("ToggleEva1Display() called.");
        if (eva1DisplayPanel != null)
        {
            Debug.Log($"Attempting to activate eva1DisplayPanel: {eva1DisplayPanel.name}. Current state: {eva1DisplayPanel.activeSelf}");
            eva1DisplayPanel.SetActive(true);
            Debug.Log($"eva1DisplayPanel: {eva1DisplayPanel.name} new state: {eva1DisplayPanel.activeSelf}");
        }
        else
        {
            Debug.LogWarning("EVA1 Display Panel is not assigned in the inspector.");
        }

        if (eva2DisplayPanel != null)
        {
            Debug.Log($"Attempting to deactivate eva2DisplayPanel: {eva2DisplayPanel.name}. Current state: {eva2DisplayPanel.activeSelf}");
            eva2DisplayPanel.SetActive(false);
            Debug.Log($"eva2DisplayPanel: {eva2DisplayPanel.name} new state: {eva2DisplayPanel.activeSelf}");
        }
        else
        {
            Debug.LogWarning("EVA2 Display Panel is not assigned (when trying to deactivate in ToggleEva1Display).");
        }
    }

    public void ToggleEva2Display()
    {
        Debug.Log("ToggleEva2Display() called.");
        if (eva2DisplayPanel != null)
        {
            Debug.Log($"Attempting to activate eva2DisplayPanel: {eva2DisplayPanel.name}. Current state: {eva2DisplayPanel.activeSelf}");
            eva2DisplayPanel.SetActive(true);
            Debug.Log($"eva2DisplayPanel: {eva2DisplayPanel.name} new state: {eva2DisplayPanel.activeSelf}");
        }
        else
        {
            Debug.LogWarning("EVA2 Display Panel is not assigned in the inspector.");
        }

        if (eva1DisplayPanel != null)
        {
            Debug.Log($"Attempting to deactivate eva1DisplayPanel: {eva1DisplayPanel.name}. Current state: {eva1DisplayPanel.activeSelf}");
            eva1DisplayPanel.SetActive(false);
            Debug.Log($"eva1DisplayPanel: {eva1DisplayPanel.name} new state: {eva1DisplayPanel.activeSelf}");
        }
         else
        {
            Debug.LogWarning("EVA1 Display Panel is not assigned (when trying to deactivate in ToggleEva2Display).");
        }
    }
} 