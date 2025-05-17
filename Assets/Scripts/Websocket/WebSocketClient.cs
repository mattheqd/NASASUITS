using System;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using System.Text;
using GeoSampling;

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
    public string name; // Added name
    public RockComposition composition; // Added nested composition
    // Removed: oxygen, water, co2, h2, n2, other (moved to composition), temperature, pressure, humidity, light
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
public class Vector2Data
{
    public float x;
    public float y;
}

[Serializable]
public class ImuData
{
    public int evaId;
    public Vector2Data position;
    public float heading;
}

[Serializable]
public class CombinedImuData
{
    public ImuData eva1;
    public ImuData eva2;
    public long timestamp;
}

[Serializable]
public class BiometricsData
{
    public int evaId;
    public float heartRate;
    public float temperature;
    public float o2Consumption;
    public float co2Production;
    public float suitPressureTotal;
    public float helmetCo2;
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

[Serializable]
public class WsHighFrequencyDataMessage
{
    public string type;
    public HighFrequencyData data;
    public bool success;
    public WsError error;
}

[Serializable]
public class WsImuDataMessage
{
    public string type;
    public CombinedImuData data;
    public bool success;
    public WsError error;
}

[Serializable]
public class WsBiometricsDataMessage
{
    public string type;
    public BiometricsData data;
    public bool success;
    public WsError error;
}

public class WebSocketClient : MonoBehaviour
{
    private WebSocket ws;
    private Dictionary<string, List<Action<object>>> messageHandlers = new Dictionary<string, List<Action<object>>>();
    private string serverUrl = "wss://a7c4-76-78-137-104.ngrok-free.app/ws";
    

    private bool isConnected = false;
    private float reconnectDelay = 5f;
    private float lastReconnectAttempt = 0f;
    private bool isReconnecting = false;
    
    // Rate limiting variables
    private float lastImuProcessTime = 0f;
    private float lastRockDataProcessTime = 0f;
    private float lastDcuDataProcessTime = 0f;
    private float lastUiaDataProcessTime = 0f;
    private float lastHighFreqProcessTime = 0f;
    private float lastBiometricsProcessTime = 0f;
    private const float IMU_RATE_LIMIT = 1.0f; // 1 second rate limit
    private const float ROCK_DATA_RATE_LIMIT = 1.0f; // 1 second rate limit
    private const float DCU_DATA_RATE_LIMIT = 1.0f; // 1 second rate limit
    private const float UIA_DATA_RATE_LIMIT = 1.0f; // 1 second rate limit
    private const float HIGH_FREQ_RATE_LIMIT = 1.0f; // 1 second rate limit
    private const float BIOMETRICS_RATE_LIMIT = 1.0f; // 1 second rate limit
    
    // Queue for thread-safe message handling
    private readonly Queue<string> messageQueue = new Queue<string>();
    private readonly object queueLock = new object();
    
    // Rock data tracking variables
    private static bool hasNewRockData = false;
    private static RockData latestRockData = null;
    private static Dictionary<int, RockData> rockDataByEvaId = new Dictionary<int, RockData>();
    private static Dictionary<int, int> lastSpecIdByEvaId = new Dictionary<int, int>(); // Track last spec ID by EVA ID
    private static Dictionary<int, string> rockDataHashByEvaId = new Dictionary<int, string>(); // Track full data hash by EVA ID
    private static bool initialDataReceivedForDebug = false; // Debug flag to track first data
    private static bool ignoreFirstData = true; // Flag to ignore the first data received
    private static int lastProcessedSpecId = -1; // Track the last processed spec ID

    public static WebSocketClient Instance { get; private set; } // create a single instance of the websocket client

    // Latest data properties that other components can access
    public static UiaData LatestUiaData { get; private set; }
    public static DcuData LatestDcuData { get; private set; }
    
    // Latest high frequency data
    public static HighFrequencyData LatestHighFrequencyData { get; private set; }
    
    // Latest IMU data
    public static CombinedImuData LatestImuData { get; private set; }
    
    // Latest biometrics data for EVA1 and EVA2
    public static BiometricsData LatestEva1BiometricsData { get; private set; }
    public static BiometricsData LatestEva2BiometricsData { get; private set; }

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
        Subscribe("high_frequency", HandleHighFrequencyDataMessage);
        Subscribe("imu_data", HandleImuDataMessage);
        Subscribe("biometrics_data", HandleBiometricsDataMessage);
        ConnectToServer();
    }

