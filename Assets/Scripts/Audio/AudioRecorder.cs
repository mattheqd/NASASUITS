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
        transcriptsFolderPath = Application.persistentDataPath + "/Transcripts";
        
        if (Microphone.devices.Length > 0)
        {
            microphoneDevice = Microphone.devices[0];
            Debug.Log($"Using microphone: {microphoneDevice}");
        }
        else
        {
            Debug.LogError("No microphone found!");
        }

        audioSource = gameObject.AddComponent<AudioSource>();

        if (recordButton != null)
            recordButton.onClick.AddListener(ToggleRecording);
        
        if (playButton != null)
            playButton.onClick.AddListener(PlayRecording);
            
        if (voiceMemoPanel != null)
        {
            Image panelImage = voiceMemoPanel.GetComponent<Image>();
            voiceMemoPanel.SetActive(true);
        }
            
        InitializeWaveformBars();
        
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
        
        // Start streaming audio to server
        if (useWebSocketTranscription && WebSocketClient.Instance != null)
        {
            if (streamingCoroutine != null)
                StopCoroutine(streamingCoroutine);
            // starts a coroutine thread that takes audio data and sends it to the server
            // this is called after the recording is complete to save a persistent copy of the transcript to the server
            streamingCoroutine = StartCoroutine(StreamAudioToServer());
        }
        
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

            // Send the entire recording for transcription
            if (useWebSocketTranscription && WebSocketClient.Instance != null)
            {
                // Generate a unique session ID for this recording
                currentSessionId = System.Guid.NewGuid().ToString();
                
                // Convert float array to PCM16 bytes
                byte[] pcmBytes = new byte[samples.Length * 2];
                for (int i = 0; i < samples.Length; i++)
                {
                    short pcmValue = (short)(samples[i] * 32767);
                    byte[] pcmSample = BitConverter.GetBytes(pcmValue);
                    pcmBytes[i * 2] = pcmSample[0];
                    pcmBytes[i * 2 + 1] = pcmSample[1];
                }
                
                // Convert to base64
                string base64Audio = Convert.ToBase64String(pcmBytes);
                
                // Send the entire recording
                WebSocketClient.Instance.Send("transcribe_audio", new Dictionary<string, object> {
                    { "session_id", currentSessionId },
                    { "audio_data", base64Audio },
                    { "sample_rate", sampleRate },
                    { "channels", 1 }
                });
            }
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
        
        UpdateStatus("Recording complete. Awaiting transcription...");
    }

    private void OnTranscriptionReceived(object data) {
        string transcription = data.ToString();
        currentTranscription = transcription;

        UnityMainThreadDispatcher.Instance().Enqueue(() => {
            if (transcriptionText != null)
                transcriptionText.text = transcription;

            SaveTranscription(transcription);
        });
    }

    // Get the current transcription
    public string GetCurrentTranscription()
    {
        return currentTranscription;
    }

    private void SaveTranscription(string transcription) {
        if (string.IsNullOrEmpty(transcription)) return;
        
        // generate a filename with timestamp
        if (string.IsNullOrEmpty(transcriptionFilePath)) {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            transcriptionFilePath = Path.Combine(transcriptsFolderPath, $"transcription_{timestamp}.txt"); 
        }
        // write all the transcription text to the file
        File.WriteAllText(transcriptionFilePath, transcription);
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

    // ------ Streaming Function ------
    // generated by claude 3.7
    // function is called after a recording
    // outputs a base64 encoded string of the audio data which can be stored in the server
    // the server will store a transcription session with the audio data
    private IEnumerator StreamAudioToServer()
    {
        // Generate a unique session ID for this recording
        currentSessionId = System.Guid.NewGuid().ToString();
        
        // Tell the server we're starting a new transcription session
        WebSocketClient.Instance.Send("start_transcription", new Dictionary<string, object> {
            { "session_id", currentSessionId },
            { "sample_rate", sampleRate },
            { "channels", 1 } // Mono audio
        });
        
        Debug.Log($"Started audio streaming session: {currentSessionId}");
        
        // Calculate how many samples to send in each chunk
        int samplesPerChunk = (int)(sampleRate * audioChunkDuration);
        float[] audioChunk = new float[samplesPerChunk];
        int lastPosition = 0;
        
        while (isRecording && recording != null)
        {
            // Get current position in the recording (i.e. the current index of the audio clip)
            // ex: 10000 can represent the 10000th sample in the audio clip. this is arbitrary
            int currentPosition = Microphone.GetPosition(microphoneDevice);
            
            // If we have enough new samples to send a chunk
            if (currentPosition > lastPosition + samplesPerChunk || 
                (currentPosition < lastPosition && currentPosition + recording.samples - lastPosition > samplesPerChunk))
            {
                // Handle wrap-around case (i.e. the audio clip is looping back to the beginning)
                if (currentPosition < lastPosition)
                {
                    // Get samples from end of buffer
                    int samplesAtEnd = recording.samples - lastPosition;
                    float[] endSamples = new float[samplesAtEnd];
                    recording.GetData(endSamples, lastPosition);
                    
                    // Get samples from beginning of buffer
                    int samplesAtBeginning = samplesPerChunk - samplesAtEnd;
                    float[] beginSamples = new float[samplesAtBeginning];
                    recording.GetData(beginSamples, 0);
                    
                    // Combine them
                    Array.Copy(endSamples, 0, audioChunk, 0, samplesAtEnd);
                    Array.Copy(beginSamples, 0, audioChunk, samplesAtEnd, samplesAtBeginning);
                }
                else
                {
                    // Simple case - just get the chunk
                    recording.GetData(audioChunk, lastPosition);
                }
                
                // Convert float array to PCM16 bytes (16-bit signed integers)
                // PCM is a way to represent audio data as a sequence of numbers
                // this makes it easier to transport audio data over the network because it's in binary format
                byte[] pcmBytes = new byte[audioChunk.Length * 2]; // 2 bytes per sample for 16-bit
                for (int i = 0; i < audioChunk.Length; i++)
                {
                    short pcmValue = (short)(audioChunk[i] * 32767);
                    byte[] pcmSample = BitConverter.GetBytes(pcmValue);
                    pcmBytes[i * 2] = pcmSample[0];
                    pcmBytes[i * 2 + 1] = pcmSample[1];
                }
                
                // Convert binary data to base64 string for JSON transport
                string base64Audio = Convert.ToBase64String(pcmBytes);
                
                // Send the audio chunk to the server
                WebSocketClient.Instance.Send("audio_chunk", new Dictionary<string, object> {
                    { "session_id", currentSessionId },
                    { "audio_data", base64Audio }
                });
                
                // Update last position
                lastPosition = (lastPosition + samplesPerChunk) % recording.samples;
                
                // Log for debugging
                Debug.Log($"Sent audio chunk: {pcmBytes.Length} bytes");
            }
            
            yield return new WaitForSeconds(audioChunkDuration / 2); // Check twice per chunk duration
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