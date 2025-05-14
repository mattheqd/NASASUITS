using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProcedureAutomation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ProcedureDisplay procedureDisplay;
    
    [Header("Automation Settings")]
    [SerializeField] private float checkInterval = 0.5f; // How often to check for data changes (seconds)
    [SerializeField] private bool automationEnabled = true;
    
    [Header("Debug UI")]
    [SerializeField] private GameObject debugPanel;
    [SerializeField] private TextMeshProUGUI debugText;
    [SerializeField] private Button debugToggleButton;
    
    // Mapping for step verification
    private Dictionary<int, StepVerifier> stepVerifiers = new Dictionary<int, StepVerifier>();
    
    // Define indices for the "Connect UIA to DCU and start Depress" task
    // These will be updated by ProcedureSetup
    private int uiaConnectTaskStartIndex = 0;
    private int uiaConnectTaskEndIndex = 0;
    
    // Track current step
    private int currentStepIndex = 0;
    
    // Debug string builder
    private System.Text.StringBuilder debugInfo = new System.Text.StringBuilder();
    private bool showDebug = false;
    
    private void Awake()
    {
        // Start checking for data changes
        StartCoroutine(CheckVerificationRoutine());
        
        // Set up debug UI if available
        if (debugToggleButton != null)
        {
            debugToggleButton.onClick.AddListener(ToggleDebugPanel);
        }
        
        if (debugPanel != null)
        {
            debugPanel.SetActive(showDebug);
        }
        
        Debug.Log("ProcedureAutomation initialized and ready");
    }
    
    // Log the status when enabled
    private void OnEnable()
    {
        if (procedureDisplay == null)
        {
            Debug.LogError("ProcedureAutomation: No ProcedureDisplay reference set!");
            procedureDisplay = GetComponent<ProcedureDisplay>();
            
            if (procedureDisplay == null)
            {
                procedureDisplay = FindObjectOfType<ProcedureDisplay>();
                if (procedureDisplay != null)
                {
                    Debug.Log("ProcedureAutomation: Auto-found ProcedureDisplay component");
                }
            }
        }
        
        Debug.Log($"ProcedureAutomation enabled. Automation is {(automationEnabled ? "ON" : "OFF")}");
    }
    
    private IEnumerator CheckVerificationRoutine()
    {
        while (true)
        {
            if (automationEnabled && stepVerifiers.Count > 0)
            {
                // Check the current step against the latest data
                VerifyCurrentStep();
                
                // Update debug panel if active
                if (showDebug && debugText != null)
                {
                    debugText.text = debugInfo.ToString();
                }
            }
            yield return new WaitForSeconds(checkInterval);
        }
    }
    
    // Toggle debug panel visibility
    public void ToggleDebugPanel()
    {
        showDebug = !showDebug;
        if (debugPanel != null)
        {
            debugPanel.SetActive(showDebug);
        }
    }
    
    // Called by ProcedureSetup when task indices are determined
    public void UpdateTaskIndices(int startIndex, int endIndex)
    {
        uiaConnectTaskStartIndex = startIndex;
        uiaConnectTaskEndIndex = endIndex;
        currentStepIndex = startIndex;
        
        Debug.Log($"Updated task indices: Start={startIndex}, End={endIndex}");
        
        // Set up the verifiers for the steps
        SetupVerifiers();
        
        // Enable automation
        automationEnabled = true;
    }
    
    // For compatibility with ProceduresFlowManager
    public void SetProcedureState(string procedureName, string taskName, int stepIndex)
    {
        // We only care about the "Connect UIA to DCU and start Depress" task
        if (taskName == "Connect UIA to DCU and start Depress")
        {
            // Calculate the absolute step index
            currentStepIndex = uiaConnectTaskStartIndex + stepIndex;
            Debug.Log($"ProcedureAutomation: Now monitoring {procedureName}, {taskName}, step {stepIndex} (absolute index: {currentStepIndex})");
            
            // Make sure automation is enabled
            automationEnabled = true;
        }
        else
        {
            // Not handling other tasks
            Debug.Log($"ProcedureAutomation: Not handling task {taskName}");
            automationEnabled = false;
        }
    }
    
    private void SetupVerifiers()
    {
        // Clear existing verifiers
        stepVerifiers.Clear();
        
        // Step 1 - "EV1 verify umbilical connection from UIA to DCU" - Manual step
        AddStepVerifier(uiaConnectTaskStartIndex, 
            (uiaData, dcuData) => false); // Manual verification needed
        
        // Step 2 - "EV-1, EMU PWR – ON" - Check UIA emu1_power
        AddStepVerifier(uiaConnectTaskStartIndex + 1, 
            (uiaData, dcuData) => {
                if (uiaData != null) {
                    // Specifically look at emu1_power (for EVA1)
                    float value = uiaData.emu1_power;
                    Debug.Log($"[EVA1] Checking EMU power: {value}");
                    
                    // For binary values, any value > 0 should be considered ON
                    return value > 0;
                }
                return false;
            });
        
        // Step 3 - "BATT – UMB" - Check DCU battery
        AddStepVerifier(uiaConnectTaskStartIndex + 2, 
            (uiaData, dcuData) => {
                if (dcuData != null) {
                    // Check if this DCU data belongs to EVA1
                    Debug.Log($"[DCU CHECK] Current DCU EVA ID: {dcuData.evaId}");
                    
                    if (dcuData.evaId != 1) {
                        Debug.LogWarning($"[VERIFICATION FAILED] Ignoring DCU data for EVA {dcuData.evaId}, we only care about EVA1");
                        return false;
                    }
                    
                    float value = dcuData.battery;
                    Debug.Log($"[EVA1] Checking battery: {value}");
                    
                    bool result = value > 0;
                    Debug.Log($"[BATTERY CHECK] Value: {value}, Result: {result}");
                    
                    // For binary values, any value > 0 should be considered ON
                    return result;
                }
                return false;
            });
        
        // Step 4 - "DEPRESS PUMP PWR – ON" - Check UIA depress_pump
        AddStepVerifier(uiaConnectTaskStartIndex + 3, 
            (uiaData, dcuData) => {
                if (uiaData != null) {
                    // We only care about depress_pump from UIA for EVA1
                    float value = uiaData.depress_pump;
                    Debug.Log($"[EVA1] Checking depress pump: {value}");
                    
                    // For binary values, any value > 0 should be considered ON
                    return value > 0;
                }
                return false;
            });
        
        Debug.Log($"Set up {stepVerifiers.Count} verifiers for the UIA to DCU connection task (EVA1 only)");
    }
    
    private void AddStepVerifier(int absoluteStepIndex, System.Func<UiaData, DcuData, bool> verifier)
    {
        stepVerifiers[absoluteStepIndex] = new StepVerifier { Verify = verifier };
        Debug.Log($"Added verifier for step index {absoluteStepIndex}");
    }
    
    private void VerifyCurrentStep()
    {
        // Clear debug info
        debugInfo.Clear();
        debugInfo.AppendLine("=== PROCEDURE AUTOMATION DEBUG ===");
        debugInfo.AppendLine($"Current Step Index: {currentStepIndex}");
        debugInfo.AppendLine($"Task Range: {uiaConnectTaskStartIndex}-{uiaConnectTaskEndIndex}");
        debugInfo.AppendLine($"Automation Enabled: {automationEnabled}");
        
        // Check if we're in the target task range
        if (currentStepIndex < uiaConnectTaskStartIndex || currentStepIndex > uiaConnectTaskEndIndex)
        {
            debugInfo.AppendLine("OUTSIDE TASK RANGE - Automation disabled for this step");
            Debug.Log($"Current step {currentStepIndex} is outside our task range ({uiaConnectTaskStartIndex}-{uiaConnectTaskEndIndex})");
            return;
        }
            
        // Get the latest data from WebSocketClient's static properties
        UiaData uiaData = WebSocketClient.LatestUiaData;
        DcuData dcuData = WebSocketClient.LatestDcuData;
        
        // Add data to debug info
        debugInfo.AppendLine("\n=== CURRENT DATA VALUES (EVA1) ===");
        if (uiaData != null)
        {
            debugInfo.AppendLine("UIA Data for EVA1:");
            debugInfo.AppendLine($"  EMU1 Power: {uiaData.emu1_power}");
            debugInfo.AppendLine($"  EV1 Supply: {uiaData.ev1_supply}");
            debugInfo.AppendLine($"  EV1 Waste: {uiaData.ev1_waste}");
            debugInfo.AppendLine($"  EV1 Oxygen: {uiaData.ev1_oxygen}");
            debugInfo.AppendLine($"  Depress Pump: {uiaData.depress_pump}");
            debugInfo.AppendLine($"  O2 Vent: {uiaData.o2_vent}");
        }
        else
        {
            debugInfo.AppendLine("UIA Data: NULL");
        }
        
        if (dcuData != null)
        {
            debugInfo.AppendLine("\nDCU Data:");
            debugInfo.AppendLine($"  EVA ID: {dcuData.evaId}");
            debugInfo.AppendLine($"  Is EVA1: {(dcuData.evaId == 1 ? "YES" : "NO")}");
            debugInfo.AppendLine($"  Battery: {dcuData.battery}");
            debugInfo.AppendLine($"  Oxygen: {dcuData.oxygen}");
            debugInfo.AppendLine($"  Comm: {dcuData.comm}");
            debugInfo.AppendLine($"  Fan: {dcuData.fan}");
            debugInfo.AppendLine($"  Pump: {dcuData.pump}");
            debugInfo.AppendLine($"  CO2: {dcuData.co2}");
        }
        else
        {
            debugInfo.AppendLine("\nDCU Data: NULL");
        }
        
        // Debug logging
        if (uiaData != null) {
            Debug.Log($"[EVA1] Current UIA Data - EMU Power: {uiaData.emu1_power}, Depress Pump: {uiaData.depress_pump}");
        }
        
        if (dcuData != null) {
            Debug.Log($"Current DCU Data - EVA ID: {dcuData.evaId}, Battery: {dcuData.battery}");
        }
        
        // Only proceed if we have the necessary data
        if (uiaData == null && dcuData == null) {
            debugInfo.AppendLine("\nERROR: No UIA or DCU data available for verification");
            Debug.LogWarning("No UIA or DCU data available for verification");
            return;
        }
        
        debugInfo.AppendLine($"\n=== VERIFICATION FOR STEP {currentStepIndex} ===");
        
        if (stepVerifiers.TryGetValue(currentStepIndex, out StepVerifier verifier))
        {
            // Check if the step is verified
            bool verified = verifier.Verify(uiaData, dcuData);
            debugInfo.AppendLine($"VERIFICATION RESULT: {verified}");
            Debug.Log($"Step verification result: {verified}");
            
            if (verified)
            {
                debugInfo.AppendLine("STEP VERIFIED! Advancing to next step");
                Debug.Log($"Step verified! Absolute index: {currentStepIndex}");
                
                // Complete the step and move to next one
                if (procedureDisplay != null)
                {
                    procedureDisplay.CompleteCurrentStep();
                    
                    // Move to next step
                    currentStepIndex++;
                    
                    // If we've reached the end of our task, disable automation
                    if (currentStepIndex > uiaConnectTaskEndIndex)
                    {
                        Debug.Log("Reached end of automated task");
                        automationEnabled = false;
                    }
                }
                else
                {
                    Debug.LogError("Cannot complete step - ProcedureDisplay is null");
                }
            }
            else
            {
                // Debug info about what we're expecting
                int stepRelativeIndex = currentStepIndex - uiaConnectTaskStartIndex;
                
                switch (stepRelativeIndex)
                {
                    case 0:
                        debugInfo.AppendLine("WAITING FOR: Manual verification (press the button)");
                        break;
                    case 1:
                        debugInfo.AppendLine($"WAITING FOR: EMU1 Power > 0 (current: {(uiaData != null ? uiaData.emu1_power : "NULL")})");
                        break;
                    case 2:
                        if (dcuData != null && dcuData.evaId != 1)
                            debugInfo.AppendLine($"WAITING FOR: DCU data for EVA1 (current evaId: {dcuData.evaId})");
                        else
                            debugInfo.AppendLine($"WAITING FOR: Battery > 0 (current: {(dcuData != null ? dcuData.battery : "NULL")})");
                        break;
                    case 3:
                        debugInfo.AppendLine($"WAITING FOR: Depress Pump > 0 (current: {(uiaData != null ? uiaData.depress_pump : "NULL")})");
                        break;
                }
            }
        }
        else
        {
            debugInfo.AppendLine("ERROR: No verifier found for this step index!");
            Debug.LogWarning($"No verifier found for index: {currentStepIndex}");
        }
    }
    
    // Method to manually complete the current step (for steps that can't be automatically verified)
    public void ManualCompleteStep()
    {
        if (procedureDisplay != null)
        {
            procedureDisplay.CompleteCurrentStep();
            currentStepIndex++;
            Debug.Log($"Manually completed step. Now at absolute index: {currentStepIndex}");
            
            // If we've reached the end of our task, disable automation
            if (currentStepIndex > uiaConnectTaskEndIndex)
            {
                Debug.Log("Reached end of automated task");
                automationEnabled = false;
            }
        }
    }
    
    // Helper class for step verification
    private class StepVerifier
    {
        public System.Func<UiaData, DcuData, bool> Verify;
    }
    
    // Enable or disable automation via inspector or other scripts
    public void SetAutomationEnabled(bool enabled)
    {
        automationEnabled = enabled;
    }
} 