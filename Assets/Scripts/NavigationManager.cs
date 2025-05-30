using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class NavigationManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject poiListPanel; // Panel containing the list of POI buttons
    public GameObject navigationPanel; // Panel containing the minimap
    public GameObject pathOptionsPanel; // Panel containing path options
    public GameObject toolbarPanel; // Panel containing the toolbar
    public Button poiButtonPrefab; // Prefab for POI buttons
    public Transform poiListContent; // Content transform for the POI buttons
    public Button resetPathButton; // Button to clear the current path
    public Button dropPinButton; // Button to drop a pin at current location
    public TextMeshProUGUI selectedPathText; // Text field to show selected path type
    public TextMeshProUGUI distanceText; // Text field to show distance
    public Image backgroundImage; // Reference to the background image

    [Header("Path Options")]
    public Button safestPathButton;
    public Button recommendedPathButton;
    public Button directPathButton;
    public Button finishButton;

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

    private POILocation selectedPOI; // Store the selected POI for the two-step process
    private NavigationSystem.PathType currentPathType; // Store the current path type
    private float updateTimer = 0f; // Timer for updating path
    private const float UPDATE_INTERVAL = 1f; // Update every second
    private Button lastSelectedButton = null; // Track the last selected button

    private void Start()
    {
        Debug.Log("[NavigationManager] Starting initialization...");

        // Set up background image
        if (backgroundImage != null)
        {
            // Ensure background is behind other elements
            backgroundImage.transform.SetAsFirstSibling();
            
            // Set the RectTransform to stretch and fill
            RectTransform rectTransform = backgroundImage.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            
            // Disable raycast target so it doesn't interfere with UI interactions
            backgroundImage.raycastTarget = false;
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
        if (pathOptionsPanel == null)
        {
            Debug.LogError("[NavigationManager] Path Options Panel not assigned!");
            return;
        }
        if (toolbarPanel == null)
        {
            Debug.LogError("[NavigationManager] Toolbar Panel not assigned!");
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

        // Set up path option buttons
        if (safestPathButton != null)
        {
            safestPathButton.onClick.AddListener(() => OnPathOptionSelected(NavigationSystem.PathType.Safest));
        }
        if (recommendedPathButton != null)
        {
            recommendedPathButton.onClick.AddListener(() => OnPathOptionSelected(NavigationSystem.PathType.Recommended));
        }
        if (directPathButton != null)
        {
            directPathButton.onClick.AddListener(() => OnPathOptionSelected(NavigationSystem.PathType.Direct));
        }
        if (finishButton != null)
        {
            finishButton.onClick.AddListener(OnFinishButtonClicked);
        }

        // Set up reset button
        resetPathButton.onClick.AddListener(ResetCurrentPath);

        // Set up drop pin button
        if (dropPinButton != null)
        {
            dropPinButton.onClick.AddListener(OnDropPinClicked);
        }
        else
        {
            Debug.LogError("[NavigationManager] Drop Pin Button not assigned!");
        }

        // Initialize POI list
        PopulatePOIList();

        // Show initial panels
        ShowInitialPanels();

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

    private void ShowInitialPanels()
    {
        Debug.Log("[NavigationManager] Showing initial panels");
        if (poiListPanel != null) poiListPanel.SetActive(true);
        if (navigationPanel != null) navigationPanel.SetActive(true);
        if (pathOptionsPanel != null) pathOptionsPanel.SetActive(false);
        if (toolbarPanel != null) toolbarPanel.SetActive(true);
        if (selectedPathText != null) selectedPathText.gameObject.SetActive(false);
    }

    private void ShowPathOptionsPanel()
    {
        Debug.Log("[NavigationManager] Showing path options panel");
        if (poiListPanel != null) poiListPanel.SetActive(false);
        if (toolbarPanel != null) toolbarPanel.SetActive(false);
        if (pathOptionsPanel != null) pathOptionsPanel.SetActive(true);
    }

    private void ResetCurrentPath()
    {
        Debug.Log("[NavigationManager] Resetting current path");
        navigationSystem.ClearCurrentPath();
        ShowInitialPanels();
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
        Debug.Log($"[NavigationManager] POI selected: {poi.name} at position ({poi.position.x}, {poi.position.y})");
        selectedPOI = poi;
        currentPathType = NavigationSystem.PathType.None; // Set to None when POI is selected
        ShowPathOptionsPanel();

        // Show the text fields
        if (selectedPathText != null)
        {
            selectedPathText.gameObject.SetActive(true);
            selectedPathText.text = "";
        }
        if (distanceText != null)
        {
            distanceText.gameObject.SetActive(true);
            distanceText.text = "";
        }
    }

    private void Update()
    {
        if (selectedPOI != null && currentPathType != NavigationSystem.PathType.None)
        {
            updateTimer += Time.deltaTime;
            if (updateTimer >= UPDATE_INTERVAL)
            {
                updateTimer = 0f;
                UpdatePathAndETA();
            }
        }
    }

    private void UpdatePathAndETA()
    {
        if (selectedPOI == null || navigationSystem == null) return;

        // Only update if we have a valid path type selected
        if (currentPathType == NavigationSystem.PathType.None)
        {
            return;
        }

        // Force path recalculation with current position
        navigationSystem.UpdatePathToLocation(selectedPOI.position, currentPathType);
        
        // Get the current path from navigation system
        List<NavigationSystem.Node> currentPath = navigationSystem.GetCurrentPath();
        if (currentPath == null || currentPath.Count < 2)
        {
            Debug.LogWarning("[NavigationManager] No valid path found for update");
            return;
        }

        // Calculate new distance and ETA
        float pathDistance = CalculatePathDistance(selectedPOI.position);
        float etaMinutes = CalculateETA(pathDistance);
        
        // Convert to minutes and seconds
        int minutes = Mathf.FloorToInt(etaMinutes);
        int seconds = Mathf.FloorToInt((etaMinutes - minutes) * 60);
        
        // Update the text fields
        if (selectedPathText != null)
        {
            selectedPathText.gameObject.SetActive(true);
            selectedPathText.text = $"{minutes}:{seconds:D2}";
        }
        if (distanceText != null)
        {
            distanceText.gameObject.SetActive(true);
            distanceText.text = $"{Mathf.Round(pathDistance)}";
        }
    }

    private void OnPathOptionSelected(NavigationSystem.PathType pathType)
    {
        if (selectedPOI == null)
        {
            Debug.LogError("[NavigationManager] No POI selected!");
            return;
        }

        Debug.Log($"[NavigationManager] Selected path type: {pathType} for POI: {selectedPOI.name}");
        
        // Store the current path type
        currentPathType = pathType;
        
        // Show the text field
        if (selectedPathText != null)
        {
            selectedPathText.gameObject.SetActive(true);
        }

        // Reset previous button's outline
        if (lastSelectedButton != null)
        {
            var outline = lastSelectedButton.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = new Color(0.518f, 0.518f, 0.518f, 0.5f); // #848484 with 128 opacity
            }
        }

        // Highlight the selected button
        Button selectedButton = null;
        switch (pathType)
        {
            case NavigationSystem.PathType.Safest:
                selectedButton = safestPathButton;
                break;
            case NavigationSystem.PathType.Recommended:
                selectedButton = recommendedPathButton;
                break;
            case NavigationSystem.PathType.Direct:
                selectedButton = directPathButton;
                break;
        }

        if (selectedButton != null)
        {
            var outline = selectedButton.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = Color.white; // Set outline to white
            }
            lastSelectedButton = selectedButton;
        }
        
        // Only update path when a path type is explicitly selected
        navigationSystem.UpdatePathToLocation(selectedPOI.position, pathType);
    }

    private float CalculatePathDistance(Vector2 destination)
    {
        if (navigationSystem == null) return 0f;

        // Get the current path from the navigation system
        List<NavigationSystem.Node> path = navigationSystem.GetCurrentPath();
        if (path == null || path.Count < 2) return 0f;

        float totalDistance = 0f;
        // Calculate distance between each consecutive node in the path
        for (int i = 0; i < path.Count - 1; i++)
        {
            totalDistance += Vector2.Distance(path[i].position, path[i + 1].position);
        }

        return totalDistance;
    }

    private float CalculateETA(float distanceMeters)
    {
        // Convert 3 mph to meters per minute
        // 3 mph = 4.82803 km/h = 80.4672 meters per minute
        float speedMetersPerMinute = 80.4672f;
        
        // Calculate time in minutes
        return distanceMeters / speedMetersPerMinute;
    }

    private void OnFinishButtonClicked()
    {
        Debug.Log("[NavigationManager] Finish button clicked");
        
        // Clear the current path
        navigationSystem.ClearCurrentPath();
        
        // Hide path options and show POI list and toolbar
        pathOptionsPanel.SetActive(false);
        poiListPanel.SetActive(true);
        if (toolbarPanel != null) toolbarPanel.SetActive(true);
        
        // Reset and hide the text fields
        if (selectedPathText != null)
        {
            selectedPathText.text = "";
            selectedPathText.gameObject.SetActive(false);
        }
        if (distanceText != null)
        {
            distanceText.text = "";
            distanceText.gameObject.SetActive(false);
        }
        
        // Reset button outline
        if (lastSelectedButton != null)
        {
            var outline = lastSelectedButton.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = new Color(0.518f, 0.518f, 0.518f, 0.5f); // #848484 with 128 opacity
            }
            lastSelectedButton = null;
        }
        
        // Clear current POI and path type to stop updates
        selectedPOI = null;
        currentPathType = NavigationSystem.PathType.None;
        updateTimer = 0f; // Reset the update timer
    }

    private void OnDropPinClicked()
    {
        Vector2 pinPosition = navigationSystem.DropPin();
        if (pinPosition != Vector2.zero)
        {
            // Create a new POI for the dropped pin
            POILocation newPOI = new POILocation
            {
                name = $"Pin {poiLocations.Count + 1}",
                position = pinPosition,
                description = "Dropped pin location"
            };
            
            AddNewPOI(newPOI);
        }
    }

    private void AddNewPOI(POILocation newPOI)
    {
        poiLocations.Add(newPOI);
        
        // Create button for the new POI
        Button poiButton = Instantiate(poiButtonPrefab, poiListContent);
        if (poiButton != null)
        {
            // Set button text
            TextMeshProUGUI buttonText = poiButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = newPOI.name;
            }

            // Add click handler
            POILocation capturedPoi = newPOI;
            poiButton.onClick.AddListener(() => NavigateToPOI(capturedPoi));

            // Ensure the button is properly sized and positioned
            RectTransform rectTransform = poiButton.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0, 1);
                rectTransform.anchorMax = new Vector2(1, 1);
                rectTransform.pivot = new Vector2(0.5f, 1);
                rectTransform.anchoredPosition = Vector2.zero;
            }
        }

        // Add POI icon to the minimap
        if (navigationSystem != null)
        {
            navigationSystem.PlacePOIIcon(newPOI.position);
        }
    }
} 