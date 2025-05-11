//* manages the overall geosampling process
using UnityEngine;
using System.Collections.Generic;

public class GeosamplingManager : MonoBehaviour {
    [SerializeField] private GameObject startDisplay; // starting panel with notification and instructions preview
    [SerializeField] private GameObject scanRockPanel; // scan rock panel
    [SerializeField] private GameObject captureOptionsPanel; // options panel (take picture vs take notes)
    [SerializeField] private GameObject photoPanel; // photo panel
    [SerializeField] private AudioRecorder audioRecorder; // page for audio recording
    [SerializeField] private GameObject sampleInfoPanel; //information about sample from tss
    [SerializeField] private GameObject confirmationPanel; // confirm sample info
    
    // store the current state (i.e. which panel or rock is being displayed)
    private enum GeosamplingState {
        Procedure,
        ScanRock,
        CaptureOptions,
        Photo,
        VoiceMemo,
        SampleInfo,
        Confirmation
    }

    // create a new state and sample for each rock collected
    // this will be created when the user scans a rock
    private GeosamplingState currentState = GeosamplingState.Procedure;
    private GeosamplingState currentSample = new GeosamplingState();
}