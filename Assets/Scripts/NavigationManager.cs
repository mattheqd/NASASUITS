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
    public Button resetPathButton; // Button to clear the current path
    public Button dropPinButton; // Button to drop a pin at current location

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

    private void Start()
    {
        Debug.Log("[NavigationManager] Starting initialization...");

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
        if (resetPathButton == null)
        {
            Debug.LogError("[NavigationManager] Reset Path Button not assigned!");
            return;
        }
        if (dropPinButton == null)
        {
            Debug.LogError("[NavigationManager] Drop Pin Button not assigned!");
            return;
        }

        // Set up reset button
        resetPathButton.onClick.AddListener(ResetCurrentPath);

        // Set up drop pin button
        if (dropPinButton != null)
        {
            dropPinButton.onClick.AddListener(() => navigationSystem.DropPin());
        }
        else
        {
            Debug.LogError("[NavigationManager] Drop Pin Button not assigned!");
        }

        // Initialize POI list
        PopulatePOIList();

        // Show both panels
        ShowBothPanels();

        // Place POI icons on the minimap
        if (navigationSystem != null && poiLocations != null && poiLocations.Count > 0)
        {
            var poiPositions = new List<Vector2>();
            foreach (var poi in poiLocations)
            {
                poiPositions.Add(poi.position);
            }
            navigationSystem.PlacePOIIcons(poiPositions);
        }
        
        Debug.Log("[NavigationManager] Initialization complete");
    }

    private void ResetCurrentPath()
    {
        Debug.Log("[NavigationManager] Resetting current path");
        navigationSystem.ClearCurrentPath();
    }

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

    private void NavigateToPOI(POILocation poi)
    {
        Debug.Log($"[NavigationManager] Navigating to POI: {poi.name} at position ({poi.position.x}, {poi.position.y})");
        
        // Update the path to the new POI location
        navigationSystem.UpdatePathToLocation(poi.position);
    }

    private void ShowBothPanels()
    {
        Debug.Log("[NavigationManager] Showing both panels");
        if (poiListPanel != null) poiListPanel.SetActive(true);
        if (navigationPanel != null) navigationPanel.SetActive(true);
    }
} 