using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class HandToggleButton : MonoBehaviour
{
    [SerializeField] private GameObject panelToControl;
    private XRSimpleInteractable interactable;
    private BoxCollider boxCollider;
    private bool isHovered = false;
    private float hoverStartTime;
    private const float HOVER_DURATION = 0.5f; // Time in seconds to hold hover before toggling

    private void Start()
    {
        Debug.Log("HandToggleButton: Start called");
        
        // Get or add XRSimpleInteractable
        interactable = GetComponent<XRSimpleInteractable>();
        if (interactable == null)
        {
            Debug.Log("HandToggleButton: Adding XRSimpleInteractable component");
            interactable = gameObject.AddComponent<XRSimpleInteractable>();
        }

        // Get or add BoxCollider
        boxCollider = GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            Debug.Log("HandToggleButton: Adding BoxCollider component");
            boxCollider = gameObject.AddComponent<BoxCollider>();
        }

        // Configure BoxCollider
        boxCollider.isTrigger = false;
        boxCollider.size = new Vector3(200, 80, 1);
        boxCollider.center = Vector3.zero;

        // Configure XRSimpleInteractable
        interactable.selectMode = InteractableSelectMode.Single;
        interactable.interactionLayers = InteractionLayerMask.GetMask("Default", "Hand");

        // Remove any existing listeners to prevent duplicates
        interactable.selectEntered.RemoveAllListeners();
        interactable.hoverEntered.RemoveAllListeners();
        interactable.hoverExited.RemoveAllListeners();
        interactable.selectExited.RemoveAllListeners();
        
        // Add event listeners
        interactable.selectEntered.AddListener(OnSelectEntered);
        interactable.hoverEntered.AddListener(OnHoverEntered);
        interactable.hoverExited.AddListener(OnHoverExited);
        interactable.selectExited.AddListener(OnSelectExited);
        
        Debug.Log("HandToggleButton: Event listeners added");

        // Verify panel reference
        if (panelToControl == null)
        {
            Debug.LogError("HandToggleButton: Panel to control is not assigned!");
        }

    
    }

    private void Update()
    {
        if (isHovered && Time.time - hoverStartTime >= HOVER_DURATION)
        {
            Debug.Log("HandToggleButton: Hover duration reached, toggling panel");
            TogglePanel();
            isHovered = false; // Reset hover state after toggle
        }
    }

    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        Debug.Log($"HandToggleButton: Hover entered! Interactor: {args.interactorObject.transform.name}, Type: {args.interactorObject.GetType()}");
        isHovered = true;
        hoverStartTime = Time.time;
    }

    private void OnHoverExited(HoverExitEventArgs args)
    {
        Debug.Log($"HandToggleButton: Hover exited! Interactor: {args.interactorObject.transform.name}, Type: {args.interactorObject.GetType()}");
        isHovered = false;
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        Debug.Log($"HandToggleButton: Select entered! Interactor: {args.interactorObject.transform.name}, Type: {args.interactorObject.GetType()}");
        TogglePanel();
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        Debug.Log($"HandToggleButton: Select exited! Interactor: {args.interactorObject.transform.name}, Type: {args.interactorObject.GetType()}");
    }

    private void TogglePanel()
    {
        if (panelToControl != null)
        {
            Debug.Log($"HandToggleButton: Toggling panel. Current state: {panelToControl.activeSelf}");
            panelToControl.SetActive(!panelToControl.activeSelf);
            Debug.Log($"HandToggleButton: New panel state: {panelToControl.activeSelf}");
        }
        else
        {
            Debug.LogError("HandToggleButton: Panel reference is null when trying to toggle!");
        }
    }

    private void OnDestroy()
    {
        if (interactable != null)
        {
            interactable.selectEntered.RemoveListener(OnSelectEntered);
            interactable.hoverEntered.RemoveListener(OnHoverEntered);
            interactable.hoverExited.RemoveListener(OnHoverExited);
            interactable.selectExited.RemoveListener(OnSelectExited);
        }
    }
} 