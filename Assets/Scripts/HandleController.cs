using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class HandleController : MonoBehaviour
{
    [SerializeField] private Transform tableTransform;
    [SerializeField] private float followSpeed = 5f;
    
    private XRGrabInteractable grabInteractable;
    private Vector3 offset;
    private bool isGrabbed = false;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        
        // Set up grab events
        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        isGrabbed = true;
        offset = tableTransform.position - transform.position;
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        isGrabbed = false;
    }

    private void Update()
    {
        if (isGrabbed)
        {
            // Smoothly move the table to follow the handle
            Vector3 targetPosition = transform.position + offset;
            tableTransform.position = Vector3.Lerp(tableTransform.position, targetPosition, Time.deltaTime * followSpeed);
        }
    }

    private void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrab);
            grabInteractable.selectExited.RemoveListener(OnRelease);
        }
    }
} 