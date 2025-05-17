using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using GeoSampling;

public class SampleDetailsPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI sampleTypeText;
    [SerializeField] private TextMeshProUGUI locationText;
    [SerializeField] private TextMeshProUGUI transcriptionText;
    [SerializeField] private RawImage sampleImage;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button deleteButton;

    [Header("Rock Composition UI")]
    [SerializeField] private TextMeshProUGUI rockNameText;
    [SerializeField] private TextMeshProUGUI rockSiO2Text;
    [SerializeField] private TextMeshProUGUI rockAl2O3Text;
    [SerializeField] private TextMeshProUGUI rockMnOText;
    [SerializeField] private TextMeshProUGUI rockCaOText;
    [SerializeField] private TextMeshProUGUI rockP2O3Text;
    [SerializeField] private TextMeshProUGUI rockTiO2Text;
    [SerializeField] private TextMeshProUGUI rockFeOText;
    [SerializeField] private TextMeshProUGUI rockMgOText;
    [SerializeField] private TextMeshProUGUI rockK2OText;
    [SerializeField] private TextMeshProUGUI rockOtherText;

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

        // Update rock composition data if available
        if (currentSample.rockComposition != null)
        {
            if (rockNameText != null)
                rockNameText.text = currentSample.rockComposition.rockName;
            
            if (rockSiO2Text != null)
                rockSiO2Text.text = currentSample.rockComposition.SiO2.ToString();
            
            if (rockAl2O3Text != null)
                rockAl2O3Text.text = currentSample.rockComposition.Al2O3.ToString();
            
            if (rockMnOText != null)
                rockMnOText.text = currentSample.rockComposition.MnO.ToString();
            
            if (rockCaOText != null)
                rockCaOText.text = currentSample.rockComposition.CaO.ToString();
            
            if (rockP2O3Text != null)
                rockP2O3Text.text = currentSample.rockComposition.P2O3.ToString();
            
            if (rockTiO2Text != null)
                rockTiO2Text.text = currentSample.rockComposition.TiO2.ToString();
            
            if (rockFeOText != null)
                rockFeOText.text = currentSample.rockComposition.FeO.ToString();
            
            if (rockMgOText != null)
                rockMgOText.text = currentSample.rockComposition.MgO.ToString();
            
            if (rockK2OText != null)
                rockK2OText.text = currentSample.rockComposition.K2O.ToString();
            
            if (rockOtherText != null)
                rockOtherText.text = currentSample.rockComposition.Other.ToString();
        }
        else
        {
            // Clear rock composition fields if no data
            if (rockNameText != null) rockNameText.text = "---";
            if (rockSiO2Text != null) rockSiO2Text.text = "---";
            if (rockAl2O3Text != null) rockAl2O3Text.text = "---";
            if (rockMnOText != null) rockMnOText.text = "---";
            if (rockCaOText != null) rockCaOText.text = "---";
            if (rockP2O3Text != null) rockP2O3Text.text = "---";
            if (rockTiO2Text != null) rockTiO2Text.text = "---";
            if (rockFeOText != null) rockFeOText.text = "---";
            if (rockMgOText != null) rockMgOText.text = "---";
            if (rockK2OText != null) rockK2OText.text = "---";
            if (rockOtherText != null) rockOtherText.text = "---";
        }

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