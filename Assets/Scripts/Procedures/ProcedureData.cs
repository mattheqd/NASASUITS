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
// A single step in a task for each procedure
[Serializable]
public class InstructionStep
{
    public string instructionText; // instruction text (ex: "Press the button")
    public bool requiresConfirmation = true; // user must confirm (click a button or voice interaction to continue)
    public InstructionStatus status = InstructionStatus.NotStarted; // Track status instead of separate booleans
}
// A single task in a list of tasks for each procedure
[Serializable]
public class Task
{
    public string taskName; // name of the task (ex: "Egress")
    public List<InstructionStep> instructionSteps; // list of steps for the task
}


// list of tasks make up a procedure
[Serializable]
public class Procedure
{
    public string procedureName; // name of the procedure (ex: "Egress")
    public List<Task> tasks; // list of tasks for the procedure
}

// Procedures database stored in a container
[System.Serializable]
public class ProcedureContainer
{
    public List<Procedure> procedures = new List<Procedure>();
}