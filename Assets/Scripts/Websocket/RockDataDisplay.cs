using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Concurrent;

public class RockDataDisplay : MonoBehaviour
{
    public static RockDataDisplay Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    [Header("EVA 1 Display")]
    public TextMeshProUGUI eva1SpecId;
    public TextMeshProUGUI eva1Oxygen;
    public TextMeshProUGUI eva1Water;
    public TextMeshProUGUI eva1CO2;
    public TextMeshProUGUI eva1H2;
    public TextMeshProUGUI eva1N2;
    public TextMeshProUGUI eva1Other;
    public TextMeshProUGUI eva1Temperature;
    public TextMeshProUGUI eva1Pressure;
    public TextMeshProUGUI eva1Humidity;
    public TextMeshProUGUI eva1Light;

    private ConcurrentQueue<RockData> pendingUpdates = new ConcurrentQueue<RockData>();

    private void Start()
    {
        Debug.Log("RockDataDisplay: Starting and subscribing to rock_data");
        if (WebSocketClient.Instance == null)
        {
            Debug.LogError("RockDataDisplay: WebSocketClient.Instance is null! Make sure WebSocketClient is in the scene and active.");
            return;
        }
        
        WebSocketClient.Instance.Subscribe("rock_data", HandleRockData);
        ValidateTextComponents();
    }

    private void ValidateTextComponents()
    {
        Debug.Log("RockDataDisplay: Validating TextMeshPro components...");
        if (eva1SpecId == null) Debug.LogError("eva1SpecId is not assigned!");
        if (eva1Oxygen == null) Debug.LogError("eva1Oxygen is not assigned!");
        if (eva1Water == null) Debug.LogError("eva1Water is not assigned!");
        if (eva1CO2 == null) Debug.LogError("eva1CO2 is not assigned!");
        if (eva1H2 == null) Debug.LogError("eva1H2 is not assigned!");
        if (eva1N2 == null) Debug.LogError("eva1N2 is not assigned!");
        if (eva1Other == null) Debug.LogError("eva1Other is not assigned!");
        if (eva1Temperature == null) Debug.LogError("eva1Temperature is not assigned!");
        if (eva1Pressure == null) Debug.LogError("eva1Pressure is not assigned!");
        if (eva1Humidity == null) Debug.LogError("eva1Humidity is not assigned!");
        if (eva1Light == null) Debug.LogError("eva1Light is not assigned!");
    }

    private void OnDestroy()
    {
        if (WebSocketClient.Instance != null)
        {
            WebSocketClient.Instance.Unsubscribe("rock_data", HandleRockData);
        }
    }

    private void Update()
    {
        while (pendingUpdates.TryDequeue(out RockData rockData))
        {
            Debug.Log($"RockDataDisplay: [MainThread] Checking specId {rockData.specId}");
            if (rockData.specId != 0)
            {
                UpdateEVA1Display(rockData);
            }
        }
    }

    public void HandleRockData(object data)
    {
        try
        {
            Debug.Log($"RockDataDisplay: Received rock data: {data}");
            Debug.Log($"RockDataDisplay: EVA ID: {((RockData)data).evaId}");
            Debug.Log($"RockDataDisplay: SPEC ID: {((RockData)data).specId}");
            Debug.Log($"RockDataDisplay: Oxygen: {((RockData)data).oxygen}%");
            Debug.Log($"RockDataDisplay: Water: {((RockData)data).water}%");
            Debug.Log($"RockDataDisplay: CO2: {((RockData)data).co2}%");
            Debug.Log($"RockDataDisplay: H2: {((RockData)data).h2}%");
            Debug.Log($"RockDataDisplay: N2: {((RockData)data).n2}%");
            Debug.Log($"RockDataDisplay: Other: {((RockData)data).other}%");
            Debug.Log($"RockDataDisplay: Temperature: {((RockData)data).temperature}°C");
            Debug.Log($"RockDataDisplay: Pressure: {((RockData)data).pressure} Pa");
            Debug.Log($"RockDataDisplay: Humidity: {((RockData)data).humidity}%");
            Debug.Log($"RockDataDisplay: Light: {((RockData)data).light} lux");
            RockData rockData = data as RockData;
            if (rockData == null)
            {
                Debug.LogError("RockDataDisplay: Data is not a RockData object");
                return;
            }
            Debug.Log($"RockDataDisplay: Parsed data for EVA {rockData.evaId}");
            Debug.Log($"RockDataDisplay: EVA {rockData.evaId}, SPEC {rockData.specId}, O2 {rockData.oxygen}, Water {rockData.water}, CO2 {rockData.co2}, H2 {rockData.h2}, N2 {rockData.n2}, Other {rockData.other}, Temp {rockData.temperature}, Pressure {rockData.pressure}, Humidity {rockData.humidity}, Light {rockData.light}");
            pendingUpdates.Enqueue(rockData);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"RockDataDisplay: Error handling rock data: {e.Message}\nStack trace: {e.StackTrace}");
        }
    }

    private void UpdateEVA1Display(RockData data)
    {
        Debug.Log($"UpdateEVA1Display called with: EVA {data.evaId}, SPEC {data.specId}, O2 {data.oxygen}, Water {data.water}, CO2 {data.co2}, H2 {data.h2}, N2 {data.n2}, Other {data.other}, Temp {data.temperature}, Pressure {data.pressure}, Humidity {data.humidity}, Light {data.light}");
        if (eva1SpecId != null) eva1SpecId.text = $"SPEC ID: {data.specId}";
        if (eva1Oxygen != null) eva1Oxygen.text = $"Oxygen: {data.oxygen:F2}%";
        if (eva1Water != null) eva1Water.text = $"Water: {data.water:F2}%";
        if (eva1CO2 != null) eva1CO2.text = $"CO2: {data.co2:F2}%";
        if (eva1H2 != null) eva1H2.text = $"H2: {data.h2:F2}%";
        if (eva1N2 != null) eva1N2.text = $"N2: {data.n2:F2}%";
        if (eva1Other != null) eva1Other.text = $"Other: {data.other:F2}%";
        if (eva1Temperature != null) eva1Temperature.text = $"Temperature: {data.temperature:F2}°C";
        if (eva1Pressure != null) eva1Pressure.text = $"Pressure: {data.pressure:F2} Pa";
        if (eva1Humidity != null) eva1Humidity.text = $"Humidity: {data.humidity:F2}%";
        if (eva1Light != null) eva1Light.text = $"Light: {data.light:F2} lux";
    }
} 