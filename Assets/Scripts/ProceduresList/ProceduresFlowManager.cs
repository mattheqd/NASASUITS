//* Handles UI logic for procedures workflow
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
        startProcedureButton.onClick.AddListener(ShowProcedurePreview);
        startSamplingButton.onClick.AddListener(ShowGeoSamplingPreview);
    }

    //---- Starting screen for procedures and geo sampling ----//
    // the starting screen for procedures and geo sampling are the same
    // show the task preview panel for a single procedure (ex: egress)
    private void ShowProcedurePreview()
    {
        proceduresListPanel.SetActive(false); // hide the procedures list
        procedurePreviewPanel.SetActive(true); // show the procedure preview
    }
    private void ShowGeoSamplingPreview()
    {
        proceduresListPanel.SetActive(false);
        procedurePreviewPanel.SetActive(true);
    }

    //---- Helper functions to display steps and details ----//
    // goes to the first step
    // private void ShowProcedureDetails()
    // {
    //     procedurePreviewPanel.SetActive(false);
    //     taskPanel.SetActive(true); 
    // }
    // not used.
    // private void ShowProceduresList()
    // {
    //     procedurePreviewPanel.SetActive(false);
    //     taskPanel.SetActive(false);
    //     proceduresListPanel.SetActive(true);
    // }
} 