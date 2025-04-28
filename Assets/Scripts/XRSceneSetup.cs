using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Hands;

public class XRSceneSetup : MonoBehaviour
{
    [SerializeField]
    private GameObject persistentPanelPrefab;
    
    private XRHandTrackingEvents handTrackingEvents;
    private GameObject persistentPanel;
    
    void Start()
    {
        // Set up hand tracking
        SetupHandTracking();
        
        // Create and position the persistent panel
        SetupPersistentPanel();
    }
    
    private void SetupHandTracking()
    {
        // Create hand tracking events component
        handTrackingEvents = gameObject.AddComponent<XRHandTrackingEvents>();
        handTrackingEvents.updateType = XRHandTrackingEvents.UpdateTypes.Dynamic;
        
        // Subscribe to hand tracking events
        handTrackingEvents.jointsUpdated.AddListener(OnHandJointsUpdated);
    }
    
    private void SetupPersistentPanel()
    {
        if (persistentPanelPrefab != null)
        {
            // Create the panel
            persistentPanel = Instantiate(persistentPanelPrefab);
            
            // Make it a child of the XR Origin
            persistentPanel.transform.SetParent(transform, false);
            
            // Position it in front of the user
            persistentPanel.transform.localPosition = new Vector3(0, 0, 2f);
            persistentPanel.transform.localRotation = Quaternion.identity;
            
            // Add a script to make it follow the user's gaze
            var panelFollower = persistentPanel.AddComponent<PanelFollower>();
            panelFollower.Initialize(transform);
        }
        else
        {
            Debug.LogWarning("Persistent Panel Prefab not assigned in XRSceneSetup!");
        }
    }
    
    private void OnHandJointsUpdated(XRHandJointsUpdatedEventArgs args)
    {
        // Handle hand tracking updates here
        // This will be called whenever hand joint data is updated
    }
} 