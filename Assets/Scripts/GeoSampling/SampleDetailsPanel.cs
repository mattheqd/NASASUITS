using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;

public class SampleDetailsPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI sampleTypeText;
    [SerializeField] private TextMeshProUGUI locationText;
    [SerializeField] private TextMeshProUGUI transcriptionText;
    [SerializeField] private RawImage sampleImage;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button deleteButton;

    private GeoSampleData currentSample;
    private GeoSamplesListUI samplesListUI;

    private void Start()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseClicked);
        }

        if (deleteButton != null)
        {
            deleteButton.onClick.AddListener(OnDeleteClicked);
        }

        // Find the GeoSamplesListUI component
        samplesListUI = FindObjectOfType<GeoSamplesListUI>();
    }

    private void OnDestroy()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(OnCloseClicked);
        }

        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveListener(OnDeleteClicked);
        }
    }

    public void ShowSampleDetails(GeoSampleData sample)
    {
        currentSample = sample;
        UpdateUI();
        gameObject.SetActive(true);
    }

    private void UpdateUI()
    {
        if (currentSample == null) return;

        // Update text fields
        if (sampleTypeText != null)
            sampleTypeText.text = $"Type: {currentSample.sampleType}";
        
        if (locationText != null)
            locationText.text = $"Location: {currentSample.location}";
        
        if (transcriptionText != null)
            transcriptionText.text = $"Notes: {currentSample.voiceTranscription}";

        // Load and display sample image if available
        if (sampleImage != null)
        {
            Debug.Log($"Sample image path: {currentSample.imagePath}");
            
            if (!string.IsNullOrEmpty(currentSample.imagePath))
            {
                try
                {
                    if (File.Exists(currentSample.imagePath))
                    {
                        Debug.Log($"Loading image from: {currentSample.imagePath}");
                        byte[] imageData = File.ReadAllBytes(currentSample.imagePath);
                        Texture2D texture = new Texture2D(2, 2);
                        texture.LoadImage(imageData);
                        sampleImage.texture = texture;
                        sampleImage.gameObject.SetActive(true);
                        Debug.Log($"Image loaded successfully. Size: {texture.width}x{texture.height}");
                    }
                    else
                    {
                        Debug.LogError($"Image file does not exist at path: {currentSample.imagePath}");
                        sampleImage.gameObject.SetActive(false);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error loading sample image: {e.Message}\nStack trace: {e.StackTrace}");
                    sampleImage.gameObject.SetActive(false);
                }
            }
            else
            {
                Debug.Log("No image path provided for sample");
                sampleImage.gameObject.SetActive(false);
            }
        }
        else
        {
            Debug.LogError("Sample image RawImage component is not assigned!");
        }
    }

    private void OnCloseClicked()
    {
        gameObject.SetActive(false);
        currentSample = null;
    }

    private void OnDeleteClicked()
    {
        if (currentSample != null)
        {
            // Delete the sample from storage
            GeoSampleStorage.Instance.DeleteSample(currentSample.sampleId);
            
            // Delete the image file if it exists
            if (!string.IsNullOrEmpty(currentSample.imagePath) && File.Exists(currentSample.imagePath))
            {
                File.Delete(currentSample.imagePath);
            }

            // Refresh the samples list
            if (samplesListUI != null)
            {
                samplesListUI.RefreshSamplesList();
            }

            // Hide the details panel
            OnCloseClicked();
        }
    }
} 