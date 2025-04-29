using UnityEngine;
using UnityEngine.UI;

public class PanelToggleButton : MonoBehaviour
{
    [SerializeField] private GameObject panelToToggle;
    private Button button;

    private void Start()
    {
        // Get the Button component
        button = GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError("PanelToggleButton: No Button component found!");
            return;
        }

        // Add the click listener
        button.onClick.AddListener(TogglePanel);

        // Verify panel reference
        if (panelToToggle == null)
        {
            Debug.LogError("PanelToggleButton: Panel to toggle is not assigned!");
        }
    }

    private void TogglePanel()
    {
        if (panelToToggle != null)
        {
            // Toggle the panel's active state
            panelToToggle.SetActive(!panelToToggle.activeSelf);
            Debug.Log($"PanelToggleButton: Panel toggled. New state: {panelToToggle.activeSelf}");
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(TogglePanel);
        }
    }
} 