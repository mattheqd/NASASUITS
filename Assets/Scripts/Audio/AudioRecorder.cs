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

    private AudioClip recording;
    private bool isRecording = false;
    private string microphoneDevice;
    private float startRecordingTime;
    private AudioSource audioSource;
    private string currentTranscription = "";
    
    // Path to save recordings and transcriptions
    private string transcriptsFolderPath;
    private string recordingsFolderPath;
    private string transcriptionFilePath;
    private string recordingFilePath;

    // Waveform visualization
    private RectTransform[] waveformBars;
    private Coroutine waveformCoroutine;

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
        
        // Set up folders for transcriptions and recordings
        transcriptsFolderPath = Path.Combine(Application.dataPath, "Transcripts");
        recordingsFolderPath = Path.Combine(Application.dataPath, "Recordings");
        
        // Generate filenames with timestamps
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        transcriptionFilePath = Path.Combine(transcriptsFolderPath, $"transcription_{timestamp}.txt");
        recordingFilePath = Path.Combine(recordingsFolderPath, $"recording_{timestamp}.mp3");
        
        Debug.Log("Transcription will be saved to: " + transcriptionFilePath);
        Debug.Log("Recording will be saved to: " + recordingFilePath);
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
            
            // Save the recording as MP3
            SaveRecordingAsMP3(recording);
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
        
        // For now, just display a placeholder message
        if (transcriptionText != null)
        {
            transcriptionText.text = "Recording saved. Transcription will be processed on the server.";
        }
        
        UpdateStatus("Recording saved!");
    }

    private void SaveRecordingAsMP3(AudioClip clip)
    {
        try
        {
            // Ensure the directory exists
            if (!Directory.Exists(recordingsFolderPath))
            {
                Directory.CreateDirectory(recordingsFolderPath);
            }
            
            // Generate a new filename with timestamp
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            recordingFilePath = Path.Combine(recordingsFolderPath, $"recording_{timestamp}.wav");
            
            // Convert to WAV and save (Unity doesn't support direct MP3 encoding)
            SavWav.Save(recordingFilePath, clip);
            
            Debug.Log("Recording saved to: " + recordingFilePath);
            
            // TODO: Send the recording to the backend server for transcription
            // This would typically be done via a web request or other network communication
            
            // For demonstration, we'll just create a placeholder transcription file
            if (!Directory.Exists(transcriptsFolderPath))
            {
                Directory.CreateDirectory(transcriptsFolderPath);
            }
            
            transcriptionFilePath = Path.Combine(transcriptsFolderPath, $"transcription_{timestamp}.txt");
            File.WriteAllText(transcriptionFilePath, "Awaiting transcription from server...");
            
            Debug.Log("Placeholder transcription file created at: " + transcriptionFilePath);
        }
        catch (Exception e)
        {
            Debug.LogError("Error saving recording: " + e.Message);
        }
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

// Helper class to save AudioClip as WAV file
// Source: http://wiki.unity3d.com/index.php/SaveWavUtil
public static class SavWav
{
    const int HEADER_SIZE = 44;

    public static bool Save(string filepath, AudioClip clip)
    {
        if (!filepath.ToLower().EndsWith(".wav"))
        {
            filepath = filepath + ".wav";
        }

        var samples = new float[clip.samples];
        clip.GetData(samples, 0);

        try
        {
            using (var fileStream = CreateEmpty(filepath))
            {
                ConvertAndWrite(fileStream, samples);
                WriteHeader(fileStream, clip);
            }
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("Error saving WAV file: " + e.Message);
            return false;
        }
    }

    private static FileStream CreateEmpty(string filepath)
    {
        var fileStream = new FileStream(filepath, FileMode.Create);
        byte emptyByte = new byte();

        for (int i = 0; i < HEADER_SIZE; i++)
        {
            fileStream.WriteByte(emptyByte);
        }

        return fileStream;
    }

    private static void ConvertAndWrite(FileStream fileStream, float[] samples)
    {
        Int16[] intData = new Int16[samples.Length];

        for (int i = 0; i < samples.Length; i++)
        {
            intData[i] = (short)(samples[i] * 32767);
        }

        byte[] byteArray = new byte[intData.Length * 2];
        Buffer.BlockCopy(intData, 0, byteArray, 0, byteArray.Length);
        fileStream.Write(byteArray, 0, byteArray.Length);
    }

    private static void WriteHeader(FileStream fileStream, AudioClip clip)
    {
        var hz = clip.frequency;
        var channels = clip.channels;
        var samples = clip.samples;

        fileStream.Seek(0, SeekOrigin.Begin);

        byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
        fileStream.Write(riff, 0, 4);

        byte[] chunkSize = BitConverter.GetBytes(fileStream.Length - 8);
        fileStream.Write(chunkSize, 0, 4);

        byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
        fileStream.Write(wave, 0, 4);

        byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
        fileStream.Write(fmt, 0, 4);

        byte[] subChunk1 = BitConverter.GetBytes(16);
        fileStream.Write(subChunk1, 0, 4);

        UInt16 one = 1;
        byte[] audioFormat = BitConverter.GetBytes(one);
        fileStream.Write(audioFormat, 0, 2);

        byte[] numChannels = BitConverter.GetBytes(channels);
        fileStream.Write(numChannels, 0, 2);

        byte[] sampleRate = BitConverter.GetBytes(hz);
        fileStream.Write(sampleRate, 0, 4);

        byte[] byteRate = BitConverter.GetBytes(hz * channels * 2);
        fileStream.Write(byteRate, 0, 4);

        UInt16 blockAlign = (ushort)(channels * 2);
        fileStream.Write(BitConverter.GetBytes(blockAlign), 0, 2);

        UInt16 bps = 16;
        byte[] bitsPerSample = BitConverter.GetBytes(bps);
        fileStream.Write(bitsPerSample, 0, 2);

        byte[] datastring = System.Text.Encoding.UTF8.GetBytes("data");
        fileStream.Write(datastring, 0, 4);

        byte[] subChunk2 = BitConverter.GetBytes(samples * channels * 2);
        fileStream.Write(subChunk2, 0, 4);
    }
} 