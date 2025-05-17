using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using GeoSampling;

public class GeoSamplesListUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform samplesListContainer; // Container for sample buttons
    [SerializeField] private GameObject sampleButtonPrefab; // Prefab for sample buttons
    [SerializeField] private GameObject sampleDetailsPanel; // Panel to show sample details
    [SerializeField] private Button closeDetailsButton; // Button to close details panel

    private List<GameObject> sampleButtons = new List<GameObject>();

    private void Start()
    {
        // Set up close button listener
        if (closeDetailsButton != null)
        {
            closeDetailsButton.onClick.AddListener(CloseSampleDetails);
        }

        // Initially hide details panel
        if (sampleDetailsPanel != null)
        {
            sampleDetailsPanel.SetActive(false);
        }

        // Load and display samples
        RefreshSamplesList();
    }

    private void OnDestroy()
    {
        if (closeDetailsButton != null)
        {
            closeDetailsButton.onClick.RemoveListener(CloseSampleDetails);
        }
    }

    public void RefreshSamplesList()
    {
        // Clear existing buttons
        foreach (var button in sampleButtons)
        {
            Destroy(button);
        }
        sampleButtons.Clear();

        // Get all samples from storage
        var samples = GeoSampleStorage.Instance.GetAllSamples();

        // Create a button for each sample
        foreach (var sample in samples)
        {
            CreateSampleButton(sample);
        }
    }

    private void CreateSampleButton(GeoSampleData sample)
    {
        if (sampleButtonPrefab == null || samplesListContainer == null) return;

        // Instantiate button under the content area
        GameObject buttonObj = Instantiate(sampleButtonPrefab, samplesListContainer);
        sampleButtons.Add(buttonObj);

        // Ensure the button is properly set up in the layout
        RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.localScale = Vector3.one;
            rectTransform.anchoredPosition = Vector2.zero;
        }

        // Set button text
        TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = sample.sampleType;
        }

        // Set button click handler
        Button button = buttonObj.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => ShowSampleDetails(sample));
        }
    }

    private void ShowSampleDetails(GeoSampleData sample)
    {
        if (sampleDetailsPanel == null) return;

        // Get the SampleDetailsPanel component
        SampleDetailsPanel detailsPanel = sampleDetailsPanel.GetComponent<SampleDetailsPanel>();
        if (detailsPanel != null)
        {
            detailsPanel.ShowSampleDetails(sample);
        }
    }

    private void CloseSampleDetails()
    {
        if (sampleDetailsPanel != null)
        {
            sampleDetailsPanel.SetActive(false);
        }
    }
} 