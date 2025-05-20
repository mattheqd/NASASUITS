using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;
using ProcedureSystem;
using TMPro;
using System.Collections;
using GeoSampling;

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
    [SerializeField] private GameObject sampleDetailsPanel;    // Final screen - Sample Details in hierarchy

    [Header("UI Elements")]
    [SerializeField] private Button egressButton;          // Button to go from TasksList to TasksInfo
    [SerializeField] private Button samplingButton;   
    [SerializeField] private Button ingressButton; // Added for Ingress procedure
    [SerializeField] private Button samplingStart; 
    [SerializeField] private Button backButton;            // Button to go back from TasksInfo to TasksList
    [SerializeField] private Button startButton;           // Button to go from TasksInfo to Procedures
    [SerializeField] private Button verifyManuallyButton;  // Button to manually verify umbilical connection
    [SerializeField] private Button completeScanning;  // Button to manually verify umbilical connection
    [SerializeField] private Button completePicture;  // Button to manually verify umbilical connection
    [SerializeField] private Button completeVoice;  // Button to manually verify umbilical connection
    [SerializeField] private Button completeGps;  // Button to manually verify umbilical connection
    [SerializeField] private Button checkRockDataButton; // Button to check rock data
    [SerializeField] private Button scanAgainButton; // Button to start a new scan
    [SerializeField] private Button finishButton; // Button to return to procedures list

    [Header("Component References")]
    [SerializeField] private ProcedureDisplay procedureDisplay; // Main procedure handler
    //[SerializeField] private ProcedureAutomation procedureAutomation; // Handles automation of steps
    [SerializeField] private WebCamController webCamController; // Reference to camera controller
    [SerializeField] private AudioRecorder audioRecorder; // Reference to audio recorder
    [SerializeField] private TextMeshProUGUI coordinateText; // Text to display coordinates
    [SerializeField] private TextMeshProUGUI rockDataText; // Text to display rock data
    
    [Header("Rock Data UI Fields")]
    [SerializeField] private GameObject compositionDataPanel; // Master panel containing all composition data
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

    [Header("Sample Details")]
    [SerializeField] private GameObject sampleInfoPrefab; // Prefab for displaying sample info
    private SampleDetailsPanel currentSampleInfo; // Reference to the current sample info instance

    [Header("WebSocket")]
    [SerializeField] private WebSocketClient webSocketClient;

    [Header("Procedure Data")]
    [SerializeField] private ProcedureLoader procedureLoader;
    private Procedure procedureData;

    // Target task name for this MVP
    private const string EGRESS_PROCEDURE_NAME = "EVA Egress"; // Renamed for clarity
    private const string INGRESS_PROCEDURE_NAME = "EVA Ingress"; // Added
    private string currentProcedureNameToStart; // Added to track which procedure to start from info panel
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

        // Find ProcedureLoader if not set in inspector
        if (procedureLoader == null)
            procedureLoader = FindObjectOfType<ProcedureLoader>();

        // Ensure onProcedureCompleted listener is only added once
        if (procedureDisplay != null)
        {
            procedureDisplay.onProcedureCompleted.RemoveListener(ShowTasksList);
            procedureDisplay.onProcedureCompleted.AddListener(ShowTasksList);
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
        sampleDetailsPanel.SetActive(false);
        
        // Remove all existing listeners first to prevent duplicates
        egressButton.onClick.RemoveAllListeners();
        backButton.onClick.RemoveAllListeners();
        startButton.onClick.RemoveAllListeners();
        samplingButton.onClick.RemoveAllListeners();
        samplingStart.onClick.RemoveAllListeners();
        completeScanning.onClick.RemoveAllListeners();
        completePicture.onClick.RemoveAllListeners();
        completeVoice.onClick.RemoveAllListeners();
        completeGps.onClick.RemoveAllListeners();
        scanAgainButton.onClick.RemoveAllListeners();
        finishButton.onClick.RemoveAllListeners();
        
        if (ingressButton != null)
        {
            ingressButton.onClick.RemoveAllListeners();
        }
        
        if (checkRockDataButton != null)
        {
            checkRockDataButton.onClick.RemoveAllListeners();
        }
        
        if (verifyManuallyButton != null)
        {
            verifyManuallyButton.onClick.RemoveAllListeners();
        }
        
        // Add listeners after removing all existing ones
        egressButton.onClick.AddListener(ShowTasksInfo);
        backButton.onClick.AddListener(ShowTasksList);
        startButton.onClick.AddListener(() => ShowProcedure(currentProcedureNameToStart));
        samplingButton.onClick.AddListener(ShowSampling);
        samplingStart.onClick.AddListener(StartScan);
        completeScanning.onClick.AddListener(CompleteScan);
        completePicture.onClick.AddListener(CompletePicture);
        completeVoice.onClick.AddListener(CompleteVoice);
        completeGps.onClick.AddListener(CompleteGps);
        scanAgainButton.onClick.AddListener(StartNewScan);
        finishButton.onClick.AddListener(FinishSampling);

        
        if (ingressButton != null)
        {
            ingressButton.onClick.AddListener(ShowIngressProcedureInfo);
        }
        
        if (checkRockDataButton != null)
        {
            checkRockDataButton.onClick.AddListener(CheckForRockData);
        }
        
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
    private void StartNewScan()
    {
        sampleDetailsPanel.SetActive(false);
        StartScan();
    }

    private void FinishSampling()
    {
        sampleDetailsPanel.SetActive(false);
        proceduresListPanel.SetActive(true);
    }

    private void StartScan()
    {
        // Create a new sample when starting the geosampling process
        currentSample = GeoSampleData.CreateNew("", "", "");
        
        // Capture the current rock data as our baseline for comparison
        CaptureBaselineRockData();
        
        // Start monitoring for rock data changes
        StartRockDataMonitoring();
        
        // Hide composition data panel initially
        if (compositionDataPanel != null) compositionDataPanel.SetActive(false);
        
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
                rockDataText.text = "<color=#FFCC00>Scan a rock sample</color>";
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
                rockDataText.text = $"<color=#FFCC00>Scan a rock sample</color> {loadingStates[animationFrame]}";
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
                    
                    // Update the baseline to the new data to prevent multiple detections
                    baselineRockData = currentRockData;
                    
                    // Show analyzing message
                    if (rockDataText != null)
                    {
                        rockDataText.text = "<color=#FFCC00>Analyzing Sample...</color>";
                    }
                    
                    // Wait for 5 seconds to show analyzing message
                    yield return new WaitForSeconds(3f);
                    
                    // Display the new rock data details
                    DisplayRockDataDetails(currentRockData);
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
            // Get the latest rock data
            RockData currentRockData = WebSocketClient.GetRockDataForEva(currentEvaId);
            if (currentRockData != null && currentRockData.composition != null)
            {
                // Format the rock name: remove underscores and capitalize first letters
                string formattedName = string.Join(" ", 
                    currentRockData.name.Split('_')
                        .Select(word => char.ToUpper(word[0]) + word.Substring(1).ToLower())
                );
                currentSample.sampleType = formattedName;

                // Save rock composition data
                currentSample.rockComposition = new RockComposition
                {
                    SiO2 = currentRockData.composition.SiO2,
                    Al2O3 = currentRockData.composition.Al2O3,
                    MnO = currentRockData.composition.MnO,
                    CaO = currentRockData.composition.CaO,
                    P2O3 = currentRockData.composition.P2O3,
                    TiO2 = currentRockData.composition.TiO2,
                    FeO = currentRockData.composition.FeO,
                    MgO = currentRockData.composition.MgO,
                    K2O = currentRockData.composition.K2O,
                    Other = currentRockData.composition.Other,
                    rockName = currentRockData.name
                };
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
            
            // Show the sample details panel
            gpsPanel.SetActive(false);
            sampleDetailsPanel.SetActive(true);

            // Create and display the sample info
            if (sampleInfoPrefab != null)
            {
                // Destroy any existing sample info
                if (currentSampleInfo != null)
                {
                    Destroy(currentSampleInfo.gameObject);
                }

                // Create new sample info
                GameObject sampleInfoObj = Instantiate(sampleInfoPrefab, sampleDetailsPanel.transform);
                currentSampleInfo = sampleInfoObj.GetComponent<SampleDetailsPanel>();
                
                if (currentSampleInfo != null)
                {
                    currentSampleInfo.ShowSampleDetails(currentSample);
                }
            }
            
            // Clear the current sample to prevent duplicates
            currentSample = null;
        }
        else
        {
            Debug.LogWarning("[GEOSAMPLE] No current sample to save GPS data to");
            gpsPanel.SetActive(false);
            proceduresListPanel.SetActive(true);
        }
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
        currentProcedureNameToStart = EGRESS_PROCEDURE_NAME; // Set for Egress
        // proceduresInfoPanel is where the procedure title/description would be shown before starting.
        // For now, we are keeping a generic info panel. If specific info per procedure is needed, this logic would change.
        proceduresInfoPanel.SetActive(true); 
        // todo: Potentially update a title text on proceduresInfoPanel to "EVA Egress"
    }

    // Added method to show info for Ingress procedure
    private void ShowIngressProcedureInfo()
    {
        proceduresListPanel.SetActive(false);
        proceduresPanel.SetActive(false);
        currentProcedureNameToStart = INGRESS_PROCEDURE_NAME; // Set for Ingress
        // Assuming the same generic info panel is used. Update if Ingress has a unique info screen.
        proceduresInfoPanel.SetActive(true); 
        // todo: Potentially update a title text on proceduresInfoPanel to "EVA Ingress"
    }

    // Show third panel (Procedures) when pressing Start
    private void ShowProcedure(string procedureName)
    {
        Debug.Log($"ShowProcedure called for: {procedureName}");
        proceduresListPanel.SetActive(false);
        proceduresInfoPanel.SetActive(false);
        proceduresPanel.SetActive(true);

        if (procedureLoader != null)
            Debug.Log($"ProcedureLoader found, LoadedProcedures count: {procedureLoader.LoadedProcedures.Count}");
        else
            Debug.LogError("ProcedureLoader is null!");

        Procedure selectedProcedure = null;
        if (procedureLoader != null && procedureLoader.LoadedProcedures.Count > 0)
        {
            selectedProcedure = procedureLoader.LoadedProcedures.FirstOrDefault(p => p.procedureName == procedureName);
            if (selectedProcedure != null)
            {
                Debug.Log($"Selected procedure: {selectedProcedure.procedureName}, tasks: {selectedProcedure.tasks?.Count}");
            }
            else
            {
                Debug.LogError($"Procedure named '{procedureName}' not found in ProcedureLoader!");
                ShowTasksList(); // Go back to list if procedure not found
                return;
            }
        }
        else
        {
            Debug.LogError("No procedures loaded in ProcedureLoader!");
            ShowTasksList(); // Go back to list
            return;
        }

        if (procedureDisplay != null && selectedProcedure != null)
        {
            // Remove any existing listeners before loading the new procedure
            if (procedureDisplay.onProcedureCompleted != null)
            {
                procedureDisplay.onProcedureCompleted.RemoveAllListeners();
                procedureDisplay.onProcedureCompleted.AddListener(ShowTasksList);
            }
            
            Debug.Log($"Calling LoadProcedure for: {selectedProcedure.procedureName}");
            procedureDisplay.LoadProcedure(selectedProcedure);
        }
        else
        {
            Debug.LogError("ProcedureDisplay or selectedProcedure is missing");
            ShowTasksList(); // Go back to list
        }
    }

    // Method to manually verify the first step (umbilical connection)
    private void VerifyManualStep()
    {
        if (procedureDisplay != null)
        {
            // Mark the current step as complete in ProcedureDisplay
            procedureDisplay.NextStep();
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
                rockDataText.text = "<color=#FFCC00>Start scanning for rock samples</color>";
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
                // Show composition data panel
                if (compositionDataPanel != null) compositionDataPanel.SetActive(true);
                
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
                // Hide composition data panel if no composition data
                if (compositionDataPanel != null) compositionDataPanel.SetActive(false);
            }
        }
        else
        {
            if (rockEvaIdText != null) rockEvaIdText.text = placeholder;
            if (rockSpecIdText != null) rockSpecIdText.text = placeholder;
            if (rockNameText != null) rockNameText.text = placeholder;
            // Hide composition data panel if no data
            if (compositionDataPanel != null) compositionDataPanel.SetActive(false);
        }
    }
} 