//* manages the overall geosampling process
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using System;

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
        Start,  
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

    // Data structure to hold sample information
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
        if (photoButton != null)
            photoButton.onClick.AddListener(() => SetState(GeosamplingState.Photo));
            
        if (recordingButton != null)
            recordingButton.onClick.AddListener(() => SetState(GeosamplingState.VoiceMemo));
            
        if (confirmButton != null)
            confirmButton.onClick.AddListener(ConfirmSample);
    }
    
    private void OnDestroy() {
        // Remove button listeners
        if (photoButton != null)
            photoButton.onClick.RemoveListener(() => SetState(GeosamplingState.Photo));
            
        if (recordingButton != null)
            recordingButton.onClick.RemoveListener(() => SetState(GeosamplingState.VoiceMemo));
            
        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(ConfirmSample);
    }
    
    // set the state when some signal is received
    // ex: when the user clicks the photo button, we will switch to the photo panel and change the state
    private void SetState(GeosamplingState newState) {
        currentState = newState; 

        // hide all panels before showing the new one
        startDisplay.SetActive(false);
        scanRockPanel.SetActive(false);
        captureOptionsPanel.SetActive(false);
        photoPanel.SetActive(false);
        if (audioRecorder != null && audioRecorder.gameObject != null)
            audioRecorder.gameObject.SetActive(false);
        sampleInfoPanel.SetActive(false);
        confirmationPanel.SetActive(false);

        // show appropriate panel based on the new state
        switch (currentState) {
            case GeosamplingState.Start:
                startDisplay.SetActive(true);
                break;
            case GeosamplingState.ScanRock:
                scanRockPanel.SetActive(true);
                break;
            case GeosamplingState.CaptureOptions:
                captureOptionsPanel.SetActive(true);
                break;
            case GeosamplingState.Photo:
                photoPanel.SetActive(true);
                break;
            case GeosamplingState.VoiceMemo:
                if (audioRecorder != null && audioRecorder.gameObject != null) {
                    audioRecorder.gameObject.SetActive(true);
                    // Check if ShowVoiceMemoPanel method exists
                    // If not, you'll need to add it to AudioRecorder.cs
                }
                break;
            case GeosamplingState.SampleInfo:
                sampleInfoPanel.SetActive(true);
                UpdateSampleInfoDisplay();
                break;
            case GeosamplingState.Confirmation:
                confirmationPanel.SetActive(true);
                DisplaySampleSummary();
                break;
        }
    }

    // Add methods to handle state transitions without next/back buttons
    public void OnScanComplete() {
        SetState(GeosamplingState.CaptureOptions);
    }

    public void OnPhotoComplete() {
        // Store photo path
        // currentSample.photoPath = ... (get from photo capture component)
        SetState(GeosamplingState.CaptureOptions);
    }

    public void OnVoiceMemoComplete() {
        // Get transcription from AudioRecorder
        if (audioRecorder != null) {
            currentSample.transcription = audioRecorder.GetCurrentTranscription();
        }
        SetState(GeosamplingState.CaptureOptions);
    }

    public void OnCaptureOptionsComplete() {
        SetState(GeosamplingState.SampleInfo);
    }

    // private void SaveSampleData() {
    //     // sample name and location will be sent by the tss
    //     if (sampleNameInput != null && sampleNameInput.text != null) {
    //         currentSample.sampleName = sampleNameInput.text;
    //         currentSample.location = sampleLocationInput.text;
    //     }
    //     currentSample.timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        
    // }

    // change the sample info after a scan
    private void UpdateSampleInfoDisplay() {
        if (sampleNameInput != null && !string.IsNullOrEmpty(currentSample.sampleName)) {
            sampleNameInput.text = currentSample.sampleName;
        }
        
        if (sampleLocationInput != null && !string.IsNullOrEmpty(currentSample.location)) {
            sampleLocationInput.text = currentSample.location;
        }
    }

    // display sample info on the ui
    private void DisplaySampleSummary() {
        // get all the text elements in the confirmation panel 
        TextMeshProUGUI[] textElements = confirmationPanel.GetComponentsInChildren<TextMeshProUGUI>();
        // update the text elements with the current sample data
        foreach (var text in textElements) {
            if (text.name.Contains("SampleName")) {
                text.text = currentSample.sampleName;
            }
            else if (text.name.Contains("Location")) {
                text.text = currentSample.location;
            }
            else if (text.name.Contains("Timestamp")) {
                text.text = currentSample.timestamp;
            }
            else if (text.name.Contains("Transcription")) {
                text.text = currentSample.transcription;
            }
        }
    }

    // confirm each sample one by one to save in the database
    // ? will TSS auto update this when the rock is scanned? 
    private void ConfirmSample() {
        // Reset and return to start
        ResetSample();
        SetState(GeosamplingState.Start);
    }
    
    // Reset the current sample data to an empty sample
    private void ResetSample() {
        currentSample = new SampleData();
    }
}