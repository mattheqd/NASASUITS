using UnityEngine;
using UnityEngine.UI;

public class SimpleNavigationSystem : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform minimapRect;
    public GameObject playerIconPrefab;
    private RectTransform playerIcon;
    private WebSocketClient webSocketClient;
    private bool isInitialized = false;

    void Start()
    {
        webSocketClient = FindObjectOfType<WebSocketClient>();
        if (webSocketClient == null)
        {
            Debug.LogError("WebSocketClient not found. Navigation system will not work properly.");
            return;
        }

        GameObject iconObj = Instantiate(playerIconPrefab, minimapRect);
        playerIcon = iconObj.GetComponent<RectTransform>();
        if (playerIcon == null)
        {
            Debug.LogError("Failed to get RectTransform from player icon!");
        }

        StartCoroutine(WaitForInitialImuData());
    }

    void Update()
    {
        if (!isInitialized) return;

        if (WebSocketClient.LatestImuData != null && 
            WebSocketClient.LatestImuData.eva1 != null && 
            WebSocketClient.LatestImuData.eva1.position != null)
        {
            Vector2 currentPos = new Vector2(
                WebSocketClient.LatestImuData.eva1.position.x,
                WebSocketClient.LatestImuData.eva1.position.y
            );
            UpdateAgentUI(currentPos);
        }
    }

    public Vector2 WorldToMinimap(Vector2 worldPos)
    {
        float xMin = -5730f, xMax = -5590f;
        float yMin = -10080f, yMax = -9940f;
        float xNorm = (worldPos.x - xMin) / (xMax - xMin);
        float yNorm = (worldPos.y - yMin) / (yMax - yMin);
        float xUI = xNorm * minimapRect.rect.width;
        float yUI = yNorm * minimapRect.rect.height;
        return new Vector2(xUI, yUI);
    }

    void UpdateAgentUI(Vector2 agentWorldPos)
    {
        if (playerIcon != null)
        {
            Vector2 minimapPos = WorldToMinimap(agentWorldPos);
            playerIcon.anchoredPosition = minimapPos;
        }
    }

    private System.Collections.IEnumerator WaitForInitialImuData()
    {
        while (WebSocketClient.LatestImuData == null || 
               WebSocketClient.LatestImuData.eva1 == null || 
               WebSocketClient.LatestImuData.eva1.position == null)
        {
            yield return new WaitForSeconds(0.1f);
        }

        Vector2 startPos = new Vector2(
            WebSocketClient.LatestImuData.eva1.position.x,
            WebSocketClient.LatestImuData.eva1.position.y
        );
        
        UpdateAgentUI(startPos);
        isInitialized = true;
    }

    void OnDrawGizmos()
    {
        float xMin = -5730f, xMax = -5590f;
        float yMin = -10080f, yMax = -9940f;
        
        Gizmos.color = Color.white;
        Vector3 topLeft = new Vector3(xMin, 0, yMax);
        Vector3 topRight = new Vector3(xMax, 0, yMax);
        Vector3 bottomLeft = new Vector3(xMin, 0, yMin);
        Vector3 bottomRight = new Vector3(xMax, 0, yMin);

        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft, topLeft);
    }
} 