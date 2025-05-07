//* handles confirmation and saving of the picture/recording
using UnityEngine;
using UnityEngine.UI;

public class ConfirmButtonHandler : MonoBehaviour
{
    [SerializeField] private AudioRecorder audioRecorder;
    [SerializeField] private Button confirmButton;
    
    private void Start()
    {
        if (confirmButton == null)
        {
            confirmButton = GetComponent<Button>();
        }
        
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        }
    }
    
    private void OnDestroy()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(OnConfirmButtonClicked);
        }
    }
    
    private void OnConfirmButtonClicked()
    {
        if (audioRecorder != null)
        {
            audioRecorder.ShowVoiceMemoPanel();
        }
    }
}
