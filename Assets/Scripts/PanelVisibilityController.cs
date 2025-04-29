using UnityEngine;
using UnityEngine.UI;

public class PanelVisibilityController : MonoBehaviour
{
    [SerializeField] private GameObject panelToToggle;
    [SerializeField] private Button toggleButton;
    
    private void Awake()
    {
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(TogglePanelVisibility);
        }
    }

    private void TogglePanelVisibility()
    {
        if (panelToToggle != null)
        {
            panelToToggle.SetActive(!panelToToggle.activeSelf);
        }
    }

    private void OnDestroy()
    {
        if (toggleButton != null)
        {
            toggleButton.onClick.RemoveListener(TogglePanelVisibility);
        }
    }
} 