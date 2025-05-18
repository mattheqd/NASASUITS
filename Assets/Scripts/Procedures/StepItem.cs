using UnityEngine;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(LayoutElement))]
public class StepItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI stepNumberText;
    [SerializeField] private TextMeshProUGUI stepInstructionText;
    [SerializeField] private GameObject completionIndicator; // Optional indicator for completion
    [SerializeField] private GameObject activeIndicator; // Optional indicator for active step
    private LayoutElement layoutElement;

    //private AutomationTrigger currentAutoTrigger; // Added for automation
    //public AutomationTrigger CurrentAutoTrigger => currentAutoTrigger; // Getter for automation trigger

    private void Awake()
    {
        layoutElement = GetComponent<LayoutElement>();
        if (layoutElement != null)
        {
            // Make height flexible based on content
            layoutElement.preferredHeight = -1;
            layoutElement.flexibleHeight = 1;
        }
    }

    // Original method - Keep for backward compatibility
    public void SetStep(int stepNumber, string instruction)
    {
        ConfigureStep(stepNumber, instruction);
    }

    // New method called by ProcedureDisplay
    public void ConfigureStep(int stepNumber, string instruction)
    {
        if (stepNumberText != null)
            stepNumberText.text = $"Step {stepNumber}";
        if (stepInstructionText != null)
            stepInstructionText.text = instruction;
    }

    // Original method - Keep for backward compatibility
    public void SetColor(Color color)
    {
        SetStepColor(color);
    }
    
    // New method called by ProcedureDisplay
    public void SetStepColor(Color color)
    {
        if (stepNumberText != null)
            stepNumberText.color = color;
        if (stepInstructionText != null)
            stepInstructionText.color = color;
    }
    
    // Methods for step state
    public void MarkCompleted(bool completed)
    {
        if (completionIndicator != null)
            completionIndicator.SetActive(completed);
            
        // Optional: Change the color to indicate completion
        if (completed && stepNumberText != null && stepInstructionText != null)
        {
            Color completedColor = new Color(0.556f, 0.556f, 0.556f); // #8E8E8E
            stepNumberText.color = completedColor;
            stepInstructionText.color = completedColor;
        }
    }
    
    public void SetActiveStep(bool active)
    {
        if (activeIndicator != null)
            activeIndicator.SetActive(active);
    }

    // public void SetAutomationTrigger(AutomationTrigger trigger) // Added for automation
    // {
    //     currentAutoTrigger = trigger;
    // }

    // Placeholder for DCUManager interaction
    // You'll need to replace this with your actual DCU data access
    public interface IDCUManager 
    {
        string GetValue(string key);
    }

    // public bool ShouldAutoComplete(IDCUManager dcuManager) // Added for automation
    // {
    //     if (dcuManager == null || currentAutoTrigger == null || !currentAutoTrigger.enabled)
    //     {
    //         return false;
    //     }
    //     // Replace this with your actual DCU value checking logic
    //     string currentValue = dcuManager.GetValue(currentAutoTrigger.dcuKey);
    //     return currentValue == currentAutoTrigger.expectedValue;
    // }
} 