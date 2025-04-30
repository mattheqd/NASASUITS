/**
 * Data structure for procedures
 * - all instructions and procedures are mapped to numerical IDs and stored in a DB
 */
using System;
using System.Collections.Generic;
using UnityEngine;

// A single step in a list of instructions for each procedure
[Serializable]
public class InstructionStep
{
    public string instructionText; // instruction text (ex: "Press the button")
    public bool requiresConfirmation = true; // user must confirm (click a button or voice interaction to continue)
    public bool isCompleted = false; // default to false
    public bool isSkipped = false; // user can skip steps

}

// list of instructions make up a procedure
[Serializable]
public class Procedure
{
    public string procedureName; // name of the procedure (ex: "Egress")
    public string procedureDescription; // description of the procedure
    public List<InstructionStep> instructionSteps; // list of instructions for the procedure
}

// Procedures database stored in a container
[System.Serializable]
public class ProcedureContainer
{
    public List<Procedure> procedures = new List<Procedure>();
}

// Procedure manager to load and store procedures - claude 3.7
public class ProcedureManager : MonoBehaviour
{
    private static ProcedureManager _instance; // singleton instance
    public static ProcedureManager Instance => _instance; 
    
    private ProcedureContainer allProcedures;
    
    // Awake is called when script is loaded
    // destroy duplicate instances
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            LoadProcedures();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // Load procedures from JSON file
    private void LoadProcedures()
    {
        // Load JSON from a TextAsset in Resources folder
        TextAsset jsonFile = Resources.Load<TextAsset>("procedures_data");
        if (jsonFile != null)
        {
            allProcedures = JsonUtility.FromJson<ProcedureContainer>(jsonFile.text);
        }
        else
        {
            Debug.LogError("Procedures data file not found!");
            allProcedures = new ProcedureContainer();
        }
    }
    
    // Get procedure by name
    public Procedure GetProcedureByName(string name)
    {
        return allProcedures.procedures.Find(p => p.procedureName == name);
    }
    
    // Get all procedures
    public List<Procedure> GetAllProcedures()
    {
        return allProcedures.procedures;
    }
}
