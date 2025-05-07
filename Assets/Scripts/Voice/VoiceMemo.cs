//* Script for voice memo during geosampling. 
//* handles recording, transcription, and waveform visualization
//* the api picks up the microphone input (ex: built in mic on glasses vs laptop)

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Windows.Speech; // For Windows speech recognition
using System.Linq;
using System.Collections.Generic;

public class VoiceMemoRecorder : MonoBehaviour {
    [Header("UI References")]
    [SerializeField] private GameObject voiceMemoPanel; // empty gameobject to store voice memo components
    [SerializeField] private TextMeshProUGUI transcriptionText; // test of transcribed audio
    [SerializeField] private Button actionButton; // button to start/stop recording
    [SerializeField] private Image microphoneIcon; // static icon
    [SerializeField] private GameObject waveformVisualizer; // visualizer when person talks

    [Header("Audio Settings")]
    [SerializeField] private int recordingDurationSeconds = 30; // recording duration
    [SerializeField] private int sampleRate = 16000; // audio is sample at 16000 Hz (samples per second)
    [SerializeField] private string microphoneName = null; // default microphone

    // Speech recognition components
    private DictationRecognizer dictationRecognizer; // listens to speech input from microphone and transcribes it
    private bool isRecording = false;
    private AudioClip recordingClip; // audio clip to store the recording

    // waveform
    private RectTransform[] waveformBars; // array of wave bars
    private Coroutine waveformCoroutine; // updates wave visualizer

    // Activate/deactivate voice memo actions
    private void Awake() {
        if (voiceMemoPanel != null)
            voiceMemoPanel.SetActive(false);
        if (actionButton !=null)
            actionButton.onClick.AddListener(ToggleRecording);
    }
    // Remove event listeners when destroyed
    private void OnDestroy() {
        // Clean up resources
        if (dictationRecognizer != null)
        {
            dictationRecognizer.DictationResult -= OnDictationResult;
            dictationRecognizer.DictationComplete -= OnDictationComplete;
            dictationRecognizer.DictationError -= OnDictationError;
            dictationRecognizer.Dispose();
        }
        
        if (actionButton != null)
            actionButton.onClick.RemoveListener(ToggleRecording);
    }

    // show panel
    public void ShowVoiceMemoPanel() {
        if (transcriptionText != null)
            transcriptionText.text = "";
        if (voiceMemoPanel != null)
        voiceMemoPanel.SetActive(true);
        if (waveformVisualizer != null)
            waveformVisualizer.SetActive(false);
    }

    // toggle recording
    public void ToggleRecording() {
        if (!Recording)
            StartRecording();
        else
            StopRecording();
    }

    private void StartRecording()
    {
        // Initialize dictation recognizer if needed
        if (dictationRecognizer == null)
        {
            dictationRecognizer = new DictationRecognizer();
            dictationRecognizer.DictationResult += OnDictationResult;
            dictationRecognizer.DictationComplete += OnDictationComplete;
            dictationRecognizer.DictationError += OnDictationError;
        }
        
        // Start recording audio
        recordedClip = Microphone.Start(microphoneName, false, recordingDurationSeconds, sampleRate);
        
        // Start dictation recognition
        dictationRecognizer.Start();
        
        // Update UI
        isRecording = true;
        if (waveformVisualizer != null)
            waveformVisualizer.SetActive(true);
            
        // Change button text to "Done" if needed
        if (actionButton != null && actionButton.GetComponentInChildren<TextMeshProUGUI>() != null)
            actionButton.GetComponentInChildren<TextMeshProUGUI>().text = "Done";
    }
    
    private void StopRecording()
    {
        // Stop recording audio
        Microphone.End(microphoneName);
        
        // Stop dictation recognition
        dictationRecognizer.Stop();
        
        // Update UI
        isRecording = false;
        if (waveformVisualizer != null)
            waveformVisualizer.SetActive(false);
            
        // Hide panel after recording
        if (voiceMemoPanel != null)
            voiceMemoPanel.SetActive(false);
            
        // save memo to a database
        SaveMemo(transcriptionText.text);
    }
    
    private void SaveMemo(string memoText)
    {
        // TODO: save memo to a database
    }
    
    // Returns the transcription result and confidence level
    private void OnDictationResult(string text, ConfidenceLevel confidence)
    {
        if (transcriptionText != null)
            transcriptionText.text += text + " ";
    }
    
    private void OnDictationError(string error, int hresult)
    {
        Debug.LogError("Dictation error: " + error);
    }
}