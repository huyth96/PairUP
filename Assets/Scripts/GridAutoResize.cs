using UnityEngine;
using UnityEngine.UI;

public class GridAutoResize : MonoBehaviour
{
    public GridLayoutGroup grid;
    public int rows = 8;
    public int cols = 10;
    public float spacing = 5f;

    void Start()
    {
        Resize();
    }

    void Resize()
    {
        RectTransform rt = grid.GetComponent<RectTransform>();
        Vector2 areaSize = rt.rect.size;

        // trừ khoảng spacing
        float cellWidth = (areaSize.x - spacing * (cols - 1)) / cols;
        float cellHeight = (areaSize.y - spacing * (rows - 1)) / rows;

        float cellSize = Mathf.Min(cellWidth, cellHeight); // để ô vuông
        grid.cellSize = new Vector2(cellSize, cellSize);
        grid.spacing = new Vector2(spacing, spacing);
    }
}
