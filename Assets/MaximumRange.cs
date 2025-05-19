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
}