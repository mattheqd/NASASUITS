//* Referenced from research paper: https://ntrs.nasa.gov/citations/20190031945
//* 0.5 meters/second is the planning range for mapping the terrain
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using Thresholds;
using TMPro;

public class MaxRangeCalculator : MonoBehaviour
{
    //*--------- Variables -----------
    // UI elements
    [SerializeField] private TextMeshProUGUI RangeDisplayText; // Text to display the maximum range
    
    // Configuration parameters
    [Header("Astronaut Speed Settings")]
    [SerializeField] private float nominalWalkingSpeed = 0.5f; // m/s (average of 0.45-0.55 meters/second)
    [SerializeField] private float nominalRunningSpeed = 0.7f; // m/s (upper safe range on flat terrain, up to 0.7 meters)
    [SerializeField] private float difficulTerrainSpeed = 0.3f; // m/s (lower range with inclines and obstacles)
    
    [Header("Safety Margins")]
    [SerializeField] private float safetyMarginPercent = 20f; // Safety buffer percentage
    [SerializeField] private float cautionThresholdMeters = 500f; // Distance at which to show caution
    [SerializeField] private float criticalThresholdMeters = 200f; // Distance at which to show critical warning
    
    [Header("Refresh Rate")]
    [SerializeField] private float updateInterval = 1.0f; // How often to update calculations

    // Private variables to track
    private float timeToNextUpdate = 0f; // Time until next update based on updateInterval
    private float currentMaxRange = 0f; // Current maximum range based on the limiting factor
    private string limitingFactor = ""; // The main limiting factor (oxygen, CO2, etc.)

    // Reference to telemetry monitor for threshold information
    private TelemetryMonitor telemetryMonitor;

    //*--------- Unity Functions -----------
    void Start()
    {
        // reference  to telemetry monitor
        telemetryMonitor = FindObjectOfType<TelemetryMonitor>();
        CalculateMaxRange();
        timeToNextUpdate = updateInterval; // Set the timer for the next update
        
        // Set default text if no data available
        if (RangeDisplayText != null && string.IsNullOrEmpty(RangeDisplayText.text))
        {
            RangeDisplayText.text = "Initializing telemetry...";
            RangeDisplayText.color = Color.white;
        }
        
        Debug.Log("MaxRangeCalculator initialized. First calculation performed.");
    }
    // update the maximum range every updateInterval seconds
    void Update()
    {
        timeToNextUpdate -= Time.deltaTime;
        if (timeToNextUpdate <= 0)
        {
            CalculateMaxRange();
            timeToNextUpdate = updateInterval; // Reset timer for the next interval
        }
    }

