using UnityEngine;
using TMPro;

public class AlertPrefabController : MonoBehaviour
{
    private TextMeshProUGUI alertText;

    void Awake()
    {
        alertText = GetComponentInChildren<TextMeshProUGUI>();
        HideAlert();
    }

    public void SetAlertData(string message, Color color)
    {
        if (alertText == null || string.IsNullOrEmpty(message))
        {
            HideAlert();
            return;
        }

        alertText.text = message;
        alertText.color = color;
        gameObject.SetActive(true);
    }

    public void HideAlert()
    {
        gameObject.SetActive(false);
    }
} 