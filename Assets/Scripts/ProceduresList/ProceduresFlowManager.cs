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
    [SerializeField] private StepItem stepItemPrefab; // the prefab for each step in the procedure
    
    [Header("Procedure References")]
    [SerializeField] private ProcedureDisplay procedureDisplay; // main container for all the procedures

    private void Awake()
    {
        proceduresListPanel.SetActive(false);
        procedurePreviewPanel.SetActive(false);
        startProcedureButton.onClick.AddListener(ShowProceduresPreview);
        startSamplingButton.onClick.AddListener(ShowGeoSamplingPanel);
        backButton.onClick.AddListener(ShowProceduresList);
    }

    // show the task preview panel
    private void ShowProcedurePreview()
    {
        proceduresListPanel.SetActive(false);
        procedurePreviewPanel.SetActive(true);
        PopulateFirstSteps("Egress");
    }

    private void ShowProcedureDetails()
    {
        procedurePreviewPanel.SetActive(false);
        taskPanel.SetActive(true);
    }

    private void ShowProceduresList()
    {
        procedurePreviewPanel.SetActive(false);
        taskPanel.SetActive(false);
        proceduresListPanel.SetActive(true);
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
        for (int i = 0; i < proc.instructionSteps.Count; i++)
        {
            var item = Instantiate(stepItemPrefab, stepsContainer);
            item.SetStep(i + 1, proc.instructionSteps[i].instructionText);
        }
    }
} 