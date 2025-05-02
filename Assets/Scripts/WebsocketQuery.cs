using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;

public class TssWebsocket : MonoBehaviour
{
    // HTTP endpoint instead of WebSocket
    public string httpUrl = "https://27ed-128-195-95-17.ngrok-free.app";
    
    // How often to poll for data (in seconds)
    public float pollingInterval = 0.5f;
    
    // Flag to track if we're currently fetching
    private bool isFetching = false;
    
    // Latest telemetry data
    private TssApiResponse latestData = new TssApiResponse();
    
    // Data Structures (Mirroring Backend JSON)
    [System.Serializable]
    public class TelemetryData
    {
        public long timestamp;
        public string type;
    }

    [System.Serializable]
    public class HighFrequencyData : TelemetryData
    {
        // EVA1 DCU (Commands 2-7)
        public float eva1_batt;
        public float eva1_oxy;
        public float eva1_comm;
        public float eva1_fan;
        public float eva1_pump;
        public float eva1_co2;
        // EVA1 IMU (Commands 17-19)
        public float eva1_imu_posx;
        public float eva1_imu_posy;
        public float eva1_imu_heading;
        // EVA2 IMU (Commands 20-22)
        public float eva2_imu_posx;
        public float eva2_imu_posy;
        public float eva2_imu_heading;
        // ROVER (Commands 23-25)
        public float rover_posx;
        public float rover_posy;
        public float rover_qr_id;
    }

    [System.Serializable]
    public class LowFrequencyData : TelemetryData
    {
        // EVA2 DCU (Commands 8-13)
        public float eva2_batt;
        public float eva2_oxy;
        public float eva2_comm;
        public float eva2_fan;
        public float eva2_pump;
        public float eva2_co2;
        // Error States (Commands 14-16)
        public float o2_error;
        public float pump_error;
        public float fan_error;
        // EVA 1 SPEC (Commands 26-36)
        public float eva1_spec_id;
        public float eva1_spec_oxy;
        public float eva1_spec_water;
        public float eva1_spec_co2;
        public float eva1_spec_h2;
        public float eva1_spec_n2;
        public float eva1_spec_other;
        public float eva1_spec_temp;
        public float eva1_spec_pres;
        public float eva1_spec_humid;
        public float eva1_spec_light;
        // EVA 2 SPEC (Commands 37-47)
        public float eva2_spec_id;
        public float eva2_spec_oxy;
        public float eva2_spec_water;
        public float eva2_spec_co2;
        public float eva2_spec_h2;
        public float eva2_spec_n2;
        public float eva2_spec_other;
        public float eva2_spec_temp;
        public float eva2_spec_pres;
        public float eva2_spec_humid;
        public float eva2_spec_light;
        // UIA (Commands 48-57)
        public float uia_emu1_power;
        public float uia_ev1_supply;
        public float uia_ev1_waste;
        public float uia_ev1_oxygen;
        public float uia_emu2_power;
        public float uia_ev2_supply;
        public float uia_ev2_waste;
        public float uia_ev2_oxygen;
        public float uia_o2_vent;
        public float uia_depress_pump;
        // TELEMETRY / EVA (Commands 58-118)
        public float eva_time;
        // EVA1 Telemetry (Commands 59-80)
        public float eva1_batt_time_left;
        public float eva1_oxy_pri_storage;
        public float eva1_oxy_sec_storage;
        public float eva1_oxy_pri_pressure;
        public float eva1_oxy_sec_pressure;
        public float eva1_suit_pressure_oxy;
        public float eva1_suit_pressure_co2;
        public float eva1_suit_pressure_other;
        public float eva1_suit_pressure_total;
        public float eva1_scrubber_a_pressure;
        public float eva1_scrubber_b_pressure;
        public float eva1_h2o_gas_pressure;
        public float eva1_h2o_liquid_pressure;
        public float eva1_oxy_consumption;
        public float eva1_co2_production;
        public float eva1_fan_pri_rpm;
        public float eva1_fan_sec_rpm;
        public float eva1_helmet_pressure_co2;
        public float eva1_heart_rate;
        public float eva1_temperature;
        public float eva1_coolant_gas_pressure;
        public float eva1_coolant_liquid_pressure;
        // EVA2 Telemetry (Commands 81-102)
        public float eva2_batt_time_left;
        public float eva2_oxy_pri_storage;
        public float eva2_oxy_sec_storage;
        public float eva2_oxy_pri_pressure;
        public float eva2_oxy_sec_pressure;
        public float eva2_suit_pressure_oxy;
        public float eva2_suit_pressure_co2;
        public float eva2_suit_pressure_other;
        public float eva2_suit_pressure_total;
        public float eva2_scrubber_a_pressure;
        public float eva2_scrubber_b_pressure;
        public float eva2_h2o_gas_pressure;
        public float eva2_h2o_liquid_pressure;
        public float eva2_oxy_consumption;
        public float eva2_co2_production;
        public float eva2_fan_pri_rpm;
        public float eva2_fan_sec_rpm;
        public float eva2_helmet_pressure_co2;
        public float eva2_heart_rate;
        public float eva2_temperature;
        public float eva2_coolant_gas_pressure;
        public float eva2_coolant_liquid_pressure;
        // Generic EVA States (Commands 103-118)
        public float eva_state_103;
        public float eva_state_104;
        public float eva_state_105;
        public float eva_state_106;
        public float eva_state_107;
        public float eva_state_108;
        public float eva_state_109;
        public float eva_state_110;
        public float eva_state_111;
        public float eva_state_112;
        public float eva_state_113;
        public float eva_state_114;
        public float eva_state_115;
        public float eva_state_116;
        public float eva_state_117;
        public float eva_state_118;
        // Pressurized Rover Telemetry (Commands 119-166)
        public float pr_telemetry_119;
        public float pr_telemetry_120;
        public float pr_telemetry_121;
        public float pr_telemetry_122;
        public float pr_telemetry_123;
        public float pr_telemetry_124;
        public float pr_telemetry_125;
        public float pr_telemetry_126;
        public float pr_telemetry_127;
        public float pr_telemetry_128;
        public float pr_telemetry_129;
        public float pr_telemetry_130;
        public float pr_telemetry_131;
        public float pr_telemetry_132;
        public float pr_telemetry_133;
        public float pr_telemetry_134;
        public float pr_telemetry_135;
        public float pr_telemetry_136;
        public float pr_telemetry_137;
        public float pr_telemetry_138;
        public float pr_telemetry_139;
        public float pr_telemetry_140;
        public float pr_telemetry_141;
        public float pr_telemetry_142;
        public float pr_telemetry_143;
        public float pr_telemetry_144;
        public float pr_telemetry_145;
        public float pr_telemetry_146;
        public float pr_telemetry_147;
        public float pr_telemetry_148;
        public float pr_telemetry_149;
        public float pr_telemetry_150;
        public float pr_telemetry_151;
        public float pr_telemetry_152;
        public float pr_telemetry_153;
        public float pr_telemetry_154;
        public float pr_telemetry_155;
        public float pr_telemetry_156;
        public float pr_telemetry_157;
        public float pr_telemetry_158;
        public float pr_telemetry_159;
        public float pr_telemetry_160;
        public float pr_telemetry_161;
        public float pr_telemetry_162;
        public float pr_telemetry_163;
        public float pr_telemetry_164;
        public float pr_telemetry_165;
        public float pr_telemetry_166;
        // Pressurized Rover LIDAR (Command 167)
        public float pr_lidar;
    }

