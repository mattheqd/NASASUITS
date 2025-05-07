//* Script for voice memo during geosampling. 
//* handles recording, transcription, and waveform visualization
//* the api picks up the microphone input (ex: built in mic on glasses vs laptop)
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;
using TMPro;

public class AudioRecorder : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button recordButton;
    [SerializeField] private Button playButton;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Recording Settings")]
    [SerializeField] private int sampleRate = 44100;
    [SerializeField] private int maxRecordingTime = 300; // 5 minutes in seconds

    private AudioClip recording;
    private bool isRecording = false;
    private string microphoneDevice;
    private float startRecordingTime;
    private AudioSource audioSource;

    private void Awake()
    {
        // Get the default microphone
        if (Microphone.devices.Length > 0)
        {
            microphoneDevice = Microphone.devices[0];
            Debug.Log($"Using microphone: {microphoneDevice}");
        }
        else
        {
            Debug.LogError("No microphone found!");
        }

        // Setup audio source for playback
        audioSource = gameObject.AddComponent<AudioSource>();

        // Setup button listeners
        if (recordButton != null)
            recordButton.onClick.AddListener(ToggleRecording);
        
        if (playButton != null)
            playButton.onClick.AddListener(PlayRecording);
    }

    private void OnDestroy()
    {
        if (recordButton != null)
            recordButton.onClick.RemoveListener(ToggleRecording);
        
        if (playButton != null)
            playButton.onClick.RemoveListener(PlayRecording);
    }

    private void ToggleRecording()
    {
        if (!isRecording)
        {
            StartRecording();
        }
        else
        {
            StopRecording();
        }
    }

    private void StartRecording()
    {
        if (string.IsNullOrEmpty(microphoneDevice))
        {
            UpdateStatus("No microphone available!");
            return;
        }

        // Start recording
        recording = Microphone.Start(microphoneDevice, false, maxRecordingTime, sampleRate);
        isRecording = true;
        startRecordingTime = Time.time;

        // Update UI
        if (recordButton != null)
            recordButton.GetComponentInChildren<TextMeshProUGUI>().text = "Stop Recording";
        
        UpdateStatus("Recording...");
    }

    private void StopRecording()
    {
        if (!isRecording) return;

        // Stop recording
        Microphone.End(microphoneDevice);
        isRecording = false;

        // Trim the recording to actual length
        float recordingLength = Time.time - startRecordingTime;
        AudioClip trimmedClip = AudioClip.Create("Recording", 
            (int)(recordingLength * sampleRate), 
            1, 
            sampleRate, 
            false);

        float[] samples = new float[(int)(recordingLength * sampleRate)];
        recording.GetData(samples, 0);
        trimmedClip.SetData(samples, 0);

        recording = trimmedClip;

        // Update UI
        if (recordButton != null)
            recordButton.GetComponentInChildren<TextMeshProUGUI>().text = "Start Recording";
        
        UpdateStatus("Recording saved!");
    }

    private void PlayRecording()
    {
        if (recording == null)
        {
            UpdateStatus("No recording available!");
            return;
        }

        if (audioSource.isPlaying)
        {
            audioSource.Stop();
            UpdateStatus("Playback stopped");
        }
        else
        {
            audioSource.clip = recording;
            audioSource.Play();
            UpdateStatus("Playing recording...");
        }
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
        
        Debug.Log(message);
    }

    private void Update()
    {
        // Update recording time display
        if (isRecording && statusText != null)
        {
            float recordingTime = Time.time - startRecordingTime;
            statusText.text = $"Recording... {recordingTime:F1}s";
        }
    }
} 