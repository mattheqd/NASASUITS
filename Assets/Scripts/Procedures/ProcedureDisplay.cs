//* Manager to control the display of procedure content, step progression, and updating the ui to reflect the procedure state
//* The Procedure Flow Manager will call this class to display the procedure or steps

/**
 * These are the UI rules for the display:
 * ------ COLORS ------
 * - inProgressTextColor: #252525
 * - inProgressBackgroundColor: #00DDFF
 * - completedTextColor: #8E8E8E
 * - defaultTextColor: #FFFFFF
 * ------ DISPLAY RULES ------
 * - The progress bar should display according to the number of steps completed for each procedure
 * - Each step in a task will be accompanied by a number according to its order
 * - A check mark will display on any tasks that are finished
 * - If a task is in progress, a turning wheel will display
 * ------ DATA STRUCTURE ------
 * - ProcedureName: Stores a series of tasks (ex: Egress, Ingress, etc.)
 * - TaskName: stores a series of steps (ex: "Verify LTV Location", "Connect UIA to DCU and start Depress ")
 * - StepName: stores a single step (ex: "Verify ping has been received from LTV", "Verify worksite POI locations have been provided by LTV")
 * EXAMPLE:
 * - TitleText: "Procedure: Open the airlock door"
 * - Step text: "Step 1 of 3"
 * - Step indicators: 3 circles, 1 circle is green, 1 circle is gray, 1 circle is gray
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

// Enum to track the status of instruction steps
public enum ProcedureStepStatus
{
    NotStarted,
    InProgress,
    Completed,
    Skipped
}

// Class to define groups of interface elements
public class ProcedureDisplay : MonoBehaviour
{
    /*------ UI Components ------*/
    // UI components for panel (title, panel, description, step number, progress)
    [Header("UI References")]
    [SerializeField] private GameObject DisplayPanel; // main panel for the display of procedures and steps
    [SerializeField] private TextMeshProUGUI TitleText; // ex: "Procedure: Open the airlock door" or "Task: Egress"
    // [SerializeField] private TextMeshProUGUI procedureDescriptionText;
    [SerializeField] private TextMeshProUGUI TaskStepText; // ex: "Step 1 of 3"
    [SerializeField] private TextMeshProUGUI TaskProgressText; // Shows "X/Y steps completed"
    [SerializeField] private Transform TaskStepsPanel; // Container for all steps
    [SerializeField] private StepItem TaskStepItemPrefab; // Prefab for individual step items

    // Navigation buttons to move next or start procedure
    [Header("Navigation Controls")]
    [SerializeField] private Button TaskNextButton; // go to the next task
    // [SerializeField] private Button procedureCompleteStepButton; 
    [SerializeField] private Button TaskSkipStepButton; // skip the current step
    // removing this button for now
    // [SerializeField] private Button procedureNextTaskButton; // go to the next task

    // Progress indicators (i.e. progress bar)
    [Header("Progress Indicators")]
    [SerializeField] private Transform TaskStepIndicatorContainer;
    [SerializeField] private GameObject TaskStepIndicatorPrefab;
    [SerializeField] private Color ActiveStepColor = Color.cyan;
    [SerializeField] private Color InactiveStepColor = Color.gray;
    [SerializeField] private Color CompletedStepColor = Color.cyan;

    // Events triggered when user interacts with UI
    public UnityEvent onProcedureCompleted;

    // store current procedure as a reference
    private Procedure CurrentProcedure; 
    private int currentStepIndex = 0;
    private List<GameObject> TaskStepIndicators = new List<GameObject>(); // list of step indicators for the progress bar
    private List<GameObject> TaskStepItems = new List<GameObject>(); // list of step item GameObjects

    //*------ Functions to control the display ------*/
    // Initialize the display
    private void Awake()
    {
        // Check for required components
        if (TitleText == null || TaskStepText == null || 
            TaskNextButton == null ||
            TaskStepIndicatorContainer == null || TaskStepIndicatorPrefab == null ||
            TaskProgressText == null || TaskStepsPanel == null || TaskStepItemPrefab == null)
        {
            Debug.LogError("ProcedureDisplay: Missing required UI component references!");
        }
        
        // Initialize the procedure display
        InitializeProcedureDisplay();
    }

    private void InitializeProcedureDisplay()
    {
        // Set up initial state
        if (TitleText != null) TitleText.text = "Procedure";
        if (TaskStepText != null) TaskStepText.text = "";
        if (TaskProgressText != null) TaskProgressText.text = "0/0 steps completed";
        
        // Set up button listeners
        if (TaskNextButton != null) TaskNextButton.onClick.AddListener(NextStep);
        if (TaskSkipStepButton != null) TaskSkipStepButton.onClick.AddListener(SkipStep);

        CreateTaskStepIndicators(0); // Create step indicators with 0 steps initially
    }

    // Remove listeners for buttons when the display is no longer in view
    private void OnDestroy()
    {
        if (TaskNextButton != null)
            TaskNextButton.onClick.RemoveListener(NextStep);
        if (TaskSkipStepButton != null)
            TaskSkipStepButton.onClick.RemoveListener(SkipStep);
    }

    // Load a procedure with all its steps
    public void LoadProcedure(Procedure procedure)
    {
        CurrentProcedure = procedure;
        currentStepIndex = 0;

        // Hide all parent game objects if it's null
        if (CurrentProcedure == null)
        {
            if (DisplayPanel != null)
                DisplayPanel.SetActive(false);
            return;
        }

        // Show the panel
        if (DisplayPanel != null)
            DisplayPanel.SetActive(true);

        // Update the title
        if (TitleText != null)
            TitleText.text = CurrentProcedure.procedureName;

        // Create step indicators based on step count
        CreateTaskStepIndicators(CurrentProcedure.instructionSteps.Count);

        // Create the step items
        CreateTaskStepItems(CurrentProcedure.instructionSteps);

        // Display the first step
        DisplayCurrentStep();

    }

    // Move to the next step
    public void NextStep()
    {
        // Don't move if there's no procedure or steps
        if (CurrentProcedure == null || CurrentProcedure.instructionSteps.Count == 0)
        {
            Debug.LogWarning("ProcedureDisplay: Cannot advance step - no procedure loaded");
            return;
        }

        // Check if this is the last step
        if (currentStepIndex >= CurrentProcedure.instructionSteps.Count - 1)
        {
            // We're at the last step, mark procedure as complete
            Debug.Log("ProcedureDisplay: Procedure complete!");
            onProcedureCompleted?.Invoke();
            return;
        }

        // Increment the step index and update display
        currentStepIndex++;
        DisplayCurrentStep();
        
        Debug.Log($"ProcedureDisplay: Advanced to step {currentStepIndex + 1} of {CurrentProcedure.instructionSteps.Count}");
    }

    // Skip the current step
    public void SkipStep()
    {
        NextStep();
        Debug.Log("ProcedureDisplay: Skipped current step");
    }

    // Jump to a specific step index
    public void JumpToStep(int stepIndex)
    {
        // Don't move if there's no procedure or steps
        if (CurrentProcedure == null || CurrentProcedure.instructionSteps.Count == 0)
        {
            Debug.LogWarning("ProcedureDisplay: Cannot jump to step - no procedure loaded");
            return;
        }

        // Bounds check
        if (stepIndex < 0 || stepIndex >= CurrentProcedure.instructionSteps.Count)
        {
            Debug.LogError($"ProcedureDisplay: Step index {stepIndex} is out of bounds (0-{CurrentProcedure.instructionSteps.Count - 1})");
            return;
        }

        // Set the current step and update display
        currentStepIndex = stepIndex;
        DisplayCurrentStep();
        
        Debug.Log($"ProcedureDisplay: Jumped to step {currentStepIndex + 1} of {CurrentProcedure.instructionSteps.Count}");
    }

    // Wait for canvas updates to avoid layout issues
    private IEnumerator WaitForCanvasUpdate()
    {
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
    }

    // Display the current step
    private void DisplayCurrentStep()
    {
        if (CurrentProcedure == null || currentStepIndex < 0)
        {
            // No step is active
            TaskStepText.text = "";
            TaskProgressText.text = $"0/{CurrentProcedure?.instructionSteps.Count ?? 0} steps completed";
            return;
        }

        if (currentStepIndex >= CurrentProcedure.instructionSteps.Count)
            return;

        // Update step text
        TaskStepText.text = CurrentProcedure.instructionSteps[currentStepIndex].instructionText;
        TaskProgressText.text = $"{currentStepIndex + 1}/{CurrentProcedure.instructionSteps.Count} steps completed";

        // Update step indicators
        for (int i = 0; i < TaskStepIndicators.Count; i++)
        {
            if (TaskStepIndicators[i] != null)
            {
                Image indicatorImage = TaskStepIndicators[i].GetComponent<Image>();
                if (indicatorImage != null)
                {
                    if (i < currentStepIndex)
                        indicatorImage.color = CompletedStepColor;
                    else if (i == currentStepIndex)
                        indicatorImage.color = ActiveStepColor;
                    else
                        indicatorImage.color = InactiveStepColor;
                }
            }
        }

        // Update step items
        for (int i = 0; i < TaskStepItems.Count; i++)
        {
            if (TaskStepItems[i] != null)
            {
                StepItem stepItem = TaskStepItems[i].GetComponent<StepItem>();
                if (stepItem != null)
                {
                    if (i < currentStepIndex)
                        stepItem.MarkCompleted(true);
                    else if (i == currentStepIndex)
                        stepItem.SetActiveStep(true);
                    else
                    {
                        stepItem.MarkCompleted(false);
                        stepItem.SetActiveStep(false);
                    }
                }
            }
        }
    }

    // step count is based on number of steps required in current procedure
    private void CreateTaskStepIndicators(int stepCount)
    {
        // clear existing indicators
        foreach (var indicator in TaskStepIndicators) {
            if (indicator != null)
                Destroy(indicator);
        }
        TaskStepIndicators.Clear();

        // create new indicators
        for (int i = 0; i < stepCount; ++i) {
            GameObject indicator = Instantiate(TaskStepIndicatorPrefab, TaskStepIndicatorContainer);
            TaskStepIndicators.Add(indicator);
            
            // Set initial color
            Image indicatorImage = indicator.GetComponent<Image>();
            if (indicatorImage != null)
                indicatorImage.color = InactiveStepColor;
        }
    }

    // Create step items in the steps panel
    private void CreateTaskStepItems(List<InstructionStep> steps)
    {
        // Clear existing step items
        foreach (var item in TaskStepItems)
        {
            if (item != null)
                Destroy(item);
        }
        TaskStepItems.Clear();

        // Ensure steps panel is properly positioned relative to master container
        RectTransform panelRect = TaskStepsPanel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            // Reset any existing offset
            panelRect.anchoredPosition = Vector2.zero;
        }

        // Create new step items
        for (int i = 0; i < steps.Count; i++)
        {
            // Create the step item and ensure it's properly parented
            StepItem stepItem = Instantiate(TaskStepItemPrefab);
            stepItem.transform.SetParent(TaskStepsPanel, false);
            TaskStepItems.Add(stepItem.gameObject);

            // Set step number and text
            stepItem.ConfigureStep(i + 1, steps[i].instructionText);
            stepItem.SetStepColor(InactiveStepColor);
        }

        // Force multiple canvas updates
        Canvas.ForceUpdateCanvases();
        StartCoroutine(WaitForCanvasUpdate());
    }

    public void CompleteCurrentStep()
    {
        if (CurrentProcedure == null || currentStepIndex < 0 || 
            currentStepIndex >= CurrentProcedure.instructionSteps.Count)
        {
            Debug.LogWarning("ProcedureDisplay: Cannot complete step - invalid procedure or step index");
            return;
        }
        
        // Mark the current step as completed
        if (currentStepIndex < TaskStepItems.Count && TaskStepItems[currentStepIndex] != null)
        {
            StepItem stepItem = TaskStepItems[currentStepIndex].GetComponent<StepItem>();
            if (stepItem != null)
                stepItem.MarkCompleted(true);
        }
        
        // Move to the next step
        NextStep();
    }

    public void LoadCustomProcedure(Procedure procedure)
    {
        // Simply redirect to the existing LoadProcedure method
        LoadProcedure(procedure);
    }

    //---------------Procedure Check Functions ---------------//
    // when no procedure is loaded
    public bool IsProcedureActive()
    {
        return CurrentProcedure != null && DisplayPanel.activeSelf;
    }
    // when a procedure is loaded
    public bool IsProcedureActive(Procedure procedure)
    {
        if (CurrentProcedure == null || procedure == null)
            return false;
            
        return CurrentProcedure == procedure && DisplayPanel.activeSelf;
    }
    // when a string of the procedure name is passed in
    public bool IsProcedureActive(string procedureName)
    {
        if (CurrentProcedure == null || string.IsNullOrEmpty(procedureName))
            return false;
            
        return CurrentProcedure.procedureName == procedureName && DisplayPanel.activeSelf;
    }
    public void SetProcedureActive(Procedure procedure, bool isActive)
    {
        if (isActive)
        {
            LoadProcedure(procedure);
        }
        else if (CurrentProcedure == procedure)
        {
            // Unload only if this is the current procedure
            CurrentProcedure = null;
            if (DisplayPanel != null)
                DisplayPanel.SetActive(false);
        }
    }
}