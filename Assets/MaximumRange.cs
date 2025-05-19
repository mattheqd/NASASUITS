//* Referenced from research paper: https://ntrs.nasa.gov/citations/20190031945
//* 0.5 meters/second is the planning range for mapping the terrain
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using Thresholds;

public class MaxRangeCalculator : MonoBehaviour
{
    //*--------- Variables -----------
    // UI elements
    [SerializeField] private Text rangeDisplayText; // Text to display the maximum range
    [SerializeField] private Image rangeColorIndicator; // White = safe, Orange = caution, Red = critical
    
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
    [SerializeField] private float updateInterval = 5.0f; // How often to update calculations

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
    }
    // update the maximum range every updateInterval seconds
    void Update()
    {
        timeToNextUpdate -= Time.deltaTime;
        if (timeToNextUpdate <= 0) {
            CalculateMaxRange();
            timeToNextUpdate = updateInterval;
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
        // get latest telemetry data from WS client
        BiometricsData eva1Bio = WebSocketClient.LatestEva1BiometricsData;
        HighFrequencyData highFreqData = WebSocketClient.LatestHighFrequencyData;

        // track limiting factors
        float timeRemaining = float.MaxValue;
        limitingFactor = "";

        if (eva1Bio == null || highFreqData == null) {
            return;
        }

        // 1. check oxygen time remaining
        SingleEvaTelemetryData evaTelemetry = WebSocketClient.LatestEva1TelemetryData;
        float oxygenTimeRemaining = evaTelemetry != null ? evaTelemetry.o2TimeLeft : 0;
        if (oxygenTimeRemaining > 0 && oxygenTimeRemaining < timeRemaining) {
            timeRemaining = oxygenTimeRemaining;
            limitingFactor = "Oxygen";
        }

        // 2. Battery time remaining
        float batteryTimeRemaining = evaTelemetry != null ? evaTelemetry.batteryTimeLeft : 0;
        if (batteryTimeRemaining > 0 && batteryTimeRemaining < timeRemaining)
        {
            timeRemaining = batteryTimeRemaining;
            limitingFactor = "Battery";
        }

        // 3. CO2 scrubber capacity remaining (calculated from CO2 production rate if available)
        if (eva1Bio != null && eva1Bio.co2Production > 0)
        {
            // Estimate CO2 scrubber capacity based on CO2 production rate
            // Typical scrubber can handle about 8 hours of CO2 at normal production rates
            float scrubberACapacity = evaTelemetry.scrubberAPressure;
            float scrubberBCapacity = evaTelemetry.scrubberBPressure;
            
            // Get the total remaining capacity across both scrubbers
            float totalScrubberCapacity = scrubberACapacity + scrubberBCapacity;
            
            // If we have scrubber data, estimate time based on current CO2 production rate
            if (totalScrubberCapacity > 0)
            {
                // Calculate remaining time based on current CO2 production rate (simplified model)
                // Assuming a full scrubber can handle 8 hours of CO2 at 0.1 units/min production rate
                float normalCO2Rate = 0.1f; // baseline CO2 production rate
                float normalScrubberCapacity = 100f; // baseline full capacity
                float scrubberTimeRemaining = (totalScrubberCapacity / normalScrubberCapacity) * 
                                             (normalCO2Rate / eva1Bio.co2Production) * 
                                             8 * 3600; // convert to seconds
                
                if (scrubberTimeRemaining > 0 && scrubberTimeRemaining < timeRemaining)
                {
                    timeRemaining = scrubberTimeRemaining;
                    limitingFactor = "CO2 Scrubber";
                }
            }
        }
        
        // 4. Coolant level (if critically low)
        if (evaTelemetry.coolantLevel < 20f)  // Assuming 20% is critically low
        {
            // Estimate remaining time based on coolant level (simplified model)
            // Assume coolant depletes linearly over an 8 hour period
            float coolantTimeRemaining = (evaTelemetry.coolantLevel / 100f) * 8 * 3600; // convert to seconds
            
            if (coolantTimeRemaining > 0 && coolantTimeRemaining < timeRemaining)
            {
                timeRemaining = coolantTimeRemaining;
                limitingFactor = "Coolant";
            }
        }
        
        // Apply safety margin
        float safeTimeRemaining = timeRemaining * (1.0f - safetyMarginPercent / 100f);
        
        // Convert time to distance (out and back)
        // We divide by 2 because astronaut must return to base (round trip)
        float oneWayTimeRemaining = safeTimeRemaining / 2.0f;
        float maxDistance = oneWayTimeRemaining * nominalWalkingSpeed;
        
        // Update the current max range
        currentMaxRange = maxDistance;
        
        // Debug log the calculation
        Debug.Log($"Max Range Calculation: {limitingFactor} limited to {FormatTimeRemaining(timeRemaining)} → " +
                  $"{FormatTimeRemaining(safeTimeRemaining)} (with safety margin) → " +
                  $"{FormatTimeRemaining(oneWayTimeRemaining)} one-way → " +
                  $"{currentMaxRange:F0}m");
        
        // Format distance nicely
        string distanceStr = FormatDistance(currentMaxRange);
        
        // Update text with calculated values
        if (rangeDisplayText != null)
        {
            rangeDisplayText.text = $"Max Range: {distanceStr}\nLimited by: {limitingFactor}";
            
            // Set warning colors based on range
            if (currentMaxRange <= criticalThresholdMeters)
            {
                rangeDisplayText.color = Color.red;
                // if (rangeWarningIcon != null) rangeWarningIcon.color = Color.red;
            }
            else if (currentMaxRange <= cautionThresholdMeters)
            {
                rangeDisplayText.color = Color.yellow;
                // if (rangeWarningIcon != null) rangeWarningIcon.color = Color.yellow;
            }
            else
            {
                rangeDisplayText.color = Color.green;
                // if (rangeWarningIcon != null) rangeWarningIcon.color = Color.green;
            }
        }
    }
    
    // Helper method to format time remaining in a readable format
    private string FormatTimeRemaining(float seconds)
    {
        TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
        return string.Format("{0:D2}:{1:D2}:{2:D2}", 
            timeSpan.Hours, 
            timeSpan.Minutes, 
            timeSpan.Seconds);
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
        BiometricsData eva1Bio = WebSocketClient.LatestEva1BiometricsData;
        
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
        if (eva1Bio != null)
        {
            report += $"\nCurrent Rates:\n";
            report += $"O2 Consumption: {eva1Bio.o2Consumption:F2} L/min\n";
            report += $"CO2 Production: {eva1Bio.co2Production:F2} L/min\n";
        }
        
        return report;
    }
}