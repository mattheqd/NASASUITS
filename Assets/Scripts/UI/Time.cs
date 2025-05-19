using UnityEngine;
using UnityEngine.UI; // Required for Image component
using TMPro; // Required for TextMeshProUGUI components

public class Time : MonoBehaviour
{
    public TextMeshProUGUI timeText;

    private void Update()
    {
        if (timeText != null)
        {
            timeText.text = WebSocketClient.LatestEva1TelemetryData.timestamp.ToString();
        }
    }
    
}