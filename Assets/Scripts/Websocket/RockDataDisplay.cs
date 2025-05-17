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
    public TextMeshProUGUI eva1NameText;
    public TextMeshProUGUI eva1SiO2Text;
    public TextMeshProUGUI eva1Al2O3Text;
    public TextMeshProUGUI eva1MnOText;
    public TextMeshProUGUI eva1CaOText;
    public TextMeshProUGUI eva1P2O3Text;
    public TextMeshProUGUI eva1TiO2Text;
    public TextMeshProUGUI eva1FeOText;
    public TextMeshProUGUI eva1MgOText;
    public TextMeshProUGUI eva1K2OText;
    public TextMeshProUGUI eva1Other;

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
        if (eva1NameText == null) Debug.LogError("eva1NameText is not assigned!");
        if (eva1SiO2Text == null) Debug.LogError("eva1SiO2Text is not assigned!");
        if (eva1Al2O3Text == null) Debug.LogError("eva1Al2O3Text is not assigned!");
        if (eva1MnOText == null) Debug.LogError("eva1MnOText is not assigned!");
        if (eva1CaOText == null) Debug.LogError("eva1CaOText is not assigned!");
        if (eva1P2O3Text == null) Debug.LogError("eva1P2O3Text is not assigned!");
        if (eva1TiO2Text == null) Debug.LogError("eva1TiO2Text is not assigned!");
        if (eva1FeOText == null) Debug.LogError("eva1FeOText is not assigned!");
        if (eva1MgOText == null) Debug.LogError("eva1MgOText is not assigned!");
        if (eva1K2OText == null) Debug.LogError("eva1K2OText is not assigned!");
        if (eva1Other == null) Debug.LogError("eva1Other (composition) is not assigned!");
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
            Debug.Log($"RockDataDisplay: [MainThread] Checking specId {rockData.specId}, Name: {rockData.name}");
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
            RockData rockData = data as RockData;
            if (rockData == null)
            {
                Debug.LogError("RockDataDisplay: Data is not a RockData object");
                return;
            }

            Debug.Log($"RockDataDisplay: Received rock data for EVA {rockData.evaId}, SpecID: {rockData.specId}, Name: {rockData.name}");
            if (rockData.composition != null)
            {
                Debug.Log($"RockDataDisplay: Composition - SiO2: {rockData.composition.SiO2}%, Other: {rockData.composition.Other}%");
            }
            else
            {
                Debug.Log("RockDataDisplay: Composition data is null.");
            }
            
            pendingUpdates.Enqueue(rockData);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"RockDataDisplay: Error handling rock data: {e.Message}\nStack trace: {e.StackTrace}");
        }
    }

    private void UpdateEVA1Display(RockData data)
    {
        string placeholder = "---";
        Debug.Log($"UpdateEVA1Display called with: EVA {data.evaId}, SPEC {data.specId}, Name: {data.name}");

        if (eva1SpecId != null) eva1SpecId.text = $"SPEC ID: {data.specId}";
        if (eva1NameText != null) eva1NameText.text = $"Name: {data.name ?? placeholder}";

        if (data.composition != null)
        {
            Debug.Log("UpdateEVA1Display: Updating composition fields.");
            if (eva1SiO2Text != null) eva1SiO2Text.text = $"SiO2: {data.composition.SiO2:F2}%";
            if (eva1Al2O3Text != null) eva1Al2O3Text.text = $"Al2O3: {data.composition.Al2O3:F2}%";
            if (eva1MnOText != null) eva1MnOText.text = $"MnO: {data.composition.MnO:F2}%";
            if (eva1CaOText != null) eva1CaOText.text = $"CaO: {data.composition.CaO:F2}%";
            if (eva1P2O3Text != null) eva1P2O3Text.text = $"P2O3: {data.composition.P2O3:F2}%";
            if (eva1TiO2Text != null) eva1TiO2Text.text = $"TiO2: {data.composition.TiO2:F2}%";
            if (eva1FeOText != null) eva1FeOText.text = $"FeO: {data.composition.FeO:F2}%";
            if (eva1MgOText != null) eva1MgOText.text = $"MgO: {data.composition.MgO:F2}%";
            if (eva1K2OText != null) eva1K2OText.text = $"K2O: {data.composition.K2O:F2}%";
            if (eva1Other != null) eva1Other.text = $"Other: {data.composition.Other:F2}%";
        }
        else
        {
            Debug.LogWarning("UpdateEVA1Display: RockData.composition is null. Clearing composition fields.");
            if (eva1SiO2Text != null) eva1SiO2Text.text = $"SiO2: {placeholder}";
            if (eva1Al2O3Text != null) eva1Al2O3Text.text = $"Al2O3: {placeholder}";
            if (eva1MnOText != null) eva1MnOText.text = $"MnO: {placeholder}";
            if (eva1CaOText != null) eva1CaOText.text = $"CaO: {placeholder}";
            if (eva1P2O3Text != null) eva1P2O3Text.text = $"P2O3: {placeholder}";
            if (eva1TiO2Text != null) eva1TiO2Text.text = $"TiO2: {placeholder}";
            if (eva1FeOText != null) eva1FeOText.text = $"FeO: {placeholder}";
            if (eva1MgOText != null) eva1MgOText.text = $"MgO: {placeholder}";
            if (eva1K2OText != null) eva1K2OText.text = $"K2O: {placeholder}";
            if (eva1Other != null) eva1Other.text = $"Other: {placeholder}";
        }
    }
} 