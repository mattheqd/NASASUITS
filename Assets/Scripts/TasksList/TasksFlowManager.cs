using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;

public class TasksFlowManager : MonoBehaviour
{
    [SerializeField] private GameObject tasksListPanel;
    [SerializeField] private GameObject taskInfoPanel;
    [SerializeField] private GameObject procedurePanel;
    [SerializeField] private Button egressButton;
    [SerializeField] private Button startButton;
    [SerializeField] private Button backButton;
    [SerializeField] private StepItem stepItemPrefab;
    [SerializeField] private Transform stepsContainer;

    private List<Procedure> firstThreeProcedures;

    private void Awake()
    {
        taskInfoPanel.SetActive(false);
        procedurePanel.SetActive(false);
        egressButton.onClick.AddListener(ShowTaskInfo);
        startButton.onClick.AddListener(ShowProcedure);
        backButton.onClick.AddListener(ShowTasksList);
    }

    private void ShowTaskInfo()
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
            Debug.LogError($"TasksFlowManager: Procedure '{procedureName}' not found");
            return;
        }
        if (stepItemPrefab == null)
        {
            Debug.LogError("TasksFlowManager: stepItemPrefab is not assigned");
            return;
        }
        if (stepsContainer == null)
        {
            Debug.LogError("TasksFlowManager: stepsContainer is not assigned");
            return;
        }
        foreach (Transform child in stepsContainer) Destroy(child.gameObject);
        int count = Mathf.Min(3, proc.instructionSteps.Count);
        for (int i = 0; i < count; i++)
        {
            var item = Instantiate(stepItemPrefab, stepsContainer);
            item.SetStep(i + 1, proc.instructionSteps[i].instructionText);
        }
    }
} 