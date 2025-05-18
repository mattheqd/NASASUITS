using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ProcedureListWrapper
{
    public List<Procedure> procedures;
}

public class ProcedureLoader : MonoBehaviour
{
    public List<Procedure> LoadedProcedures { get; private set; } = new List<Procedure>();

    void Awake()
    {
        // Load the JSON text asset from Resources
        TextAsset jsonText = Resources.Load<TextAsset>("procedure_data");
        if (jsonText == null)
        {
            Debug.LogError("Could not find procedure_data.json in Resources!");
            return;
        }

        // Parse the JSON into your wrapper class
        ProcedureListWrapper wrapper = JsonUtility.FromJson<ProcedureListWrapper>(jsonText.text);

        if (wrapper != null && wrapper.procedures != null && wrapper.procedures.Count > 0)
        {
            LoadedProcedures = wrapper.procedures;
        }
        else
        {
            Debug.LogError("No procedures found in JSON!");
        }
    }
} 