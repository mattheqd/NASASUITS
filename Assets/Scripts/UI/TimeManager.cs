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
            if (combined != null && combined.timestamp > 0)
            {
                var dateTime = DateTimeOffset.FromUnixTimeMilliseconds(combined.timestamp).UtcDateTime;
                timeText.text = dateTime.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");
            }
            else
            {
                timeText.text = "No Data";
            }
        }
    }
    
}