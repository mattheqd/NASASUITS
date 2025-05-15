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
    [SerializeField] private GameObject StepPanel; // Container for a single step
    [SerializeField] private GameObject StepItemPrefab; // Prefab for a single step

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
    [SerializeField] private GameObject StepIndicatorTemplate;
    [SerializeField] private Color ActiveStepColor = Color.cyan;
    [SerializeField] private Color InactiveStepColor = Color.gray;
    [SerializeField] private Color CompletedStepColor = Color.cyan;

    // Events triggered when user interacts with UI
    public UnityEvent onProcedureCompleted;

    // store current procedure as a reference
    private ProcedureSystem.Procedure CurrentProcedure; 
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
            TaskStepIndicatorContainer == null || StepIndicatorTemplate == null ||
            TaskProgressText == null || TaskStepsPanel == null || StepIndicatorTemplate == null)
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

    //* -------- Extract procedure data from JSON --------*//
    // Load a procedure with all its steps from the JSON file
    public void LoadProcedure(ProcedureSystem.Procedure procedure)
    {
        if (procedure == null)
        {
            Debug.LogError("ProcedureDisplay: Cannot load null procedure");
            return;
        }

        // Store reference to current procedure
        CurrentProcedure = procedure;
        currentStepIndex = 0;

        // Set title (procedure name or task name)
        if (TitleText != null)
        {
            TitleText.text = $"{procedure.procedureName}";
            // If we are displaying a task instead of a procedure, display the task name
            if (!string.IsNullOrEmpty(procedure.taskName) && procedure.taskName != procedure.procedureName)
            {
                TitleText.text = $"{procedure.taskName}";
            }
        }

        // Set up step indicators
        CreateTaskStepIndicators(procedure.instructionSteps.Count);

        // Clear any existing step items
        foreach (GameObject item in TaskStepItems)
        {
            Destroy(item);
        }
        TaskStepItems.Clear();

        // Create new step items
        for (int i = 0; i < procedure.instructionSteps.Count; i++)
        {
            GameObject stepObj = Instantiate(StepItemPrefab, TaskStepsPanel);
            StepItem stepItem = stepObj.GetComponent<StepItem>();
            
            if (stepItem != null)
            {
                stepItem.SetStep(i + 1, procedure.instructionSteps[i].instructionText);
                TaskStepItems.Add(stepObj);
            }
            
            // Initially hide all steps except the first one
            stepObj.SetActive(i == 0);
        }

        // Update step text and progress
        UpdateStepText();
        UpdateProgressIndicators();

        // Enable the panel
        if (DisplayPanel != null) 
            DisplayPanel.SetActive(true);
            
        Debug.Log($"Loaded procedure '{procedure.procedureName}' with {procedure.instructionSteps.Count} steps");
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
            // We're at the last step, mark this procedure as complete
            Debug.Log("ProcedureDisplay: Procedure complete!");
            onProcedureCompleted?.Invoke();
            return;
        }

        // Increment the step index and update display
        currentStepIndex++;
        DisplayCurrentStep();
        
        Debug.Log($"ProcedureDisplay: Advanced to step {currentStepIndex + 1} of {CurrentProcedure.instructionSteps.Count}P: {CurrentProcedure.procedureName}, Step Text: {CurrentProcedure.instructionSteps[currentStepIndex].instructionText}");
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
            return;
        }

        // Set the current step and update display
        currentStepIndex = stepIndex;
        DisplayCurrentStep();
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
            TaskStepText.text = "No steps available";
            TaskProgressText.text = $"0/{CurrentProcedure?.instructionSteps.Count ?? 0} steps completed";
            return;
        }

        if (currentStepIndex >= CurrentProcedure.instructionSteps.Count)
            return;

        // Update step text
        TaskStepText.text = CurrentProcedure.instructionSteps[currentStepIndex].instructionText;
        TaskProgressText.text = $"{currentStepIndex}/{CurrentProcedure.instructionSteps.Count} steps completed";

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

        // Show only the current step, hide others
        for (int i = 0; i < TaskStepItems.Count; i++)
        {
            if (TaskStepItems[i] != null)
            {
                // Only show the current step
                TaskStepItems[i].SetActive(i == currentStepIndex);
                
                // Update visual state
                StepItem stepItem = TaskStepItems[i].GetComponent<StepItem>();
                if (stepItem != null)
                {
                    stepItem.SetActiveStep(i == currentStepIndex);
                }
            }
        }
        
        // Update progress indicators
        UpdateProgressIndicators();
    }

    // step count is based on number of steps required in current procedure
    private void CreateTaskStepIndicators(int stepCount)
    {
        // Clear existing indicators
        foreach (GameObject indicator in TaskStepIndicators)
        {
            Destroy(indicator);
        }
        TaskStepIndicators.Clear();

        // Create new indicators
        for (int i = 0; i < stepCount; i++)
        {
            GameObject indicator = Instantiate(StepItemPrefab, TaskStepIndicatorContainer);
            Image indicatorImage = indicator.GetComponent<Image>();
            if (indicatorImage != null)
            {
                indicatorImage.color = (i == 0) ? ActiveStepColor : InactiveStepColor;
            }
            TaskStepIndicators.Add(indicator);
        }
    }

    // Create step items in the steps panel
    private void CreateTaskStepItems(List<ProcedureSystem.InstructionStep> steps)
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
            GameObject stepObj = Instantiate(StepItemPrefab, TaskStepsPanel);
            StepItem stepItem = stepObj.GetComponent<StepItem>();
            
            if (stepItem != null)
            {
                stepItem.SetStep(i + 1, steps[i].instructionText);
                TaskStepItems.Add(stepObj);
            }
            else
            {
                Debug.LogError("StepItem component not found on instantiated prefab");
            }
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

    public void LoadCustomProcedure(ProcedureSystem.Procedure procedure)
    {
        if (procedure == null)
        {
            Debug.LogError("ProcedureDisplay: Cannot load null procedure");
            return;
        }

        // Store reference to current procedure
        CurrentProcedure = procedure;
        currentStepIndex = 0;

        // Set procedure title
        if (TitleText != null)
        {
            TitleText.text = $"Procedure: {procedure.procedureName}";
            if (!string.IsNullOrEmpty(procedure.taskName) && procedure.taskName != procedure.procedureName)
            {
                TitleText.text += $" - {procedure.taskName}";
            }
        }

        // Set up step indicators
        CreateTaskStepIndicators(procedure.instructionSteps.Count);

        // Clear any existing step items
        foreach (GameObject item in TaskStepItems)
        {
            Destroy(item);
        }
        TaskStepItems.Clear();

        // Create new step items
        for (int i = 0; i < procedure.instructionSteps.Count; i++)
        {
            GameObject stepObj = Instantiate(StepItemPrefab, TaskStepsPanel);
            StepItem stepItem = stepObj.GetComponent<StepItem>();
            
            if (stepItem != null)
            {
                stepItem.SetStep(i + 1, procedure.instructionSteps[i].instructionText);
                TaskStepItems.Add(stepObj);
            }
            else
            {
                Debug.LogError("StepItem component not found on instantiated prefab");
            }
            
            // Make all steps visible for now - we'll hide them later if needed
            stepObj.SetActive(true);
        }

        // Update step text and progress
        UpdateStepText();
        UpdateProgressIndicators();

        // Make sure the display panel is active
        if (DisplayPanel != null) 
        {
            DisplayPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("ProcedureDisplay: DisplayPanel reference is missing!");
        }
        
        Debug.Log($"ProcedureDisplay: Loaded procedure '{procedure.procedureName}' with {procedure.instructionSteps.Count} steps");
    }

    //---------------Procedure Check Functions ---------------//
    // when no procedure is loaded
    public bool IsProcedureActive()
    {
        return CurrentProcedure != null;
    }
    // when a procedure is loaded
    public bool IsProcedureActive(ProcedureSystem.Procedure procedure)
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
    public void SetProcedureActive(ProcedureSystem.Procedure procedure, bool isActive)
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

    // Make sure you have this method to update step text
    private void UpdateStepText()
    {
        if (CurrentProcedure == null || CurrentProcedure.instructionSteps.Count == 0)
        {
            if (TaskStepText != null) TaskStepText.text = "No steps available";
            if (TaskProgressText != null) TaskProgressText.text = "0/0 steps completed";
            return;
        }

        // Update step counter
        if (TaskStepText != null)
        {
            TaskStepText.text = $"Step {currentStepIndex + 1} of {CurrentProcedure.instructionSteps.Count}";
        }

        // Update progress text
        if (TaskProgressText != null)
        {
            TaskProgressText.text = $"{currentStepIndex}/{CurrentProcedure.instructionSteps.Count} steps completed";
        }
    }

    // Make sure step indicators are created correctly
    private void UpdateProgressIndicators()
    {
        if (CurrentProcedure == null || currentStepIndex < 0 || currentStepIndex >= CurrentProcedure.instructionSteps.Count)
        {
            Debug.LogWarning("ProcedureDisplay: Invalid step index for updating progress indicators");
            return;
        }

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
    }
}