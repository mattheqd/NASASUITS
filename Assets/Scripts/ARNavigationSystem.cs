using UnityEngine;
using System.Collections.Generic;

public class ARNavigationSystem : MonoBehaviour
{
    [Header("Hardcoded Test Values")]
    public Vector2 startPosition = new Vector2(0f, 0f); // User's current position
    public Vector2 endPosition = new Vector2(10f, 10f); // Destination
    public List<Vector2> hazards = new List<Vector2>(); // List of hazard positions
    public float compassOrientation = 0f; // User's compass orientation in degrees

    [Header("Visualization Settings")]
    public float nodeSize = 1f;
    public float hazardRadius = 2f;
    public Material pathMaterial;
    public Material hazardMaterial;

    private LineRenderer pathRenderer;
    private List<Vector2> currentPath;
    private List<GameObject> hazardVisuals;
    private bool isPathVisible = false;

    private void Start()
    {
        // Initialize hardcoded hazards for testing
 

        // Create path renderer
        pathRenderer = gameObject.AddComponent<LineRenderer>();
        pathRenderer.material = pathMaterial;
        pathRenderer.startWidth = 0.2f;
        pathRenderer.endWidth = 0.2f;
        pathRenderer.positionCount = 0; // Initially hide the path

        // Create hazard visuals
        hazardVisuals = new List<GameObject>();
        foreach (var hazard in hazards)
        {
            GameObject hazardObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hazardObj.transform.position = new Vector3(hazard.x, 0f, hazard.y);
            hazardObj.transform.localScale = new Vector3(hazardRadius * 2, 0.1f, hazardRadius * 2);
            hazardObj.GetComponent<Renderer>().material = hazardMaterial;
            hazardVisuals.Add(hazardObj);
        }
    }

    private void Update()
    {
        if (isPathVisible)
        {
            UpdatePathVisualization();
        }
    }

    // Public method to be called from a button press
    public void TogglePath()
    {
        if (!isPathVisible)
        {
            CalculatePath();
            isPathVisible = true;
        }
        else
        {
            // Hide the path
            pathRenderer.positionCount = 0;
            isPathVisible = false;
        }
    }

    private void CalculatePath()
    {
        // Simple A* implementation
        List<Vector2> openSet = new List<Vector2>();
        List<Vector2> closedSet = new List<Vector2>();
        Dictionary<Vector2, Vector2> cameFrom = new Dictionary<Vector2, Vector2>();
        Dictionary<Vector2, float> gScore = new Dictionary<Vector2, float>();
        Dictionary<Vector2, float> fScore = new Dictionary<Vector2, float>();

        openSet.Add(startPosition);
        gScore[startPosition] = 0;
        fScore[startPosition] = Vector2.Distance(startPosition, endPosition);

        while (openSet.Count > 0)
        {
            Vector2 current = GetLowestFScore(openSet, fScore);
            if (current == endPosition)
            {
                currentPath = ReconstructPath(cameFrom, current);
                return;
            }

            openSet.Remove(current);
            closedSet.Add(current);

            foreach (Vector2 neighbor in GetNeighbors(current))
            {
                if (closedSet.Contains(neighbor) || IsHazard(neighbor))
                    continue;

                float tentativeGScore = gScore[current] + Vector2.Distance(current, neighbor);

                if (!openSet.Contains(neighbor))
                    openSet.Add(neighbor);
                else if (tentativeGScore >= gScore[neighbor])
                    continue;

                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeGScore;
                fScore[neighbor] = gScore[neighbor] + Vector2.Distance(neighbor, endPosition);
            }
        }

        // If we get here, no path was found
        currentPath = new List<Vector2>();
    }

    private List<Vector2> GetNeighbors(Vector2 position)
    {
        List<Vector2> neighbors = new List<Vector2>();
        float step = nodeSize;

        // Add 8-directional neighbors
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;
                neighbors.Add(new Vector2(position.x + x * step, position.y + y * step));
            }
        }

        return neighbors;
    }

    private bool IsHazard(Vector2 position)
    {
        foreach (Vector2 hazard in hazards)
        {
            if (Vector2.Distance(position, hazard) < hazardRadius)
                return true;
        }
        return false;
    }

    private Vector2 GetLowestFScore(List<Vector2> openSet, Dictionary<Vector2, float> fScore)
    {
        Vector2 lowest = openSet[0];
        float lowestScore = fScore[lowest];

        foreach (Vector2 node in openSet)
        {
            if (fScore[node] < lowestScore)
            {
                lowest = node;
                lowestScore = fScore[node];
            }
        }

        return lowest;
    }

    private List<Vector2> ReconstructPath(Dictionary<Vector2, Vector2> cameFrom, Vector2 current)
    {
        List<Vector2> path = new List<Vector2>();
        path.Add(current);

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Add(current);
        }

        path.Reverse();
        return path;
    }

    private void UpdatePathVisualization()
    {
        if (currentPath == null || currentPath.Count == 0)
            return;

        pathRenderer.positionCount = currentPath.Count;
        for (int i = 0; i < currentPath.Count; i++)
        {
            // Convert 2D path to 3D world space, considering compass orientation
            Vector2 pathPoint = currentPath[i];
            float angle = compassOrientation * Mathf.Deg2Rad;
            Vector3 worldPoint = new Vector3(
                pathPoint.x * Mathf.Cos(angle) - pathPoint.y * Mathf.Sin(angle),
                0f,
                pathPoint.x * Mathf.Sin(angle) + pathPoint.y * Mathf.Cos(angle)
            );
            pathRenderer.SetPosition(i, worldPoint);
        }
    }
} 