/**
 * Manager that loads and accesses procedure data from JSON files
 */
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ProcedureManager : MonoBehaviour
{
    private static ProcedureManager _instance;
    public static ProcedureManager Instance { get { return _instance; } }

    // Container for all procedures
    private ProcedureContainer procedureContainer;
    
    // Raw JSON data for task extraction
    private JsonProcedureContainer rawJsonData;
    
    // JSON structure that matches the structure in procedure_data.json
    [System.Serializable]
    private class JsonProcedure
    {
        public string procedureName;
        public string procedureDescription;
        public List<JsonTask> tasks = new List<JsonTask>();
    }
    
    [System.Serializable]
    private class JsonTask
    {
        public string taskName;
        public string taskDescription;
        public List<JsonStep> steps = new List<JsonStep>();
    }
    
    [System.Serializable]
    private class JsonStep
    {
        public string instructionText;
        public string location;
        public string status;
    }
    
    [System.Serializable]
    private class JsonProcedureContainer
    {
        public List<JsonProcedure> procedures = new List<JsonProcedure>();
    }
    
    //*------ Functions ------*/
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Load procedures from JSON
        LoadProceduresFromJSON();
    }
    
    // Load procedures from JSON file in Resources folder
    private void LoadProceduresFromJSON()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("procedure_data");
        
        if (jsonFile)
        {
            // Parse JSON to intermediate structure and store for task extraction
            rawJsonData = JsonUtility.FromJson<JsonProcedureContainer>(jsonFile.text);
            
            // Convert to our runtime structure
            procedureContainer = new ProcedureContainer();
            procedureContainer.procedures = new List<Procedure>();
            
            foreach (var jsonProc in rawJsonData.procedures)
            {
                Procedure procedure = new Procedure();
                procedure.procedureName = jsonProc.procedureName;
                procedure.procedureDescription = jsonProc.procedureDescription;
                procedure.instructionSteps = new List<InstructionStep>();
                
                // Flatten tasks and steps into instructionSteps
                foreach (var task in jsonProc.tasks)
                {
                    // For each task, add all its steps to the procedure's instructionSteps
                    foreach (var step in task.steps)
                    {
                        InstructionStep instructionStep = new InstructionStep();
                        instructionStep.instructionText = step.instructionText;
                        
                        // Convert status if needed
                        instructionStep.status = InstructionStatus.NotStarted;
                        
                        procedure.instructionSteps.Add(instructionStep);
                    }
                }
                
                procedureContainer.procedures.Add(procedure);
            }
            
            Debug.Log($"Loaded {procedureContainer.procedures.Count} procedures with a total of " +
                      $"{procedureContainer.procedures.Sum(p => p.instructionSteps.Count)} steps");
        }
        else
        {
            Debug.LogError("Failed to load procedure_data.json");
        }
    }
    
    // Get a procedure by name
    public Procedure GetProcedure(string procedureName)
    {
        if (procedureContainer == null || procedureContainer.procedures == null)
        {
            Debug.LogError("Procedure container not initialized");
            return null;
        }
        
        foreach (Procedure procedure in procedureContainer.procedures)
        {
            if (procedure.procedureName == procedureName)
            {
                Debug.Log($"Found procedure '{procedureName}' with {procedure.instructionSteps.Count} steps");
                return procedure;
            }
        }
        
        Debug.LogWarning($"Procedure '{procedureName}' not found");
        return null;
    }
    
    // Get only a specific task from a procedure
    public Procedure GetSpecificTask(string procedureName, string taskName)
    {
        if (rawJsonData == null || rawJsonData.procedures == null)
        {
            Debug.LogError("JSON data not initialized");
            return null;
        }
        
        // Find the specific procedure
        foreach (var jsonProc in rawJsonData.procedures)
        {
            if (jsonProc.procedureName == procedureName)
            {
                // Find the specific task
                foreach (var task in jsonProc.tasks)
                {
                    if (task.taskName == taskName)
                    {
                        // Create a new procedure with just this task's steps
                        Procedure taskProcedure = new Procedure();
                        taskProcedure.procedureName = procedureName + ": " + taskName;
                        taskProcedure.procedureDescription = task.taskDescription;
                        taskProcedure.instructionSteps = new List<InstructionStep>();
                        
                        // Add only this task's steps
                        foreach (var step in task.steps)
                        {
                            InstructionStep instructionStep = new InstructionStep();
                            instructionStep.instructionText = step.instructionText;
                            instructionStep.status = InstructionStatus.NotStarted;
                            taskProcedure.instructionSteps.Add(instructionStep);
                        }
                        
                        Debug.Log($"Extracted task '{taskName}' from procedure '{procedureName}' with {taskProcedure.instructionSteps.Count} steps");
                        return taskProcedure;
                    }
                }
                
                Debug.LogWarning($"Task '{taskName}' not found in procedure '{procedureName}'");
                return null;
            }
        }
        
        Debug.LogWarning($"Procedure '{procedureName}' not found for task extraction");
        return null;
    }
    
    // Get all available procedures
    public List<Procedure> GetAllProcedures()
    {
        if (procedureContainer == null)
        {
            return new List<Procedure>();
        }
        
        return procedureContainer.procedures;
    }
    
    // Static method to ensure there is a ProcedureManager in the scene
    public static ProcedureManager GetOrCreateInstance()
    {
        if (_instance != null)
            return _instance;
            
        // Try to find an existing manager
        ProcedureManager manager = FindObjectOfType<ProcedureManager>();
        if (manager != null)
        {
            _instance = manager;
            return _instance;
        }
        
        // Create a new manager if none exists
        GameObject managerObject = new GameObject("ProcedureManager");
        _instance = managerObject.AddComponent<ProcedureManager>();
        Debug.Log("Created new ProcedureManager instance");
        return _instance;
    }
}