using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class NavigationSystem : MonoBehaviour
{
    [System.Serializable]
    public class Node
    {
        public Vector2 position;
        public List<Node> neighbors = new List<Node>();
        public bool isObstacle;
        public float gCost; // Cost from start to this node
        public float hCost; // Heuristic cost from this node to end
        public Node parent; // For path reconstruction

        public float fCost { get { return gCost + hCost; } }
    }

    public class Minimap
    {
        public Vector2 centerPosition;
        public int size = 10; // 10x10 grid
        public List<Node> visibleNodes = new List<Node>();
    }

    private List<Node> allNodes = new List<Node>();
    private Minimap minimap = new Minimap();
    private Vector2 currentPlayerPosition;
    private Node startNode;
    private Node endNode;

    [Header("UI References")]
    public RectTransform minimapRect;
    public GameObject playerIconPrefab;
    public GameObject pathDotPrefab;
    public Button startNavigationButton; // New button reference
    private RectTransform playerIcon;
    private List<GameObject> pathDots = new List<GameObject>();
    public GameObject poiIconPrefab; // Prefab for POI icons
    private List<GameObject> poiIcons = new List<GameObject>();
    public GameObject pinPrefab; // Prefab for dropped pins
    private List<GameObject> droppedPins = new List<GameObject>();
    public GameObject airlockPrefab; // Prefab for airlock location
    private GameObject airlockIcon; // Reference to the airlock icon instance

    public Vector2 endLocation = new Vector2(-5600, -9940);
    private Vector2 airlockLocation = new Vector2(-5673.19f, -10033.67f); // Default airlock position
    private List<Node> currentPath = new List<Node>();
    private bool isMoving = false;
    private bool shouldRecalculatePath = false;
    private float moveTimer = 0f;
    private WebSocketClient webSocketClient;
    private bool isInitialized = false;

    // Predefined coordinates for movement
    private readonly Vector2[] movementCoordinates = new Vector2[]
    {
        new Vector2(-5673.19f, -10033.67f),
        new Vector2(-5673.09f, -10032.39f),
        new Vector2(-5672.30f, -10029.74f),
        new Vector2(-5672.11f, -10027.78f),
        new Vector2(-5671.91f, -10025.82f),
        new Vector2(-5671.71f, -10023.89f),
        new Vector2(-5671.71f, -10021.89f),
        new Vector2(-5669.16f, -10018.06f),
        new Vector2(-5668.97f, -10015.51f),
        new Vector2(-5668.42f, -10014.83f),
        new Vector2(-5668.38f, -10013.85f),
        new Vector2(-5667.21f, -10012.83f),
        new Vector2(-5666.55f, -10010.98f),
        new Vector2(-5667.30f, -10008.94f),
        new Vector2(-5666.91f, -10006.58f),
        new Vector2(-5665.53f, -10004.26f),
        new Vector2(-5665.53f, -10002.26f),
        new Vector2(-5662.20f, -10000.99f),
        new Vector2(-5661.12f, -10000.11f),
        new Vector2(-5695.55f, -9999.32f),
        new Vector2(-5695.55f, -9999.32f),
        new Vector2(-5661.12f, -10000.11f),
        new Vector2(-5662.20f, -10000.99f),
        new Vector2(-5665.53f, -10002.26f),
        new Vector2(-5665.53f, -10004.26f),
        new Vector2(-5666.91f, -10006.58f),
        new Vector2(-5667.30f, -10008.94f),
        new Vector2(-5666.55f, -10010.98f),
        new Vector2(-5667.21f, -10012.83f),
        new Vector2(-5668.38f, -10013.85f),
        new Vector2(-5668.42f, -10014.83f),
        new Vector2(-5668.97f, -10015.51f),
        new Vector2(-5669.16f, -10018.06f),
        new Vector2(-5671.71f, -10021.89f),
        new Vector2(-5671.71f, -10023.89f),
        new Vector2(-5671.91f, -10025.82f),
        new Vector2(-5672.11f, -10027.78f),
        new Vector2(-5672.30f, -10029.74f),
        new Vector2(-5673.09f, -10032.39f),
        new Vector2(-5673.19f, -10033.67f)
    };

    void Start()
    {
        // Find WebSocketClient
        webSocketClient = FindObjectOfType<WebSocketClient>();
        if (webSocketClient == null)
        {
            Debug.LogError("WebSocketClient not found. Navigation system will not work properly.");
            return;
        }

        // Set up button listener
        if (startNavigationButton != null)
        {
            startNavigationButton.onClick.AddListener(OnStartNavigationPressed);
        }
        else
        {
            Debug.LogError("Start Navigation Button not assigned in inspector!");
        }

        // Initialize the system immediately
        Debug.Log("[NavigationSystem] Starting initial system setup");
        StartCoroutine(InitializeNavigationSystem());
    }

    public void StartNavigation()
    {
        Debug.Log("[NavigationSystem] Starting navigation via public method");
        StartCoroutine(InitializeNavigationSystem());
    }

    private void OnStartNavigationPressed()
    {
        Debug.Log("[NavigationSystem] Start Navigation button pressed");
        StartNavigation();
    }

    private void CleanupExistingSystem()
    {
        Debug.Log("[NavigationSystem] Cleaning up existing system...");
        
        // Clean up existing player icon
        if (playerIcon != null)
        {
            Debug.Log("[NavigationSystem] Destroying existing player icon");
            Destroy(playerIcon.gameObject);
            playerIcon = null;
        }

        // Clean up existing airlock icon
        if (airlockIcon != null)
        {
            Debug.Log("[NavigationSystem] Destroying existing airlock icon");
            Destroy(airlockIcon);
            airlockIcon = null;
        }

        // Clean up existing path dots
        Debug.Log($"[NavigationSystem] Cleaning up {pathDots.Count} path dots");
        foreach (var dot in pathDots)
        {
            if (dot != null)
            {
                Destroy(dot);
            }
        }
        pathDots.Clear();

        // Reset state
        Debug.Log("[NavigationSystem] Resetting system state");
        allNodes.Clear();
        minimap.visibleNodes.Clear();
        currentPath.Clear();
        isMoving = false;
        shouldRecalculatePath = false;
        moveTimer = 0f;
        isInitialized = false;
    }

    private System.Collections.IEnumerator InitializeNavigationSystem()
    {
        Debug.Log("[NavigationSystem] Starting initialization...");
        
        // Clean up any existing system
        CleanupExistingSystem();

        // Create player icon
        Debug.Log("[NavigationSystem] Creating new player icon");
        GameObject iconObj = Instantiate(playerIconPrefab, minimapRect);
        playerIcon = iconObj.GetComponent<RectTransform>();
        if (playerIcon == null)
        {
            Debug.LogError("[NavigationSystem] Failed to get RectTransform from player icon!");
        }
        else
        {
            // Set the size and pivot of the player icon
            playerIcon.sizeDelta = new Vector2(500, 500); // Set size to 200x200
            playerIcon.pivot = Vector2.zero; // Set pivot to (0,0)
            playerIcon.anchorMin = Vector2.zero; // Set anchor min to (0,0)
            playerIcon.anchorMax = Vector2.zero; // Set anchor max to (0,0)
        }

        // Create airlock icon
        if (airlockPrefab != null)
        {
            Debug.Log("[NavigationSystem] Creating airlock icon");
            GameObject airlockObj = Instantiate(airlockPrefab, minimapRect);
            RectTransform airlockRect = airlockObj.GetComponent<RectTransform>();
            if (airlockRect != null)
            {
                Vector2 minimapPos = WorldToMinimap(airlockLocation);
                airlockRect.anchoredPosition = minimapPos;
                airlockRect.anchorMin = Vector2.zero;
                airlockRect.anchorMax = Vector2.zero;
                airlockRect.pivot = Vector2.zero;
                airlockRect.sizeDelta = new Vector2(200, 200); // Set size to 200x200
                airlockIcon = airlockObj;
            }
        }
        else
        {
            Debug.LogWarning("[NavigationSystem] Airlock prefab not assigned!");
        }
        
        Debug.Log("[NavigationSystem] Initializing nodes");
        InitializeNodes();
        Debug.Log($"[NavigationSystem] Initialized {allNodes.Count} nodes");
        
        Debug.Log("[NavigationSystem] Updating minimap");
        UpdateMinimap();
        Debug.Log($"[NavigationSystem] Minimap has {minimap.visibleNodes.Count} visible nodes");
        
        // Wait for initial IMU data to position the player icon
        Debug.Log("[NavigationSystem] Waiting for initial IMU data...");
        yield return StartCoroutine(WaitForInitialImuData());
        
        isInitialized = true;
        Debug.Log("[NavigationSystem] Initialization complete!");
    }

    void Update()
    {
        if (!isInitialized) return;

        // Check for path recalculation toggle (e.g., space bar)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            shouldRecalculatePath = true;
        }

        // Recalculate path if needed
        if (shouldRecalculatePath)
        {
            if (WebSocketClient.LatestImuData != null && 
                WebSocketClient.LatestImuData.eva1 != null && 
                WebSocketClient.LatestImuData.eva1.position != null)
            {
                Vector2 currentPos = new Vector2(
                    WebSocketClient.LatestImuData.eva1.position.x,
                    WebSocketClient.LatestImuData.eva1.position.y
                );
                CalculateAndDrawPath(currentPos);
            }
            shouldRecalculatePath = false;
        }

        // Update agent position from IMU data every second
        moveTimer += Time.deltaTime;
        if (moveTimer >= 1f)
        {
            moveTimer = 0f;
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
    }

    private void InitializeNodes()
    {
        allNodes.Clear();
        int gridLines = 15; // 15x15 grid lines
        float xStart = -5730f;
        float yStart = -9940f;
        float xEnd = -5590f;
        float yEnd = -10080f;
        float increment = 10f;

        Debug.Log("=== Hazard Node Coordinates ===");
        // Create nodes at each grid line intersection
        for (int i = 0; i < gridLines; i++)
        {
            for (int j = 0; j < gridLines; j++)
            {
                float x = xStart + i * increment;
                float y = yStart - j * increment; // y decreases as j increases
                bool isHazard = IsHazardCoordinate(i, j);
                Node node = new Node
                {
                    position = new Vector2(x, y),
                    isObstacle = isHazard
                };
                allNodes.Add(node);
                
                if (isHazard)
                {
                    Debug.Log($"Hazard at grid position (i={i}, j={j}) -> world position ({x}, {y})");
                }
            }
        }
        Debug.Log("=== End Hazard Coordinates ===");

        // Connect neighboring nodes (8-way connectivity)
        foreach (Node node in allNodes)
        {
            foreach (Node otherNode in allNodes)
            {
                if (node == otherNode) continue;
                float dx = Mathf.Abs(node.position.x - otherNode.position.x);
                float dy = Mathf.Abs(node.position.y - otherNode.position.y);
                if ((dx == increment && dy == 0) || (dx == 0 && dy == increment) || (dx == increment && dy == increment))
                {
                    node.neighbors.Add(otherNode);
                }
            }
        }
    }

    private bool IsHazardCoordinate(int i, int j)
    {
        // i and j are already in 0-14 range from the grid creation
        switch (j)
        {
            case 0:
                return i >= 0 && i <= 14;
            case 1:
                return (i >= 0 && i <= 6) || (i >= 8 && i <= 14);
            case 2:
                return (i >= 0 && i <= 6) || (i >= 12 && i <= 14);
            case 3:
                return (i >= 0 && i <= 6) || i == 11 || i == 14;
            case 4:
                return (i >= 0 && i <= 7) || i == 11 || i == 12 || i == 14;
            case 5:
                return (i >= 0 && i <= 7) || i == 11 || i == 13 || i == 14;
            case 6:
                return i == 0 || i == 1 || (i >= 4 && i <= 6) || i == 13 || i == 14;
            case 7:
            case 8:
            case 10:
                return i == 0 || i == 1 || (i >= 7 && i <= 14);
            case 9:
                return i == 0 || (i >= 7 && i <= 14);
            case 11:
            case 12:
                return (i >= 0 && i <= 5) || (i >= 8 && i <= 14);
            case 13:
            case 14:
                return (i >= 0 && i <= 5) || (i >= 7 && i <= 14);
            default:
                return false;
        }
    }

    private void SimulatePlayerMovement()
    {
        // Simulate player movement with proxy data
        currentPlayerPosition += new Vector2(
            Random.Range(-0.1f, 0.1f),
            Random.Range(-0.1f, 0.1f)
        );
    }

    private void UpdateMinimap()
    {
        minimap.centerPosition = currentPlayerPosition;
        minimap.visibleNodes.Clear();

        // Get nodes within the minimap bounds
        float halfSize = minimap.size / 2f;
        foreach (Node node in allNodes)
        {
            if (Mathf.Abs(node.position.x - currentPlayerPosition.x) <= halfSize &&
                Mathf.Abs(node.position.y - currentPlayerPosition.y) <= halfSize)
            {
                minimap.visibleNodes.Add(node);
            }
        }
    }

    public List<Node> FindPath(Vector2 startPos, Vector2 endPos)
    {
        startNode = FindNearestNode(startPos);
        endNode = FindNearestNode(endPos);

        if (startNode == null || endNode == null)
            return null;

        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            Node currentNode = openSet.OrderBy(x => x.fCost).First();

            if (currentNode == endNode)
                return RetracePath(startNode, endNode);

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            foreach (Node neighbor in currentNode.neighbors)
            {
                if (closedSet.Contains(neighbor) || neighbor.isObstacle)
                    continue;

                float newMovementCostToNeighbor = currentNode.gCost + Vector2.Distance(currentNode.position, neighbor.position);
                if (newMovementCostToNeighbor < neighbor.gCost || !openSet.Contains(neighbor))
                {
                    neighbor.gCost = newMovementCostToNeighbor;
                    neighbor.hCost = Vector2.Distance(neighbor.position, endNode.position);
                    neighbor.parent = currentNode;

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

        return null; // No path found
    }

    private Node FindNearestNode(Vector2 position)
    {
        Node nearest = null;
        float minDistance = float.MaxValue;

        foreach (Node node in allNodes)
        {
            float distance = Vector2.Distance(node.position, position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = node;
            }
        }

        if (nearest == null)
        {
            Debug.LogError($"[NavigationSystem] No nearest node found for position ({position.x}, {position.y})");
        }
        else
        {
            Debug.Log($"[NavigationSystem] Found nearest node at ({nearest.position.x}, {nearest.position.y}) with distance {minDistance}");
        }
        
        return nearest;
    }

    private List<Node> RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;

        // First add the end node
        path.Add(endNode);

        // Then add all nodes in between
        while (currentNode != startNode)
        {
            currentNode = currentNode.parent;
            path.Add(currentNode);
        }

        // Reverse the list to get start->end order
        path.Reverse();

        // Debug log the path order
        Debug.Log("Path order:");
        for (int i = 0; i < path.Count; i++)
        {
            Debug.Log($"Node {i}: ({path[i].position.x}, {path[i].position.y})");
        }

        return path;
    }

    // For debugging and visualization
    void OnDrawGizmos()
    {
        if (allNodes == null) return;

        // Draw all nodes
        foreach (Node node in allNodes)
        {
            if (node.isObstacle)
            {
                // Draw hazard nodes in red
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(new Vector3(node.position.x, 0, node.position.y), 0.2f);
            }
            else
            {
                // Draw normal nodes in white
                Gizmos.color = Color.white;
                Gizmos.DrawSphere(new Vector3(node.position.x, 0, node.position.y), 0.1f);
            }
        }

        // Draw minimap bounds
        Gizmos.color = Color.yellow;
        float halfSize = minimap.size / 2f;
        Vector3 center = new Vector3(minimap.centerPosition.x, 0, minimap.centerPosition.y);
        Gizmos.DrawWireCube(center, new Vector3(minimap.size, 0.1f, minimap.size));

        // Draw visible nodes in minimap
        Gizmos.color = Color.green;
        foreach (Node node in minimap.visibleNodes)
        {
            Gizmos.DrawSphere(new Vector3(node.position.x, 0.1f, node.position.y), 0.15f);
        }

        // Draw current path if it exists
        if (currentPath != null && currentPath.Count > 0)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < currentPath.Count - 1; i++)
            {
                Vector3 start = new Vector3(currentPath[i].position.x, 0.2f, currentPath[i].position.y);
                Vector3 end = new Vector3(currentPath[i + 1].position.x, 0.2f, currentPath[i + 1].position.y);
                Gizmos.DrawLine(start, end);
            }
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
            Debug.Log($"[NavigationSystem] Updating agent UI from world pos ({agentWorldPos.x}, {agentWorldPos.y}) to minimap pos ({minimapPos.x}, {minimapPos.y})");
            playerIcon.anchoredPosition = minimapPos;
        }
        else
        {
            Debug.LogWarning("[NavigationSystem] Cannot update agent UI - playerIcon is null!");
        }
    }

    void DrawPathUI(List<Node> path)
    {
        // Clear old dots
        foreach (var dot in pathDots) Destroy(dot);
        pathDots.Clear();

        // Start with the start node position
        Vector2 previousPos = WorldToMinimap(startNode.position);

        // Process each node in the path
        for (int i = 0; i < path.Count; i++)
        {
            var node = path[i];
            // Get current position
            Vector2 currentPos = WorldToMinimap(node.position);

            // Create line from previous position to current position
            GameObject lineObj = Instantiate(pathDotPrefab, minimapRect);
            RectTransform lineRect = lineObj.GetComponent<RectTransform>();
            Image line = lineObj.GetComponent<Image>();
            line.color = Color.yellow;

            // Set anchors and pivot to (0,0)
            lineRect.anchorMin = Vector2.zero;
            lineRect.anchorMax = Vector2.zero;
            lineRect.pivot = Vector2.zero;

            // Calculate line properties
            Vector2 direction = currentPos - previousPos;
            float distance = direction.magnitude;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // Set line properties
            lineRect.anchoredPosition = previousPos;
            lineRect.sizeDelta = new Vector2(distance, lineRect.sizeDelta.y); // Use prefab's natural height
            lineRect.localRotation = Quaternion.Euler(0, 0, angle);
            pathDots.Add(lineObj);

            // Update previous position for next iteration
            previousPos = currentPos;
        }
    }

    void CalculateAndDrawPath(Vector2 startPos)
    {
        Debug.Log($"[NavigationSystem] Calculating path from ({startPos.x}, {startPos.y}) to ({endLocation.x}, {endLocation.y})");
        
        // Find nearest nodes for start and end positions
        startNode = FindNearestNode(startPos);
        endNode = FindNearestNode(endLocation);
        
        if (startNode == null || endNode == null)
        {
            Debug.LogError("[NavigationSystem] Failed to find valid start or end nodes!");
            if (startNode == null) Debug.LogError("[NavigationSystem] Start node is null");
            if (endNode == null) Debug.LogError("[NavigationSystem] End node is null");
            return;
        }
        
        Debug.Log($"[NavigationSystem] Found start node at ({startNode.position.x}, {startNode.position.y})");
        Debug.Log($"[NavigationSystem] Found end node at ({endNode.position.x}, {endNode.position.y})");
        
        currentPath = FindPath(startPos, endLocation);
        if (currentPath != null && currentPath.Count > 0)
        {
            Debug.Log($"[NavigationSystem] Path found with {currentPath.Count} nodes");
            DrawPathUI(currentPath);
            UpdateAgentUI(startPos);
        }
        else
        {
            Debug.LogWarning("[NavigationSystem] No path found!");
        }
    }

    private System.Collections.IEnumerator WaitForInitialImuData()
    {
        Debug.Log("[NavigationSystem] Starting to wait for IMU data...");
        int waitCount = 0;
        
        // Wait until we have valid IMU data
        while (WebSocketClient.LatestImuData == null || 
               WebSocketClient.LatestImuData.eva1 == null || 
               WebSocketClient.LatestImuData.eva1.position == null)
        {
            waitCount++;
            if (waitCount % 10 == 0) // Log every 10 attempts
            {
                Debug.Log($"[NavigationSystem] Still waiting for IMU data... (attempt {waitCount})");
                if (WebSocketClient.LatestImuData == null)
                    Debug.Log("[NavigationSystem] LatestImuData is null");
                else if (WebSocketClient.LatestImuData.eva1 == null)
                    Debug.Log("[NavigationSystem] eva1 data is null");
                else if (WebSocketClient.LatestImuData.eva1.position == null)
                    Debug.Log("[NavigationSystem] eva1 position is null");
            }
            yield return new WaitForSeconds(0.1f);
        }

        // Get initial position from IMU
        Vector2 startPos = new Vector2(
            WebSocketClient.LatestImuData.eva1.position.x,
            WebSocketClient.LatestImuData.eva1.position.y
        );
        
        Debug.Log($"[NavigationSystem] Got initial EVA1 position from IMU: ({startPos.x}, {startPos.y})");
        
        // Update player position
        UpdateAgentUI(startPos);
    }

    public void UpdatePathToLocation(Vector2 newEndLocation)
    {
        Debug.Log($"[NavigationSystem] Updating path to new location: ({newEndLocation.x}, {newEndLocation.y})");
        
        if (!isInitialized)
        {
            Debug.LogWarning("[NavigationSystem] System not initialized yet, cannot update path");
            return;
        }

        endLocation = newEndLocation;
        
        // Get current position from IMU
        if (WebSocketClient.LatestImuData != null && 
            WebSocketClient.LatestImuData.eva1 != null && 
            WebSocketClient.LatestImuData.eva1.position != null)
        {
            Vector2 currentPos = new Vector2(
                WebSocketClient.LatestImuData.eva1.position.x,
                WebSocketClient.LatestImuData.eva1.position.y
            );
            CalculateAndDrawPath(currentPos);
        }
        else
        {
            Debug.LogWarning("[NavigationSystem] No IMU data available for path update");
        }
    }

    public void ClearCurrentPath()
    {
        Debug.Log("[NavigationSystem] Clearing current path");
        
        // Clear path dots
        foreach (var dot in pathDots)
        {
            if (dot != null)
            {
                Destroy(dot);
            }
        }
        pathDots.Clear();
        
        // Clear path data
        currentPath.Clear();
    }

    /// <summary>
    /// Places POI icons on the minimap at the given world positions.
    /// </summary>
    /// <param name="poiPositions">List of world positions for POIs</param>
    public void PlacePOIIcons(List<Vector2> poiPositions)
    {
        ClearPOIIcons();
        if (poiIconPrefab == null)
        {
            Debug.LogError("[NavigationSystem] POI Icon Prefab not assigned!");
            return;
        }
        foreach (var pos in poiPositions)
        {
            GameObject poiObj = Instantiate(poiIconPrefab, minimapRect);
            RectTransform poiRect = poiObj.GetComponent<RectTransform>();
            if (poiRect != null)
            {
                Vector2 minimapPos = WorldToMinimap(pos);
                poiRect.anchoredPosition = minimapPos;
                poiRect.anchorMin = Vector2.zero;
                poiRect.anchorMax = Vector2.zero;
                poiRect.pivot = Vector2.zero;
                poiRect.sizeDelta = new Vector2(200, 200); // Set size to 300x300
            }
            
            poiIcons.Add(poiObj);
        }
    }

    /// <summary>
    /// Clears all POI icons from the minimap.
    /// </summary>
    public void ClearPOIIcons()
    {
        foreach (var icon in poiIcons)
        {
            if (icon != null)
                Destroy(icon);
        }
        poiIcons.Clear();
    }

    /// <summary>
    /// Drops a pin at the current IMU position.
    /// </summary>
    public void DropPin()
    {
        if (pinPrefab == null)
        {
            Debug.LogError("[NavigationSystem] Pin Prefab not assigned!");
            return;
        }
        if (WebSocketClient.LatestImuData == null || 
            WebSocketClient.LatestImuData.eva1 == null || 
            WebSocketClient.LatestImuData.eva1.position == null)
        {
            Debug.LogWarning("[NavigationSystem] No IMU data available for dropping pin.");
            return;
        }
        Vector2 currentPos = new Vector2(
            WebSocketClient.LatestImuData.eva1.position.x,
            WebSocketClient.LatestImuData.eva1.position.y
        );
        GameObject pinObj = Instantiate(pinPrefab, minimapRect);
        RectTransform pinRect = pinObj.GetComponent<RectTransform>();
        if (pinRect != null)
        {
            Vector2 minimapPos = WorldToMinimap(currentPos);
            pinRect.anchoredPosition = minimapPos;
            pinRect.anchorMin = Vector2.zero;
            pinRect.anchorMax = Vector2.zero;
            pinRect.pivot = new Vector2(0.5f, 0.5f);
        }
        droppedPins.Add(pinObj);
    }

    /// <summary>
    /// Clears all dropped pins from the minimap.
    /// </summary>
    public void ClearDroppedPins()
    {
        foreach (var pin in droppedPins)
        {
            if (pin != null)
                Destroy(pin);
        }
        droppedPins.Clear();
    }
} 