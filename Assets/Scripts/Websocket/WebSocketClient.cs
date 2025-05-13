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
    public string type;
    public float eva1_spec_id;
    public float eva1_spec_oxy;
    public float eva1_spec_water;
    public float eva1_spec_co2;
    public float eva1_spec_h2;
    public float eva1_spec_n2;
    public float eva1_spec_other;
    public float eva1_spec_temp;
    public float eva1_spec_pres;
    public float eva1_spec_humid;
    public float eva1_spec_light;
    // ...add more fields as needed for your use case
}

[Serializable]
public class DcuData
{
    public int evaId;
    public float battery;
    public float oxygen;
    public float comm;
    public float fan;
    public float pump;
    public float co2;
}

[Serializable]
public class UiaData
{
    public float emu1_power;
    public float ev1_supply;
    public float ev1_waste;
    public float ev1_oxygen;
    public float emu2_power;
    public float ev2_supply;
    public float ev2_waste;
    public float ev2_oxygen;
    public float o2_vent;
    public float depress_pump;
}

[Serializable]
public class WsRockDataMessage
{
    public string type;
    public RockData data;
    public bool success;
    public WsError error;
}

[Serializable]
public class WsDcuDataMessage
{
    public string type;
    public DcuData data;
    public bool success;
    public WsError error;
}

[Serializable]
public class WsUiaDataMessage
{
    public string type;
    public UiaData data;
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
        Subscribe("rock_data", HandleRockDataMessage);
        Subscribe("dcu_data", HandleDcuDataMessage);
        Subscribe("uia_data", HandleUiaDataMessage);
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
                
                if (message.Contains("\"type\":\"rock_data\"") || message.Contains("\"type\": \"rock_data\""))
                {
                    WsRockDataMessage wsRockDataMsg = JsonUtility.FromJson<WsRockDataMessage>(message);
                    if (wsRockDataMsg != null && wsRockDataMsg.data != null)
                    {
                        Debug.Log($"[RockData] EVA {wsRockDataMsg.data.evaId}, SPEC {wsRockDataMsg.data.specId}, O2 {wsRockDataMsg.data.oxygen}%, Water {wsRockDataMsg.data.water}%, CO2 {wsRockDataMsg.data.co2}%, H2 {wsRockDataMsg.data.h2}%, N2 {wsRockDataMsg.data.n2}%, Other {wsRockDataMsg.data.other}%, Temp {wsRockDataMsg.data.temperature}°C, Pressure {wsRockDataMsg.data.pressure}Pa, Humidity {wsRockDataMsg.data.humidity}%, Light {wsRockDataMsg.data.light}lux");
                        HandleRockDataMessage(wsRockDataMsg.data);
                    }
                }
                else if (message.Contains("\"type\":\"dcu_data\"") || message.Contains("\"type\": \"dcu_data\""))
                {
                    WsDcuDataMessage wsDcuDataMsg = JsonUtility.FromJson<WsDcuDataMessage>(message);
                    if (wsDcuDataMsg != null && wsDcuDataMsg.data != null)
                    {
                        Debug.Log($"[DcuData] EVA {wsDcuDataMsg.data.evaId}, Battery {wsDcuDataMsg.data.battery}%, Oxygen {wsDcuDataMsg.data.oxygen}%, Comm {wsDcuDataMsg.data.comm}%, Fan {wsDcuDataMsg.data.fan}%, Pump {wsDcuDataMsg.data.pump}%, CO2 {wsDcuDataMsg.data.co2}%");
                        HandleDcuDataMessage(wsDcuDataMsg.data);
                    }
                }
                else if (message.Contains("\"type\":\"uia_data\"") || message.Contains("\"type\": \"uia_data\""))
                {
                    WsUiaDataMessage wsUiaDataMsg = JsonUtility.FromJson<WsUiaDataMessage>(message);
                    if (wsUiaDataMsg != null && wsUiaDataMsg.data != null)
                    {
                        Debug.Log($"[UiaData] EMU1 Power {wsUiaDataMsg.data.emu1_power}%, EV1 Supply {wsUiaDataMsg.data.ev1_supply}%, EV1 Waste {wsUiaDataMsg.data.ev1_waste}%, EV1 Oxygen {wsUiaDataMsg.data.ev1_oxygen}%, EMU2 Power {wsUiaDataMsg.data.emu2_power}%, EV2 Supply {wsUiaDataMsg.data.ev2_supply}%, EV2 Waste {wsUiaDataMsg.data.ev2_waste}%, EV2 Oxygen {wsUiaDataMsg.data.ev2_oxygen}%, O2 Vent {wsUiaDataMsg.data.o2_vent}%, Depress Pump {wsUiaDataMsg.data.depress_pump}%");
                        HandleUiaDataMessage(wsUiaDataMsg.data);
                    }
                }
                else
                {
                    WsMessage wsMessage = JsonUtility.FromJson<WsMessage>(message);
                    HandleMessage(wsMessage);
                }
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

    private void HandleRockDataMessage(object data)
    {
        Debug.Log($"[HandleRockDataMessage] Data: {data}");
        RockData rockData = data as RockData;
        if (rockData != null)
        {
            Debug.Log($"[HandleRockDataMessage] EVA {rockData.evaId}, SPEC {rockData.specId}, O2 {rockData.oxygen}, Water {rockData.water}, CO2 {rockData.co2}, H2 {rockData.h2}, N2 {rockData.n2}, Other {rockData.other}, Temp {rockData.temperature}, Pressure {rockData.pressure}, Humidity {rockData.humidity}, Light {rockData.light}");
        }
        else
        {
            Debug.LogError($"[HandleRockDataMessage] Data is not a RockData object, it is {data?.GetType()}");
        }
        if (RockDataDisplay.Instance != null)
        {
            RockDataDisplay.Instance.HandleRockData(data);
        }
    }

    private void HandleDcuDataMessage(object data)
    {
        Debug.Log($"[HandleDcuDataMessage] Data: {data}");
        DcuData dcuData = data as DcuData;
        if (dcuData != null)
        {
            Debug.Log($"[HandleDcuDataMessage] EVA {dcuData.evaId}, Battery {dcuData.battery}%, Oxygen {dcuData.oxygen}%, Comm {dcuData.comm}%, Fan {dcuData.fan}%, Pump {dcuData.pump}%, CO2 {dcuData.co2}%");
        }
        else
        {
            Debug.LogError($"[HandleDcuDataMessage] Data is not a DcuData object, it is {data?.GetType()}");
        }
        // You can add additional handling here later
    }

    private void HandleUiaDataMessage(object data)
    {
        Debug.Log($"[HandleUiaDataMessage] Data: {data}");
        UiaData uiaData = data as UiaData;
        if (uiaData != null)
        {
            Debug.Log($"[HandleUiaDataMessage] EMU1 Power {uiaData.emu1_power}%, EV1 Supply {uiaData.ev1_supply}%, EV1 Waste {uiaData.ev1_waste}%, EV1 Oxygen {uiaData.ev1_oxygen}%, EMU2 Power {uiaData.emu2_power}%, EV2 Supply {uiaData.ev2_supply}%, EV2 Waste {uiaData.ev2_waste}%, EV2 Oxygen {uiaData.ev2_oxygen}%, O2 Vent {uiaData.o2_vent}%, Depress Pump {uiaData.depress_pump}%");
        }
        else
        {
            Debug.LogError($"[HandleUiaDataMessage] Data is not a UiaData object, it is {data?.GetType()}");
        }
        // You can add additional handling here later
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
