using UnityEngine;

public class PanelFollower : MonoBehaviour
{
    private Transform xrOrigin;
    private Vector3 offset;
    
    public void Initialize(Transform origin)
    {
        xrOrigin = origin;
        offset = transform.position - xrOrigin.position;
    }
    
    void Update()
    {
        if (xrOrigin != null)
        {
            // Update position based on XR Origin's position and rotation
            transform.position = xrOrigin.position + xrOrigin.rotation * offset;
            transform.rotation = xrOrigin.rotation;
        }
    }
} 