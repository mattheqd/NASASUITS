using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class TssButtonFetcher : MonoBehaviour
{
    public string ngrokUrl = "https://b36e-128-195-95-16.ngrok-free.app";
    public TextMeshProUGUI displayText;

    [System.Serializable]
    public class TelemetryData
    {
        public long timestamp;
        public string type;
    }

    [System.Serializable]
    public class HighFrequencyData : TelemetryData
    {
        public float eva1_batt;
        public float eva1_oxy;
        public float eva1_comm;
        public float eva1_fan;
        public float eva1_pump;
        public float eva1_co2;
        public float eva1_imu_posx;
        public float eva1_imu_posy;
        public float eva1_imu_heading;
        public float eva2_imu_posx;
        public float eva2_imu_posy;
        public float eva2_imu_heading;
        public float rover_posx;
        public float rover_posy;
        public float rover_qr_id;
    }

    [System.Serializable]
    public class LowFrequencyData : TelemetryData
    {
        public float eva2_batt;
        public float eva2_oxy;
        public float eva2_comm;
        public float eva2_fan;
        public float eva2_pump;
        public float eva2_co2;
        public float o2_error;
        public float pump_error;
        public float fan_error;
        public float eva_time;
        public float eva1_batt_time_left;
        public float eva2_batt_time_left;
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
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(FetchTssDataOnClick);
        }
        else
        {
            Debug.LogError("TssButtonFetcher must be attached to a UI Button component");
        }

        if (displayText == null)
        {
            Debug.LogWarning("Display Text is not assigned. Please assign a TextMeshProUGUI component in the inspector.");
        }
    }

    public void FetchTssDataOnClick()
    {
        Debug.Log("Button clicked! Fetching TSS data...");
        StartCoroutine(FetchTssData());
    }

    IEnumerator FetchTssData()
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(ngrokUrl))
        {
            yield return webRequest.SendWebRequest();

            switch (webRequest.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                    Debug.LogError("Connection Error: " + webRequest.error);
                    break;
                case UnityWebRequest.Result.ProtocolError:
                    Debug.LogError("HTTP Error: " + webRequest.error);
                    break;
                case UnityWebRequest.Result.Success:
                    Debug.Log("Data received successfully");
                    ParseTssData(webRequest.downloadHandler.text);
                    break;
            }
        }
    }

    void ParseTssData(string jsonData)
    {
        try
        {
            TssApiResponse response = JsonUtility.FromJson<TssApiResponse>(jsonData);
            string displayString = "";

            if (response != null && response.success)
            {
                Debug.Log("Successfully parsed TSS data.");
                displayString += "TSS DATA REPORT\n--------------------\n\n";

                if (response.highFrequency != null)
                {
                    Debug.Log($"High Frequency Timestamp: {response.highFrequency.timestamp}");
                    
                    displayString += "<b>HIGH FREQUENCY DATA</b>\n";
                    displayString += $"Timestamp: {response.highFrequency.timestamp}\n";
                    displayString += $"EVA1 Battery: {response.highFrequency.eva1_batt}\n";
                    displayString += $"EVA1 Oxygen: {response.highFrequency.eva1_oxy}\n";
                    displayString += $"EVA1 Position: ({response.highFrequency.eva1_imu_posx}, {response.highFrequency.eva1_imu_posy})\n";
                    displayString += $"EVA2 Position: ({response.highFrequency.eva2_imu_posx}, {response.highFrequency.eva2_imu_posy})\n";
                    displayString += $"Rover Position: ({response.highFrequency.rover_posx}, {response.highFrequency.rover_posy})\n\n";
                    
                    Debug.Log($"EVA1 Battery: {response.highFrequency.eva1_batt}");
                    Debug.Log($"EVA1 Oxygen: {response.highFrequency.eva1_oxy}");
                    Debug.Log($"EVA1 Position: ({response.highFrequency.eva1_imu_posx}, {response.highFrequency.eva1_imu_posy})");
                    Debug.Log($"EVA2 Position: ({response.highFrequency.eva2_imu_posx}, {response.highFrequency.eva2_imu_posy})");
                    Debug.Log($"Rover Position: ({response.highFrequency.rover_posx}, {response.highFrequency.rover_posy})");
                }
                else
                {
                    Debug.LogWarning("No high-frequency data received in this response.");
                    displayString += "No high-frequency data available.\n\n";
                }

                if (response.lowFrequency != null)
                {
                    Debug.Log($"Low Frequency Timestamp: {response.lowFrequency.timestamp}");
                    
                    displayString += "<b>LOW FREQUENCY DATA</b>\n";
                    displayString += $"Timestamp: {response.lowFrequency.timestamp}\n";
                    displayString += $"EVA2 Battery: {response.lowFrequency.eva2_batt}\n";
                    displayString += $"EVA2 Oxygen: {response.lowFrequency.eva2_oxy}\n";
                    displayString += $"Error States - O2: {response.lowFrequency.o2_error}, Pump: {response.lowFrequency.pump_error}, Fan: {response.lowFrequency.fan_error}\n";
                    displayString += $"EVA Time: {response.lowFrequency.eva_time}\n";
                    displayString += $"EVA1 Battery Time Left: {response.lowFrequency.eva1_batt_time_left}\n";
                    displayString += $"EVA2 Battery Time Left: {response.lowFrequency.eva2_batt_time_left}\n";
                    
                    Debug.Log($"EVA2 Battery: {response.lowFrequency.eva2_batt}");
                    Debug.Log($"EVA2 Oxygen: {response.lowFrequency.eva2_oxy}");
                    Debug.Log($"Error States - O2: {response.lowFrequency.o2_error}, Pump: {response.lowFrequency.pump_error}, Fan: {response.lowFrequency.fan_error}");
                    Debug.Log($"EVA Time: {response.lowFrequency.eva_time}");
                    Debug.Log($"EVA1 Battery Time Left: {response.lowFrequency.eva1_batt_time_left}");
                    Debug.Log($"EVA2 Battery Time Left: {response.lowFrequency.eva2_batt_time_left}");
                }
                else
                {
                    Debug.LogWarning("No low-frequency data received in this response.");
                    displayString += "No low-frequency data available.\n";
                }
            }
            else
            {
                Debug.LogError("Failed to parse TSS data or success flag was false.");
                displayString = "Failed to parse TSS data.";
            }
            
            // Update the UI Text component
            if (displayText != null)
            {
                displayText.text = displayString;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing JSON: {e.Message}\nJSON: {jsonData}");
            if (displayText != null)
            {
                displayText.text = "Error parsing data from server.";
            }
        }
    }
} 