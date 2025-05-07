//* handles the button for the voice memo recorder
using UnityEngine;
using UnityEngine.UI;

public class VoiceMemoButton : MonoBehaviour
{
    [SerializeField] private AudioRecorder audioRecorder;
    
    private void Start()
    {
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(ActivateVoiceMemo);
        }
        else
        {
            Debug.LogError("VoiceMemoButton must be attached to a UI Button component");
        }
        
        if (audioRecorder == null)
        {
            Debug.LogWarning("Audio Recorder is not assigned. Please assign it in the inspector.");
        }
    }
    
    private void OnDestroy()
    {
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveListener(ActivateVoiceMemo);
        }
    }
    
    private void ActivateVoiceMemo()
    {
        if (audioRecorder != null)
        {
            audioRecorder.ShowVoiceMemoPanel();
        }
    }
}