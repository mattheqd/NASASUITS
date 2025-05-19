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
    private Image backgroundImage; // Reference to the background image component
    private bool isCompleted = false; // Track completion state
    private bool isActive = false; // Track active state

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
        
        // Get the background image component
        backgroundImage = GetComponent<Image>();
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
        isCompleted = completed;
        if (completionIndicator != null)
            completionIndicator.SetActive(completed);
            
        // Only update text color if not active
        if (!isActive && stepNumberText != null && stepInstructionText != null)
        {
            Color textColor = completed ? new Color(0.556f, 0.556f, 0.556f) : Color.white; // Grey when completed, white when not
            stepNumberText.color = textColor;
            stepInstructionText.color = textColor;
        }
    }
    
    public void SetActiveStep(bool active)
    {
        isActive = active;
        if (activeIndicator != null)
            activeIndicator.SetActive(active);
            
        // Change the background color to cyan when active and text to black
        if (backgroundImage != null)
        {
            if (active)
            {
                backgroundImage.color = new Color(0, 1, 1, 1); // Cyan color
                if (stepNumberText != null)
                    stepNumberText.color = Color.black;
                if (stepInstructionText != null)
                    stepInstructionText.color = Color.black;
            }
            else
            {
                backgroundImage.color = new Color(0.16862746f, 0.1764706f, 0.18431373f, 1); // Default dark color
                // Reset text color based on completion state
                Color textColor = isCompleted ? new Color(0.556f, 0.556f, 0.556f) : Color.white;
                if (stepNumberText != null)
                    stepNumberText.color = textColor;
                if (stepInstructionText != null)
                    stepInstructionText.color = textColor;
            }
        }
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