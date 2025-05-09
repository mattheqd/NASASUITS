//* Script for voice memo during geosampling. 
//* handles recording, transcription, and waveform visualization
//* the api picks up the microphone input (ex: built in mic on glasses vs laptop)
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;
using TMPro;
using System;
using System.Collections.Generic;

public class AudioRecorder : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button recordButton;
    [SerializeField] private Button playButton;
    [SerializeField] private TextMeshProUGUI statusText; // display the recording status (recording, not recording, etc)
    [SerializeField] private GameObject voiceMemoPanel; // empty panel to contain voice features
    [SerializeField] private TextMeshProUGUI transcriptionText;
    [SerializeField] private GameObject waveformVisualizer;

    [Header("Recording Settings")]
    [SerializeField] private int sampleRate = 44100;
    [SerializeField] private int maxRecordingTime = 300; // 5 minutes in seconds
    
    [Header("Waveform Visualization")]
    [SerializeField] private int waveformBarCount = 10;
    [SerializeField] private float waveformUpdateInterval = 0.05f;
    [SerializeField] private float waveformAmplitudeMultiplier = 50f;
    [SerializeField] private float waveformMinHeight = 5f;
    [SerializeField] private float waveformMaxHeight = 50f;

    [Header("WebSocket Settings")]
    [SerializeField] private bool useWebSocketTranscription = true;
    [SerializeField] private float audioChunkDuration = 1.0f; // Send audio in 1-second chunks

    private AudioClip recording;
    private bool isRecording = false;
    private string microphoneDevice;
    private float startRecordingTime;
    private AudioSource audioSource;
    private string currentTranscription = "";
    
    // Path to save recordings and transcriptions
    private string transcriptsFolderPath;
    private string transcriptionFilePath;

    // Waveform visualization
    private RectTransform[] waveformBars;
    private Coroutine waveformCoroutine;
    
    // WebSocket streaming
    private Coroutine streamingCoroutine;
    private string currentSessionId;

    //----- Initialize/Destructor Functions -------
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
            
        // Initialize UI
        if (voiceMemoPanel != null)
        {
            // Make sure panel is visible when active
            Image panelImage = voiceMemoPanel.GetComponent<Image>();
            voiceMemoPanel.SetActive(true);
        }
            
        // Initialize waveform bars
        InitializeWaveformBars();
        
        // subscribe to websocket client to get transcription results
        // transcript is sent to a function that updates the transcription text on the UI
        if (WebSocketClient.Instance != null)
            WebSocketClient.Instance.Subscribe("transcription", OnTranscriptionReceived);
    }

    private void OnDestroy()
    {
        if (recordButton != null)
            recordButton.onClick.RemoveListener(ToggleRecording);
        
        if (playButton != null)
            playButton.onClick.RemoveListener(PlayRecording);
            
        // Stop coroutine if running
        if (waveformCoroutine != null)
            StopCoroutine(waveformCoroutine);
    }
    
    // ----- Interface Functions -----
    private void InitializeWaveformBars()
    {
        if (waveformVisualizer == null) return;
        
        // Clear any existing children
        foreach (Transform child in waveformVisualizer.transform)
        {
            Destroy(child.gameObject);
        }
        
        // Create new bars
        waveformBars = new RectTransform[waveformBarCount];
        float barWidth = waveformVisualizer.GetComponent<RectTransform>().rect.width / waveformBarCount;
        
        for (int i = 0; i < waveformBarCount; i++)
        {
            GameObject barObj = new GameObject($"WaveformBar_{i}");
            barObj.transform.SetParent(waveformVisualizer.transform, false);
            
            Image barImage = barObj.AddComponent<Image>();
            barImage.color = Color.white;
            
            RectTransform rectTransform = barObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 0.5f);
            rectTransform.anchorMax = new Vector2(0, 0.5f);
            rectTransform.sizeDelta = new Vector2(barWidth * 0.8f, waveformMinHeight);
            rectTransform.anchoredPosition = new Vector2(i * barWidth + barWidth * 0.5f, 0);
            
            waveformBars[i] = rectTransform;
        }
        
        // Initially hide the waveform
        waveformVisualizer.SetActive(false);
    }

    public void ShowVoiceMemoPanel()
    {
        if (transcriptionText != null)
            transcriptionText.text = "";
            
        if (voiceMemoPanel != null)
        {
            voiceMemoPanel.SetActive(true);
            
            // Make sure panel is visible
            Image panelImage = voiceMemoPanel.GetComponent<Image>();
            if (panelImage != null)
            {
                Color color = panelImage.color;
                if (color.a < 0.5f)
                {
                    color.a = 0.8f;
                    panelImage.color = color;
                }
            }
        }
            
        if (waveformVisualizer != null)
            waveformVisualizer.SetActive(false);
            
        currentTranscription = "";
        
        UpdateStatus("Ready to record");
    }

    // ----- Recording Functions -----
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

        // Clear previous transcription
        currentTranscription = "";
        if (transcriptionText != null)
            transcriptionText.text = "";

        // Start recording by streaming audio to the server
        recording = Microphone.Start(microphoneDevice, false, maxRecordingTime, sampleRate);
        isRecording = true;
        startRecordingTime = Time.time;
        
        // Update UI
        if (recordButton != null && recordButton.GetComponentInChildren<TextMeshProUGUI>() != null)
            recordButton.GetComponentInChildren<TextMeshProUGUI>().text = "Stop Recording";
        
        // Show waveform visualizer
        if (waveformVisualizer != null)
            waveformVisualizer.SetActive(true);
            
        // Start waveform visualization
        // destroy any existing waveform coroutine
        if (waveformCoroutine != null)
            StopCoroutine(waveformCoroutine);
        waveformCoroutine = StartCoroutine(VisualizeWaveform());
        
        UpdateStatus("Recording...");
    }

    private void StopRecording()
    {
        if (!isRecording) return;

        // Stop recording
        Microphone.End(microphoneDevice);
        isRecording = false;

        // Only process recording if we have one
        if (recording != null) {
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
        }

        // Update UI
        if (recordButton != null && recordButton.GetComponentInChildren<TextMeshProUGUI>() != null)
            recordButton.GetComponentInChildren<TextMeshProUGUI>().text = "Start Recording";
            
        // Hide waveform visualizer
        if (waveformVisualizer != null)
            waveformVisualizer.SetActive(false);
            
        // Stop waveform visualization
        if (waveformCoroutine != null)
        {
            StopCoroutine(waveformCoroutine);
            waveformCoroutine = null;
        }
        
        // stop audio streaming
        if (streamingCoroutine != null) {
            StopCoroutine(streamingCoroutine); // stops the streaming function
            streamingCoroutine = null; // clear anything inside the streaming coroutine

            // notify the server when the transcription session is over
            if (WebSocketClient.Instance != null) {
                // create a new dictionary to store the session id 
                // session id maps to each transcription session
                // the transcription session stores the audio data represented as a base64 encoded string
                // base64 is a way to encode binary data into a string of ASCII characters. this is used to transport audio data.
                WebSocketClient.Instance.Send("end_transcription", new Dictionary<string, object> {
                    {"session_id", currentSessionId}
                });
            }
        }
        UpdateStatus("Recording complete. Awaiting transcription...");
    }

    private void OnTranscriptionReceived(object, data) {
        // data is a string containing the json
        string transcription = data.ToString();
        // current transcription is what the user is currently recording
        // after the session, a local copy of the transcription is saved
        currentTranscription = transcription;

        // update ui on the main thread by adding the transcription to the queue
        UnityMainThreadDispatcher.Instance().Enqueue(() => {
            // update the transcription text on the ui to the received transcript
            if (transcriptionText != null)
                transcriptionText.text = transcription;

            // save a local copy of the transcription to the file system
            // this copy can only be accessed during the session
            // after, the session will be saved to the server
            SaveTranscription(transcription);
        })
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

    // sends the status (ex: error, recording, etc)
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

    private IEnumerator VisualizeWaveform()
    {
        if (waveformBars == null || waveformBars.Length == 0) yield break;
        
        float[] samples = new float[1024];
        
        while (isRecording && recording != null)
        {
            try
            {
                // Get the current position in the recording
                int position = Microphone.GetPosition(microphoneDevice);
                if (position > 0 && position < recording.samples)
                {
                    // Get samples from the recording
                    recording.GetData(samples, Mathf.Max(0, position - samples.Length));
                    
                    // Update waveform bars
                    for (int i = 0; i < waveformBars.Length; i++)
                    {
                        if (waveformBars[i] != null)
                        {
                            int sampleIndex = i * (samples.Length / waveformBars.Length);
                            if (sampleIndex < samples.Length)
                            {
                                float amplitude = Mathf.Abs(samples[sampleIndex]) * waveformAmplitudeMultiplier;
                                float height = Mathf.Clamp(amplitude, waveformMinHeight, waveformMaxHeight);
                                waveformBars[i].sizeDelta = new Vector2(waveformBars[i].sizeDelta.x, height);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error in waveform visualization: " + e.Message);
            }
            
            yield return new WaitForSeconds(waveformUpdateInterval);
        }
    }
}