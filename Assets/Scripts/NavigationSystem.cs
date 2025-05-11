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

    public RectTransform minimapRect;
    public GameObject playerIconPrefab;
    private RectTransform playerIcon;
    public GameObject pathDotPrefab;
    private List<GameObject> pathDots = new List<GameObject>();

    public Vector2 startLocation = new Vector2(-5720, -10060);
    public Vector2 endLocation = new Vector2(-5600, -9940);
    private List<Node> currentPath = new List<Node>();
    private bool isMoving = false;
    private bool shouldRecalculatePath = false;
    private int currentCoordinateIndex = 0;
    private float moveTimer = 0f;

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
        // Create player icon
        GameObject iconObj = Instantiate(playerIconPrefab, minimapRect);
        playerIcon = iconObj.GetComponent<RectTransform>();
        
        InitializeNodes();
        UpdateMinimap();
        CalculateAndDrawPath();
    }

    void Update()
    {
        // Check for path recalculation toggle (e.g., space bar)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            shouldRecalculatePath = true;
        }

        // Recalculate path if needed
        if (shouldRecalculatePath)
        {
            CalculateAndDrawPath();
            shouldRecalculatePath = false;
        }

        // Handle movement
        if (Input.GetKeyDown(KeyCode.M))
        {
            isMoving = !isMoving;
            if (isMoving)
            {
                currentCoordinateIndex = 0;
                moveTimer = 0f;
            }
        }

        if (isMoving)
        {
            moveTimer += Time.deltaTime;
            if (moveTimer >= 1f)
            {
                moveTimer = 0f;
                if (currentCoordinateIndex < movementCoordinates.Length)
                {
                    UpdateAgentUI(movementCoordinates[currentCoordinateIndex]);
                    currentCoordinateIndex++;
                }
                else
                {
                    isMoving = false;
                    Debug.Log("Completed movement sequence");
                }
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

        // Create nodes at each grid line intersection
        for (int i = 0; i < gridLines; i++)
        {
            for (int j = 0; j < gridLines; j++)
            {
                float x = xStart + i * increment;
                float y = yStart - j * increment; // y decreases as j increases
                Node node = new Node
                {
                    position = new Vector2(x, y),
                    isObstacle = false // No obstacles for now
                };
                allNodes.Add(node);
                Debug.Log($"Created node at position: ({x}, {y})");
            }
        }

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

        Debug.Log($"Finding nearest node to ({position.x}, {position.y}). Found node at ({nearest.position.x}, {nearest.position.y})");
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
            Gizmos.color = node.isObstacle ? Color.red : Color.white;
            Gizmos.DrawSphere(new Vector3(node.position.x, 0, node.position.y), 0.1f);
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
            playerIcon.anchoredPosition = WorldToMinimap(agentWorldPos);
        }
    }

    void DrawPathUI(List<Node> path)
    {
        // Clear old dots
        foreach (var dot in pathDots) Destroy(dot);
        pathDots.Clear();

        // Create a simple white sprite for the line
        Texture2D lineTexture = new Texture2D(1, 1);
        lineTexture.SetPixel(0, 0, Color.white);
        lineTexture.Apply();
        Sprite lineSprite = Sprite.Create(lineTexture, new Rect(0, 0, 1, 1), new Vector2(0, 0));

        // Start with the start node position
        Vector2 previousPos = WorldToMinimap(startNode.position);

        // Process each node in the path
        for (int i = 0; i < path.Count; i++)
        {
            var node = path[i];
            // Get current position
            Vector2 currentPos = WorldToMinimap(node.position);

            // Create line from previous position to current position
            GameObject lineObj = new GameObject($"Line");
            lineObj.transform.SetParent(minimapRect);
            RectTransform lineRect = lineObj.AddComponent<RectTransform>();
            Image line = lineObj.AddComponent<Image>();
            line.sprite = lineSprite;
            line.color = Color.yellow;
            line.type = Image.Type.Simple;

            // Set anchors and pivot to (0,0)
            lineRect.anchorMin = Vector2.zero;
            lineRect.anchorMax = Vector2.zero;
            lineRect.pivot = Vector2.zero;

            // Calculate line properties
            Vector2 direction = currentPos - previousPos;
            float distance = direction.magnitude * 0.5f; // Halve the distance
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // Set line properties
            lineRect.anchoredPosition = previousPos;
            lineRect.sizeDelta = new Vector2(distance, 6f);
            lineRect.localRotation = Quaternion.Euler(0, 0, angle);
            pathDots.Add(lineObj);

            // Update previous position for next iteration
            previousPos = currentPos;
        }
    }

    void CalculateAndDrawPath()
    {
        Debug.Log($"Calculating path from ({startLocation.x}, {startLocation.y}) to ({endLocation.x}, {endLocation.y})");
        currentPath = FindPath(startLocation, endLocation);
        if (currentPath != null && currentPath.Count > 0)
        {
            Debug.Log($"Path found with {currentPath.Count} nodes");
            DrawPathUI(currentPath);
            UpdateAgentUI(startLocation);
        }
        else
        {
            Debug.LogWarning("No path found!");
        }
    }
} 