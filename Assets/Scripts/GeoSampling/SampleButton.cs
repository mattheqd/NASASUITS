using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SampleButton : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI buttonText;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    public void SetText(string text)
    {
        if (buttonText != null)
        {
            buttonText.text = text;
        }
    }
} 