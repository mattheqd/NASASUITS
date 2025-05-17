using UnityEngine;
using UnityEngine.UI; // Required for Image component
using TMPro; // Required for TextMeshProUGUI components

public class BiometricGauge : MonoBehaviour
{
    [Header("UI Elements (Assign in Inspector)")]
    public Image gaugeImage;            // The UI Image component used for the radial fill
    public TextMeshProUGUI titleText;   // Text for the gauge's title (e.g., "CO2 Pressure")
    public TextMeshProUGUI valueText;   // Text for the numerical value (e.g., "3.42")
    public TextMeshProUGUI unitText;    // Text for the unit (e.g., "PSI")
    public Image iconDisplay;           // Image to display the icon

    [Header("Gauge Behavior")]
    [Tooltip("Is a higher value considered better? (e.g., true for Oxygen Level, false for CO2 Level)")]
    public bool highValueIsGood = true;

    [Header("Thresholds (as fill percentage 0.0 to 1.0)")]
    [Tooltip("Below this percentage, the state is determined based on 'highValueIsGood'. For highValueIsGood=true, this is the upper limit of Critical. For highValueIsGood=false, this is the upper limit of Good.")]
    [Range(0f, 1f)]
    public float lowThreshold = 0.25f; 

    [Tooltip("Above this percentage, the state is determined based on 'highValueIsGood'. For highValueIsGood=true, this is the lower limit of Good. For highValueIsGood=false, this is the lower limit of Critical. The range between low and high threshold is Warning.")]
    [Range(0f, 1f)]
    public float highThreshold = 0.75f;

    [Header("Colors for States")]
    public Color colorGood = Color.green;
    public Color colorWarning = Color.yellow;
    public Color colorCritical = Color.red;

    private float currentDisplayValue;
    private float currentMinValue;
    private float currentMaxValue;

    void Awake()
    {
        if (lowThreshold >= highThreshold)
        {
            Debug.LogWarning($"BiometricGauge ({gameObject.name}): Low threshold ({lowThreshold}) should be less than high threshold ({highThreshold}). Please check Inspector values.");
        }
    }

    public void SetData(string title, Sprite icon, string unit, float value, float minValue, float maxValue)
    {
        if (titleText != null) titleText.text = title;

        if (iconDisplay != null)
        {
            iconDisplay.sprite = icon;
            iconDisplay.enabled = (icon != null); 
        }

        if (unitText != null) unitText.text = unit;

        currentDisplayValue = value;
        currentMinValue = minValue;
        // Ensure maxValue is not less than minValue to prevent division by zero or negative range
        currentMaxValue = Mathf.Max(minValue, maxValue);


        UpdateGaugeVisuals();
    }

    private void UpdateGaugeVisuals()
    {
        float clampedValue = Mathf.Clamp(currentDisplayValue, currentMinValue, currentMaxValue);

        if (valueText != null)
        {
            valueText.text = clampedValue.ToString("F2");
        }

        if (gaugeImage != null)
        {
            float fillPercentage = 0f;
            if (currentMaxValue - currentMinValue > 0) // Avoid division by zero if min equals max
            {
                fillPercentage = (clampedValue - currentMinValue) / (currentMaxValue - currentMinValue);
            }
            else // Handle cases where minValue == maxValue
            {
                // If value is at or above min (which is also max), it's full, otherwise empty.
                // Or, if always non-negative, could just be 1f if currentDisplayValue >= currentMinValue.
                fillPercentage = (clampedValue >= currentMinValue) ? 1f : 0f;
            }
            
            gaugeImage.fillAmount = fillPercentage; // Assumes Image type is "Filled" and Method is "Radial" in Inspector

            if (highValueIsGood)
            {
                // Standard: Low fill is bad, high fill is good
                if (fillPercentage < lowThreshold)
                {
                    gaugeImage.color = colorCritical;
                }
                else if (fillPercentage < highThreshold)
                {
                    gaugeImage.color = colorWarning;
                }
                else
                {
                    gaugeImage.color = colorGood;
                }
            }
            else // Low value is good (e.g., CO2 levels, where higher fill means higher, worse CO2)
            {
                // Inverted: Low fill is good, high fill is bad
                if (fillPercentage > highThreshold) 
                {
                    gaugeImage.color = colorCritical;
                }
                else if (fillPercentage > lowThreshold) 
                {
                    gaugeImage.color = colorWarning;
                }
                else 
                {
                    gaugeImage.color = colorGood;
                }
            }
        }
    }
} 