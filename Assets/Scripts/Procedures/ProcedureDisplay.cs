/**
 * Manager to control display of procedures and tracking of progress/status
 * - Define UI components and displays
 * - Define functions to control the display
 * EXAMPLE:
 * - Title: "Procedure: Open the airlock door"
 * - Description: "This procedure will guide you through the process of opening the airlock door."
 * - Progress text: "Step 1 of 3"
 * - Step text: "Open the airlock door"
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
    [SerializeField] private TextMeshProUGUI progressText;

    // Navigation buttons to move next, previous, skip, or return to main menu
    [Header("Navigation Controls")]
    [SerializeField] private Button nextButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button homeButton;
    [SerializeField] private Button completeButton; // confirmation button to complete the procedure
    [SerializeField] private Button continueButton;

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

    //*------ Functions to control the display ------*/
    // Initialize the display
    private void Awake()
    {
        if (procedurePanel == null || titleText == null || 
            descriptionText == null || stepText == null || 
            progressText == null || nextButton == null || 
            backButton == null || homeButton == null ||
            completeButton == null)
        {
            Debug.LogError("ProcedureDisplay: Missing required UI component references!");
        }
        
        // Add listeners for buttons to check for button presses
        if (nextButton != null)
            nextButton.onClick.AddListener(GoToNextStep);
        
        if (backButton != null)
            backButton.onClick.AddListener(GoToPreviousStep);
        
        if (homeButton != null)
            homeButton.onClick.AddListener(ReturnToHome);
        
        if (completeButton != null)
        {
            completeButton.onClick.AddListener(CompleteProcedure);
            completeButton.gameObject.SetActive(false); // Hide complete button initially
        }

        if (continueButton != null)
            continueButton.onClick.AddListener(StartInstructions);
    }

    // Remove listeners for buttons when the display is no longer in view
    private void OnDestroy()
    {
        if (nextButton != null)
            nextButton.onClick.RemoveListener(GoToNextStep);
        
        if (backButton != null)
            backButton.onClick.RemoveListener(GoToPreviousStep);
        
        if (homeButton != null)
            homeButton.onClick.RemoveListener(ReturnToHome);
        
        if (completeButton != null)
            completeButton.onClick.RemoveListener(CompleteProcedure);

        if (continueButton != null)
            continueButton.onClick.RemoveListener(StartInstructions);
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
        currentStepIndex = -1; // indicate procedure has not started (start screen at the title and description)
        
        // Reset all steps
        foreach (var step in currentProcedure.instructionSteps)
        {
            step.isCompleted = false;
            step.isSkipped = false;
        }
        
        // Setup UI
        titleText.text = procedure.procedureName;
        descriptionText.text = procedure.procedureDescription;
        CreateStepIndicators(procedure.instructionSteps.Count);
        DisplayCurrentStep();
        procedurePanel.SetActive(true);
        UpdateNavigationButtons();

        // Show only continue button initially
        if (nextButton != null) nextButton.gameObject.SetActive(false);
        if (backButton != null) backButton.gameObject.SetActive(false);
        if (homeButton != null) homeButton.gameObject.SetActive(true);
        if (completeButton != null) completeButton.gameObject.SetActive(false);
        
        // Rename next button to "Continue" for the intro screen
        if (continueButton != null) continueButton.gameObject.SetActive(true);
    }
    
    //*------ Navigation Functions ------*/
    // Go to the next step in the procedure
    // called when user presses on the next button 
    public void GoToNextStep()
    {
        // Mark current step as completed
        currentProcedure.instructionSteps[currentStepIndex].isCompleted = true;
        
        // Move to next step
        currentStepIndex++;
        
        // Update display
        DisplayCurrentStep();
        
        // Update button states
        UpdateNavigationButtons();
    }
    
    // Go to the previous step
    public void GoToPreviousStep()
    {
        if (currentProcedure == null || currentStepIndex <= 0)
        {
            return;
        }
        
        // Move to previous step
        currentStepIndex--;
        
        // Update display
        DisplayCurrentStep();
        
        // Update button states
        UpdateNavigationButtons();
    }
    
    // Control when navigation buttons are enabled/disabled
    private void UpdateNavigationButtons()
    {
        if (currentProcedure == null) return;

        // If the user has not confirmed the procedure yet
        if (currentStepIndex == -1) {
            if (continueButton != null) continueButton.gameObject.SetActive(true);
            if (nextButton != null) nextButton.gameObject.SetActive(false);
            if (backButton != null) backButton.gameObject.SetActive(false);
            if (completeButton != null) completeButton.gameObject.SetActive(false);
            return;
        }

        // Hide continue button once instructions start
        if (continueButton != null) continueButton.gameObject.SetActive(false);
        
        // Back button is disabled on first step
        if (backButton != null) backButton.interactable = (currentStepIndex > 0);
        
        // Next button is enabled for all but last step
        if (nextButton != null) nextButton.gameObject.SetActive(currentStepIndex < currentProcedure.instructionSteps.Count - 1);
        
        // Complete button is only shown on the last step
        if (completeButton != null) completeButton.gameObject.SetActive(currentStepIndex == currentProcedure.instructionSteps.Count - 1);
    }
    
    // Displays when user confirms completion of the procedure
    public void CompleteProcedure()
    {
        if (currentProcedure == null) return;
        
        // Mark the final step as completed
        if (currentStepIndex < currentProcedure.instructionSteps.Count)
            currentProcedure.instructionSteps[currentStepIndex].isCompleted = true;
        
        // Display completion message
        stepText.text = "Procedure completed successfully!";
        progressText.text = "Completed";
        
        // Hide navigation buttons
        if (nextButton != null) nextButton.gameObject.SetActive(false);
        if (backButton != null) backButton.gameObject.SetActive(false);
        if (completeButton != null) completeButton.gameObject.SetActive(false);
        
        // Show only home button
        if (homeButton != null) homeButton.gameObject.SetActive(true);
        
        // Invoke completion event
        onProcedureCompleted.Invoke();
        
        // Update all indicators to completed
        foreach (var indicator in stepIndicators)
        {
            if (indicator != null)
            {
                Image indicatorImage = indicator.GetComponent<Image>();
                if (indicatorImage != null)
                    indicatorImage.color = completedStepColor;
            }
        }
    }
    
    // TODO: Activate navigation path to home (airlock)
    public void ReturnToHome()
    {
        // hide panel
        procedurePanel.SetActive(false);

        // reset current procedure
        currentProcedure = null;
        currentStepIndex = 0;
    }

    // function to start instructions sequence (i.e. confirm follow through for a procedure)
    public void StartInstructions() {
        if (currentProcedure == null) return;
        currentStepIndex = 0; // set to first instruction
        DisplayCurrentStep();
        UpdateNavigationButtons();
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

    // display current step the user is on (ex: "Step 1 of 3")
    private void DisplayCurrentStep() {
        if (currentProcedure == null) return;
        
        // If we're at the intro screen (index = -1)
        if (currentStepIndex == -1) {
            // Just show the description, not a specific step
            stepText.text = "Click Continue to start the procedure";
            progressText.text = "";
            return;
        }
        
        // Check if the index is valid
        if (currentStepIndex < 0 || currentStepIndex >= currentProcedure.instructionSteps.Count) {
            Debug.LogError("Invalid step index: " + currentStepIndex);
            return;
        }

        // get current step
        InstructionStep step = currentProcedure.instructionSteps[currentStepIndex];

        // update step text
        stepText.text = step.instructionText;
        progressText.text = $"Step {currentStepIndex + 1} of {currentProcedure.instructionSteps.Count}";

        // update step indicators (i.e. current number for the progress bar)
        for (int i = 0; i < stepIndicators.Count; ++i) {
            if (stepIndicators[i] != null) {
                // indicatorImage is the image component representing each step in the progress bar
                Image indicatorImage = stepIndicators[i].GetComponent<Image>();

                // update color of indicator based on current step
                if (indicatorImage != null) {
                    // previous steps are completed (past completed steps)
                    if (i < currentStepIndex)
                        indicatorImage.color = completedStepColor;
                    // current step is active (current step)
                    else if (i == currentStepIndex)
                        indicatorImage.color = activeStepColor;
                    // future steps are inactive (future steps)
                    else
                        indicatorImage.color = inactiveStepColor;
                }
            }
        }
    }
}