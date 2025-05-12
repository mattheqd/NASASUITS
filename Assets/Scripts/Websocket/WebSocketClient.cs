using System;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using System.Text;

// Message sent to the server
[Serializable]
public class WsMessage{
    public string type; // type of the message (ex: string)    
    public object data; // data that will be sent to the server to process
    public bool success;
    public WsError error;
}

[Serializable]
public class WsError
{
    public string message;
    public int code;
}

[Serializable]
public class RockData
{
    public int evaId;
    public int specId;
    public float oxygen;
    public float water;
    public float co2;
    public float h2;
    public float n2;
    public float other;
    public float temperature;
    public float pressure;
    public float humidity;
    public float light;
}

[Serializable]
public class TelemetryData
{
    public long timestamp;
    public string type;
    public Dictionary<string, float> data;
}

[Serializable]
public class HighFrequencyData
{
    public long timestamp;
    public Dictionary<string, float> data;
}

[Serializable]
public class LowFrequencyData
{
    public long timestamp;
    public Dictionary<string, float> data;
}

[Serializable]
public class WsRockDataMessage
{
    public string type;
    public RockData data;
    public bool success;
    public WsError error;
}

public class WebSocketClient : MonoBehaviour
{
    private WebSocket ws;
    private Dictionary<string, List<Action<object>>> messageHandlers = new Dictionary<string, List<Action<object>>>();
    private string serverUrl = "ws://localhost:3000/ws";
    private bool isConnected = false;
    private float reconnectDelay = 5f;
    private float lastReconnectAttempt = 0f;
    private bool isReconnecting = false;

    public static WebSocketClient Instance { get; private set; } // create a single instance of the websocket client

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); 
        ConnectToServer();
        ConnectToServer();
    }

    private void Update()
    {
        if (!isConnected && !isReconnecting && Time.time - lastReconnectAttempt > reconnectDelay)
        {
            Debug.Log("Attempting to reconnect to WebSocket server...");
            ConnectToServer();
        }
    }

    private void ConnectToServer() {
        if (ws != null)
        {
            ws.Close();
            ws = null;
        }

        isReconnecting = true;
        lastReconnectAttempt = Time.time;
        
        try
        {
            ws = new WebSocket(serverUrl);
            
            ws.OnOpen += (sender, e) => {
                isConnected = true;
                isReconnecting = false;
                Debug.Log("WebSocket Connected");
            };

            ws.OnMessage += (sender, e) => {
                string message = e.Data;
                Debug.Log($"[WS RAW] {message}");
                WsMessage wsMessage = JsonUtility.FromJson<WsMessage>(message);
                HandleMessage(wsMessage);
            };

            ws.OnClose += (sender, e) => {
                isConnected = false;
                isReconnecting = false;
                Debug.Log($"WebSocket Closed: {e.Reason}");
            };

            ws.OnError += (sender, e) => {
                Debug.LogError($"WebSocket Error: {e.Message}");
            };

            ws.Connect();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error connecting to WebSocket: {ex.Message}");
            isReconnecting = false;
        }
    }

    // subscribe to a message type (i.e. only receive messages of type "string")
    // takes in the type of message and a function to be called every time the message is received
    public void Subscribe(string messageType, Action<object> callback) {
        if (!messageHandlers.ContainsKey(messageType)) {
            messageHandlers[messageType] = new List<Action<object>>();
        }
        messageHandlers[messageType].Add(callback);
    }

    public void Unsubscribe(string messageType, Action<object> callback) {
        if (messageHandlers.ContainsKey(messageType)) {
            messageHandlers[messageType].Remove(callback);
            Debug.Log($"Unsubscribed from message type: {messageType}");
        }
    }

    // ---- Handle Message Communication ----
    // processes the message received from the server
    private void HandleMessage(WsMessage message) {
        if (messageHandlers.ContainsKey(message.type)) {
            foreach (var handler in messageHandlers[message.type]) {
                handler(message.data);
            }
        }
    }
    public void Send(string type, object data)
    {
        if (!isConnected) {
            Debug.LogWarning("WebSocket is not connected. Message not sent.");
            return;
        }

        try
        {
            WsMessage message = new WsMessage {
                type = type,
                data = data,
                success = true
            };
            
            string json = JsonUtility.ToJson(message);
            ws.Send(json);
            Debug.Log($"Sent message of type: {type}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error sending message: {ex.Message}");
        }
    }

    // remove ws connection if the unity application quits
    private void OnApplicationQuit()
    {
        if (ws != null && isConnected) {
            ws.Close();
        }
    }

    private void OnDestroy()
    {
        if (ws != null) {
            ws.Close();
        }
    }
}
