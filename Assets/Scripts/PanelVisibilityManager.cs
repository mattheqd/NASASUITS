using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class PanelVisibilityManager : MonoBehaviour
{
    [SerializeField] private GameObject[] panels; // Array of panels to control
    [SerializeField] private XRSimpleInteractable toggleButton; // The button that will toggle visibility
    [SerializeField] private bool startVisible = true; // Whether panels should be visible at start

    private bool isVisible;

    private void Start()
    {
        // Initialize visibility state
        isVisible = startVisible;
        UpdatePanelVisibility();

        // Subscribe to button events
        if (toggleButton != null)
        {
            toggleButton.selectEntered.AddListener(OnToggleButtonPressed);
        }
    }

    private void OnToggleButtonPressed(SelectEnterEventArgs args)
    {
        // Toggle visibility state
        isVisible = !isVisible;
        UpdatePanelVisibility();
    }

    private void UpdatePanelVisibility()
    {
        foreach (GameObject panel in panels)
        {
            if (panel != null)
            {
                panel.SetActive(isVisible);
            }
        }
    }

    private void OnDestroy()
    {
        // Clean up event listeners
        if (toggleButton != null)
        {
            toggleButton.selectEntered.RemoveListener(OnToggleButtonPressed);
        }
    }
} 