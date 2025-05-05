/**
 * Manager to control display of procedures and tracking of progress/status
 * - Define UI components and displays
 * - Define functions to control the display
 * EXAMPLE:
 * - Title: "Procedure: Open the airlock door"
 * - Description: "This procedure will guide you through the process of opening the airlock door."
 * - Step text: "Step 1 of 3"
 * - Step indicators: 3 circles, 1 circle is green, 1 circle is gray, 1 circle is gray
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

// Class to define groups of interface elements
public class ProcedureDisplay : MonoBehaviour
{
    /*------ UI Components ------*/
    // UI components for panel (title, panel, description, step number, progress)
    [Header("UI References")]
    [SerializeField] private GameObject procedurePanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI stepText;
    [SerializeField] private TextMeshProUGUI progressText; // Shows "X/Y steps completed"
    [SerializeField] private Transform stepsPanel; // Container for all steps
    [SerializeField] private StepItem stepItemPrefab; // Prefab for individual step items

    // Navigation buttons to move next or start procedure
    [Header("Navigation Controls")]
    [SerializeField] private Button nextButton;

    // Progress indicators (i.e. progress bar)
    [Header("Progress Indicators")]
    [SerializeField] private Transform stepIndicatorContainer;
    [SerializeField] private GameObject stepIndicatorPrefab;
    [SerializeField] private Color activeStepColor = Color.cyan;
    [SerializeField] private Color inactiveStepColor = Color.gray;
    [SerializeField] private Color completedStepColor = Color.cyan;

    // Events triggered when user interacts with UI
    public UnityEvent onProcedureCompleted;

    // store current procedure as a reference
    private Procedure currentProcedure;
    private int currentStepIndex = 0;
    private List<GameObject> stepIndicators = new List<GameObject>(); // list of step indicators for the progress bar
    private List<GameObject> stepItems = new List<GameObject>(); // list of step item GameObjects

    //*------ Functions to control the display ------*/
    // Initialize the display
    private void Awake()
    {
        // Check for required components
        if (titleText == null || descriptionText == null || stepText == null || 
            nextButton == null ||
            stepIndicatorContainer == null || stepIndicatorPrefab == null ||
            progressText == null || stepsPanel == null || stepItemPrefab == null)
        {
            Debug.LogError("ProcedureDisplay: Missing required UI component references!");
        }
        
        // Initialize the procedure display
        InitializeProcedureDisplay();
    }

    private void InitializeProcedureDisplay()
    {
        // Set up initial state
        if (titleText != null) titleText.text = "Procedure";
        if (descriptionText != null) descriptionText.text = "Procedure Description";
        if (stepText != null) stepText.text = "";
        if (progressText != null) progressText.text = "0/0 steps completed";
        
        // Set up button listeners
        if (nextButton != null) nextButton.onClick.AddListener(GoToNextStep);

        CreateStepIndicators(0); // Create step indicators with 0 steps initially
    }

    // Remove listeners for buttons when the display is no longer in view
    private void OnDestroy()
    {
        if (nextButton != null)
            nextButton.onClick.RemoveListener(GoToNextStep);
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
            step.isCompleted = false;
            step.isSkipped = false;
        }
        
        // Setup UI
        titleText.text = procedure.procedureName;
        descriptionText.text = procedure.procedureDescription;
        stepText.text = "";
        progressText.text = $"0/{procedure.instructionSteps.Count} steps completed";
        
        CreateStepIndicators(procedure.instructionSteps.Count);
        CreateStepItems(procedure.instructionSteps);
        
        // Make sure panel is visible
        procedurePanel.SetActive(true);
        
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
        return currentProcedure != null && procedurePanel.activeSelf;
    }

    // step indicators for progress bar
    // step count is based on number of steps required in current procedure
    private void CreateStepIndicators(int stepCount)
    {
        // clear existing indicators
        foreach (var indicator in stepIndicators) {
            if (indicator != null)
                Destroy(indicator);
        }
        stepIndicators.Clear();

        // create new indicators
        for (int i = 0; i < stepCount; ++i) {
            GameObject indicator = Instantiate(stepIndicatorPrefab, stepIndicatorContainer);
            stepIndicators.Add(indicator);
            
            // Set initial color
            Image indicatorImage = indicator.GetComponent<Image>();
            if (indicatorImage != null)
                indicatorImage.color = inactiveStepColor;
        }
    }

    // Create step items in the steps panel
    private void CreateStepItems(List<InstructionStep> steps)
    {
        // Clear existing step items
        foreach (var item in stepItems)
        {
            if (item != null)
                Destroy(item);
        }
        stepItems.Clear();

        // Ensure steps panel is properly positioned relative to master container
        RectTransform panelRect = stepsPanel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            // Reset any existing offset
            panelRect.anchoredPosition = Vector2.zero;
        }

        // Create new step items
        for (int i = 0; i < steps.Count; i++)
        {
            // Create the step item and ensure it's properly parented
            StepItem stepItem = Instantiate(stepItemPrefab);
            stepItem.transform.SetParent(stepsPanel, false);
            stepItems.Add(stepItem.gameObject);

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
            stepText.text = "";
            progressText.text = $"0/{currentProcedure?.instructionSteps.Count ?? 0} steps completed";
            return;
        }

        if (currentStepIndex >= currentProcedure.instructionSteps.Count)
            return;

        // Update step text
        stepText.text = currentProcedure.instructionSteps[currentStepIndex].instructionText;
        progressText.text = $"{currentStepIndex + 1}/{currentProcedure.instructionSteps.Count} steps completed";

        // Update step indicators
        for (int i = 0; i < stepIndicators.Count; i++)
        {
            if (stepIndicators[i] != null)
            {
                Image indicatorImage = stepIndicators[i].GetComponent<Image>();
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
        for (int i = 0; i < stepItems.Count; i++)
        {
            if (stepItems[i] != null)
            {
                TextMeshProUGUI[] texts = stepItems[i].GetComponentsInChildren<TextMeshProUGUI>();
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