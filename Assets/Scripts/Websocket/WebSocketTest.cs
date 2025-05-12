using UnityEngine;
using System;

public class WebSocketTest : MonoBehaviour
{
    void Start()
    {
        if (WebSocketClient.Instance == null)
        {
            Debug.LogError("WebSocketClient not found in scene. Creating one...");
            GameObject wsClient = new GameObject("WebSocketClient");
            wsClient.AddComponent<WebSocketClient>();
        }

        Debug.Log("Setting up WebSocket subscriptions...");
        WebSocketClient.Instance.Subscribe("rock_data", HandleRockData);
        WebSocketClient.Instance.Subscribe("test", HandleTestMessage);
        Debug.Log("WebSocket subscriptions complete");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SendTestMessage();
        }
    }

    private void HandleRockData(object data)
    {
        try
        {
            RockData rockData = JsonUtility.FromJson<RockData>(data.ToString());
            Debug.Log($"Received Rock Data - EVA ID: {rockData.evaId}, Temperature: {rockData.temperature}, Pressure: {rockData.pressure}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error handling rock data: {e.Message}");
        }
    }

    private void HandleTestMessage(object data)
    {
        Debug.Log($"Received test message: {data}");
    }

    private void SendTestMessage()
    {
        if (WebSocketClient.Instance == null)
        {
            Debug.LogError("WebSocketClient is not available!");
            return;
        }

        var testData = new { message = "Hello from Unity!" };
        WebSocketClient.Instance.Send("test", testData);
        Debug.Log("Sent test message to server");
    }
} 