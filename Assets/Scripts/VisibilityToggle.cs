using UnityEngine;
using UnityEngine.UI;

public class VisibilityToggle : MonoBehaviour
{
    public GameObject targetObject;
    public Button toggleButton;
    private bool isVisible = true;

    void Start()
    {
        if (targetObject == null)
        {
            targetObject = gameObject;
        }

        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(ToggleVisibility);
        }
    }

    public void ToggleVisibility()
    {
        isVisible = !isVisible;
        targetObject.SetActive(isVisible);
    }
} 