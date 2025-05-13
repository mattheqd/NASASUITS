using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;

public class ProceduresFlowManager : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private GameObject tasksListPanel;     // First screen - TasksList in hierarchy
    [SerializeField] private GameObject tasksInfoPanel;     // Second screen - TasksInfo in hierarchy
    [SerializeField] private GameObject proceduresPanel;    // Third screen - Procedures in hierarchy
    
    [Header("UI Elements")]
    [SerializeField] private Button egressButton;          // Button to go from TasksList to TasksInfo
    [SerializeField] private Button backButton;            // Button to go back from TasksInfo to TasksList
    [SerializeField] private Button startButton;           // Button to go from TasksInfo to Procedures

    [SerializeField] private Transform stepsContainer;     // Contains series of steps in TasksInfo
    [SerializeField] private StepItem stepItemPrefab;      // Prefab for each step
    
    [Header("Procedure References")]
    [SerializeField] private ProcedureDisplay procedureDisplay; // Main procedure handler

    private void Awake()
    {
        // Make sure only the first panel is active at start
        tasksListPanel.SetActive(true);
        tasksInfoPanel.SetActive(false);
        proceduresPanel.SetActive(false);
        
        // Set up button listeners
        egressButton.onClick.AddListener(ShowTasksInfo);
        backButton.onClick.AddListener(ShowTasksList);
        startButton.onClick.AddListener(ShowProcedures);
    }

    //* ---- Starting screens----//
    // the starting screen for procedures and geo sampling are the same
    // show the task preview panel for a single procedure (ex: egress)
    private void ShowProcedurePreview()
    // Show first panel (TasksList)
    private void ShowTasksList()
    {
        tasksInfoPanel.SetActive(false);
        proceduresPanel.SetActive(false);
        tasksListPanel.SetActive(true);
    }

    // Show second panel (TasksInfo) when selecting Egress
    private void ShowTasksInfo()
    {
        tasksListPanel.SetActive(false);
        proceduresPanel.SetActive(false);
        tasksInfoPanel.SetActive(true);
        
        // Populate steps for the selected procedure
        PopulateFirstSteps("Egress");
    }

    // Show third panel (Procedures) when pressing Start
    private void ShowProcedures()
    {
        tasksListPanel.SetActive(false);
        tasksInfoPanel.SetActive(false);
        proceduresPanel.SetActive(true);
    }

    // Populate the steps in the TasksInfo panel
    private void PopulateFirstSteps(string procedureName)
    {
        Debug.Log($"ProceduresFlowManager: PopulateFirstSteps called for '{procedureName}'");
        
        if (ProcedureManager.Instance == null)
        {
            Debug.LogError("ProceduresFlowManager: ProcedureManager.Instance is null");
            return;
        }
        
        var proc = ProcedureManager.Instance.GetProcedure(procedureName);
        if (proc == null)
        {
            Debug.LogError($"ProceduresFlowManager: Procedure '{procedureName}' not found");
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
        
        // Populate steps for this procedure
        int count = Mathf.Min(5, proc.instructionSteps.Count); // Show first 5 steps or less
        for (int i = 0; i < count; i++)
        {
            var item = Instantiate(stepItemPrefab, stepsContainer);
            item.SetStep(i + 1, proc.instructionSteps[i].instructionText);
        }
    }
} 