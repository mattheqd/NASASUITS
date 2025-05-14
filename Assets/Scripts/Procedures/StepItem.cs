using UnityEngine;
using TMPro;

public class StepItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI stepNumberText;
    [SerializeField] private TextMeshProUGUI stepInstructionText;
    [SerializeField] private GameObject completionIndicator; // Optional indicator for completion
    [SerializeField] private GameObject activeIndicator; // Optional indicator for active step

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
            
        // Optional: Change the color to indicate active state
        if (active && stepNumberText != null && stepInstructionText != null)
        {
            Color activeColor = new Color(0.145f, 0.145f, 0.145f); // #252525
            stepNumberText.color = activeColor;
            stepInstructionText.color = activeColor;
        }
    }
} 