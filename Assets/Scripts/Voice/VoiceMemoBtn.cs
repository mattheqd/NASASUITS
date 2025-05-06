//* Button in voice memo panel that starts/stops recording
using UnityEngine;
using UnityEngine.UI;

public class VoiceMemoBtn : MonoBehaviour {
    [SerializeField] private VoiceMemoRecorder voiceMemoRecorder;

    // Activate voice memo panel when user clicks button
    private void Start() {
        Button button = GetComponent<Button>();
        if (button) { button.onClick.AddListener(ActivateVoiceMemo);}
    }

    // Destroy event listener when button is stopped or user navigates away from panel
    private void OnDestroy() {
        Button button = GetComponent<Button>();
        if (button) { button.onClick.RemoveListener(ActivateVoiceMemo);}
    }

    // when user clicks button, display the voice memo panel screen
    private void ActivateVoiceMemo() {
        if (voiceMemoRecorder) { voiceMemoRecorder.ShowVoiceMemoPanel(); }
    }
}