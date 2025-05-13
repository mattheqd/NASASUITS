using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProcedureAutomation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ProcedureDisplay procedureDisplay;
    
    // Mapping for step verification
    private Dictionary<string, StepVerifier> stepVerifiers = new Dictionary<string, StepVerifier>();
    
    // Current procedure state
    private string currentProcedureName;
    private string currentTaskName;
    private int currentStepIndex;
    private bool automationEnabled = true;
    
    private void Awake()
    {
        // Set up WebSocket subscriptions
        if (WebSocketClient.Instance != null)
        {
            WebSocketClient.Instance.Subscribe("uia_data", OnUiaDataReceived);
            WebSocketClient.Instance.Subscribe("dcu_data", OnDcuDataReceived);
        }
        else
        {
            Debug.LogWarning("WebSocketClient instance not found - automation won't work until it's available");
            StartCoroutine(TryConnectLater());
        }
        
        // Set up step verifiers for "Connect UIA to DCU and start Depress" task
        SetupEgressVerifiers();
    }
    
    private IEnumerator TryConnectLater()
    {
        yield return new WaitForSeconds(2f);
        if (WebSocketClient.Instance != null)
        {
            WebSocketClient.Instance.Subscribe("uia_data", OnUiaDataReceived);
            WebSocketClient.Instance.Subscribe("dcu_data", OnDcuDataReceived);
        }
        else
        {
            Debug.LogWarning("WebSocketClient still not available");
        }
    }
    
    private void SetupEgressVerifiers()
    {
        // Add verifiers for specific steps in the "Connect UIA to DCU and start Depress" task
        
        // Step 1 - Manual verification (umbilical connection)
        AddStepVerifier("EVA Egress", "Connect UIA to DCU and start Depress", 0, 
            (uiaData, dcuData) => false); // Manual step, no auto-verification
        
        // Step 2 - "EV-1, EMU PWR – ON" - Check UIA data
        AddStepVerifier("EVA Egress", "Connect UIA to DCU and start Depress", 1, 
            (uiaData, dcuData) => uiaData != null && uiaData.emu1_power > 0);
        
        // Step 3 - "BATT – UMB" - Check DCU data
        AddStepVerifier("EVA Egress", "Connect UIA to DCU and start Depress", 2, 
            (uiaData, dcuData) => dcuData != null && dcuData.battery > 0);
        
        // Step 4 - "DEPRESS PUMP PWR – ON" - Check UIA data
        AddStepVerifier("EVA Egress", "Connect UIA to DCU and start Depress", 3, 
            (uiaData, dcuData) => uiaData != null && uiaData.depress_pump > 0);
    }
    
    public void SetProcedureState(string procedureName, string taskName, int stepIndex)
    {
        currentProcedureName = procedureName;
        currentTaskName = taskName;
        currentStepIndex = stepIndex;
        
        // Log the current state for debugging
        Debug.Log($"ProcedureAutomation: Now monitoring {procedureName}, {taskName}, Step {stepIndex}");
    }
    
    private void AddStepVerifier(string procedureName, string taskName, int stepIndex, System.Func<UiaData, DcuData, bool> verifier)
    {
        string key = GetStepKey(procedureName, taskName, stepIndex);
        stepVerifiers[key] = new StepVerifier { Verify = verifier };
    }
    
    private string GetStepKey(string procedureName, string taskName, int stepIndex)
    {
        return $"{procedureName}|{taskName}|{stepIndex}";
    }
    
    private void OnUiaDataReceived(object data)
    {
        if (!automationEnabled) return;
        
        UiaData uiaData = data as UiaData;
        if (uiaData == null) return;
        
        // Check if current step can be verified
        VerifyCurrentStep(uiaData, null);
    }
    
    private void OnDcuDataReceived(object data)
    {
        if (!automationEnabled) return;
        
        DcuData dcuData = data as DcuData;
        if (dcuData == null) return;
        
        // Check if current step can be verified
        VerifyCurrentStep(null, dcuData);
    }
    
    private void VerifyCurrentStep(UiaData uiaData, DcuData dcuData)
    {
        if (string.IsNullOrEmpty(currentProcedureName) || string.IsNullOrEmpty(currentTaskName))
            return;
            
        string stepKey = GetStepKey(currentProcedureName, currentTaskName, currentStepIndex);
        if (stepVerifiers.TryGetValue(stepKey, out StepVerifier verifier))
        {
            // Get the last known values if one is null
            UiaData latestUia = uiaData ?? GetLastKnownUiaData();
            DcuData latestDcu = dcuData ?? GetLastKnownDcuData();
            
            // Check if the step is verified
            if (verifier.Verify(latestUia, latestDcu))
            {
                Debug.Log($"ProcedureAutomation: Step verified! {currentProcedureName}, {currentTaskName}, Step {currentStepIndex}");
                
                // Complete the step and move to next one
                if (procedureDisplay != null)
                {
                    procedureDisplay.CompleteCurrentStep();
                }
                
                // Update our tracking
                currentStepIndex++;
            }
        }
    }
    
    // Cache for last known values
    private UiaData lastUiaData;
    private DcuData lastDcuData;
    
    private UiaData GetLastKnownUiaData()
    {
        return lastUiaData;
    }
    
    private DcuData GetLastKnownDcuData()
    {
        return lastDcuData;
    }
    
    // Helper class for step verification
    private class StepVerifier
    {
        public System.Func<UiaData, DcuData, bool> Verify;
    }
    
    // Enable or disable automation
    public void SetAutomationEnabled(bool enabled)
    {
        automationEnabled = enabled;
    }
} 