using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ARPanelManager : MonoBehaviour
{
    [SerializeField] private GameObject arPanel; // The panel to show in AR
    [SerializeField] private XRSimpleInteractable toggleButton; // The button for hand interaction
    [SerializeField] private float panelDistance = 1.5f; // Distance from camera
    [SerializeField] private bool startVisible = true;

    private Camera mainCamera;
    private bool isVisible;

    private void Start()
    {
        mainCamera = Camera.main;
        isVisible = startVisible;
        
        // Initialize panel position
        if (arPanel != null)
        {
            arPanel.SetActive(isVisible);
            UpdatePanelPosition();
        }

        // Setup button interaction
        if (toggleButton != null)
        {
            toggleButton.selectEntered.AddListener(OnToggleButtonPressed);
        }
    }

    private void Update()
    {
        if (isVisible && arPanel != null)
        {
            UpdatePanelPosition();
        }
    }

    private void UpdatePanelPosition()
    {
        if (mainCamera != null)
        {
            // Position the panel in front of the camera
            Vector3 targetPosition = mainCamera.transform.position + mainCamera.transform.forward * panelDistance;
            arPanel.transform.position = targetPosition;
            
            // Make the panel face the camera
            arPanel.transform.LookAt(mainCamera.transform);
            arPanel.transform.Rotate(0, 180, 0); // Flip to face the camera
        }
    }

    private void OnToggleButtonPressed(SelectEnterEventArgs args)
    {
        isVisible = !isVisible;
        if (arPanel != null)
        {
            arPanel.SetActive(isVisible);
        }
    }

    private void OnDestroy()
    {
        if (toggleButton != null)
        {
            toggleButton.selectEntered.RemoveListener(OnToggleButtonPressed);
        }
    }
} 