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
 * - Title: "Procedure: Open the airlock door"
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
        [SerializeField] private GameObject procedureDisplayPanel; // main panel for the display of procedures and steps
        [SerializeField] private TextMeshProUGUI procedureTitleText;
        // [SerializeField] private TextMeshProUGUI procedureDescriptionText;
        [SerializeField] private TextMeshProUGUI procedureStepText; // ex: "Step 1 of 3"
        [SerializeField] private TextMeshProUGUI procedureProgressText; // Shows "X/Y steps completed"
        [SerializeField] private Transform procedureStepsPanel; // Container for all steps
        [SerializeField] private StepItem procedureStepItemPrefab; // Prefab for individual step items

        // Navigation buttons to move next or start procedure
        [Header("Navigation Controls")]
        [SerializeField] private Button procedureNextButton; // go to the next step
        // [SerializeField] private Button procedureCompleteStepButton; 
        [SerializeField] private Button procedureSkipStepButton; // skip the current step
        // removing this button for now
        // [SerializeField] private Button procedureNextTaskButton; // go to the next task

        // Progress indicators (i.e. progress bar)
        [Header("Progress Indicators")]
        [SerializeField] private Transform procedureStepIndicatorContainer;
        [SerializeField] private GameObject procedureStepIndicatorPrefab;
        [SerializeField] private Color activeStepColor = Color.cyan;
        [SerializeField] private Color inactiveStepColor = Color.gray;
        [SerializeField] private Color completedStepColor = Color.cyan;

        // Events triggered when user interacts with UI
        public UnityEvent onProcedureCompleted;

        // store current procedure as a reference
        private Procedure currentProcedure; 
        private int currentStepIndex = 0;
        private List<GameObject> procedureStepIndicators = new List<GameObject>(); // list of step indicators for the progress bar
        private List<GameObject> procedureStepItems = new List<GameObject>(); // list of step item GameObjects

        //*------ Functions to control the display ------*/
        // Initialize the display
        private void Awake()
        {
            // Check for required components
            if (procedureTitleText == null || procedureStepText == null || 
                procedureNextButton == null ||
                procedureStepIndicatorContainer == null || procedureStepIndicatorPrefab == null ||
                procedureProgressText == null || procedureStepsPanel == null || procedureStepItemPrefab == null)
            {
                Debug.LogError("ProcedureDisplay: Missing required UI component references!");
            }
            
            // Initialize the procedure display
            InitializeProcedureDisplay();
        }

        private void InitializeProcedureDisplay()
        {
            // Set up initial state
            if (procedureTitleText != null) procedureTitleText.text = "Procedure";
            if (procedureStepText != null) procedureStepText.text = "";
            if (procedureProgressText != null) procedureProgressText.text = "0/0 steps completed";
            
            // Set up button listeners
            if (procedureNextButton != null) procedureNextButton.onClick.AddListener(GoToNextStep);

            CreateProcedureStepIndicators(0); // Create step indicators with 0 steps initially
        }

        // Remove listeners for buttons when the display is no longer in view
        private void OnDestroy()
        {
            if (procedureNextButton != null)
                procedureNextButton.onClick.RemoveListener(GoToNextStep);
        }

        // load sets of instructions based on a procedure name
        public void LoadProcedure(string procedureName)
        {
            if (ProcedureManager.Instance == null)
            {
                Debug.LogError("ProcedureManager instance not found!");
                return;
            }

            // get the procedure from the manager
            Procedure procedure = ProcedureManager.Instance.GetProcedure(procedureName);
            if (procedure != null) DisplayProcedure(procedure);
        }

        // display procedure
        public void DisplayProcedure(Procedure procedure)
        {
            // Store the current procedure
            currentProcedure = procedure;
            currentStepIndex = -1; // Start with no step highlighted
            
            // Reset all steps
            foreach (var step in currentProcedure.instructionSteps)
            {
                step.status = InstructionStatus.NotStarted;
            }
            
            // Setup UI
            procedureTitleText.text = procedure.procedureName;
            procedureStepText.text = "";
            procedureProgressText.text = $"0/{procedure.instructionSteps.Count} steps completed";
            
            CreateProcedureStepIndicators(procedure.instructionSteps.Count);
            CreateProcedureStepItems(procedure.instructionSteps);
            
            // Make sure panel is visible
            procedureDisplayPanel.SetActive(true);
            
            // Display current state (no step highlighted)
            DisplayCurrentStep();
        }
        
        //*------ Navigation Functions ------*/
        // Go to the next step in the procedure
        // called when user presses on the next button 
        public void GoToNextStep()
        {
            if (currentProcedure == null) return;

            // If we're at the last step, go back to the first step
            if (currentStepIndex >= currentProcedure.instructionSteps.Count - 1)
            {
                currentStepIndex = 0;
            }
            else
            {
                currentStepIndex++;
            }
            
            // Update display
            DisplayCurrentStep();
        }
        
        // Check if currently inside a procedure
        public bool IsProcedureActive()
        {
            return currentProcedure != null && procedureDisplayPanel.activeSelf;
        }

        // step indicators for progress bar
        // step count is based on number of steps required in current procedure
        private void CreateProcedureStepIndicators(int stepCount)
        {
            // clear existing indicators
            foreach (var indicator in procedureStepIndicators) {
                if (indicator != null)
                    Destroy(indicator);
            }
            procedureStepIndicators.Clear();

            // create new indicators
            for (int i = 0; i < stepCount; ++i) {
                GameObject indicator = Instantiate(procedureStepIndicatorPrefab, procedureStepIndicatorContainer);
                procedureStepIndicators.Add(indicator);
                
                // Set initial color
                Image indicatorImage = indicator.GetComponent<Image>();
                if (indicatorImage != null)
                    indicatorImage.color = inactiveStepColor;
            }
        }

        // Create step items in the steps panel
        private void CreateProcedureStepItems(List<InstructionStep> steps)
        {
            // Clear existing step items
            foreach (var item in procedureStepItems)
            {
                if (item != null)
                    Destroy(item);
            }
            procedureStepItems.Clear();

            // Ensure steps panel is properly positioned relative to master container
            RectTransform panelRect = procedureStepsPanel.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                // Reset any existing offset
                panelRect.anchoredPosition = Vector2.zero;
            }

            // Create new step items
            for (int i = 0; i < steps.Count; i++)
            {
                // Create the step item and ensure it's properly parented
                StepItem stepItem = Instantiate(procedureStepItemPrefab);
                stepItem.transform.SetParent(procedureStepsPanel, false);
                procedureStepItems.Add(stepItem.gameObject);

                // Set step number and text
                stepItem.SetStep(i + 1, steps[i].instructionText);
                stepItem.SetColor(inactiveStepColor);
            }

            // Force multiple canvas updates
            Canvas.ForceUpdateCanvases();
            StartCoroutine(ForceUpdateCanvasesDelayed());
        }

        private IEnumerator ForceUpdateCanvasesDelayed()
        {
            yield return new WaitForEndOfFrame();
            Canvas.ForceUpdateCanvases();
            yield return new WaitForEndOfFrame();
            Canvas.ForceUpdateCanvases();
        }

        // Display the current step
        private void DisplayCurrentStep()
        {
            if (currentProcedure == null || currentStepIndex < 0)
            {
                // No step is active
                procedureStepText.text = "";
                procedureProgressText.text = $"0/{currentProcedure?.instructionSteps.Count ?? 0} steps completed";
                return;
            }

            if (currentStepIndex >= currentProcedure.instructionSteps.Count)
                return;

            // Update step text
            procedureStepText.text = currentProcedure.instructionSteps[currentStepIndex].instructionText;
            procedureProgressText.text = $"{currentStepIndex + 1}/{currentProcedure.instructionSteps.Count} steps completed";

            // Update step indicators
            for (int i = 0; i < procedureStepIndicators.Count; i++)
            {
                if (procedureStepIndicators[i] != null)
                {
                    Image indicatorImage = procedureStepIndicators[i].GetComponent<Image>();
                    if (indicatorImage != null)
                    {
                        if (i < currentStepIndex)
                            indicatorImage.color = completedStepColor;
                        else if (i == currentStepIndex)
                            indicatorImage.color = activeStepColor;
                        else
                            indicatorImage.color = inactiveStepColor;
                    }
                }
            }

            // Update step items
            for (int i = 0; i < procedureStepItems.Count; i++)
            {
                if (procedureStepItems[i] != null)
                {
                    TextMeshProUGUI[] texts = procedureStepItems[i].GetComponentsInChildren<TextMeshProUGUI>();
                    foreach (var text in texts)
                    {
                        if (i < currentStepIndex)
                            text.color = completedStepColor;
                        else if (i == currentStepIndex)
                            text.color = activeStepColor;
                        else
                            text.color = inactiveStepColor;
                    }
                }
            }
        }
    }