using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class SimpleARPanel : MonoBehaviour
{
    [SerializeField] private GameObject panel; // The panel to show/hide
    [SerializeField] private bool startVisible = true;

    private Camera mainCamera;
    private bool isVisible;

    private void Start()
    {
        mainCamera = Camera.main;
        isVisible = startVisible;
        
        if (panel != null)
        {
            panel.SetActive(isVisible);
        }
    }

    public void ToggleVisibility()
    {
        isVisible = !isVisible;
        if (panel != null)
        {
            panel.SetActive(isVisible);
        }
    }
} 