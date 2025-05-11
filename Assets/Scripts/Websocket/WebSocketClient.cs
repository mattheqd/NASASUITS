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
}

public class WebSocketClient : MonoBehaviour
{
    private WebSocket ws;
    private Dictionary<string, List<Action<object>>> messageHandlers = new Dictionary<string, List<Action<object>>>();
    private string serverUrl = "ws://localhost:3000/ws";
    private bool isConnected = false;

    public static WebSocketClient Instance { get; private set; } // create a single instance of the websocket client

    private void Awake() {
        // destroy past instances
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); 
        ConnectToServer();
    }
    private void ConnectToServer() {
        ws = new WebSocket(serverUrl);
        
        ws.OnOpen += (sender, e) => {
            isConnected = true;
            Debug.Log("WebSocket Connected");
        };

        ws.OnMessage += (sender, e) => {
            string message = e.Data;
            WsMessage wsMessage = JsonUtility.FromJson<WsMessage>(message);
            HandleMessage(wsMessage);
        };

        ws.OnClose += (sender, e) => {
            isConnected = false;
            Debug.Log("WebSocket Closed");
        };

        ws.OnError += (sender, e) => {
            Debug.LogError("WebSocket Error: " + e.Message);
        };

        ws.Connect();
    }
    // subscribe to a message type (i.e. only receive messages of type "string")
    // takes in the type of message and a function to be called every time the message is received
    public void Subscribe(string messageType, Action<object> callback) {
        if (!messageHandlers.ContainsKey(messageType)) { // if there is not currently a list of handlers for this message type
            messageHandlers[messageType] = new List<Action<object>>();
        }
        messageHandlers[messageType].Add(callback); // add the callback to the list of handlers for this message type
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
            Debug.LogWarning("WebSocket is not connected");
            return;
        }

        WsMessage message = new WsMessage {
            type = type,
            data = data,
            success = true
        };
        
        string json = JsonUtility.ToJson(message);
        ws.Send(json);
    }

    // remove ws connection if the unity application quits
    private void OnApplicationQuit()
    {
        if (ws != null && isConnected) {
            ws.Close();
        }
    }
}
