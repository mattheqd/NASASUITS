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
                HandleRawMessage(message);
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
        if (message == null || string.IsNullOrEmpty(message.type))
        {
            return;
        }

        switch (message.type)
        {
            case "rock_data":
                HandleRockDataObject(message);
                break;
            case "high-frequency":
                HandleHighFrequencyData(message.data);
                break;
            case "low-frequency":
                Debug.Log("[DEBUG] Received low-frequency data");
                HandleLowFrequencyData(message.data);
                break;
            default:
                if (messageHandlers.ContainsKey(message.type)) {
                    foreach (var handler in messageHandlers[message.type]) {
                        try
                        {
                            handler(message.data);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"Error in message handler for type {message.type}: {ex.Message}");
                        }
                    }
                }
                break;
        }
    }

    private void HandleRockDataObject(WsMessage message)
    {
        try
        {
            string rawJson = JsonUtility.ToJson(message);
            Debug.Log($"[ROCK DATA OBJ] Raw message JSON: {rawJson}");
            WsRockDataMessage rockMsg = JsonUtility.FromJson<WsRockDataMessage>(rawJson);
            if (rockMsg == null || rockMsg.data == null)
            {
                Debug.LogError("[ROCK DATA OBJ] Failed to parse rock data object");
                return;
            }
            Debug.Log($"[ROCK DATA OBJ] Parsed: EVA {rockMsg.data.evaId}, SPEC {rockMsg.data.specId}");
            if (messageHandlers.ContainsKey("rock_data"))
            {
                foreach (var handler in messageHandlers["rock_data"])
                {
                    try
                    {
                        handler(rockMsg.data);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error in rock data handler: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error handling rock data object: {ex.Message}");
        }
    }

    private void HandleHighFrequencyData(object data)
    {
        try
        {
            if (data == null)
            {
                return;
            }

            string jsonData = data.ToString();
            HighFrequencyData hfData = JsonUtility.FromJson<HighFrequencyData>(jsonData);
            if (hfData == null || hfData.data == null)
            {
                return;
            }

            if (messageHandlers.ContainsKey("high-frequency"))
            {
                foreach (var handler in messageHandlers["high-frequency"])
                {
                    try
                    {
                        handler(data);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error in high frequency data handler: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error handling high frequency data: {ex.Message}");
        }
    }

    private void HandleLowFrequencyData(object data)
    {
        Debug.Log("[DEBUG] HandleLowFrequencyData called");
        try
        {
            Debug.Log($"[LOW FREQ] Raw: {data}");
            if (data == null)
            {
                return;
            }

            string jsonData = data.ToString();
            Debug.Log($"[LOW FREQ] JSON: {jsonData}");
            LowFrequencyData lfData = JsonUtility.FromJson<LowFrequencyData>(jsonData);
            if (lfData == null || lfData.data == null)
            {
                Debug.Log("[LOW FREQ] No data in this update");
                return;
            }
            Debug.Log($"[DEBUG] lfData.data type: {lfData.data.GetType()} count: {lfData.data.Count}");
            foreach (var kvp in lfData.data)
            {
                Debug.Log($"[DEBUG] lfData.data key: {kvp.Key} value: {kvp.Value}");
            }

            RockData rockData = new RockData
            {
                evaId = 1,
                specId = lfData.data.ContainsKey("eva1_spec_id") ? (int)lfData.data["eva1_spec_id"] : 0,
                oxygen = lfData.data.ContainsKey("eva1_spec_oxy") ? lfData.data["eva1_spec_oxy"] : 0,
                water = lfData.data.ContainsKey("eva1_spec_water") ? lfData.data["eva1_spec_water"] : 0,
                co2 = lfData.data.ContainsKey("eva1_spec_co2") ? lfData.data["eva1_spec_co2"] : 0,
                h2 = lfData.data.ContainsKey("eva1_spec_h2") ? lfData.data["eva1_spec_h2"] : 0,
                n2 = lfData.data.ContainsKey("eva1_spec_n2") ? lfData.data["eva1_spec_n2"] : 0,
                other = lfData.data.ContainsKey("eva1_spec_other") ? lfData.data["eva1_spec_other"] : 0,
                temperature = lfData.data.ContainsKey("eva1_spec_temp") ? lfData.data["eva1_spec_temp"] : 0,
                pressure = lfData.data.ContainsKey("eva1_spec_pres") ? lfData.data["eva1_spec_pres"] : 0,
                humidity = lfData.data.ContainsKey("eva1_spec_humid") ? lfData.data["eva1_spec_humid"] : 0,
                light = lfData.data.ContainsKey("eva1_spec_light") ? lfData.data["eva1_spec_light"] : 0
            };

            Debug.Log($"[DEBUG] Forwarding RockData from low-frequency to rock_data handler. Handler count: {(messageHandlers.ContainsKey("rock_data") ? messageHandlers["rock_data"].Count : 0)}");
            if (messageHandlers.ContainsKey("rock_data"))
            {
                foreach (var handler in messageHandlers["rock_data"])
                {
                    try
                    {
                        Debug.Log("[DEBUG] Calling rock_data handler");
                        handler(rockData);
                        Debug.Log("[DEBUG] Called rock_data handler");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error in rock data handler (from low-freq): {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error handling low frequency data: {ex.Message}");
        }
    }

    private void HandleRawMessage(string message)
    {
        Debug.Log($"[WS RAW] {message}");
        try
        {
            WsRockDataMessage wsRockDataMsg = JsonUtility.FromJson<WsRockDataMessage>(message);
            if (wsRockDataMsg != null && wsRockDataMsg.type == "rock_data" && wsRockDataMsg.data != null)
            {
                Debug.Log($"[ROCK DATA RAW] {message}");
                Debug.Log($"[ROCK DATA OBJ] Parsed: EVA {wsRockDataMsg.data.evaId}, SPEC {wsRockDataMsg.data.specId}");
                if (messageHandlers.ContainsKey("rock_data"))
                {
                    foreach (var handler in messageHandlers["rock_data"])
                    {
                        try
                        {
                            handler(wsRockDataMsg.data);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"Error in rock data handler: {ex.Message}");
                        }
                    }
                }
            }
            else
            {
                // fallback to old handler for other types
                WsMessage wsMessage = JsonUtility.FromJson<WsMessage>(message);
                HandleMessage(wsMessage);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error handling raw message: {ex.Message}");
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
