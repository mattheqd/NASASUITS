using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Threading.Tasks;

public class WebCamController : MonoBehaviour
{
    [SerializeField]
    private RawImage displayImage;
    
    [SerializeField]
    private TextMeshProUGUI debugText;

    private Texture2D cameraTexture;
    private int cameraWidth;
    private int cameraHeight;

    private void LogToScreen(string message)
    {
        if (debugText != null)
        {
            debugText.text += message + "\n";
            // Keep only the last 5 messages to avoid text overflow
            string[] messages = debugText.text.Split('\n');
            if (messages.Length > 5)
            {
                debugText.text = string.Join("\n", messages, messages.Length - 5, 5);
            }
        }
        // Also log to Unity console for debugging
        Debug.Log(message);
    }

    async void Start()
    {
        if (debugText != null)
        {
            debugText.text = ""; // Clear previous logs
        }

        // Check if device has camera
        if (!NativeCamera.DeviceHasCamera())
        {
            LogToScreen("No camera found on device!");
            return;
        }

        // Request camera permission
        var permission = await NativeCamera.RequestPermissionAsync(true);
        if (permission != NativeCamera.Permission.Granted)
        {
            LogToScreen("Camera permission denied!");
            return;
        }

        // Set up the display image
        if (displayImage != null)
        {
            // Get the actual size of the RawImage
            cameraWidth = Mathf.RoundToInt(displayImage.rectTransform.rect.width);
            cameraHeight = Mathf.RoundToInt(displayImage.rectTransform.rect.height);
            
            // Center the RawImage
            displayImage.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            displayImage.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            displayImage.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            LogToScreen($"RawImage size set to {cameraWidth}x{cameraHeight}");
        }
    }

    // Call this method to take a photo
    public void TakePhoto()
    {
        if (NativeCamera.IsCameraBusy())
            return;

        NativeCamera.TakePicture((path) =>
        {
            if (path != null)
            {
                // Load the captured image with the RawImage's dimensions
                Texture2D texture = NativeCamera.LoadImageAtPath(path, Mathf.Max(cameraWidth, cameraHeight));
                if (texture != null)
                {
                    // Update the display
                    if (cameraTexture != null)
                    {
                        Destroy(cameraTexture);
                    }
                    cameraTexture = texture;
                    displayImage.texture = cameraTexture;
                    LogToScreen($"Photo taken and displayed: {texture.width}x{texture.height}");
                }
                else
                {
                    LogToScreen("Failed to load captured image");
                }
            }
            else
            {
                LogToScreen("Camera capture cancelled");
            }
        }, Mathf.Max(cameraWidth, cameraHeight));
    }

    void OnDestroy()
    {
        if (cameraTexture != null)
        {
            Destroy(cameraTexture);
        }
    }
} 