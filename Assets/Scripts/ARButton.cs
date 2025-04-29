using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ARButton : XRSimpleInteractable
{
    [SerializeField] private SimpleARPanel panelToControl;

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        if (panelToControl != null)
        {
            panelToControl.ToggleVisibility();
        }
    }
} 