using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;

public class TasksFlowManager : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private GameObject proceduresListPanel; // the list of procedures. starting screen
    [SerializeField] private GameObject procedurePreviewPanel; // info about the procedure (ex: time it will take, consumables lost, etc)
    [SerializeField] private GameObject taskPanel; // displays a task in the procedure
    [SerializeField] private GameObject geoSamplingPanel; // Added reference to GeoSampling workflow
    
    [Header("UI Elements")]
    [SerializeField] private Button startProcedureButton; // start the procedure workflow (ex: egress)
    [SerializeField] private Button startSamplingButton; // Added button for Geosampling workflow

    [SerializeField] private Transform stepsContainer; // contains series of steps for each task
    [SerializeField] private StepItem stepItemPrefab;
    
    [Header("Procedure References")]
    [SerializeField] private ProcedureDisplay procedureDisplay; // main container for all the procedures

    private void Awake()
    {
        taskInfoPanel.SetActive(false);
        procedurePanel.SetActive(false);
        egressButton.onClick.AddListener(ShowTaskInfo);
        startButton.onClick.AddListener(ShowProcedure);
        backButton.onClick.AddListener(ShowTasksList);
    }

    // show the task preview panel
    private void ShowProceduresPreview()
    {
        tasksListPanel.SetActive(false);
        taskInfoPanel.SetActive(true);
        PopulateFirstSteps("Egress");
    }

    private void ShowProcedure()
    {
        taskInfoPanel.SetActive(false);
        procedurePanel.SetActive(true);
    }

    private void ShowTasksList()
    {
        taskInfoPanel.SetActive(false);
        procedurePanel.SetActive(false);
        tasksListPanel.SetActive(true);
    }

    private void PopulateFirstSteps(string procedureName)
    {
        Debug.Log($"TasksFlowManager: PopulateFirstSteps called for '{procedureName}'");
        if (ProcedureManager.Instance == null)
        {
            Debug.LogError("TasksFlowManager: ProcedureManager.Instance is null");
            return;
        }
        var proc = ProcedureManager.Instance.GetProcedure(procedureName);
        if (proc == null)
        {
            return;
        }
        if (stepItemPrefab == null)
        {
            return;
        }
        if (stepsContainer == null)
        {
            return;
        }
        // clear the current steps in the container to prepare for new steps
        foreach (Transform child in stepsContainer) Destroy(child.gameObject);
        
        // populate the steps container with the steps from the procedure
        for (int i = 0; i < count; i++)
        {
            var item = Instantiate(stepItemPrefab, stepsContainer);
            item.SetStep(i + 1, proc.instructionSteps[i].instructionText);
        }
    }
} 