    //*--------- Calculation Functions -----------
    // calculate based on:
    // - O2 time left (o2TimeLeft) and battery time left (batteryTimeLeft) on the EMU
    // - O2 consumption rate (o2ConsumptionRate) and CO2 production rate (co2Production) on the EMU
    // - O2 consumption rate (o2ConsumptionRate) and CO2 production rate (co2Production) on the EVAs
    // - The depletion rate of other consumable data (e.g. coolant, battery, etc.) listed in the websocket
    void CalculateMaxRange()
    {
        Debug.Log("Calculating max range...");
        
        // Get telemetry data directly from WebSocketClient
        SingleEvaTelemetryData evaTelemetry = WebSocketClient.LatestEva1TelemetryData;
        // BiometricsData eva1Bio = WebSocketClient.LatestEva1BiometricsData; // Uncomment if used
        // HighFrequencyData highFreqData = WebSocketClient.LatestHighFrequencyData; // Uncomment if used
        
        // Log data availability for debugging
        Debug.Log($"EVA Telemetry: {(evaTelemetry != null ? "Available" : "NULL")}");
        // Debug.Log($"EVA1 Bio: {(eva1Bio != null ? "Available" : "NULL")}");
        // Debug.Log($"High Freq Data: {(highFreqData != null ? "Available" : "NULL")}");
        
        // Variables to track limiting factors
        float timeRemaining = float.MaxValue;
        limitingFactor = "Unknown";
        
        // Check if telemetry data available
        if (evaTelemetry == null)
        {
            Debug.LogWarning("No EVA telemetry data available for MaxRangeCalculator.");
            if (RangeDisplayText != null)
            {
                RangeDisplayText.text = "Awaiting telemetry data...";
                RangeDisplayText.color = Color.yellow;
            }
            currentMaxRange = 0;
            limitingFactor = "No Data";
            return;
        }
        
        // Extract oxygen time left
        float oxygenTimeRemaining = evaTelemetry.o2TimeLeft;
        Debug.Log($"Oxygen time remaining: {oxygenTimeRemaining} seconds");
        
        // Extract battery time left
        float batteryTimeRemaining = evaTelemetry.batteryTimeLeft;
        Debug.Log($"Battery time remaining: {batteryTimeRemaining} seconds");
        
        // Find the limiting factor (the resource that will run out first)
        if (oxygenTimeRemaining <= 0 && batteryTimeRemaining <= 0) {
            timeRemaining = 0;
            limitingFactor = "All Consumables Depleted";
        } else {
            if (oxygenTimeRemaining > 0 && oxygenTimeRemaining < timeRemaining)
            {
                timeRemaining = oxygenTimeRemaining;
                limitingFactor = "Oxygen";
            }
            
            if (batteryTimeRemaining > 0 && batteryTimeRemaining < timeRemaining)
            {
                timeRemaining = batteryTimeRemaining;
                limitingFactor = "Battery";
            }
             // If one is zero and the other isn't, the non-zero one is the limit if it was previously MaxValue
            else if (limitingFactor == "Unknown") { // If neither was less than MaxValue but one might be >0
                if (oxygenTimeRemaining > 0) {
                    timeRemaining = oxygenTimeRemaining;
                    limitingFactor = "Oxygen";
                } else if (batteryTimeRemaining > 0) {
                    timeRemaining = batteryTimeRemaining;
                    limitingFactor = "Battery";
                } else { // Both are zero or negative
                    timeRemaining = 0;
                    limitingFactor = "Consumables Depleted";
                }
            }
        }

        if (timeRemaining == float.MaxValue) { // Should not happen if data is valid and >0
            timeRemaining = 0;
            limitingFactor = "Data Error";
        }
        
        // Calculate one-way time (half of total time with safety margin)
        float safetyFactor = 1.0f - (safetyMarginPercent / 100.0f);
        float oneWayTimeRemaining = (timeRemaining * safetyFactor) / 2.0f;
        
        // Calculate max range based on walking speed
        currentMaxRange = oneWayTimeRemaining * nominalWalkingSpeed;
        
        // Format the time and distance for display
        string timeStr = FormatTimeRemaining(timeRemaining);
        string distanceStr = FormatDistance(currentMaxRange);
        
        Debug.Log($"Calculated max range: {distanceStr} limited by {limitingFactor}");
        
        // Update UI
        if (RangeDisplayText != null)
        {
            // Set the range text with detailed information
            RangeDisplayText.text = $"Max Range: {distanceStr}\n" +
                                   $"Limited by: {limitingFactor}\n" +
                                   $"O2: {FormatTimeRemaining(oxygenTimeRemaining)} | Batt: {FormatTimeRemaining(batteryTimeRemaining)}";
            
            // Color coding based on danger level
            if (currentMaxRange <= criticalThresholdMeters)
            {
                RangeDisplayText.color = Color.red;
            }
            else if (currentMaxRange <= cautionThresholdMeters)
            {
                RangeDisplayText.color = Color.yellow;
            }
            else
            {
                RangeDisplayText.color = Color.white;
            }
            
            Debug.Log($"UI updated with text: {RangeDisplayText.text}");
        }
        else
        {
            Debug.LogError("RangeDisplayText is null! Make sure it's assigned in the Inspector.");
        }
    }
    
    // Helper method to format time remaining in a readable format
    private string FormatTimeRemaining(float seconds)
    {
        if (seconds < 0) seconds = 0;
        TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
        return $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
    }
    
    // Helper method to format distance in a readable format
    private string FormatDistance(float meters)
    {
        if (meters >= 1000)
        {
            return $"{meters/1000:F1} km";
        }
        return $"{meters:F0} m";
    }
    
    //* ----- Public get methods -------
    // Public method to get current max range for other systems
    public float GetCurrentMaxRange()
    {
        return currentMaxRange;
    }
    
    // Public method to get current limiting factor
    public string GetLimitingFactor()
    {
        return limitingFactor;
    }
    
    // Public method to provide a detailed report of all consumables
    public string GetDetailedConsumablesReport()
    {
        SingleEvaTelemetryData evaTelemetry = WebSocketClient.LatestEva1TelemetryData;
        // BiometricsData eva1Bio = WebSocketClient.LatestEva1BiometricsData; // Uncomment if used
        
        if (evaTelemetry == null)
            return "No telemetry data available";
            
        string report = "EVA Consumables Status:\n";
        
        // Format oxygen time
        TimeSpan oxygenTime = TimeSpan.FromSeconds(evaTelemetry.o2TimeLeft);
        report += $"Oxygen: {oxygenTime.Hours:D2}:{oxygenTime.Minutes:D2} remaining\n";
        
        // Format battery time
        TimeSpan batteryTime = TimeSpan.FromSeconds(evaTelemetry.batteryTimeLeft);
        report += $"Battery: {batteryTime.Hours:D2}:{batteryTime.Minutes:D2} remaining\n";
        
        // Add scrubber status
        report += $"CO2 Scrubber A: {evaTelemetry.scrubberAPressure:F0}%\n";
        report += $"CO2 Scrubber B: {evaTelemetry.scrubberBPressure:F0}%\n";
        
        // Add coolant level
        report += $"Coolant: {evaTelemetry.coolantLevel:F0}%\n";
        
        // Add current consumption rates if biometrics data is available
        // if (eva1Bio != null)
        // {
        //     report += $"\nCurrent Rates:\n";
        //     report += $"O2 Consumption: {eva1Bio.o2Consumption:F2} L/min\n";
        //     report += $"CO2 Production: {eva1Bio.co2Production:F2} L/min\n";
        // }
        
        return report;
    }
}