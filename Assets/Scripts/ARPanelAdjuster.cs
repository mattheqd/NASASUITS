using UnityEngine;

public class ARPanelAdjuster : MonoBehaviour
{
    [SerializeField] private float distanceFromCamera = 0.5f;
    [SerializeField] private Vector2 panelSize = new Vector2(800, 600); // Width and height in pixels
    [SerializeField] private Vector2 screenPosition = new Vector2(0.5f, 0.5f); // Center of screen (0-1)
    
    private RectTransform rectTransform;
    private Canvas canvas;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponent<Canvas>();
        
        if (rectTransform != null)
        {
            // Set the size
            rectTransform.sizeDelta = panelSize;
            
            // Set the scale to make it visible in world space
            float scale = 0.001f; // Adjust this value to make the panel a good size
            rectTransform.localScale = new Vector3(scale, scale, scale);
            
            // Position the panel
            UpdatePosition();
        }
    }

    private void Update()
    {
        UpdatePosition();
    }

    private void UpdatePosition()
    {
        if (rectTransform != null && Camera.main != null)
        {
            // Calculate position based on screen position
            Vector3 viewportPoint = new Vector3(screenPosition.x, screenPosition.y, distanceFromCamera);
            Vector3 worldPosition = Camera.main.ViewportToWorldPoint(viewportPoint);
            
            // Set the position
            transform.position = worldPosition;
            
            // Make the panel face the camera
            transform.LookAt(Camera.main.transform);
            transform.Rotate(0, 180, 0); // Flip to face the camera
        }
    }
} 