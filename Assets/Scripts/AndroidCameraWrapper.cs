using UnityEngine;
using System;

public class AndroidCameraWrapper : MonoBehaviour
{
    private AndroidJavaObject cameraPlugin;
    private bool isInitialized = false;

    void Start()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            cameraPlugin = new AndroidJavaObject("com.yourcompany.androidcamera.AndroidCameraPlugin");
            isInitialized = true;
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to initialize Android camera plugin: " + e.Message);
        }
        #endif
    }

    public void StartCamera()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        if (isInitialized && cameraPlugin != null)
        {
            try
            {
                cameraPlugin.Call("startCamera");
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to start camera: " + e.Message);
            }
        }
        #endif
    }

    public void StopCamera()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        if (isInitialized && cameraPlugin != null)
        {
            try
            {
                cameraPlugin.Call("stopCamera");
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to stop camera: " + e.Message);
            }
        }
        #endif
    }

    public bool IsCameraRunning()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        if (isInitialized && cameraPlugin != null)
        {
            try
            {
                return cameraPlugin.Call<bool>("isCameraRunning");
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to check camera status: " + e.Message);
            }
        }
        #endif
        return false;
    }

    void OnDestroy()
    {
        StopCamera();
        if (cameraPlugin != null)
        {
            cameraPlugin.Dispose();
            cameraPlugin = null;
        }
    }
} 