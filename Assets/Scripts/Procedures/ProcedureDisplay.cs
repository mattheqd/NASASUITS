using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using System.Collections;
using System.Reflection;

// Attach this script to your Procedure UI panel in Unity
public class ProcedureDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject displayPanel;
    [SerializeField] private TextMeshProUGUI taskTitleText;
    [SerializeField] private TextMeshProUGUI stepCounterText;
    [SerializeField] private StepItem currentStepItem;
    [SerializeField] private Button nextStepButton;
    [SerializeField] private Button skipStepButton;
    [SerializeField] private Image taskProgressBar;
    [SerializeField] private Image stepLocationImage;
    [SerializeField] private List<Sprite> uiaStepSprites;

    [Header("Events")]
    public UnityEvent onProcedureCompleted;

    // Data
    private Procedure currentProcedure;
    private int currentTaskIndex = 0;
    private int currentStepIndex = 0;
    private Coroutine autoVerificationCoroutine;
    private Dictionary<string, Sprite> uiaSpriteMap;

    private void Awake()
    {
        uiaSpriteMap = new Dictionary<string, Sprite>();
        if (uiaStepSprites != null)
        {
            foreach (var sprite in uiaStepSprites)
            {
                uiaSpriteMap[sprite.name] = sprite;
            }
        }
    }

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
        StopAutoVerificationCoroutine();

        if (currentProcedure == null || currentTaskIndex >= currentProcedure.tasks.Count)
        {
            Debug.Log("[ProcedureDisplay] No more tasks or currentProcedure is null");
            CompleteProcedure();
            return;
        }
        var task = currentProcedure.tasks[currentTaskIndex];
        Debug.Log($"[ProcedureDisplay] Task: {task.taskName}, Steps: {task.instructionSteps?.Count ?? 0}");
        taskTitleText.text = $"Task: {task.taskName}";
        
        currentStepIndex = 0;
        
        if (stepCounterText != null)
        {
            stepCounterText.text = $"Step {currentStepIndex + 1}/{task.instructionSteps.Count}";
        }
        
        UpdateCurrentStepDisplay();
        TryStartAutoVerificationForCurrentStep();
    }

    public void NextStep()
    {
        Debug.Log($"[ProcedureDisplay] NextStep called. currentTaskIndex={currentTaskIndex}, currentStepIndex={currentStepIndex}");
        if (currentProcedure == null || currentTaskIndex >= currentProcedure.tasks.Count) return;

        StopAutoVerificationCoroutine();

        var task = currentProcedure.tasks[currentTaskIndex];
        if (currentStepItem != null)
        {
            currentStepItem.MarkCompleted(true);
        }
        currentStepIndex++;
        if (currentStepIndex >= task.instructionSteps.Count)
        {
            Debug.Log($"[ProcedureDisplay] Task complete. Moving to next task.");
            currentTaskIndex++;
            LoadCurrentTask();
            return;
        }
        
        if (stepCounterText != null)
        {
            stepCounterText.text = $"Step {currentStepIndex + 1}/{task.instructionSteps.Count}";
        }
        
        UpdateCurrentStepDisplay();
        TryStartAutoVerificationForCurrentStep();
    }

    public void SkipStep()
    {
        NextStep();
    }

    private void UpdateCurrentStepDisplay()
    {
        if (currentProcedure == null || currentTaskIndex >= currentProcedure.tasks.Count) return;
        var task = currentProcedure.tasks[currentTaskIndex];
        if (currentStepIndex >= task.instructionSteps.Count) return;

        var currentStep = task.instructionSteps[currentStepIndex];
        if (currentStepItem != null)
        {
            currentStepItem.SetStep(currentStepIndex + 1, currentStep.instructionText);
            currentStepItem.MarkCompleted(false);
        }
        if (taskProgressBar != null && task.instructionSteps.Count > 0)
        {
            taskProgressBar.fillAmount = ((float)currentStepIndex + 1) / task.instructionSteps.Count;
        }
        if (stepLocationImage != null)
        {
            if (currentStep.location == "UIA")
            {
                Sprite spriteToShow = null;
                if (uiaSpriteMap != null && uiaSpriteMap.TryGetValue(currentStep.targetKey, out spriteToShow))
                {
                    stepLocationImage.sprite = spriteToShow;
                    stepLocationImage.enabled = true;
                }
                else
                {
                    stepLocationImage.enabled = false;
                }
            }
            else
            {
                stepLocationImage.enabled = false;
            }
        }
    }

    private void CompleteProcedure()
    {
        // Do not hide the panel here; let the manager handle UI reset
        StopAutoVerificationCoroutine();
        onProcedureCompleted?.Invoke();
    }

    private void TryStartAutoVerificationForCurrentStep()
    {
        StopAutoVerificationCoroutine();

        if (currentProcedure == null || currentTaskIndex >= currentProcedure.tasks.Count) return;
        var task = currentProcedure.tasks[currentTaskIndex];
        if (currentStepIndex >= task.instructionSteps.Count) return;

        var currentStepInstruction = task.instructionSteps[currentStepIndex];
        if (currentStepInstruction.isAutoVerifiable)
        {
            Debug.Log($"[ProcedureDisplay] Starting auto-verification for step: {currentStepInstruction.instructionText}");
            autoVerificationCoroutine = StartCoroutine(CheckAutoVerification(currentStepInstruction));
        }
        else
        {
            Debug.Log($"[ProcedureDisplay] Step not auto-verifiable: {currentStepInstruction.instructionText}");
        }
    }

    private void StopAutoVerificationCoroutine()
    {
        if (autoVerificationCoroutine != null)
        {
            Debug.Log("[ProcedureDisplay] Stopping auto-verification coroutine.");
            StopCoroutine(autoVerificationCoroutine);
            autoVerificationCoroutine = null;
        }
    }

    private IEnumerator CheckAutoVerification(InstructionStep stepToVerify)
    {
        Debug.Log($"[ProcedureDisplay] Coroutine started for: {stepToVerify.instructionText}, Target: {stepToVerify.location}.{stepToVerify.targetKey} == {stepToVerify.targetValue}");
        while (true)
        {
            yield return new WaitForSeconds(0.5f);

            object dataObject = null;
            string locationUpper = stepToVerify.location?.ToUpper();

            switch (locationUpper)
            {
                case "UIA":
                    dataObject = WebSocketClient.LatestUiaData;
                    break;
                case "DCU":
                    dataObject = WebSocketClient.LatestDcuData;
                    break;
                default:
                    Debug.LogWarning($"[ProcedureDisplay] Unknown location for auto-verification: {stepToVerify.location}");
                    yield break;
            }

            if (dataObject == null)
            {
                continue;
            }

            FieldInfo field = dataObject.GetType().GetField(stepToVerify.targetKey);
            if (field == null)
            {
                Debug.LogError($"[ProcedureDisplay] Field '{stepToVerify.targetKey}' not found in {dataObject.GetType().Name} (Location: {stepToVerify.location}). Stopping verification for this step.");
                yield break;
            }

            object fieldValue = field.GetValue(dataObject);
            if (fieldValue == null)
            {
                continue;
            }

            string currentValueString;
            if (fieldValue is float floatVal)
            {
                currentValueString = floatVal.ToString("F0");
            }
            else
            {
                currentValueString = fieldValue.ToString();
            }
            
            if (currentValueString.Equals(stepToVerify.targetValue))
            {
                Debug.Log($"[ProcedureDisplay] Auto-verified step: '{stepToVerify.instructionText}'. Current value '{currentValueString}' matches target '{stepToVerify.targetValue}'.");
                NextStep();
                yield break;
            }
        }
    }
}

// Example data classes for deserialization
[System.Serializable]
public class Procedure
{
    public string procedureName;
    public List<Task> tasks;
}

[System.Serializable]
public class Task
{
    public string taskName;
    public List<InstructionStep> instructionSteps;
}

[System.Serializable]
public class InstructionStep
{
    public string instructionText;
    public bool isAutoVerifiable;
    public string location;
    public string targetKey;
    public string targetValue;
}