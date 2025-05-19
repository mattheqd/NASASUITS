using UnityEngine;
using UnityEngine.UI; // Required for Image component
using TMPro; // Required for TextMeshProUGUI components
using System;

public class TimeManager : MonoBehaviour
{
    public TextMeshProUGUI timeText;

    private void Update()
    {
        if (timeText != null)
        {
            var combined = WebSocketClient.LatestCombinedEvaTelemetryData;
            if (combined != null && combined.evaTime > 0)
            {
                int totalSeconds = combined.evaTime;
                int hours = totalSeconds / 3600;
                int minutes = (totalSeconds % 3600) / 60;
                int seconds = totalSeconds % 60;
                timeText.text = $"EVA Time: {hours:D2}:{minutes:D2}:{seconds:D2}";
            }
            else
            {
                timeText.text = "No Data";
            }
        }
    }
    
}