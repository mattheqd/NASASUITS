using System;
using System.Collections.Generic;
using UnityEngine;
using NativeWebSocket;
using System.Text;
using System.Threading.Tasks;

// Message sent to the server
[Serializable]
public class WsMessage{
    public string type; // type of the message (ex: string)    
    public object data; // data that will be sent to the server to process
    public bool success;
}

public class WebSocketClient : MonoBehaviour
{
    private WebSocket websocket;
    // create a dictionary to store all message handlers to be called when a message is received
    private Dictionary<string, List<Action<object>>> messageHandlers = new Dictionary<string, List<Action<object>>>();
    private string serverUrl = "ws://localhost:3000/ws"; // the url used to connect to the server
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
        ConectToServer();
    }
    private async void ConnectToServer() {
        // create a new websocket connection
        websocket = new WebSocket(serverUrl);

        // ---- function definitions for events ----
        // += adds the function (ex: () => { isConnected = true; }) to the event (ex: OnOpen)
        websocket.OnOpen += () => { isConnected = true; };
        websocket.OnMessage += (bytes) => {
            string message = Encoding.UTF8.GetString(bytes);
            WsMessage wsMessage = JsonUtility.FromJson<WsMessage>(message);
            HandleMessage(wsMessage);
        };
        websocket.OnClose += () => { isConnected = false; };
        websocket.OnError += (error) => { Debug.LogError("WebSocket Error: " + error); };
        // connect to the server after the websocket is initialized
        await websocket.Connect(); 
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
        foreach (var handle in handlers) {
            handle(message.data);
        }
    }
    public async void Send(string type, object data)
    {
        VerifyConnection();
        // create a new message to send to the server 
        // ex: { type: "string", data: "hello", success: true }
        WsMessage message = new WsMessage
        {
            type = type,
            data = data,
            success = true
        };
        
        string json = JsonUtility.ToJson(message); 
        byte[] bytes = Encoding.UTF8.GetBytes(json); // ws processes messages as an array of bytes
        await websocket.Send(bytes);
    }

    // remove ws connection if the unity application quits
    private async void OnApplicationQuit()
    {
        if (websocket != null && isConnected)
            await websocket.Close();
    }

    private void VerifyConnection() {
        if (!isConnected) {
            Debug.LogWarning("WebSocket is not connected");
            ConnectToServer();
        } 
    }
}
