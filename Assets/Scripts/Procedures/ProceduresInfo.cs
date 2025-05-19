using UnityEngine;
using TMPro;

public class ProceduresInfo : MonoBehaviour
{
    [Header("EVA 1 Text Components")]
    public TextMeshProUGUI eva1CO2Pressure;
    public TextMeshProUGUI eva1O2Remaining;
    public TextMeshProUGUI eva1BatteryLeft;

    [Header("EVA 2 Text Components")]
    public TextMeshProUGUI eva2CO2Pressure;
    public TextMeshProUGUI eva2O2Remaining;
    public TextMeshProUGUI eva2BatteryLeft;

    private void Update()
    {
        UpdateEVA1Info();
        UpdateEVA2Info();
    }

    private void UpdateEVA1Info()
    {
        if (WebSocketClient.LatestEva1TelemetryData != null)
        {
            var data = WebSocketClient.LatestEva1TelemetryData;
            
            if (eva1CO2Pressure != null)
                eva1CO2Pressure.text = $"CO2: {data.suitPressureCO2:F4}";
            
            if (eva1O2Remaining != null)
                eva1O2Remaining.text = $"O2: {data.o2TimeLeft / 60:F0}m";
            
            if (eva1BatteryLeft != null)
                eva1BatteryLeft.text = $"Batt: {data.batteryTimeLeft / 60:F0}m";
        }
    }

    private void UpdateEVA2Info()
    {
        if (WebSocketClient.LatestEva2TelemetryData != null)
        {
            var data = WebSocketClient.LatestEva2TelemetryData;
            
            if (eva2CO2Pressure != null)
                eva2CO2Pressure.text = $"CO2: {data.suitPressureCO2:F4}";
            
            if (eva2O2Remaining != null)
                eva2O2Remaining.text = $"O2: {data.o2TimeLeft / 60:F0}m";
            
            if (eva2BatteryLeft != null)
                eva2BatteryLeft.text = $"Batt: {data.batteryTimeLeft / 60:F0}m";
        }
    }
} 