    [System.Serializable]
    public class TssApiResponse
    {
        public bool success;
        public HighFrequencyData highFrequency;
        public LowFrequencyData lowFrequency;
    }

    void Start()
    {
        // Start the polling coroutine
        StartCoroutine(PollForData());
    }
    
    void OnDestroy()
    {
        // Stop all coroutines when the object is destroyed
        StopAllCoroutines();
    }
    
    void Update()
    {
        // Use latestData to update your game objects, UI, etc.
        if (latestData != null && latestData.success)
        {
            UpdateGameWithTelemetryData();
        }
    }
    
    private IEnumerator PollForData()
    {
        while (true)
        {
            if (!isFetching)
            {
                StartCoroutine(FetchData());
            }
            
            // Wait for the polling interval before fetching again
            yield return new WaitForSeconds(pollingInterval);
        }
    }
    
    private IEnumerator FetchData()
    {
        isFetching = true;
        
        using (UnityWebRequest request = UnityWebRequest.Get(httpUrl))
        {
            // Send the request and wait for a response
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Received data from server");
                string jsonData = request.downloadHandler.text;
                ParseTssData(jsonData);
            }
            else
            {
                Debug.LogError($"HTTP Error: {request.error}");
            }
        }
        
        isFetching = false;
    }
    
    private void ParseTssData(string jsonData)
    {
        try
        {
            TssApiResponse response = JsonUtility.FromJson<TssApiResponse>(jsonData);
            
            if (response != null && response.success)
            {
                // Store the latest data
                latestData = response;
                
                Debug.Log("Successfully parsed TSS data.");
            }
            else
            {
                Debug.LogWarning("Failed to parse TSS data or success flag was false.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing JSON: {e.Message}\nJSON: {jsonData}");
        }
    }
    
    private void UpdateGameWithTelemetryData()
    {
        // Sample usage of telemetry data
        if (latestData.highFrequency != null)
        {
            Debug.Log($"High Frequency Timestamp: {latestData.highFrequency.timestamp}");
            Debug.Log($"EVA1 Battery: {latestData.highFrequency.eva1_batt}");
            Debug.Log($"EVA1 Oxygen: {latestData.highFrequency.eva1_oxy}");
            Debug.Log($"EVA1 Position: ({latestData.highFrequency.eva1_imu_posx}, {latestData.highFrequency.eva1_imu_posy})");
            Debug.Log($"EVA2 Position: ({latestData.highFrequency.eva2_imu_posx}, {latestData.highFrequency.eva2_imu_posy})");
            Debug.Log($"Rover Position: ({latestData.highFrequency.rover_posx}, {latestData.highFrequency.rover_posy})");
            
            // TODO: Update your game objects based on high frequency data
            // Example:
            // transform.position = new Vector3(latestData.highFrequency.rover_posx, 0, latestData.highFrequency.rover_posy);
        }
        
        if (latestData.lowFrequency != null)
        {
            Debug.Log($"Low Frequency Timestamp: {latestData.lowFrequency.timestamp}");
            Debug.Log($"EVA2 Battery: {latestData.lowFrequency.eva2_batt}");
            Debug.Log($"EVA2 Oxygen: {latestData.lowFrequency.eva2_oxy}");
            Debug.Log($"Error States - O2: {latestData.lowFrequency.o2_error}, Pump: {latestData.lowFrequency.pump_error}, Fan: {latestData.lowFrequency.fan_error}");
            Debug.Log($"EVA Time: {latestData.lowFrequency.eva_time}");
            Debug.Log($"EVA1 Battery Time Left: {latestData.lowFrequency.eva1_batt_time_left}");
            Debug.Log($"EVA2 Battery Time Left: {latestData.lowFrequency.eva2_batt_time_left}");
            
            // TODO: Update your game objects based on low frequency data
        }
    }
}