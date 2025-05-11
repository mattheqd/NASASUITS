//* manages the overall geosampling process
using UnityEngine;
using System.Collections.Generic;

public class GeosamplingManager : MonoBehaviour {
    [SerializeField] private GameObject startDisplay; // starting panel with notification and instructions preview
    [SerializeField] private GameObject scanRockPanel; // scan rock panel
    [SerializeField] private GameObject captureOptionsPanel; // options panel (take picture vs take notes)
    [SerializeField] private GameObject photoPanel; // photo panel
    [SerializeField] private AudioRecorder audioRecorder; // page for audio recording
    [SerializeField] private GameObject sampleInfoPanel; //information about sample from tss
    [SerializeField] private GameObject confirmationPanel; // confirm sample info

    [Header("Navigation Buttons")]
    // [SerializeField] private Button backButton; 
    // [SerializeField] private Button nextButton;
    [SerializeField] private Button photoButton;
    [SerializeField] private Button recordingButton;
    [SerializeField] private Button confirmButton;
    
    [Header("Sample Information Fields")]
    [SerializeField] private TMP_InputField sampleNameInput;  //? this will be sent by the tss
    [SerializeField] private TMP_InputField sampleLocationInput;  //? this will be sent by the tss
    [SerializeField] private TextMeshProUGUI sampleTimestampText; //? this will be sent by the tss
    
    // store the current state (i.e. which panel or rock is being displayed)
    private enum GeosamplingState {
        Procedure,
        ScanRock,
        CaptureOptions,
        Photo,
        VoiceMemo,
        SampleInfo,
        Confirmation
    }

    // create a new state and sample for each rock collected
    // this will be created when the user scans a rock
    private GeosamplingState currentState = GeosamplingState.Start;

    / Data structure to hold sample information
    [Serializable]
    private class SampleData {
        public string sampleName;
        public string location;
        public string timestamp;
        public string photoPath;
        public string transcription;
    }
    private SampleData currentSample = new SampleData();

    private void Start() {
        SetState(GeosamplingState.Start); // initialize start display when the scene starts

        // Set up button listeners
        if (backButton != null)
            backButton.onClick.AddListener(GoBack);
            
        if (nextButton != null)
            nextButton.onClick.AddListener(GoNext);
            
        if (photoButton != null)
            photoButton.onClick.AddListener(() => SetState(GeosamplingState.Photo));
            
        if (recordingButton != null)
            recordingButton.onClick.AddListener(() => SetState(GeosamplingState.VoiceMemo));
            
        if (confirmButton != null)
            confirmButton.onClick.AddListener(ConfirmSample);
    }
    private void OnDestroy() {
        // Remove button listeners
        if (backButton != null)
            backButton.onClick.RemoveListener(GoBack);
            
        if (nextButton != null)
            nextButton.onClick.RemoveListener(GoNext);
            
        if (photoButton != null)
            photoButton.onClick.RemoveListener(() => SetState(GeosamplingState.Photo));
            
        if (recordingButton != null)
            recordingButton.onClick.RemoveListener(() => SetState(GeosamplingState.VoiceMemo));
            
        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(ConfirmSample);
    }
}