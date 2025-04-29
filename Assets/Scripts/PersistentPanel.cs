using UnityEngine;
using UnityEngine.UI;

public class PersistentPanel : MonoBehaviour
{
    [SerializeField]
    private Canvas canvas;
    
    [SerializeField]
    private float panelDistance = 2f;
    
    void Start()
    {
        if (canvas != null)
        {
            // Set up the canvas for XR
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            
            // Set the panel's initial position
            transform.localPosition = new Vector3(0, 0, panelDistance);
            transform.localRotation = Quaternion.identity;
            
            // Make the background transparent
            var image = GetComponent<Image>();
            if (image != null)
            {
                image.color = new Color(1, 1, 1, 0.8f); // Semi-transparent white
            }
        }
        else
        {
            Debug.LogWarning("Canvas component not assigned in PersistentPanel!");
        }
    }
    
    void Update()
    {
        // Make the panel always face the camera
        if (Camera.main != null)
        {
            transform.LookAt(Camera.main.transform);
            transform.Rotate(0, 180, 0); // Flip it so text is readable
        }
    }
} 