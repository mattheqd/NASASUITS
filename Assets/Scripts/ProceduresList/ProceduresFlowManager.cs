using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;
using ProcedureSystem;
using TMPro;
using System.Collections;

public class ProceduresFlowManager : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private GameObject proceduresListPanel;     // First screen - ProceduresList in hierarchy
    [SerializeField] private GameObject proceduresInfoPanel;     // Second screen - ProceduresInfo in hierarchy
    [SerializeField] private GameObject proceduresPanel;    // Third screen - Procedures in hierarchy
    [SerializeField] private GameObject samplingPanel;    // Fourth screen - Sampling in hierarchy
    [SerializeField] private GameObject scanningPanel;    // Fifth screen - Scanning in hierarchy
    [SerializeField] private GameObject picturePanel;    // Sixth screen - Picture in hierarchy
    [SerializeField] private GameObject voicePanel;    // Seventh screen - Video in hierarchy
    [SerializeField] private GameObject gpsPanel;    // Eighth screen - Video in hierarchy

    [Header("UI Elements")]
    [SerializeField] private Button egressButton;          // Button to go from TasksList to TasksInfo
    [SerializeField] private Button samplingButton;   
     [SerializeField] private Button samplingStart; 
    [SerializeField] private Button backButton;            // Button to go back from TasksInfo to TasksList
    [SerializeField] private Button startButton;           // Button to go from TasksInfo to Procedures
    [SerializeField] private Button verifyManuallyButton;  // Button to manually verify umbilical connection
    [SerializeField] private Button completeScanning;  // Button to manually verify umbilical connection
    [SerializeField] private Button completePicture;  // Button to manually verify umbilical connection
    [SerializeField] private Button completeVoice;  // Button to manually verify umbilical connection
    [SerializeField] private Button completeGps;  // Button to manually verify umbilical connection
    [SerializeField] private Button checkRockDataButton; // Button to check rock data

    [Header("Component References")]
    [SerializeField] private Transform stepsContainer;     // Contains series of steps in TasksInfo
    [SerializeField] private StepItem stepItemPrefab;      // Prefab for each step
    [SerializeField] private ProcedureDisplay procedureDisplay; // Main procedure handler
    [SerializeField] private ProcedureAutomation procedureAutomation; // Handles automation of steps
    [SerializeField] private WebCamController webCamController; // Reference to camera controller
    [SerializeField] private AudioRecorder audioRecorder; // Reference to audio recorder
    [SerializeField] private TextMeshProUGUI coordinateText; // Text to display coordinates
    [SerializeField] private TextMeshProUGUI rockDataText; // Text to display rock data
    
    [Header("Rock Data UI Fields")]
    [SerializeField] private TextMeshProUGUI rockEvaIdText;
    [SerializeField] private TextMeshProUGUI rockSpecIdText;
    [SerializeField] private TextMeshProUGUI rockNameText;
    [SerializeField] private TextMeshProUGUI rockSiO2Text;
    [SerializeField] private TextMeshProUGUI rockAl2O3Text;
    [SerializeField] private TextMeshProUGUI rockMnOText;
    [SerializeField] private TextMeshProUGUI rockCaOText;
    [SerializeField] private TextMeshProUGUI rockP2O3Text;
    [SerializeField] private TextMeshProUGUI rockTiO2Text;
    [SerializeField] private TextMeshProUGUI rockFeOText;
    [SerializeField] private TextMeshProUGUI rockMgOText;
    [SerializeField] private TextMeshProUGUI rockK2OText;
    [SerializeField] private TextMeshProUGUI rockOtherText;

    [Header("WebSocket")]
    [SerializeField] private WebSocketClient webSocketClient;

    // Target task name for this MVP
    private const string PROCEDURE_NAME = "EVA Egress";
    private const string TARGET_TASK_NAME = "Connect UIA to DCU and start Depress";

    // Current geosample being collected
    private GeoSampleData currentSample;
    
    // Current EVA ID (mainly used for rock sampling)
    private int currentEvaId = 1; // Default to EVA 1
    
    // Rock data monitoring
    private RockData baselineRockData = null;
    private bool isMonitoringRockData = false;
    private Coroutine monitorRockDataCoroutine = null;

    private void Awake()
    {
        InitializeUI();
        
        // Find WebSocketClient if not set in inspector
        if (webSocketClient == null)
        {
            webSocketClient = FindObjectOfType<WebSocketClient>();
            if (webSocketClient == null)
            {
                Debug.LogWarning("WebSocketClient not found. Position data will not be available.");
            }
        }
    }
    
    public void InitializeUI()
    {
        // Make sure only the first panel is active at start
        proceduresListPanel.SetActive(true);
        proceduresInfoPanel.SetActive(false);
        proceduresPanel.SetActive(false);
        samplingPanel.SetActive(false);
        scanningPanel.SetActive(false);
        picturePanel.SetActive(false);
        voicePanel.SetActive(false);
        gpsPanel.SetActive(false);
        
        // Set up button listeners
        egressButton.onClick.AddListener(ShowTasksInfo);
        backButton.onClick.AddListener(ShowTasksList);
        startButton.onClick.AddListener(ShowProcedure);
        samplingButton.onClick.AddListener(ShowSampling);
        samplingStart.onClick.AddListener(StartScan);
        completeScanning.onClick.AddListener(CompleteScan);
        completePicture.onClick.AddListener(CompletePicture);
        completeVoice.onClick.AddListener(CompleteVoice);
        completeGps.onClick.AddListener(CompleteGps);
        
        // Set up rock data check button
        if (checkRockDataButton != null)
        {
            checkRockDataButton.onClick.AddListener(CheckForRockData);
        }
        
        // Connect manual verification button if available
        if (verifyManuallyButton != null)
        {
            verifyManuallyButton.onClick.AddListener(VerifyManualStep);
        }
    }
    
    // Get the current EVA position directly from WebSocketClient
    public Vector2 GetCurrentEVAPosition()
    {
        if (webSocketClient != null)
        {
            return webSocketClient.GetEVA1Position();
        }
        return Vector2.zero;
    }
    
    // Get the current EVA heading directly from WebSocketClient
    public float GetCurrentEVAHeading()
    {
        if (webSocketClient != null)
        {
            return webSocketClient.GetEVA1Heading();
        }
        return 0f;
    }

    private void StartScan()
    {
        // Create a new sample when starting the geosampling process
        currentSample = GeoSampleData.CreateNew("", "", "");
        
        // Capture the current rock data as our baseline for comparison
        CaptureBaselineRockData();
        
        // Start monitoring for rock data changes
        StartRockDataMonitoring();
        
        samplingPanel.SetActive(false);
        scanningPanel.SetActive(true);
    }
    
    private void CaptureBaselineRockData()
    {
        // Get the current rock data for the EVA we're interested in
        baselineRockData = WebSocketClient.GetRockDataForEva(currentEvaId);
        
        if (baselineRockData != null)
        {
            Debug.Log($"[SCAN] Captured baseline rock data for EVA{currentEvaId}, SpecID: {baselineRockData.specId}");
            DisplayRockDataDetails(baselineRockData); // Display baseline details
            if (rockDataText != null)
            {
                rockDataText.text = "<color=#FFCC00>Scanning for rock sample... Waiting for data to change</color>";
            }
        }
        else
        {
            Debug.Log($"[SCAN] No baseline rock data available for EVA{currentEvaId}");
            DisplayRockDataDetails(null); // Clear details
            if (rockDataText != null)
            {
                rockDataText.text = "<color=#FFCC00>No rock data available. Please ensure telemetry is connected.</color>";
            }
        }
    }
    
    private void StartRockDataMonitoring()
    {
        // Stop any existing monitoring
        StopRockDataMonitoring();
        
        // Start a new monitoring coroutine
        isMonitoringRockData = true;
        monitorRockDataCoroutine = StartCoroutine(MonitorRockDataChanges());
    }
    
    private void StopRockDataMonitoring()
    {
        isMonitoringRockData = false;
        
        if (monitorRockDataCoroutine != null)
        {
            StopCoroutine(monitorRockDataCoroutine);
            monitorRockDataCoroutine = null;
        }
    }
    
    private IEnumerator MonitorRockDataChanges()
    {
        if (baselineRockData == null)
        {
            Debug.LogWarning("[ROCK_MONITOR] Started monitoring with null baseline data");
            yield break;
        }
        
        Debug.Log($"[ROCK_MONITOR] Starting to monitor for changes from SpecID: {baselineRockData.specId}");
        
        int checkCount = 0;
        float waitTime = 0.5f; // Check every half second
        
        // Show waiting animation
        string[] loadingStates = new string[] { "⏳", "⌛" };
        int animationFrame = 0;
        
        while (isMonitoringRockData)
        {
            // Update the waiting animation
            if (rockDataText != null)
            {
                animationFrame = (animationFrame + 1) % loadingStates.Length;
                rockDataText.text = $"<color=#FFCC00>Scanning for rock sample...</color> {loadingStates[animationFrame]} Waiting for new data...";
            }
            DisplayRockDataDetails(null); // Clear details while waiting for new specId
            
            // Get the current rock data
            RockData currentRockData = WebSocketClient.GetRockDataForEva(currentEvaId);
            
            if (currentRockData != null)
            {
                // Check if the spec ID has changed
                if (currentRockData.specId != baselineRockData.specId)
                {
                    Debug.Log($"[ROCK_MONITOR] Detected rock data change: SpecID {baselineRockData.specId} -> {currentRockData.specId}");
                    
                    DisplayRockDataDetails(currentRockData); // Display new rock data details
                    if (rockDataText != null)
                    {
                        rockDataText.text = "🆕 <color=#00FF00>NEW SAMPLE DETECTED!</color>";
                    }
                    
                    // Update the sample type with the new spec ID
                    if (currentSample != null)
                    {
                        currentSample.sampleType = $"Rock Sample #{currentRockData.specId} (EVA {currentRockData.evaId})";
                        Debug.Log($"[ROCK_MONITOR] Updated sample type to: {currentSample.sampleType}");
                    }
                    
                    // Stop monitoring since we found a change
                    isMonitoringRockData = false;
                    break;
                }
                else
                {
                    Debug.Log($"[ROCK_MONITOR] Check #{checkCount}: No change in SpecID, still {currentRockData.specId}");
                }
            }
            
            yield return new WaitForSeconds(waitTime);
            checkCount++;
        }
        
        Debug.Log("[ROCK_MONITOR] Stopped monitoring for rock data changes");
    }

    private void CompleteScan()
    {
        // Stop monitoring for rock data changes when scan is complete
        StopRockDataMonitoring();
        
        // Save scanning data to current sample
        if (currentSample != null)
        {
            // Set the sample type based on scanning results if it wasn't already set
            if (string.IsNullOrEmpty(currentSample.sampleType) || currentSample.sampleType == "Rock Sample")
            {
                currentSample.sampleType = "Rock Sample"; // Or whatever type is determined from scanning
            }
        }
        scanningPanel.SetActive(false);
        picturePanel.SetActive(true);
    }

    private void CompletePicture()
    {
        // Save picture data to current sample
        if (currentSample != null && webCamController != null)
        {
            // Get the image path from WebCamController
            string imagePath = webCamController.GetCurrentImagePath();
            currentSample.imagePath = imagePath;
        }
        picturePanel.SetActive(false);
        voicePanel.SetActive(true);
    }

    private void CompleteVoice()
    {
        // Save voice transcription to current sample
        if (currentSample != null && audioRecorder != null)
        {
            // Get the transcription from AudioRecorder
            string transcription = audioRecorder.GetCurrentTranscription();
            currentSample.voiceTranscription = transcription;
        }
        
        // Update coordinate text with EVA1 position data
        if (coordinateText != null)
        {
            Vector2 evaPosition = Vector2.zero;
            
            // Get the latest IMU data if available
            if (webSocketClient != null && WebSocketClient.LatestImuData != null && 
                WebSocketClient.LatestImuData.eva1 != null && 
                WebSocketClient.LatestImuData.eva1.position != null)
            {
                // Use the latest IMU data for EVA1
                evaPosition.x = WebSocketClient.LatestImuData.eva1.position.x;
                evaPosition.y = WebSocketClient.LatestImuData.eva1.position.y;
                
                Debug.Log($"[VOICE] Got EVA1 coordinates from IMU: ({evaPosition.x}, {evaPosition.y})");
            }
            else
            {
                // Fallback to the basic position data
                evaPosition = GetCurrentEVAPosition();
                Debug.Log($"[VOICE] Using fallback EVA1 position: ({evaPosition.x}, {evaPosition.y})");
            }
            
            // Format and display the coordinates
            coordinateText.text = $"EVA1 Coordinates: ({evaPosition.x:F2}, {evaPosition.y:F2})";
        }
        
        voicePanel.SetActive(false);
        gpsPanel.SetActive(true);
    }

    private void CompleteGps()
    {
        // Save GPS data to current sample
        if (currentSample != null)
        {
            // Get coordinates from IMU data instead of just the EVA position
            Vector2 evaPosition = Vector2.zero;
            
            // Get the latest IMU data if available
            if (webSocketClient != null && WebSocketClient.LatestImuData != null && 
                WebSocketClient.LatestImuData.eva1 != null && 
                WebSocketClient.LatestImuData.eva1.position != null)
            {
                // Use the latest IMU data for EVA1
                evaPosition.x = WebSocketClient.LatestImuData.eva1.position.x;
                evaPosition.y = WebSocketClient.LatestImuData.eva1.position.y;
                
                Debug.Log($"[GPS] Using IMU data: Position ({evaPosition.x}, {evaPosition.y})");
            }
            else
            {
                // Fallback to the basic position data
                evaPosition = GetCurrentEVAPosition();
                Debug.Log($"[GPS] Using fallback position data: ({evaPosition.x}, {evaPosition.y})");
            }
            
            // Format location string with just position (no heading)
            string locationData = $"x:{evaPosition.x},y:{evaPosition.y}";
            currentSample.location = locationData;

            // Log the sample being saved
            Debug.Log($"[GEOSAMPLE] Saving sample with location: {locationData}");
            
            // Save the complete sample to storage
            GeoSampleStorage.Instance.AddSample(currentSample);
            
            // Clear the current sample to prevent duplicates
            currentSample = null;
        }
        else
        {
            Debug.LogWarning("[GEOSAMPLE] No current sample to save GPS data to");
        }
        
        gpsPanel.SetActive(false);
        proceduresListPanel.SetActive(true);
    }

    //* ---- Starting screens----//
    // the starting screen for procedures and geo sampling are the same
    // show the task preview panel for a single procedure (ex: egress)
    // private void ShowProcedurePreview()
    // Show first panel (TasksList)
    private void ShowTasksList()
    {
        proceduresInfoPanel.SetActive(false);
        proceduresPanel.SetActive(false);
        proceduresListPanel.SetActive(true);
    }

    // Show second panel (TasksInfo) when selecting Egress
    private void ShowTasksInfo()
    {
        proceduresListPanel.SetActive(false);
        proceduresPanel.SetActive(false);
        proceduresInfoPanel.SetActive(true);
        
        // Populate steps for the selected task only
        PopulateTaskSteps();
    }

    // Show third panel (Procedures) when pressing Start
    private void ShowProcedure()
    {
        proceduresListPanel.SetActive(false);
        proceduresInfoPanel.SetActive(false);
        proceduresPanel.SetActive(true);
        
        // Initialize procedure in the procedure display with only the target task
        if (procedureDisplay != null)
        {
            // Get only the specific task instead of the whole procedure
            Procedure taskProcedure = ProcedureManager.Instance.GetProcedureTask(PROCEDURE_NAME, TARGET_TASK_NAME);
            
            if (taskProcedure != null)
            {
                // Load only this task's steps
                procedureDisplay.LoadCustomProcedure(taskProcedure);
                
                // Explicitly make the display panel active
                if (procedureDisplay.transform.Find("DisplayPanel") != null)
                    procedureDisplay.transform.Find("DisplayPanel").gameObject.SetActive(true);
                
                // Set up automation for this task
                if (procedureAutomation != null)
                {
                    procedureAutomation.SetProcedureState(PROCEDURE_NAME, TARGET_TASK_NAME, 0);
                }
                else
                {
                    Debug.LogError("ProceduresFlowManager: procedureAutomation reference is missing");
                }
            }
            else
            {
                Debug.LogError($"ProceduresFlowManager: Failed to load task '{TARGET_TASK_NAME}'");
            }
        }
        else
        {
            Debug.LogError("ProceduresFlowManager: procedureDisplay reference is missing");
        }
    }

    // Populate the steps in the TasksInfo panel
    private void PopulateTaskSteps()
    {
        if (ProcedureManager.Instance == null)
        {
            Debug.LogError("ProceduresFlowManager: ProcedureManager.Instance is null");
            return;
        }
        
        // Get only the specific task
        var taskProc = ProcedureManager.Instance.GetProcedureTask(PROCEDURE_NAME, TARGET_TASK_NAME);
        if (taskProc == null)
        {
            Debug.LogError($"ProceduresFlowManager: Task '{TARGET_TASK_NAME}' not found");
            return;
        }
        
        if (stepItemPrefab == null || stepsContainer == null)
        {
            Debug.LogError("ProceduresFlowManager: Missing prefab or container reference");
            return;
        }
        
        // Clear existing steps
        foreach (Transform child in stepsContainer) 
            Destroy(child.gameObject);
        
        // Populate steps for this task
        for (int i = 0; i < taskProc.instructionSteps.Count; i++)
        {
            var item = Instantiate(stepItemPrefab, stepsContainer);
            item.SetStep(i + 1, taskProc.instructionSteps[i].instructionText);
        }
    }

    // Method to manually verify the first step (umbilical connection)
    private void VerifyManualStep()
    {
        if (procedureAutomation != null)
        {
            procedureAutomation.ManualCompleteStep();
        }
    }

    private void ShowSampling()
    {
        proceduresListPanel.SetActive(false);
        samplingPanel.SetActive(true);
    }

    // Check for new rock data and update UI
    public void CheckForRockData()
    {
        if (rockDataText == null)
        {
            Debug.LogError("Rock data text component is not assigned in inspector");
            return;
        }
        
        if (WebSocketClient.HasNewRockData())
        {
            // Get the latest rock data and clear the flag
            RockData rockData = WebSocketClient.GetAndClearLatestRockData();
            
            // Format and display the rock data
            DisplayRockDataDetails(rockData); // Display new rock data details
            if (rockDataText != null) {
                rockDataText.text = "🆕 <color=#00FF00>NEW SAMPLE DETECTED!</color>";
            }
            
            Debug.Log($"[ROCK_CHECK] Found new unique rock data from EVA{rockData.evaId}, Sample ID: {rockData.specId}");
            
            // Store the sample ID in the current sample if available
            if (currentSample != null)
            {
                currentSample.sampleType = $"Rock Sample #{rockData.specId} (EVA {rockData.evaId})";
                Debug.Log($"[ROCK_CHECK] Updated current sample type to: {currentSample.sampleType}");
            }
        }
        else
        {
            // Show instructional text rather than old data
            if (rockDataText != null) {
                rockDataText.text = "<color=#FFCC00>Scan Rock, new data will appear once complete</color>";
            }
            DisplayRockDataDetails(null); // Clear details
            Debug.Log("[ROCK_CHECK] Waiting for new rock data...");
            
            // Start checking for new data periodically
            StartCoroutine(WaitForNewRockData());
        }
    }
    
    // Coroutine to wait for new rock data
    private IEnumerator WaitForNewRockData()
    {
        // Initial wait time
        float waitTime = 1.0f;
        int checkCount = 0;
        
        // Show waiting animation
        string[] loadingStates = new string[] { "⏳", "⌛" };
        int animationFrame = 0;
        
        // Check for new data for up to 30 seconds
        while (checkCount < 30 && rockDataText != null)
        {
            // Update the waiting animation
            animationFrame = (animationFrame + 1) % loadingStates.Length;
            if (rockDataText != null) {
                rockDataText.text = $"<color=#FFCC00>Scan Rock, new data will appear once complete</color> {loadingStates[animationFrame]} Waiting for data...";
            }
            DisplayRockDataDetails(null); // Clear details while waiting
            
            yield return new WaitForSeconds(waitTime);
            
            // Check if new data is available
            if (WebSocketClient.HasNewRockData())
            {
                // Get the latest rock data and clear the flag
                RockData rockData = WebSocketClient.GetAndClearLatestRockData();
                
                DisplayRockDataDetails(rockData); // Display new rock data details
                if (rockDataText != null) {
                    rockDataText.text = "🆕 <color=#00FF00>NEW SAMPLE DETECTED!</color>";
                }
                
                Debug.Log($"[ROCK_CHECK] Found new unique rock data while waiting: EVA{rockData.evaId}, Sample ID: {rockData.specId}");
                
                // Store the sample ID in the current sample if available
                if (currentSample != null)
                {
                    currentSample.sampleType = $"Rock Sample #{rockData.specId} (EVA {rockData.evaId})";
                    Debug.Log($"[ROCK_CHECK] Updated current sample type to: {currentSample.sampleType}");
                }
                
                // New data found, exit the coroutine
                yield break;
            }
            
            checkCount++;
        }
        
        // If we exit the loop without finding new data, show a timeout message
        if (rockDataText != null)
        {
            rockDataText.text = "<color=#FF6666>No new rock data detected. Try scanning again.</color>";
            DisplayRockDataDetails(null); // Clear details on timeout
            Debug.Log("[ROCK_CHECK] Timed out waiting for new rock data");
        }
    }

    private void DisplayRockDataDetails(RockData data)
    {
        string placeholder = "---";
        if (data != null)
        {
            if (rockEvaIdText != null) rockEvaIdText.text = $"EVA {data.evaId}";
            if (rockSpecIdText != null) rockSpecIdText.text = $"Sample ID: {data.specId}";
            if (rockNameText != null) rockNameText.text = data.name ?? placeholder;

            if (data.composition != null)
            {
                if (rockSiO2Text != null) rockSiO2Text.text = $"{data.composition.SiO2:F2}%";
                if (rockAl2O3Text != null) rockAl2O3Text.text = $"{data.composition.Al2O3:F2}%";
                if (rockMnOText != null) rockMnOText.text = $"{data.composition.MnO:F2}%";
                if (rockCaOText != null) rockCaOText.text = $"{data.composition.CaO:F2}%";
                if (rockP2O3Text != null) rockP2O3Text.text = $"{data.composition.P2O3:F2}%";
                if (rockTiO2Text != null) rockTiO2Text.text = $"{data.composition.TiO2:F2}%";
                if (rockFeOText != null) rockFeOText.text = $"{data.composition.FeO:F2}%";
                if (rockMgOText != null) rockMgOText.text = $"{data.composition.MgO:F2}%";
                if (rockK2OText != null) rockK2OText.text = $"{data.composition.K2O:F2}%";
                if (rockOtherText != null) rockOtherText.text = $"{data.composition.Other:F2}%";
            }
            else
            {
                // If composition is null, set oxide fields to placeholder
                if (rockSiO2Text != null) rockSiO2Text.text = placeholder;
                if (rockAl2O3Text != null) rockAl2O3Text.text = placeholder;
                if (rockMnOText != null) rockMnOText.text = placeholder;
                if (rockCaOText != null) rockCaOText.text = placeholder;
                if (rockP2O3Text != null) rockP2O3Text.text = placeholder;
                if (rockTiO2Text != null) rockTiO2Text.text = placeholder;
                if (rockFeOText != null) rockFeOText.text = placeholder;
                if (rockMgOText != null) rockMgOText.text = placeholder;
                if (rockK2OText != null) rockK2OText.text = placeholder;
                if (rockOtherText != null) rockOtherText.text = placeholder;
            }
        }
        else
        {
            if (rockEvaIdText != null) rockEvaIdText.text = placeholder;
            if (rockSpecIdText != null) rockSpecIdText.text = placeholder;
            if (rockNameText != null) rockNameText.text = placeholder;
            // Set all composition fields to placeholder if data is null
            if (rockSiO2Text != null) rockSiO2Text.text = placeholder;
            if (rockAl2O3Text != null) rockAl2O3Text.text = placeholder;
            if (rockMnOText != null) rockMnOText.text = placeholder;
            if (rockCaOText != null) rockCaOText.text = placeholder;
            if (rockP2O3Text != null) rockP2O3Text.text = placeholder;
            if (rockTiO2Text != null) rockTiO2Text.text = placeholder;
            if (rockFeOText != null) rockFeOText.text = placeholder;
            if (rockMgOText != null) rockMgOText.text = placeholder;
            if (rockK2OText != null) rockK2OText.text = placeholder;
            if (rockOtherText != null) rockOtherText.text = placeholder;
        }
    }
} 