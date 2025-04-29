using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(Canvas))]
public class XRCanvasSetup : MonoBehaviour
{
    private Canvas canvas;
    private XRInteractionManager interactionManager;

    private void Start()
    {
        canvas = GetComponent<Canvas>();
        
        // Set up the canvas for XR
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        canvas.sortingOrder = 1;
        
        // Make sure we're on the UI layer
        gameObject.layer = LayerMask.NameToLayer("UI");

        // Find or create XR Interaction Manager
        interactionManager = FindObjectOfType<XRInteractionManager>();
        if (interactionManager == null)
        {
            Debug.LogError("No XR Interaction Manager found in scene. Please add one.");
        }
    }

    private void Update()
    {
        // Ensure the canvas is always facing the camera
        if (canvas.worldCamera != null)
        {
            transform.LookAt(canvas.worldCamera.transform);
            transform.Rotate(0, 180, 0); // Flip to face the camera
        }
    }
} 