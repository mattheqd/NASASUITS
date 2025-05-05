using UnityEngine;
using TMPro;

public class StepItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI stepNumberText;
    [SerializeField] private TextMeshProUGUI stepInstructionText;

    public void SetStep(int stepNumber, string instruction)
    {
        if (stepNumberText != null)
            stepNumberText.text = $"Step {stepNumber}";
        if (stepInstructionText != null)
            stepInstructionText.text = instruction;
    }

    public void SetColor(Color color)
    {
        if (stepNumberText != null)
            stepNumberText.color = color;
        if (stepInstructionText != null)
            stepInstructionText.color = color;
    }
} 