    private void Update()
    {
        if (!isConnected && !isReconnecting && Time.time - lastReconnectAttempt > reconnectDelay)
        {
            Debug.Log("Attempting to reconnect to WebSocket server...");
            ConnectToServer();
        }
        
        // Process queued messages on the main thread
        ProcessMessageQueue();
    }
    
    private void ProcessMessageQueue()
    {
        // Process all queued messages in this frame
        int messageCount = 0;
        string message = null;
        
        lock (queueLock)
        {
            messageCount = messageQueue.Count;
        }
        
        for (int i = 0; i < messageCount; i++)
        {
            lock (queueLock)
            {
                if (messageQueue.Count > 0)
                {
                    message = messageQueue.Dequeue();
                }
                else
                {
                    break;
                }
            }
            
            if (message != null)
            {
                ProcessWebSocketMessage(message);
            }
        }
    }
    
    private void ProcessWebSocketMessage(string message)
    {
        try
        {
            // First try to parse as a generic message to get the type
            WsMessage baseMsg = JsonUtility.FromJson<WsMessage>(message);
            
            if (baseMsg != null && !string.IsNullOrEmpty(baseMsg.type))
            {
                switch (baseMsg.type)
                {
                    case "high_frequency":
                        WsHighFrequencyDataMessage highFreqMsg = JsonUtility.FromJson<WsHighFrequencyDataMessage>(message);
                        if (highFreqMsg != null && highFreqMsg.data != null)
                        {
                            Debug.Log($"[HIGH_FREQ] Received high frequency data with timestamp: {highFreqMsg.data.timestamp}");
                            HandleHighFrequencyDataMessage(highFreqMsg.data);
                        }
                        break;
                        
                    case "rock_data":
                        WsRockDataMessage rockDataMsg = JsonUtility.FromJson<WsRockDataMessage>(message);
                        if (rockDataMsg != null && rockDataMsg.data != null)
                        {
                            HandleRockDataMessage(rockDataMsg.data);
                        }
                        break;
                        
                    case "dcu_data":
                        WsDcuDataMessage dcuDataMsg = JsonUtility.FromJson<WsDcuDataMessage>(message);
                        if (dcuDataMsg != null && dcuDataMsg.data != null)
                        {
                            HandleDcuDataMessage(dcuDataMsg.data);
                        }
                        break;
                        
                    case "uia_data":
                        WsUiaDataMessage uiaDataMsg = JsonUtility.FromJson<WsUiaDataMessage>(message);
                        if (uiaDataMsg != null && uiaDataMsg.data != null)
                        {
                            HandleUiaDataMessage(uiaDataMsg.data);
                        }
                        break;
                        
                    case "imu_data":
                        WsImuDataMessage imuDataMsg = JsonUtility.FromJson<WsImuDataMessage>(message);
                        if (imuDataMsg != null && imuDataMsg.data != null)
                        {
                            // Enhanced logging to show full IMU data contents
                            StringBuilder imuDebugBuilder = new StringBuilder();
                            imuDebugBuilder.AppendLine($"[IMU] Received IMU data with timestamp: {imuDataMsg.data.timestamp}");
                            
                            if (imuDataMsg.data.eva1 != null)
                            {
                                imuDebugBuilder.AppendLine($"  EVA1 (ID: {imuDataMsg.data.eva1.evaId}):");
                                imuDebugBuilder.AppendLine($"    Position: ({imuDataMsg.data.eva1.position.x}, {imuDataMsg.data.eva1.position.y})");
                                imuDebugBuilder.AppendLine($"    Heading: {imuDataMsg.data.eva1.heading}°");
                            }
                            else
                            {
                                imuDebugBuilder.AppendLine("  EVA1: null");
                            }
                            
                            if (imuDataMsg.data.eva2 != null)
                            {
                                imuDebugBuilder.AppendLine($"  EVA2 (ID: {imuDataMsg.data.eva2.evaId}):");
                                imuDebugBuilder.AppendLine($"    Position: ({imuDataMsg.data.eva2.position.x}, {imuDataMsg.data.eva2.position.y})");
                                imuDebugBuilder.AppendLine($"    Heading: {imuDataMsg.data.eva2.heading}°");
                            }
                            else
                            {
                                imuDebugBuilder.AppendLine("  EVA2: null");
                            }
                            
                            Debug.Log(imuDebugBuilder.ToString());
                            HandleImuDataMessage(imuDataMsg.data);
                        }
                        break;
                        
                    case "biometrics_data":
                        WsBiometricsDataMessage biometricsDataMsg = JsonUtility.FromJson<WsBiometricsDataMessage>(message);
                        if (biometricsDataMsg != null && biometricsDataMsg.data != null)
                        {
                            HandleBiometricsDataMessage(biometricsDataMsg.data);
                        }
                        break;
                        
                    default:
                        // Handle other message types through the generic handler
                        HandleMessage(baseMsg);
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error parsing WebSocket message: {ex.Message}");
            Debug.LogError($"Message content: {message}");
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
            ws.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
            
            ws.OnOpen += (sender, e) => {
                isConnected = true;
                isReconnecting = false;
                Debug.Log("WebSocket Connected");
            };

            ws.OnMessage += (sender, e) => {
                string message = e.Data;
                
                // Queue the message instead of processing it in this thread
                lock (queueLock)
                {
                    messageQueue.Enqueue(message);
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
        // Rate limit check
        if (Time.time - lastRockDataProcessTime < ROCK_DATA_RATE_LIMIT)
        {
            // Silently skip processing due to rate limit
            return;
        }
        
        lastRockDataProcessTime = Time.time;
        
        RockData rockData = data as RockData;
        if (rockData == null)
        {
            Debug.LogError($"[HandleRockDataMessage] Data is not a RockData object, it is {data?.GetType()}");
            return;
        }
        
        // Store a copy of the data, not a reference
        RockData rockDataCopy = new RockData
        {
            evaId = rockData.evaId,
            specId = rockData.specId,
            name = rockData.name,
            composition = new RockComposition // Ensure composition is deep copied if not null
            {
                SiO2 = rockData.composition?.SiO2 ?? 0f,
                Al2O3 = rockData.composition?.Al2O3 ?? 0f,
                MnO = rockData.composition?.MnO ?? 0f,
                CaO = rockData.composition?.CaO ?? 0f,
                P2O3 = rockData.composition?.P2O3 ?? 0f,
                TiO2 = rockData.composition?.TiO2 ?? 0f,
                FeO = rockData.composition?.FeO ?? 0f,
                MgO = rockData.composition?.MgO ?? 0f,
                K2O = rockData.composition?.K2O ?? 0f,
                Other = rockData.composition?.Other ?? 0f
            }
        };
        
        // Log the first data received
        if (!initialDataReceivedForDebug)
        {
            initialDataReceivedForDebug = true;
            Debug.Log($"[ROCK_DATA] First rock data message received ever: EVA{rockData.evaId} SpecID: {rockData.specId}");
            
            // Store this initial spec ID, but don't flag it as new yet
            lastSpecIdByEvaId[rockData.evaId] = rockData.specId;
            rockDataByEvaId[rockData.evaId] = rockDataCopy;
            latestRockData = rockDataCopy;
            lastProcessedSpecId = rockData.specId;
            
            // If this is the first data, just store it but don't flag as new yet
            if (ignoreFirstData)
            {
                Debug.Log("[ROCK_DATA] First data being stored but not flagged as new");
                
                if (RockDataDisplay.Instance != null)
                {
                    RockDataDisplay.Instance.HandleRockData(data);
                }
                
                return;
            }
        }
        
        // When spec ID changes, reset the "new data" flag state to ensure we detect this change
        if (rockData.specId != lastProcessedSpecId)
        {
            Debug.Log($"[ROCK_RESET] Detected new spec ID: {rockData.specId} (was {lastProcessedSpecId})");
            
            // Reset processed state for this new rock sample
            hasNewRockData = false; // Will be set to true below if this is not the first time we've seen this spec ID
            lastProcessedSpecId = rockData.specId;
        }
        
        // Check if this rock data is uniquely different from what we've seen before
        bool isUniqueData = IsUniqueRockData(rockDataCopy);
        
        // Log rock data with details of the checking process
        if (isUniqueData) 
        {
            Debug.Log($"[ROCK_DATA] NEW unique data for EVA{rockData.evaId} SpecID: {rockData.specId}, Processing at time: {Time.time}");
            
            // Print data values for debugging
            PrintRockDataDebug(rockDataCopy, "NEW DATA");
        }
        else
        {
            Debug.Log($"[ROCK_DATA] Duplicate data for EVA{rockData.evaId} SpecID: {rockData.specId}, Processing at time: {Time.time}");
            
            // Print comparison for debugging
            if (rockDataByEvaId.TryGetValue(rockData.evaId, out RockData previousData))
            {
                PrintDataComparison(previousData, rockDataCopy);
            }
        }
        
        // Always update the latest rock data
        latestRockData = rockDataCopy;
        rockDataByEvaId[rockData.evaId] = rockDataCopy;
        
        // Only set the hasNewRockData flag if this is unique data
        if (isUniqueData)
        {
            hasNewRockData = true;
            
            // When spec ID changes, update all tracking dictionaries
            lastSpecIdByEvaId[rockData.evaId] = rockData.specId;
            
            // Store the hash of the current data (keeping this for compatibility)
            string dataHash = CalculateRockDataHash(rockDataCopy);
            rockDataHashByEvaId[rockData.evaId] = dataHash;
            
            Debug.Log($"[ROCK_NEW] Setting hasNewRockData=true for SpecID: {rockData.specId}");
        }
        else
        {
            // Only update record for this EVA if we've never seen it before
            if (!lastSpecIdByEvaId.ContainsKey(rockData.evaId))
            {
                lastSpecIdByEvaId[rockData.evaId] = rockData.specId;
            }
        }
        
        if (RockDataDisplay.Instance != null)
        {
            RockDataDisplay.Instance.HandleRockData(data);
        }
    }
    
    // Debug helper to print rock data values
    private void PrintRockDataDebug(RockData data, string label)
    {
        if (data == null) return;
        
        Debug.Log($"[{label}] Rock Data for EVA{data.evaId}, SpecID: {data.specId}, Name: {data.name}");
        if (data.composition != null)
        {
            Debug.Log($"  Composition (SiO2: {data.composition.SiO2:F4}, Al2O3: {data.composition.Al2O3:F4}, MnO: {data.composition.MnO:F4}, CaO: {data.composition.CaO:F4}, P2O3: {data.composition.P2O3:F4})");
            Debug.Log($"  (TiO2: {data.composition.TiO2:F4}, FeO: {data.composition.FeO:F4}, MgO: {data.composition.MgO:F4}, K2O: {data.composition.K2O:F4}, Other: {data.composition.Other:F4})");
        }
        else
        {
            Debug.Log("  Composition: null");
        }
    }
    
    // Debug helper to print comparison between two rock data objects
    private void PrintDataComparison(RockData old, RockData current)
    {
        if (old == null || current == null) return;
        
        Debug.Log($"[ROCK_COMPARE] Comparing rock data for EVA{current.evaId}:");
        Debug.Log($"  Spec ID: {old.specId} -> {current.specId}, Changed: {old.specId != current.specId}");
        Debug.Log($"  Name: {old.name} -> {current.name}, Changed: {old.name != current.name}");

        if (old.composition != null && current.composition != null)
        {
            Debug.Log($"  SiO2: {old.composition.SiO2:F4} -> {current.composition.SiO2:F4}, Changed: {Math.Abs(old.composition.SiO2 - current.composition.SiO2) > 0.01f}");
            Debug.Log($"  Al2O3: {old.composition.Al2O3:F4} -> {current.composition.Al2O3:F4}, Changed: {Math.Abs(old.composition.Al2O3 - current.composition.Al2O3) > 0.01f}");
            // ... Add comparisons for other oxides ...
            Debug.Log($"  Other: {old.composition.Other:F4} -> {current.composition.Other:F4}, Changed: {Math.Abs(old.composition.Other - current.composition.Other) > 0.01f}");
        }
        else
        {
            Debug.Log($"  Composition comparison skipped due to null: old={old.composition == null}, current={current.composition == null}");
        }
    }
    
    // Check if rock data is unique based on spec ID
    private bool IsUniqueRockData(RockData rockData)
    {
        if (rockData == null) return false;
        
        // Check if we have seen this EVA's data before
        if (lastSpecIdByEvaId.TryGetValue(rockData.evaId, out int lastSpecId))
        {
            // Simply check if the spec ID has changed
            bool isDifferent = rockData.specId != lastSpecId;
            
            if (isDifferent)
            {
                Debug.Log($"[ROCK_DIFF] Found different SpecID for EVA{rockData.evaId}: {lastSpecId} -> {rockData.specId}");
            }
            else
            {
                Debug.Log($"[ROCK_DIFF] No change in SpecID for EVA{rockData.evaId}: still {rockData.specId}");
            }
            
            return isDifferent;
        }
        
        // First time seeing data for this EVA
        Debug.Log($"[ROCK_DIFF] First data received for EVA{rockData.evaId}, SpecID: {rockData.specId}");
        
        // Never treat first data as new - this should only be flagged when an actual change occurs
        return false;
    }
    
    // Calculate a hash based on all meaningful rock data properties
    private string CalculateRockDataHash(RockData data)
    {
        if (data == null) return string.Empty;
        
        StringBuilder hashBuilder = new StringBuilder();
        hashBuilder.Append($"{data.evaId}:{data.specId}:{data.name}:");

        if (data.composition != null)
        {
            hashBuilder.Append($"{Math.Round(data.composition.SiO2, 3)}:");
            hashBuilder.Append($"{Math.Round(data.composition.Al2O3, 3)}:");
            hashBuilder.Append($"{Math.Round(data.composition.MnO, 3)}:");
            hashBuilder.Append($"{Math.Round(data.composition.CaO, 3)}:");
            hashBuilder.Append($"{Math.Round(data.composition.P2O3, 3)}:");
            hashBuilder.Append($"{Math.Round(data.composition.TiO2, 3)}:");
            hashBuilder.Append($"{Math.Round(data.composition.FeO, 3)}:");
            hashBuilder.Append($"{Math.Round(data.composition.MgO, 3)}:");
            hashBuilder.Append($"{Math.Round(data.composition.K2O, 3)}:");
            hashBuilder.Append($"{Math.Round(data.composition.Other, 3)}");
        }
        return hashBuilder.ToString();
    }

    private void HandleDcuDataMessage(object data)
    {
        // Rate limit check
        if (Time.time - lastDcuDataProcessTime < DCU_DATA_RATE_LIMIT)
        {
            // Silently skip processing due to rate limit
            return;
        }
        
        lastDcuDataProcessTime = Time.time;
        
        DcuData dcuData = data as DcuData;
        if (dcuData == null)
        {
            Debug.LogError($"[DCU] Data is not a DcuData object: {data?.GetType()}");
            return;
        }
        
        // Log DCU data
        Debug.Log($"[DCU_DATA] EVA{dcuData.evaId}, Processing at time: {Time.time}");
        
        // Store the latest DCU data
        LatestDcuData = dcuData;
    }

    private void HandleUiaDataMessage(object data)
    {
        // Rate limit check
        if (Time.time - lastUiaDataProcessTime < UIA_DATA_RATE_LIMIT)
        {
            // Silently skip processing due to rate limit
            return;
        }
        
        lastUiaDataProcessTime = Time.time;
        
        UiaData uiaData = data as UiaData;
        if (uiaData == null)
        {
            Debug.LogError($"[UIA] Data is not a UiaData object: {data?.GetType()}");
            return;
        }
        
        // Log UIA data
        Debug.Log($"[UIA_DATA] Processing at time: {Time.time}");
        
        // Store the latest UIA data
        LatestUiaData = uiaData;
    }
    
    private void HandleHighFrequencyDataMessage(object data)
    {
        // Rate limit check
        if (Time.time - lastHighFreqProcessTime < HIGH_FREQ_RATE_LIMIT)
        {
            // Silently skip processing due to rate limit
            return;
        }
        
        lastHighFreqProcessTime = Time.time;
        
        HighFrequencyData highFreqData = data as HighFrequencyData;
        if (highFreqData == null)
        {
            Debug.LogError($"[HIGH_FREQ] Data is not a HighFrequencyData object: {data?.GetType()}");
            return;
        }
        
        // Store latest high frequency data
        LatestHighFrequencyData = highFreqData;
        
        // Log all high frequency data entries
        if (highFreqData.data != null && highFreqData.data.Count > 0)
        {
            StringBuilder logBuilder = new StringBuilder();
            logBuilder.AppendLine($"[HIGH_FREQ_DATA] Timestamp: {highFreqData.timestamp}, Entries: {highFreqData.data.Count}, Processing at time: {Time.time}");
            
            foreach (var entry in highFreqData.data)
            {
                logBuilder.AppendLine($"  {entry.Key}: {entry.Value}");
            }
            
            Debug.Log(logBuilder.ToString());
        }
        
        // Notify subscribers
        if (messageHandlers.ContainsKey("high_frequency"))
        {
            foreach (var handler in messageHandlers["high_frequency"])
            {
                handler(highFreqData);
            }
        }
    }
    
    private void HandleImuDataMessage(object data)
    {
        // Rate limit check - only process IMU data once per second
        if (Time.time - lastImuProcessTime < IMU_RATE_LIMIT)
        {
            // Silently skip processing due to rate limit
            return;
        }
        
        lastImuProcessTime = Time.time;
        
        CombinedImuData imuData = data as CombinedImuData;
        if (imuData == null)
        {
            Debug.LogError($"[IMU] Data is not a CombinedImuData object: {data?.GetType()}");
            return;
        }
        
        // Store latest IMU data
        LatestImuData = imuData;
        
        // Log IMU data
        StringBuilder logBuilder = new StringBuilder();
        logBuilder.AppendLine($"[IMU_DATA] Timestamp: {imuData.timestamp}, Processing at time: {Time.time}");
        
        if (imuData.eva1 != null)
        {
            logBuilder.AppendLine($"  EVA1:");
            logBuilder.AppendLine($"    Position: ({imuData.eva1.position.x}, {imuData.eva1.position.y})");
            logBuilder.AppendLine($"    Heading: {imuData.eva1.heading}");
        }
        
        if (imuData.eva2 != null)
        {
            logBuilder.AppendLine($"  EVA2:");
            logBuilder.AppendLine($"    Position: ({imuData.eva2.position.x}, {imuData.eva2.position.y})");
            logBuilder.AppendLine($"    Heading: {imuData.eva2.heading}");
        }
        
        Debug.Log(logBuilder.ToString());
        
        // Notify subscribers
        if (messageHandlers.ContainsKey("imu_data"))
        {
            foreach (var handler in messageHandlers["imu_data"])
            {
                handler(imuData);
            }
        }
    }
    
    private void HandleBiometricsDataMessage(object data)
    {
        // Rate limit check
        if (Time.time - lastBiometricsProcessTime < BIOMETRICS_RATE_LIMIT)
        {
            // Silently skip processing due to rate limit
            return;
        }
        
        lastBiometricsProcessTime = Time.time;
        
        BiometricsData biometricsData = data as BiometricsData;
        if (biometricsData == null)
        {
            Debug.LogError($"[BIOMETRICS] Data is not a BiometricsData object: {data?.GetType()}");
            return;
        }
        
        // Store latest biometrics data based on EVA ID
        if (biometricsData.evaId == 1)
        {
            LatestEva1BiometricsData = biometricsData;
        }
        else if (biometricsData.evaId == 2)
        {
            LatestEva2BiometricsData = biometricsData;
        }
        
        // Log biometrics data
        StringBuilder logBuilder = new StringBuilder();
        logBuilder.AppendLine($"[BIOMETRICS_DATA] EVA{biometricsData.evaId}, Processing at time: {Time.time}:");
        logBuilder.AppendLine($"  Heart Rate: {biometricsData.heartRate}");
        logBuilder.AppendLine($"  Temperature: {biometricsData.temperature}");
        logBuilder.AppendLine($"  O2 Consumption: {biometricsData.o2Consumption}");
        logBuilder.AppendLine($"  CO2 Production: {biometricsData.co2Production}");
        logBuilder.AppendLine($"  Suit Pressure: {biometricsData.suitPressureTotal}");
        logBuilder.AppendLine($"  Helmet CO2: {biometricsData.helmetCo2}");
        
        Debug.Log(logBuilder.ToString());
        
        // Notify subscribers
        if (messageHandlers.ContainsKey("biometrics_data"))
        {
            foreach (var handler in messageHandlers["biometrics_data"])
            {
                handler(biometricsData);
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

    // For testing: Get EVA position from latest high frequency data
    public Vector2 GetEVA1Position()
    {
        if (LatestHighFrequencyData != null && 
            LatestHighFrequencyData.data.TryGetValue("eva1_imu_posx", out float posX) && 
            LatestHighFrequencyData.data.TryGetValue("eva1_imu_posy", out float posY))
        {
            return new Vector2(posX, posY);
        }
        return Vector2.zero;
    }
    
    // For testing: Get EVA heading from latest high frequency data
    public float GetEVA1Heading()
    {
        if (LatestHighFrequencyData != null && 
            LatestHighFrequencyData.data.TryGetValue("eva1_imu_heading", out float heading))
        {
            return heading;
        }
        return 0f;
    }
    
    // Debug method to set UIA data for testing
    public static void SetTestUiaData(UiaData testData)
    {
        if (testData == null) return;
        
        LatestUiaData = testData;
    }
    
    // Debug method to set DCU data for testing
    public static void SetTestDcuData(DcuData testData)
    {
        if (testData == null) return;
        
        LatestDcuData = testData;
    }
    
    // Debug method to set IMU data for testing
    public static void SetTestImuData(CombinedImuData testData)
    {
        if (testData == null) return;
        
        LatestImuData = testData;
    }
    
    // Debug method to set biometrics data for testing
    public static void SetTestBiometricsData(BiometricsData testData)
    {
        if (testData == null) return;
        
        if (testData.evaId == 1)
        {
            LatestEva1BiometricsData = testData;
        }
        else if (testData.evaId == 2)
        {
            LatestEva2BiometricsData = testData;
        }
    }

    // Methods for checking and retrieving rock data

    // Check if there's new rock data available since last check
    public static bool HasNewRockData()
    {
        // Log the current state for debugging
        Debug.Log($"[ROCK_CHECK] HasNewRockData called, current flag value: {hasNewRockData}");
        return hasNewRockData;
    }
    
    // Get the latest rock data and reset the "new data" flag
    public static RockData GetAndClearLatestRockData()
    {
        Debug.Log($"[ROCK_CHECK] GetAndClearLatestRockData called, clearing flag");
        hasNewRockData = false;
        return latestRockData;
    }
    
    // Get rock data for a specific EVA without affecting the "new data" flag
    public static RockData GetRockDataForEva(int evaId)
    {
        if (rockDataByEvaId.TryGetValue(evaId, out RockData data))
        {
            Debug.Log($"[ROCK_GET] Retrieved data for EVA{evaId}, SpecID: {data.specId}");
            return data;
        }
        Debug.Log($"[ROCK_GET] No data found for EVA{evaId}");
        return null;
    }
    
    // Manually set the new rock data flag (for testing or forcing updates)
    public static void ForceRockDataFlag(bool value)
    {
        hasNewRockData = value;
        Debug.Log($"[ROCK_FORCE] Rock data flag manually set to: {value}");
    }
    
    // Format rock data as a readable string
    public static string FormatRockDataSummary(RockData data)
    {
        if (data == null) return "No rock data available";
        
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"Rock Sample ID: {data.specId}");
        sb.AppendLine($"Name: {data.name ?? "N/A"}");
        sb.AppendLine($"From EVA: {data.evaId}");
        
        if (data.composition != null)
        {
            sb.AppendLine($"Contents:");
            sb.AppendLine($"  SiO2: {data.composition.SiO2:F2}%");
            sb.AppendLine($"  Al2O3: {data.composition.Al2O3:F2}%");
            sb.AppendLine($"  MnO: {data.composition.MnO:F2}%");
            sb.AppendLine($"  CaO: {data.composition.CaO:F2}%");
            sb.AppendLine($"  P2O3: {data.composition.P2O3:F2}%");
            sb.AppendLine($"  TiO2: {data.composition.TiO2:F2}%");
            sb.AppendLine($"  FeO: {data.composition.FeO:F2}%");
            sb.AppendLine($"  MgO: {data.composition.MgO:F2}%");
            sb.AppendLine($"  K2O: {data.composition.K2O:F2}%");
            sb.AppendLine($"  Other: {data.composition.Other:F2}%");
        }
        else
        {
            sb.AppendLine("Composition: Not available");
        }
        // Environmental data removed as it's not in the new JSON
        
        return sb.ToString();
    }

    // Reset rock data tracking completely - call when you want to start fresh
    public static void ResetRockDataTracking()
    {
        hasNewRockData = false;
        lastSpecIdByEvaId.Clear();
        rockDataHashByEvaId.Clear();
        lastProcessedSpecId = -1;
        Debug.Log("[ROCK_RESET] Rock data tracking completely reset");
    }
}
