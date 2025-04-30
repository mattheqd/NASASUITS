/**
 * Manager that loads and accesses procedure data from JSON files
 */
using System.Collections.Generic;
using UnityEngine;

public class ProcedureManager : MonoBehaviour
{
    private static ProcedureManager _instance;
    public static ProcedureManager Instance { get { return _instance; } }

    // Container for all procedures
    private ProcedureContainer procedureContainer;
    
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
        
        if (jsonFile){
            procedureContainer = JsonUtility.FromJson<ProcedureContainer>(jsonFile.text);
            Debug.Log($"Loaded {procedureContainer.procedures.Count} procedures from JSON");
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
                return procedure;
            }
        }
        
        Debug.LogWarning($"Procedure '{procedureName}' not found");
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
}