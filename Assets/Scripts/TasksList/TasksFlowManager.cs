using UnityEngine;
using UnityEngine.UI;

public class TasksFlowManager : MonoBehaviour
{
    [SerializeField] private GameObject tasksListPanel;
    [SerializeField] private GameObject taskInfoPanel;
    [SerializeField] private GameObject procedurePanel;
    [SerializeField] private Button egressButton;
    [SerializeField] private Button startButton;

    private void Awake()
    {
        taskInfoPanel.SetActive(false);
        procedurePanel.SetActive(false);
        egressButton.onClick.AddListener(ShowTaskInfo);
        startButton.onClick.AddListener(ShowProcedure);
    }

    private void ShowTaskInfo()
    {
        tasksListPanel.SetActive(false);
        taskInfoPanel.SetActive(true);
    }

    private void ShowProcedure()
    {
        taskInfoPanel.SetActive(false);
        procedurePanel.SetActive(true);
    }
} 