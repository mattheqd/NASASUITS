using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PictureRecordingToggle : MonoBehaviour
{
    [SerializeField] private Button pictureButton;
    [SerializeField] private RawImage pictureIcon;
    [SerializeField] private Button recordingButton;
    [SerializeField] private RawImage recordingIcon;
    [SerializeField] private TextMeshProUGUI pictureText;
    [SerializeField] private TextMeshProUGUI recordingText;
    [SerializeField] private Color normalColor = new Color(0.1686f, 0.1765f, 0.1843f, 1f);
    [SerializeField] private Color selectedColor = new Color(0.0314f, 0.8667f, 1f, 1f);
    private static readonly Color iconTextSelectedColor = new Color(37f/255f, 37f/255f, 37f/255f, 1f);
    private static readonly Color iconTextNormalColor = Color.white;

    private void Awake()
    {
        pictureButton.onClick.AddListener(() => Select(true));
        recordingButton.onClick.AddListener(() => Select(false));
    }

    private void Select(bool pictureMode)
    {
        pictureButton.image.color       = pictureMode ? selectedColor : normalColor;
        pictureIcon.color               = pictureMode ? iconTextSelectedColor : iconTextNormalColor;
        pictureText.color               = pictureMode ? iconTextSelectedColor : iconTextNormalColor;
        recordingButton.image.color     = pictureMode ? normalColor  : selectedColor;
        recordingIcon.color             = pictureMode ? iconTextNormalColor  : iconTextSelectedColor;
        recordingText.color             = pictureMode ? iconTextNormalColor  : iconTextSelectedColor;
    }
}