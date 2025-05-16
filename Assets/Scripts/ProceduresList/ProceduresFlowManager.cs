using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;
using ProcedureSystem;

public class ProceduresFlowManager : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private GameObject proceduresListPanel;     // First screen - ProceduresList in hierarchy
    [SerializeField] private GameObject proceduresInfoPanel;     // Second screen - ProceduresInfo in hierarchy
    [SerializeField] private GameObject proceduresPanel;    // Third screen - Procedures in hierarchy
    
    [Header("UI Elements")]
    [SerializeField] private Button egressButton;          // Button to go from TasksList to TasksInfo

    [SerializeField] private Button samplingButton;   
    [SerializeField] private Button backButton;            // Button to go back from TasksInfo to TasksList
    [SerializeField] private Button startButton;           // Button to go from TasksInfo to Procedures
    [SerializeField] private Button verifyManuallyButton;  // Button to manually verify umbilical connection

    [SerializeField] private Transform stepsContainer;     // Contains series of steps in TasksInfo
    [SerializeField] private StepItem stepItemPrefab;      // Prefab for each step
    
    [Header("Procedure References")]
    [SerializeField] private ProcedureDisplay procedureDisplay; // Main procedure handler
    [SerializeField] private ProcedureAutomation procedureAutomation; // Handles automation of steps

    // Target task name for this MVP
    private const string PROCEDURE_NAME = "EVA Egress";
    private const string TARGET_TASK_NAME = "Connect UIA to DCU and start Depress";

    private void Awake()
    {
        // Make sure only the first panel is active at start
        proceduresListPanel.SetActive(true);
        proceduresInfoPanel.SetActive(false);
        proceduresPanel.SetActive(false);
        
        // Set up button listeners
        egressButton.onClick.AddListener(ShowTasksInfo);
        backButton.onClick.AddListener(ShowTasksList);
        startButton.onClick.AddListener(ShowProcedure);
        samplingButton.onClick.AddListener(ShowSampling);
        
        // Connect manual verification button if available
        if (verifyManuallyButton != null)
        {
            verifyManuallyButton.onClick.AddListener(VerifyManualStep);
        }
    }

    private void ShowSampling()
    {
        proceduresListPanel.SetActive(false);
    }

    //* ---- Starting screens----//
    // the starting screen for procedures and geo sampling are the same
    // show the task preview panel for a single procedure (ex: egress)
    // private void ShowProcedurePreview()
    // Show first panel (TasksList)
    private void ShowTasksList()
    {
        proceduresInfoPanel.SetActive(false);
        proceduresPanel.SetActive(false);
        proceduresListPanel.SetActive(true);
    }

    // Show second panel (TasksInfo) when selecting Egress
    private void ShowTasksInfo()
    {
        proceduresListPanel.SetActive(false);
        proceduresPanel.SetActive(false);
        proceduresInfoPanel.SetActive(true);
        
        // Populate steps for the selected task only
        PopulateTaskSteps();
    }

    // Show third panel (Procedures) when pressing Start
    private void ShowProcedure()
    {
        proceduresListPanel.SetActive(false);
        proceduresInfoPanel.SetActive(false);
        proceduresPanel.SetActive(true);
        
        // Initialize procedure in the procedure display with only the target task
        if (procedureDisplay != null)
        {
            // Get only the specific task instead of the whole procedure
            Procedure taskProcedure = ProcedureManager.Instance.GetProcedureTask(PROCEDURE_NAME, TARGET_TASK_NAME);
            
            if (taskProcedure != null)
            {
                Debug.Log($"ProceduresFlowManager: Loading task '{TARGET_TASK_NAME}' with {taskProcedure.instructionSteps.Count} steps");
                
                // Load only this task's steps
                procedureDisplay.LoadCustomProcedure(taskProcedure);
                
                // Explicitly make the display panel active
                if (procedureDisplay.transform.Find("DisplayPanel") != null)
                    procedureDisplay.transform.Find("DisplayPanel").gameObject.SetActive(true);
                
                // Set up automation for this task
                if (procedureAutomation != null)
                {
                    procedureAutomation.SetProcedureState(PROCEDURE_NAME, TARGET_TASK_NAME, 0);
                    Debug.Log($"ProceduresFlowManager: Setup automation for task '{TARGET_TASK_NAME}'");
                }
                else
                {
                    Debug.LogError("ProceduresFlowManager: procedureAutomation reference is missing");
                }
            }
            else
            {
                Debug.LogError($"ProceduresFlowManager: Failed to load task '{TARGET_TASK_NAME}'");
            }
        }
        else
        {
            Debug.LogError("ProceduresFlowManager: procedureDisplay reference is missing");
        }
    }

    // Populate the steps in the TasksInfo panel
    private void PopulateTaskSteps()
    {
        Debug.Log($"ProceduresFlowManager: Populating steps for '{TARGET_TASK_NAME}'");
        
        if (ProcedureManager.Instance == null)
        {
            Debug.LogError("ProceduresFlowManager: ProcedureManager.Instance is null");
            return;
        }
        
        // Get only the specific task
        var taskProc = ProcedureManager.Instance.GetProcedureTask(PROCEDURE_NAME, TARGET_TASK_NAME);
        if (taskProc == null)
        {
            Debug.LogError($"ProceduresFlowManager: Task '{TARGET_TASK_NAME}' not found");
            return;
        }
        
        if (stepItemPrefab == null || stepsContainer == null)
        {
            Debug.LogError("ProceduresFlowManager: Missing prefab or container reference");
            return;
        }
        
        // Clear existing steps
        foreach (Transform child in stepsContainer) 
            Destroy(child.gameObject);
        
        // Populate steps for this task
        for (int i = 0; i < taskProc.instructionSteps.Count; i++)
        {
            var item = Instantiate(stepItemPrefab, stepsContainer);
            item.SetStep(i + 1, taskProc.instructionSteps[i].instructionText);
        }
    }

    // Method to manually verify the first step (umbilical connection)
    private void VerifyManualStep()
    {
        if (procedureAutomation != null)
        {
            procedureAutomation.ManualCompleteStep();
        }
    }
} 