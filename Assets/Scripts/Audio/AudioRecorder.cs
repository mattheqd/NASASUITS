//* Script for voice memo during geosampling. 
//* handles recording, transcription, and waveform visualization
//* the api picks up the microphone input (ex: built in mic on glasses vs laptop)
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;
using TMPro;
using System;

public class AudioRecorder : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button recordButton;
    // [SerializeField] private Button playButton;
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

    private AudioClip recording;
    private bool isRecording = false;
    private string microphoneDevice;
    private float startRecordingTime;
    private AudioSource audioSource;
    
    // Path to save transcriptions
    private string transcriptionFilePath;

    // Waveform visualization
    private RectTransform[] waveformBars;
    private Coroutine waveformCoroutine;
    private string currentTranscription = "";

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
        
        // if (playButton != null)
        //     playButton.onClick.AddListener(PlayRecording);
            
        // Initialize UI
        if (voiceMemoPanel != null)
        {
            // Make sure panel is visible when active
            Image panelImage = voiceMemoPanel.GetComponent<Image>();
            if (panelImage != null)
            {
                Color color = panelImage.color;
                color.a = 0.8f; // Set alpha to visible
                panelImage.color = color;
            }
            
            voiceMemoPanel.SetActive(false);
        }
            
        // Initialize waveform bars
        InitializeWaveformBars();
        
        // Set up transcription file path
        transcriptionFilePath = Path.Combine(Application.persistentDataPath, "transcriptions.txt");
        Debug.Log("Transcriptions will be saved to: " + transcriptionFilePath);
    }

    private void OnDestroy()
    {
        if (recordButton != null)
            recordButton.onClick.RemoveListener(ToggleRecording);
        
        // if (playButton != null)
        //     playButton.onClick.RemoveListener(PlayRecording);
            
        // Stop coroutine if running
        if (waveformCoroutine != null)
            StopCoroutine(waveformCoroutine);
    }
    
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

        // Start recording
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
            
            // Generate transcription based on audio characteristics
            GenerateTranscription(samples, recordingLength);
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

    // Simple method to generate a transcription based on audio characteristics
    private void GenerateTranscription(float[] samples, float recordingLength)
    {
        // Calculate some basic audio metrics
        float averageAmplitude = 0;
        float maxAmplitude = 0;
        int silenceCount = 0;
        
        for (int i = 0; i < samples.Length; i += 1000) // Sample every 1000th value for performance
        {
            float amplitude = Mathf.Abs(samples[i]);
            averageAmplitude += amplitude;
            maxAmplitude = Mathf.Max(maxAmplitude, amplitude);
            
            if (amplitude < 0.01f)
                silenceCount++;
        }
        
        averageAmplitude /= (samples.Length / 1000);
        
        // Generate a simple transcription based on audio characteristics
        string transcription = $"Voice memo recorded at {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}. ";
        
        if (recordingLength < 3)
            transcription += "This was a very short recording. ";
        else if (recordingLength > 30)
            transcription += "This was a longer recording. ";
            
        if (averageAmplitude < 0.05f)
            transcription += "The audio was very quiet. ";
        else if (averageAmplitude > 0.2f)
            transcription += "The audio was quite loud. ";
            
        if (silenceCount > (samples.Length / 1000) / 3)
            transcription += "There were several moments of silence. ";
            
        transcription += $"Recording duration: {recordingLength:F1} seconds.";
        
        // Set the transcription
        currentTranscription = transcription;
        
        // Update UI
        if (transcriptionText != null)
        {
            transcriptionText.text = transcription;
        }
        
        // Save the transcription
        SaveMemo(transcription);
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
    
    private void SaveMemo(string memoText)
    {
        if (string.IsNullOrEmpty(memoText))
        {
            Debug.LogWarning("Cannot save empty memo text");
            return;
        }
        
        try
        {
            // Append to file with timestamp
            string entry = $"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}] {memoText}\n";
            File.AppendAllText(transcriptionFilePath, entry);
            
            Debug.Log("Voice memo saved: " + memoText);
        }
        catch (Exception e)
        {
            Debug.LogError("Error saving transcription: " + e.Message);
        }
    }
} 