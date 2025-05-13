//* automatically move on to the next step when the current step is completed
//* pull from the tss to identify the current status of the step/procedure/system
//* advances to the next step based on the status in the telemetry data
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StepAutomation : MonoBehaviour {
    [SerializeField] private ProcedureDisplay procedureDisplay; // the procedure that will be updated (attached to the procedure display)
    [SerializeField] private float checkInterval = 0.5f; // How often to check conditions (seconds)
}