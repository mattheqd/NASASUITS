/**
 * Data structure for procedures
 * - all instructions and procedures are mapped to numerical IDs and stored in a DB
 */
using System;
using System.Collections.Generic;
using UnityEngine;

// A single step in a list of instructions for each procedure
[Serializable]
public enum InstructionStatus
{
    NotStarted,
    InProgress,
    Completed,
    Skipped
}

[Serializable]
public class InstructionStep
{
    public string instructionText; // instruction text (ex: "Press the button")
    public bool requiresConfirmation = true; // user must confirm (click a button or voice interaction to continue)
    public InstructionStatus status = InstructionStatus.NotStarted; // Track status instead of separate booleans
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