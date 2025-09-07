using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Vẽ đường nối (axis-aligned) cho Onet, canh theo tâm icon/tile trên GridLayout.
/// - Gọi Init(tiles, rows, cols) sau khi Board đã spawn xong.
/// - Gọi DrawPath(cells) với cells là danh sách (r,c) theo hệ có border: r,c ∈ [0..rows+1].
/// - Đường sẽ tự xóa sau lifeTime, hoặc gọi ClearNow() để xóa ngay.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class PathDrawerIconClamp : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("RectTransform của grid (chứa GridLayoutGroup)")]
    public RectTransform boardArea;   // có GridLayoutGroup
    [Tooltip("Component UILine đặt trên cùng GameObject (PathLayer)")]
    public UILine uiLine;             // component UILine trên PathLayer

    [Header("Style")]
    [Tooltip("Độ dày đường vẽ (local units, nên dùng Canvas Scaler: Constant Pixel Size)")]
    public float thickness = 16f;
    [Tooltip("Tự xóa đường sau N giây (<=0 để không tự xóa)")]
    public float lifeTime = 0.6f;
    [Tooltip("Màu vẽ đường")]
    public Color lineColor = new Color(1f, 0.2f, 0.6f, 1f);

    [Header("Options")]
    [Tooltip("Ưu tiên dùng tâm icon (nếu còn) thay vì tâm ô khi vẽ đoạn trong bảng")]
    public bool preferIconCenter = true;

    // ==== cache ====
    private Tile[,] tiles; private int rows, cols;   // rows/cols: số ô thật (không tính border)
    private GridLayoutGroup glg;

    // Tâm từng cột/hàng (local trong PathLayer)
    private float[] colX;  // size cols+1, 1-based
    private float[] rowY;  // size rows+1, 1-based
    private float stepX, stepY;

    // Biên Board (local trong PathLayer) — suy vị trí r=0, r=rows+1, c=0, c=cols+1
    private float minX, maxX, minY, maxY;

    /// <summary>Gọi sau khi grid spawn xong (đã có Tile và layout ổn định).</summary>
    public void Init(Tile[,] tiles, int rows, int cols)
    {
        this.tiles = tiles; this.rows = rows; this.cols = cols;

        if (!boardArea)
        {
            Debug.LogError("[PathDrawerIconClamp] boardArea chưa gán!");
            return;
        }
        if (!uiLine)
        {
            uiLine = GetComponent<UILine>();
            if (!uiLine) Debug.LogError("[PathDrawerIconClamp] Không tìm thấy UILine!");
        }

        glg = boardArea.GetComponent<GridLayoutGroup>();
        AlignLayerToBoard();

        // Cache tâm cột/hàng (1-based)
        colX = new float[cols + 1];
        rowY = new float[rows + 1];

        for (int c = 1; c <= cols; c++) colX[c] = SafeCenterLocal(1, c).x;
        for (int r = 1; r <= rows; r++) rowY[r] = SafeCenterLocal(r, 1).y;

        stepX = EstimateStep(colX);
        stepY = Mathf.Abs(EstimateStep(rowY)); // y giảm theo hàng

        // Biên theo nửa bước ngoài cùng (không dùng padding)
        minX = colX[1] - stepX * 0.5f;
        maxX = colX[cols] + stepX * 0.5f;
        maxY = rowY[1] + stepY * 0.5f;     // top
        minY = rowY[rows] - stepY * 0.5f;  // bottom

        // style cho UILine
        if (uiLine)
        {
            uiLine.thickness = thickness;
            uiLine.color = lineColor;
        }
    }

    /// <summary>Nếu bạn thay đổi layout sau này (force rebuild, đổi spacing…), gọi lại để re-cache tâm/step.</summary>
    public void Reinit()
    {
        if (tiles == null || rows <= 0 || cols <= 0) return;
        Init(tiles, rows, cols);
    }

    /// <summary>Đổi style lúc runtime.</summary>
    public void SetStyle(float? newThickness = null, Color? newColor = null, float? newLifeTime = null)
    {
        if (newThickness.HasValue) thickness = Mathf.Max(0f, newThickness.Value);
        if (newColor.HasValue) lineColor = newColor.Value;
        if (newLifeTime.HasValue) lifeTime = newLifeTime.Value;

        if (uiLine)
        {
            uiLine.thickness = thickness;
            uiLine.color = lineColor;
        }
    }

    public void DrawPath(List<Vector2Int> cells)
    {
        if (cells == null || cells.Count < 2 || uiLine == null) return;

        var pts = new List<Vector2>(cells.Count);
        for (int i = 0; i < cells.Count; i++)
        {
            var rc = cells[i];
            pts.Add(PointOf(rc.x, rc.y)); // (r,c) -> local point
        }

        uiLine.thickness = thickness;
        uiLine.color = lineColor;
        uiLine.SetPoints(pts);

        if (lifeTime > 0f)
        {
            StopAllCoroutines();
            StartCoroutine(ClearAfter(lifeTime));
        }
    }

    public void ClearNow()
    {
        StopAllCoroutines();
        if (uiLine) uiLine.SetPoints(null);
    }

    System.Collections.IEnumerator ClearAfter(float t)
    {
        yield return new WaitForSeconds(t);
        if (uiLine) uiLine.SetPoints(null);
    }

    // ======= Geometry =======

    // Trả tâm vẽ theo hệ có border: r ∈ [0..rows+1], c ∈ [0..cols+1]
    Vector2 PointOf(int r, int c)
    {
        // Trong bảng
        if (r >= 1 && r <= rows && c >= 1 && c <= cols)
        {
            if (preferIconCenter && tiles != null &&
                tiles[r, c] != null && !tiles[r, c].removed && tiles[r, c].icon != null)
            {
                return IconCenterLocal(tiles[r, c].icon.rectTransform);
            }
            return new Vector2(colX[c], rowY[r]);
        }

        // Ngoài biên: dùng nửa bước ngoài cùng
        float x = (c == 0) ? (colX[1] - stepX * 0.5f)
                 : (c == cols + 1 ? (colX[cols] + stepX * 0.5f) : colX[Mathf.Clamp(c, 1, cols)]);
        float y = (r == 0) ? (rowY[1] + stepY * 0.5f)
                 : (r == rows + 1 ? (rowY[rows] - stepY * 0.5f) : rowY[Mathf.Clamp(r, 1, rows)]);

        x = Mathf.Clamp(x, minX, maxX);
        y = Mathf.Clamp(y, minY, maxY);
        return new Vector2(x, y);
    }

    Vector2 IconCenterLocal(RectTransform icon)
    {
        if (!icon) return Vector2.zero;
        Vector3 world = icon.TransformPoint(icon.rect.center);
        return ((RectTransform)transform).InverseTransformPoint(world);
    }

    Vector2 SafeCenterLocal(int r, int c)
    {
        if (tiles != null && r >= 1 && r <= rows && c >= 1 && c <= cols)
        {
            if (tiles[r, c] != null && tiles[r, c].icon != null)
                return IconCenterLocal(tiles[r, c].icon.rectTransform);

            var rt = tiles[r, c]?.GetComponent<RectTransform>();
            if (rt != null)
            {
                Vector3 world = rt.TransformPoint(rt.rect.center);
                return ((RectTransform)transform).InverseTransformPoint(world);
            }
        }
        // fallback: ước tính theo grid
        if (colX != null && rowY != null &&
            c >= 1 && c <= cols && r >= 1 && r <= rows)
            return new Vector2(colX[c], rowY[r]);

        return Vector2.zero;
    }

    float EstimateStep(float[] arr) // mảng 1-based
    {
        if (arr == null || arr.Length <= 2)
        {
            return glg ? (arr == colX ? (glg.cellSize.x + glg.spacing.x)
                                      : (glg.cellSize.y + glg.spacing.y))
                       : 0f;
        }

        float sum = 0; int cnt = 0;
        for (int i = 1; i < arr.Length - 1; i++)
        {
            float d = arr[i + 1] - arr[i];
            if (Mathf.Abs(d) > 0.001f) { sum += Mathf.Abs(d); cnt++; }
        }
        if (cnt == 0)
        {
            return glg ? (arr == colX ? (glg.cellSize.x + glg.spacing.x)
                                      : (glg.cellSize.y + glg.spacing.y))
                       : 0f;
        }
        return sum / cnt;
    }

    void AlignLayerToBoard()
    {
        var layer = (RectTransform)transform;
        if (!boardArea || !layer) return;

        layer.anchorMin = boardArea.anchorMin;
        layer.anchorMax = boardArea.anchorMax;
        layer.pivot = boardArea.pivot;
        layer.sizeDelta = boardArea.sizeDelta;
        layer.anchoredPosition = boardArea.anchoredPosition;
        layer.localScale = Vector3.one; // tránh méo thickness
    }
}
