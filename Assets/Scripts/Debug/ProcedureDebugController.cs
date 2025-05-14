using UnityEngine;
using UnityEngine.UI;

public class ProcedureDebugController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ProcedureAutomation procedureAutomation;
    
    [Header("UI Controls")]
    [SerializeField] private Button forceAdvanceButton;
    [SerializeField] private Button simulateEmuButton;
    [SerializeField] private Button simulateBatteryButton;
    [SerializeField] private Button simulateDepressPumpButton;
    
    private void Awake()
    {
        // Find ProcedureAutomation if not assigned
        if (procedureAutomation == null)
        {
            procedureAutomation = FindObjectOfType<ProcedureAutomation>();
            if (procedureAutomation == null)
            {
                Debug.LogError("No ProcedureAutomation found in the scene!");
            }
        }
        
        // Set up button listeners
        if (forceAdvanceButton != null)
        {
            forceAdvanceButton.onClick.AddListener(ForceAdvanceProcedure);
        }
        
        if (simulateEmuButton != null)
        {
            simulateEmuButton.onClick.AddListener(SimulateEmuPower);
        }
        
        if (simulateBatteryButton != null)
        {
            simulateBatteryButton.onClick.AddListener(SimulateBattery);
        }
        
        if (simulateDepressPumpButton != null)
        {
            simulateDepressPumpButton.onClick.AddListener(SimulateDepressPump);
        }
    }
    
    public void ForceAdvanceProcedure()
    {
        if (procedureAutomation != null)
        {
            procedureAutomation.ManualCompleteStep();
            Debug.Log("Manually forced procedure to advance");
        }
    }
    
    public void SimulateEmuPower()
    {
        UiaData uiaData = new UiaData
        {
            emu1_power = 1,
            ev1_supply = 0,
            ev1_waste = 0,
            ev1_oxygen = 0,
            emu2_power = 0,
            ev2_supply = 0,
            ev2_waste = 0,
            ev2_oxygen = 0,
            o2_vent = 0,
            depress_pump = 0
        };
        
        WebSocketClient.SetTestUiaData(uiaData);
        Debug.Log("Simulated EMU Power set to 1");
    }
    
    public void SimulateBattery()
    {
        DcuData dcuData = new DcuData
        {
            evaId = 1,
            battery = 1,
            oxygen = 0,
            comm = 0,
            fan = 0,
            pump = 0,
            co2 = 0
        };
        
        WebSocketClient.SetTestDcuData(dcuData);
        Debug.Log("Simulated Battery set to 1 for EVA1");
    }
    
    public void SimulateDepressPump()
    {
        UiaData uiaData = new UiaData
        {
            emu1_power = 1,
            ev1_supply = 0,
            ev1_waste = 0,
            ev1_oxygen = 0,
            emu2_power = 0,
            ev2_supply = 0,
            ev2_waste = 0,
            ev2_oxygen = 0,
            o2_vent = 0,
            depress_pump = 1
        };
        
        WebSocketClient.SetTestUiaData(uiaData);
        Debug.Log("Simulated Depress Pump set to 1");
    }
} 