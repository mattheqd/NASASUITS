using UnityEngine;
using System.IO;
using System.Collections.Generic;
using ProcedureSystem;

public class ProcedureSetup : MonoBehaviour
{
    [SerializeField] private ProcedureAutomation procedureAutomation;
    
    private void Awake()
    {
        // Make sure a ProcedureManager exists
        var manager = ProcedureManager.GetOrCreateInstance();
        
        // Load the specific task we care about
        LoadTargetTask();
    }
    
    private void LoadTargetTask()
    {
        try
        {
            // Get the task procedure using the updated ProcedureManager method
            Procedure taskProcedure = ProcedureManager.Instance.GetProcedureTask(
                "EVA Egress", "Connect UIA to DCU and start Depress");
                
            if (taskProcedure != null)
            {
                Debug.Log($"Found target task: {taskProcedure.taskName} with {taskProcedure.instructionSteps.Count} steps");
                
                // Initialize the procedureAutomation with the full procedure
                if (procedureAutomation != null)
                {
                    // Pass the task steps to the automation system
                    procedureAutomation.UpdateTaskIndices(0, taskProcedure.instructionSteps.Count - 1);
                    
                    Debug.Log($"Initialized automation for task '{taskProcedure.taskName}' (steps 0-{taskProcedure.instructionSteps.Count - 1})");
                    
                    // Log steps for debugging
                    for (int j = 0; j < taskProcedure.instructionSteps.Count; j++)
                    {
                        Debug.Log($"Step {j + 1}: {taskProcedure.instructionSteps[j].instructionText}");
                    }
                }
                else
                {
                    Debug.LogError("ProcedureAutomation reference is missing!");
                }
            }
            else
            {
                Debug.LogError("Failed to find the 'Connect UIA to DCU and start Depress' task in 'EVA Egress' procedure");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading target task: {e.Message}");
        }
    }
    
    // Helper classes to parse the JSON structure
    [System.Serializable]
    private class JsonRootObject
    {
        public List<JsonProcedure> procedures;
    }
    
    [System.Serializable]
    private class JsonProcedure
    {
        public string procedureName;
        public string procedureDescription;
        public List<JsonTask> tasks;
    }
    
    [System.Serializable]
    private class JsonTask
    {
        public string taskName;
        public string taskDescription;
        public List<JsonStep> steps;
    }
    
    [System.Serializable]
    private class JsonStep
    {
        public string instructionText;
    }
} 