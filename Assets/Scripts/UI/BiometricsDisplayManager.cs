using UnityEngine;
using TMPro; // If you need to display EVA ID or other general info

public class BiometricsDisplayManager : MonoBehaviour
{
    [Header("EVA 1 Gauges (Assign in Inspector)")]
    public BiometricGauge eva1_OxygenConsumptionGauge; // Example: For O2 Consumption
    public BiometricGauge eva1_CO2ProductionGauge;    // Example: For CO2 Production
    public BiometricGauge eva1_HeartRateGauge;        // Example: For Heart Rate
    public BiometricGauge eva1_TemperatureGauge;      // Example: For Body Temperature

    [Header("Icons (Assign in Inspector)")]
    public Sprite co2Icon; // Kept for potential future use or if CO2 Production uses it
    public Sprite suitPressureIcon; // Kept for potential future use
    public Sprite o2ConsumptionIcon;
    public Sprite co2ProductionIcon;
    public Sprite heartRateIcon;
    public Sprite temperatureIcon;
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

    void Update()
    {
        if (WebSocketClient.Instance == null)
        {
            Debug.LogWarning("BiometricsDisplayManager: WebSocketClient.Instance is not available.");
            return;
        }

        // Update EVA 1 Gauges
        BiometricsData eva1Data = WebSocketClient.LatestEva1BiometricsData;
        if (eva1Data != null)
        {
            if (eva1_OxygenConsumptionGauge != null) 
                eva1_OxygenConsumptionGauge.SetData("O2 Consumption", o2ConsumptionIcon, "kg/hr", eva1Data.o2Consumption, O2_CONSUMPTION_MIN, O2_CONSUMPTION_MAX);
            
            if (eva1_CO2ProductionGauge != null) 
                eva1_CO2ProductionGauge.SetData("CO2 Production", co2ProductionIcon, "kg/hr", eva1Data.co2Production, CO2_PRODUCTION_MIN, CO2_PRODUCTION_MAX);

            if (eva1_HeartRateGauge != null) 
                eva1_HeartRateGauge.SetData("Heart Rate", heartRateIcon, "BPM", eva1Data.heartRate, HEART_RATE_MIN, HEART_RATE_MAX);

            if (eva1_TemperatureGauge != null) 
                eva1_TemperatureGauge.SetData("Temperature", temperatureIcon, "°C", eva1Data.temperature, TEMPERATURE_MIN, TEMPERATURE_MAX);
        }
    }
} 