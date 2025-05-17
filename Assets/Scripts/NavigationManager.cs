using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class NavigationManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject poiListPanel; // Panel containing the list of POI buttons
    public GameObject navigationPanel; // Panel containing the minimap
    public Button poiButtonPrefab; // Prefab for POI buttons
    public Transform poiListContent; // Content transform for the POI buttons
    public Button backButton; // Button to return to POI list

    [Header("Navigation System")]
    public NavigationSystem navigationSystem; // Reference to the NavigationSystem

    [System.Serializable]
    public class POILocation
    {
        public string name;
        public Vector2 position;
        public string description;
    }

    [Header("POI Locations")]
    public List<POILocation> poiLocations = new List<POILocation>();

    private void PopulatePOIList()
    {
        Debug.Log("[NavigationManager] Starting to populate POI list...");
        
        if (poiListContent == null)
        {
            Debug.LogError("[NavigationManager] POI List Content transform is not assigned!");
            return;
        }

        if (poiButtonPrefab == null)
        {
            Debug.LogError("[NavigationManager] POI Button Prefab is not assigned!");
            return;
        }

        // Clear existing buttons
        foreach (Transform child in poiListContent)
        {
            Destroy(child.gameObject);
        }

        Debug.Log($"[NavigationManager] Creating buttons for {poiLocations.Count} POIs");

        // Create buttons for each POI
        foreach (var poi in poiLocations)
        {
            Debug.Log($"[NavigationManager] Creating button for POI: {poi.name}");
            
            // Instantiate the button
            Button poiButton = Instantiate(poiButtonPrefab, poiListContent);
            if (poiButton == null)
            {
                Debug.LogError($"[NavigationManager] Failed to instantiate button for POI: {poi.name}");
                continue;
            }

            // Set button text
            TextMeshProUGUI buttonText = poiButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = poi.name;
                Debug.Log($"[NavigationManager] Set button text to: {poi.name}");
            }
            else
            {
                Debug.LogError($"[NavigationManager] No TextMeshProUGUI component found on button for POI: {poi.name}");
            }

            // Add click handler
            POILocation capturedPoi = poi; // Capture the POI for the lambda
            poiButton.onClick.AddListener(() => NavigateToPOI(capturedPoi));
            
            // Ensure the button is properly sized and positioned
            RectTransform rectTransform = poiButton.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0, 1);
                rectTransform.anchorMax = new Vector2(1, 1);
                rectTransform.pivot = new Vector2(0.5f, 1); // Set a fixed height
                rectTransform.anchoredPosition = Vector2.zero;
            }
        }

        Debug.Log("[NavigationManager] Finished populating POI list");
    }

    private void Start()
    {
        Debug.Log("[NavigationManager] Starting initialization...");

        // Ensure navigation system is not initialized yet
        if (navigationSystem != null)
        {
            navigationSystem.enabled = false;
        }

        // Ensure panels are properly set up
        if (poiListPanel == null)
        {
            Debug.LogError("[NavigationManager] POI List Panel not assigned!");
            return;
        }
        if (navigationPanel == null)
        {
            Debug.LogError("[NavigationManager] Navigation Panel not assigned!");
            return;
        }
        if (poiListContent == null)
        {
            Debug.LogError("[NavigationManager] POI List Content transform not assigned!");
            return;
        }
        if (poiButtonPrefab == null)
        {
            Debug.LogError("[NavigationManager] POI Button Prefab not assigned!");
            return;
        }

        // Initialize POI list
        PopulatePOIList();

        // Set up back button
        if (backButton != null)
        {
            backButton.onClick.AddListener(ShowPOIList);
        }
        else
        {
            Debug.LogError("[NavigationManager] Back button not assigned!");
        }

        // Initially show POI list and hide navigation
        ShowPOIList();
        
        Debug.Log("[NavigationManager] Initialization complete");
    }

    private void NavigateToPOI(POILocation poi)
    {
        Debug.Log($"[NavigationManager] Navigating to POI: {poi.name} at position ({poi.position.x}, {poi.position.y})");
        
        // Enable navigation system
        if (navigationSystem != null)
        {
            navigationSystem.enabled = true;
        }
        
        // Set the end location in the navigation system
        navigationSystem.endLocation = poi.position;
        
        // Show navigation panel and hide POI list
        ShowNavigation();
        
        // Initialize navigation system
        navigationSystem.StartNavigation();
    }

    private void ShowPOIList()
    {
        Debug.Log("[NavigationManager] Showing POI List");
        if (poiListPanel != null) poiListPanel.SetActive(true);
        if (navigationPanel != null) navigationPanel.SetActive(false);
    }

    private void ShowNavigation()
    {
        Debug.Log("[NavigationManager] Showing Navigation");
        if (poiListPanel != null) poiListPanel.SetActive(false);
        if (navigationPanel != null) navigationPanel.SetActive(true);
    }
} 