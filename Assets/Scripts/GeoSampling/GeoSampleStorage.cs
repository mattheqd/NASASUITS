using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class GeoSampleStorage : MonoBehaviour
{
    private static GeoSampleStorage instance;
    public static GeoSampleStorage Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("GeoSampleStorage");
                instance = go.AddComponent<GeoSampleStorage>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    private List<GeoSampleData> samples = new List<GeoSampleData>();
    private string savePath;
    private const string SAVE_FILE_NAME = "geosamples.json";

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Set up save path in persistent data path
        savePath = Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
        LoadSamples();
    }

    // Add a new sample to storage
    public void AddSample(GeoSampleData sample)
    {
        samples.Add(sample);
        SaveSamples();
    }

    // Get all samples
    public List<GeoSampleData> GetAllSamples()
    {
        return samples.ToList();
    }

    // Get a specific sample by ID
    public GeoSampleData GetSampleById(string sampleId)
    {
        return samples.FirstOrDefault(s => s.sampleId == sampleId);
    }

    // Update an existing sample
    public void UpdateSample(GeoSampleData updatedSample)
    {
        int index = samples.FindIndex(s => s.sampleId == updatedSample.sampleId);
        if (index != -1)
        {
            samples[index] = updatedSample;
            SaveSamples();
        }
    }

    // Delete a sample
    public void DeleteSample(string sampleId)
    {
        GeoSampleData sample = samples.FirstOrDefault(s => s.sampleId == sampleId);
        if (sample != null)
        {
            // Delete associated image file if it exists
            if (!string.IsNullOrEmpty(sample.imagePath) && File.Exists(sample.imagePath))
            {
                File.Delete(sample.imagePath);
            }
            
            samples.Remove(sample);
            SaveSamples();
        }
    }

    // Save samples to disk
    private void SaveSamples()
    {
        try
        {
            string json = JsonUtility.ToJson(new SampleWrapper { samples = samples });
            File.WriteAllText(savePath, json);
            Debug.Log($"Saved {samples.Count} samples to {savePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error saving samples: {e.Message}");
        }
    }

    // Load samples from disk
    private void LoadSamples()
    {
        try
        {
            if (File.Exists(savePath))
            {
                string json = File.ReadAllText(savePath);
                SampleWrapper wrapper = JsonUtility.FromJson<SampleWrapper>(json);
                samples = wrapper.samples;
                Debug.Log($"Loaded {samples.Count} samples from {savePath}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading samples: {e.Message}");
            samples = new List<GeoSampleData>();
        }
    }

    // Wrapper class for JSON serialization
    [System.Serializable]
    private class SampleWrapper
    {
        public List<GeoSampleData> samples;
    }
} 