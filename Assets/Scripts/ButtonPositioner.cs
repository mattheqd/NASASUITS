using UnityEngine;
using UnityEngine.UI;

public class ButtonPositioner : MonoBehaviour
{
    [SerializeField] private RectTransform panelRectTransform;
    [SerializeField] private Vector2 buttonOffset = new Vector2(0, 0); // Offset from panel's position
    [SerializeField] private float buttonScale = 1f;

    private RectTransform buttonRectTransform;
    private Canvas canvas;
    private Vector3 lastPanelPosition;

    private void Start()
    {
        buttonRectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        if (buttonRectTransform == null || panelRectTransform == null)
        {
            Debug.LogError("ButtonPositioner requires RectTransform components on both button and panel!");
            return;
        }

        // Store initial panel position
        lastPanelPosition = panelRectTransform.position;
        
        // Set initial button position
        UpdateButtonPosition();
    }

    private void Update()
    {
        // Only update if panel position has changed
        if (panelRectTransform.position != lastPanelPosition)
        {
            UpdateButtonPosition();
            lastPanelPosition = panelRectTransform.position;
        }
    }

    private void UpdateButtonPosition()
    {
        if (buttonRectTransform == null || panelRectTransform == null) return;

        // Get the panel's current position
        Vector3 panelPosition = panelRectTransform.position;
        
        // Calculate button position based on panel's position and offset
        buttonRectTransform.position = new Vector3(
            panelPosition.x + buttonOffset.x,
            panelPosition.y + buttonOffset.y,
            panelPosition.z
        );

        // Make the button face the camera
        if (Camera.main != null)
        {
            transform.rotation = Camera.main.transform.rotation;
        }

        // Set the scale
        buttonRectTransform.localScale = new Vector3(buttonScale, buttonScale, buttonScale);
    }
} 