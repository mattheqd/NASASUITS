/**
 * Data structure for procedures
 * - all instructions and procedures are mapped to numerical IDs and stored in a DB
 */
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Add a specific namespace to avoid conflicts with global namespace
namespace ProcedureSystem
{
    [Serializable]
    public enum InstructionStatus
    {
        NotStarted,
        InProgress,
        Completed,
        Skipped
    }

    // A single step in a list of instructions for each procedure
    [Serializable]
    public class InstructionStep
    {
        public string instructionText;
        public bool isAutoVerifiable = false; 
        public bool requiresManualVerification = false;
        public float estimatedTimeSeconds = 30f;
        public string[] requiredEquipment;
        public string[] annotations;
        // public InstructionStatus status = InstructionStatus.NotStarted;
    }

    // A task is a collection of steps
    [Serializable]
    public class Task
    {
        public string taskName;
        public List<InstructionStep> instructionSteps = new List<InstructionStep>();
    }

    // A single procedure that stores a list of tasks
    [Serializable]
    public class Procedure
    {
        // Core properties
        public string procedureName;
        public string taskName;
        public string procedureDescription;
        public string categoryName;
        
        // Duration and timing
        public float totalEstimatedDuration; // in seconds
        public DateTime scheduleTime;
        
        // Instruction steps collection
        public List<InstructionStep> instructionSteps = new List<InstructionStep>();
        
        // Status tracking
        public bool isCompleted = false;
        public bool isInProgress = false;
        public int currentStepIndex = 0;
        
        // UI display properties
        public Color procedureColor = Color.white;
        public string iconPath;
        
        // References to other procedures
        public string[] prerequisiteProcedures;
        public string[] followUpProcedures;
        
        // Optional component keys
        public string[] requiredComponents;
        
        // Constructor
        public Procedure()
        {
            procedureName = "New Procedure";
            taskName = "Default Task";
            procedureDescription = "No description provided.";
            instructionSteps = new List<InstructionStep>();
        }
        
        // Helper methods
        public int GetStepCount()
        {
            return instructionSteps.Count;
        }
        
        public InstructionStep GetCurrentStep()
        {
            if (currentStepIndex >= 0 && currentStepIndex < instructionSteps.Count)
                return instructionSteps[currentStepIndex];
            return null;
        }
        
        public bool MoveToNextStep()
        {
            if (currentStepIndex < instructionSteps.Count - 1)
            {
                currentStepIndex++;
                return true;
            }
            
            // No more steps, procedure is complete
            isCompleted = true;
            isInProgress = false;
            return false;
        }
        
        public void Reset()
        {
            currentStepIndex = 0;
            isCompleted = false;
            isInProgress = true;
        }
    }

    // Collection class to manage multiple procedures
    // ex: [Egress, Ingress, Navigation, etc.]
    [Serializable]
    public class ProcedureCollection
    {
        public List<Procedure> procedures = new List<Procedure>();
    }
}