/**
 * Manager that loads and accesses procedure data from JSON files
 */
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ProcedureSystem;

public class ProcedureManager : MonoBehaviour
{
    private static ProcedureManager _instance;
    public static ProcedureManager Instance { get { return _instance; } }

    // Container for all procedures when the user first loads the procedures list
    private ProcedureCollection procedureContainer;
    
    // Raw JSON data for task extraction
    private ProcedureCollection rawJsonData;
    
    // JSON structures that match the format in procedure_data.json
    [System.Serializable]
    private class JsonProcedureContainer
    {
        public List<Procedure> procedures;
    }
    
    private void Awake()
    {
        // Singleton pattern
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(this.gameObject);
        
        // Load procedures
        LoadProcedures();
    }
    
    // creates a container that stores all procedures
    private void LoadProcedures()
    {
        // loads the json file as a series of bytes
        TextAsset jsonFile = Resources.Load<TextAsset>("procedure_data");
        
        if (jsonFile)
        {
            // JSON container stores all procedures in raw json format
            JsonProcedureContainer jsonContainer = JsonUtility.FromJson<JsonProcedureContainer>(jsonFile.text);
            
            // Create a runtime container to store all procedures
            // This is used to store and retrieve procedures at runtime
            // we will store the parsed json data into containers
            // using a separate container makes it easier to manage and retrieve procedures
            procedureContainer = new ProcedureCollection(); 
            procedureContainer.procedures = new List<Procedure>();
            
            // Copy procedures from JSON container to our runtime container
            if (jsonContainer != null && jsonContainer.procedures != null)
            {
                foreach (var proc in jsonContainer.procedures)
                {
                    procedureContainer.procedures.Add(proc); // add each procedure to the container
                }
            }
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
        
        // Find the procedure by name
        Procedure matchingProcedure = procedureContainer.procedures.Find(p => p.procedureName == procedureName);
        
        if (matchingProcedure != null)
        {
            Debug.Log($"Found procedure '{procedureName}' with {matchingProcedure.instructionSteps.Count} steps");
            return matchingProcedure;
        }
        
        Debug.LogWarning($"Procedure '{procedureName}' not found");
        return null;
    }
    
    // Get a specific task from a procedure
    public Procedure GetProcedureTask(string procedureName, string taskName)
    {
        if (procedureContainer == null || procedureContainer.procedures == null)
        {
            Debug.LogError("Procedure container not initialized");
            return null;
        }
        
        // In the new structure, each "task" is its own procedure with a taskName property
        Procedure taskProcedure = procedureContainer.procedures.Find(p => 
            p.procedureName == procedureName && p.taskName == taskName);
        
        if (taskProcedure != null)
        {
            Debug.Log($"Found task '{taskName}' in procedure '{procedureName}' with {taskProcedure.instructionSteps.Count} steps");
            return taskProcedure;
        }
        
        Debug.LogWarning($"Task '{taskName}' not found in procedure '{procedureName}'");
        return null;
    }
    
    // Get all available procedures
    public List<Procedure> GetAllProcedures()
    {
        if (procedureContainer == null || procedureContainer.procedures == null)
        {
            Debug.LogError("Procedure container not initialized");
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

    /// <summary>
    /// Returns a list of all task names for a specific procedure
    /// </summary>
    /// <param name="procedureName">Name of the procedure to get tasks for</param>
    /// <returns>List of task names within the procedure</returns>
    public List<string> LoadProcedureTasks(string procedureName)
    {
        List<string> taskNames = new List<string>();
        
        if (procedureContainer == null || procedureContainer.procedures == null)
        {
            Debug.LogError($"Procedure container not initialized when trying to load tasks for {procedureName}");
            return taskNames;
        }
        
        // Find all procedures with the matching procedureName
        var matchingProcedures = procedureContainer.procedures
            .Where(p => p.procedureName == procedureName)
            .ToList();
        
        if (matchingProcedures.Count == 0)
        {
            Debug.LogWarning($"No procedures found with name '{procedureName}'");
            return taskNames;
        }
        
        // Extract all unique task names
        foreach (var proc in matchingProcedures)
        {
            if (!string.IsNullOrEmpty(proc.taskName))
            {
                // Only add unique task names
                if (!taskNames.Contains(proc.taskName))
                {
                    taskNames.Add(proc.taskName);
                }
            }
        }
        
        // Log the results
        Debug.Log($"Found {taskNames.Count} tasks for procedure '{procedureName}': {string.Join(", ", taskNames)}");
        
        return taskNames;
    }
}
