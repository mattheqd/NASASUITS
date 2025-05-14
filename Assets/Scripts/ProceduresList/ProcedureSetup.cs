using UnityEngine;
using System.IO;
using System.Collections.Generic;

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
        // Try to load the JSON file directly
        TextAsset jsonFile = Resources.Load<TextAsset>("procedure_data");
        
        if (jsonFile == null)
        {
            Debug.LogError("Could not find procedure_data.json in Resources folder");
            return;
        }
        
        try
        {
            // Parse JSON
            var rootJson = JsonUtility.FromJson<JsonRootObject>(jsonFile.text);
            if (rootJson == null || rootJson.procedures == null || rootJson.procedures.Count == 0)
            {
                Debug.LogError("Failed to parse procedures from JSON");
                return;
            }
            
            // Look for EVA Egress procedure
            foreach (var procedure in rootJson.procedures)
            {
                if (procedure.procedureName == "EVA Egress" && procedure.tasks != null)
                {
                    Debug.Log($"Found EVA Egress procedure with {procedure.tasks.Count} tasks");
                    
                    // Find the "Connect UIA to DCU and start Depress" task
                    int taskIndex = 0;
                    int stepStartIndex = 0;
                    
                    for (int i = 0; i < procedure.tasks.Count; i++)
                    {
                        var task = procedure.tasks[i];
                        
                        if (task.taskName == "Connect UIA to DCU and start Depress")
                        {
                            Debug.Log($"Found target task: {task.taskName} with {task.steps.Count} steps");
                            
                            // Calculate absolute step indices (flattened across all tasks)
                            int taskStartIndex = stepStartIndex;
                            int taskEndIndex = stepStartIndex + task.steps.Count - 1;
                            
                            // Update the procedureAutomation with correct indices
                            if (procedureAutomation != null)
                            {
                                procedureAutomation.UpdateTaskIndices(taskStartIndex, taskEndIndex);
                                
                                // Pass the task steps to the automation system
                                Debug.Log($"Initialized automation for task '{task.taskName}' (steps {taskStartIndex}-{taskEndIndex})");
                                
                                // Log steps for debugging
                                for (int j = 0; j < task.steps.Count; j++)
                                {
                                    Debug.Log($"Step {j + 1}: {task.steps[j].instructionText}");
                                }
                            }
                            else
                            {
                                Debug.LogError("ProcedureAutomation reference is missing!");
                            }
                            
                            break;
                        }
                        
                        // Add the step count to our running total before moving to next task
                        stepStartIndex += task.steps.Count;
                    }
                    
                    break;
                }
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