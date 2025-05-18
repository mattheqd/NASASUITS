using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

// Attach this script to your Procedure UI panel in Unity
public class ProcedureDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject displayPanel;
    [SerializeField] private TextMeshProUGUI taskTitleText;
    [SerializeField] private Transform stepsPanel;
    [SerializeField] private GameObject stepItemPrefab;
    [SerializeField] private Button nextStepButton;
    [SerializeField] private Button skipStepButton;

    [Header("Events")]
    public UnityEvent onProcedureCompleted;

    // Data
    private Procedure currentProcedure;
    private int currentTaskIndex = 0;
    private int currentStepIndex = 0;
    private List<GameObject> stepItems = new List<GameObject>();

    // Call this to start a procedure
    public void LoadProcedure(Procedure procedure)
    {
        Debug.Log($"[ProcedureDisplay] LoadProcedure called for: {(procedure != null ? procedure.procedureName : "null")}");
        if (procedure == null)
        {
            Debug.LogError("[ProcedureDisplay] Procedure is null!");
            return;
        }
        Debug.Log($"[ProcedureDisplay] Procedure has {procedure.tasks?.Count ?? 0} tasks");
        currentProcedure = procedure;
        currentTaskIndex = 0;
        currentStepIndex = 0;
        displayPanel.SetActive(true);
        LoadCurrentTask();
    }

    private void LoadCurrentTask()
    {
        Debug.Log($"[ProcedureDisplay] LoadCurrentTask: currentTaskIndex={currentTaskIndex}");
        ClearStepItems();
        if (currentProcedure == null || currentTaskIndex >= currentProcedure.tasks.Count)
        {
            Debug.Log("[ProcedureDisplay] No more tasks or currentProcedure is null");
            CompleteProcedure();
            return;
        }
        var task = currentProcedure.tasks[currentTaskIndex];
        Debug.Log($"[ProcedureDisplay] Task: {task.taskName}, Steps: {task.instructionSteps?.Count ?? 0}");
        taskTitleText.text = $"{currentProcedure.procedureName} - {task.taskName}";
        currentStepIndex = 0;
        for (int i = 0; i < task.instructionSteps.Count; i++)
        {
            GameObject stepObj = Instantiate(stepItemPrefab, stepsPanel);
            StepItem stepItem = stepObj.GetComponent<StepItem>();
            if (stepItem != null)
            {
                stepItem.SetStep(i + 1, task.instructionSteps[i].instructionText);
                stepItem.MarkCompleted(false);
                stepItem.SetActiveStep(i == currentStepIndex);
            }
            stepItems.Add(stepObj);
        }
        UpdateStepDisplay();
    }

    public void NextStep()
    {
        Debug.Log($"[ProcedureDisplay] NextStep called. currentTaskIndex={currentTaskIndex}, currentStepIndex={currentStepIndex}");
        if (currentProcedure == null) return;
        var task = currentProcedure.tasks[currentTaskIndex];
        // Mark current step as completed
        if (currentStepIndex < stepItems.Count)
        {
            StepItem stepItem = stepItems[currentStepIndex].GetComponent<StepItem>();
            if (stepItem != null) stepItem.MarkCompleted(true);
        }
        currentStepIndex++;
        if (currentStepIndex >= task.instructionSteps.Count)
        {
            Debug.Log($"[ProcedureDisplay] Task complete. Moving to next task.");
            // Move to next task
            currentTaskIndex++;
            LoadCurrentTask();
            return;
        }
        UpdateStepDisplay();
    }

    public void SkipStep()
    {
        NextStep();
    }

    private void UpdateStepDisplay()
    {
        for (int i = 0; i < stepItems.Count; i++)
        {
            StepItem stepItem = stepItems[i].GetComponent<StepItem>();
            if (stepItem != null)
                stepItem.SetActiveStep(i == currentStepIndex);
        }
    }

    private void ClearStepItems()
    {
        foreach (var obj in stepItems)
        {
            Destroy(obj);
        }
        stepItems.Clear();
    }

    private void CompleteProcedure()
    {
        // Do not hide the panel here; let the manager handle UI reset
        onProcedureCompleted?.Invoke();
    }
}

// Example data classes for deserialization
[System.Serializable]
public class Procedure
{
    public string procedureName;
    public string procedureDescription;
    public List<Task> tasks;
}

[System.Serializable]
public class Task
{
    public string taskName;
    public string taskDescription;
    public List<InstructionStep> instructionSteps;
}

[System.Serializable]
public class InstructionStep
{
    public string instructionText;
} 