using UnityEngine;
using UnityEngine.UI;

public class GridOverlay : Graphic
{
    public int gridSize = 14;
    public Color gridColor = Color.black;
    public float lineThickness = 2f;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        float width = rectTransform.rect.width;
        float height = rectTransform.rect.height;
        float xStep = width / (gridSize - 1);
        float yStep = height / (gridSize - 1);

        // Draw vertical lines
        for (int i = 0; i < gridSize; i++)
        {
            float x = i * xStep;
            DrawLine(vh, new Vector2(x, 0), new Vector2(x, height), gridColor, lineThickness);
        }
        // Draw horizontal lines
        for (int j = 0; j < gridSize; j++)
        {
            float y = j * yStep;
            DrawLine(vh, new Vector2(0, y), new Vector2(width, y), gridColor, lineThickness);
        }
    }

    void DrawLine(VertexHelper vh, Vector2 start, Vector2 end, Color color, float thickness)
    {
        Vector2 direction = (end - start).normalized;
        Vector2 normal = new Vector2(-direction.y, direction.x) * (thickness / 2f);

        UIVertex[] verts = new UIVertex[4];
        verts[0].color = color;
        verts[1].color = color;
        verts[2].color = color;
        verts[3].color = color;

        verts[0].position = start + normal;
        verts[1].position = start - normal;
        verts[2].position = end - normal;
        verts[3].position = end + normal;

        vh.AddUIVertexQuad(verts);
    }
} 