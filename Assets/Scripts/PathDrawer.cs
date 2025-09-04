using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PathDrawerIconClamp : MonoBehaviour
{
    [Header("Refs")]
    public RectTransform boardArea;   // nơi có GridLayoutGroup
    public UILine uiLine;             // kéo thả component UILine ở PathLayer

    [Header("Style")]
    public float thickness = 16f;
    public float lifeTime = 0.6f;
    public float edgePadding = 4f;
    public bool pixelSnap = true;

    // cache
    private Tile[,] tiles; private int rows, cols;
    private GridLayoutGroup glg;

    // Tâm từng cột/hàng (local trong PathLayer)
    private float[] colX;  // size cols+1, 1-based
    private float[] rowY;  // size rows+1, 1-based
    private float stepX, stepY;

    // biên BoardArea (local trong PathLayer)
    private float minX, maxX, minY, maxY;

    // ================= INIT =================
    public void Init(Tile[,] tiles, int rows, int cols)
    {
        this.tiles = tiles; this.rows = rows; this.cols = cols;

        glg = boardArea.GetComponent<GridLayoutGroup>();

        // 1) Đồng bộ PathLayer trùng khít BoardArea
        AlignLayerToBoard();

        // 2) Lấy tâm icon từng ô theo LOCAL của PathLayer
        colX = new float[cols + 1];
        rowY = new float[rows + 1];

        for (int c = 1; c <= cols; c++)
            colX[c] = SafeIconCenterLocal(1, c).x;
        for (int r = 1; r <= rows; r++)
            rowY[r] = SafeIconCenterLocal(r, 1).y;

        // 3) Bước lưới
        stepX = EstimateStep(colX);
        stepY = Mathf.Abs(EstimateStep(rowY)); // y giảm dần theo hàng

        // 4) Biên Board theo 1/2 bước từ mép ngoài cùng
        minX = colX[1] - stepX * 0.5f;
        maxX = colX[cols] + stepX * 0.5f;
        maxY = rowY[1] + stepY * 0.5f;   // top
        minY = rowY[rows] - stepY * 0.5f; // bottom

        // 5) Clamp thêm theo chính rect của BoardArea (an toàn kép)
        var bRect = LocalRectIn(boardArea, (RectTransform)transform);
        minX = Mathf.Max(minX, bRect.xMin + edgePadding);
        maxX = Mathf.Min(maxX, bRect.xMax - edgePadding);
        minY = Mathf.Max(minY, bRect.yMin + edgePadding);
        maxY = Mathf.Min(maxY, bRect.yMax - edgePadding);

        // đồng bộ style cho UILine
        if (uiLine)
        {
            uiLine.thickness = thickness;
            uiLine.pixelSnap = pixelSnap;
            uiLine.cornerPatch = true;
            uiLine.endpointCapRatio = 0.5f;
        }
    }

    // ================= DRAW (gọi từ BoardManager) =================
    public void DrawPath(List<Vector2Int> cells)
    {
        if (cells == null || cells.Count < 2 || uiLine == null) return;

        // cell -> điểm local (snap trước)
        var points = new List<Vector2>(cells.Count);
        for (int i = 0; i < cells.Count; i++)
        {
            var p = PointOf(cells[i].x, cells[i].y);
            if (pixelSnap) p = new Vector2(Mathf.Round(p.x), Mathf.Round(p.y));
            points.Add(p);
        }

        // vẽ bằng mesh
        uiLine.thickness = thickness;
        uiLine.SetPoints(points);

        // auto clear
        if (lifeTime > 0f) { StopAllCoroutines(); StartCoroutine(ClearAfter(lifeTime)); }
    }

    System.Collections.IEnumerator ClearAfter(float t)
    {
        yield return new WaitForSeconds(t);
        if (uiLine) uiLine.SetPoints(null);
    }

    // ================= GEOMETRY HELPERS =================

    // Tâm điểm để vẽ theo tọa độ có border: r ∈ [0..rows+1], c ∈ [0..cols+1]
    Vector2 PointOf(int r, int c)
    {
        // Trong bảng
        if (r >= 1 && r <= rows && c >= 1 && c <= cols)
        {
            // Nếu còn tile → lấy TÂM ICON thật; nếu đã clear → xài colX/rowY đã cache
            if (tiles[r, c] != null && !tiles[r, c].removed && tiles[r, c].icon != null)
                return IconCenterLocal(tiles[r, c].icon.rectTransform);
            return new Vector2(colX[c], rowY[r]);
        }

        // Biên trái/phải: x dịch 1/2 bước, y giữ theo hàng
        float x = (c == 0) ? (colX[1] - stepX * 0.5f)
                           : (c == cols + 1 ? (colX[cols] + stepX * 0.5f) : colX[Mathf.Clamp(c, 1, cols)]);
        // Biên trên/dưới: y dịch 1/2 bước, x giữ theo cột
        float y = (r == 0) ? (rowY[1] + stepY * 0.5f)
                           : (r == rows + 1 ? (rowY[rows] - stepY * 0.5f) : rowY[Mathf.Clamp(r, 1, rows)]);

        // Clamp vào board (không vượt quá)
        x = Mathf.Clamp(x, minX + thickness * 0.5f, maxX - thickness * 0.5f);
        y = Mathf.Clamp(y, minY + thickness * 0.5f, maxY - thickness * 0.5f);
        return new Vector2(x, y);
    }

    // Trả về tâm ICON trong LOCAL của PathLayer
    Vector2 IconCenterLocal(RectTransform icon)
    {
        Vector3 world = icon.TransformPoint(icon.rect.center);
        return ((RectTransform)transform).InverseTransformPoint(world);
    }

    // Nếu icon null/trống → lấy tâm của Tile (an toàn hơn)
    Vector2 SafeIconCenterLocal(int r, int c)
    {
        if (tiles[r, c] != null && tiles[r, c].icon != null)
            return IconCenterLocal(tiles[r, c].icon.rectTransform);

        var rt = tiles[r, c].GetComponent<RectTransform>();
        Vector3 world = rt.TransformPoint(rt.rect.center);
        return ((RectTransform)transform).InverseTransformPoint(world);
    }

    // Ước lượng bước theo sai phân trung bình (bỏ trường hợp bằng 0)
    float EstimateStep(float[] arr) // 1-based
    {
        float sum = 0; int cnt = 0;
        for (int i = 1; i < arr.Length - 1; i++)
        {
            float d = arr[i + 1] - arr[i];
            if (Mathf.Abs(d) > 0.01f) { sum += Mathf.Abs(d); cnt++; }
        }
        if (cnt == 0)
        {
            // fallback theo GLG
            return (arr == colX) ? (glg.cellSize.x + glg.spacing.x)
                                 : (glg.cellSize.y + glg.spacing.y);
        }
        return sum / cnt;
    }

    // Lấy rect của boardArea trong LOCAL của PathLayer
    Rect LocalRectIn(RectTransform a, RectTransform container)
    {
        Vector3 p0 = a.TransformPoint(new Vector3(a.rect.xMin, a.rect.yMin));
        Vector3 p1 = a.TransformPoint(new Vector3(a.rect.xMax, a.rect.yMax));
        Vector2 min = container.InverseTransformPoint(p0);
        Vector2 max = container.InverseTransformPoint(p1);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    void AlignLayerToBoard()
    {
        var layer = (RectTransform)transform;
        layer.anchorMin = boardArea.anchorMin;
        layer.anchorMax = boardArea.anchorMax;
        layer.pivot = boardArea.pivot;
        layer.sizeDelta = boardArea.sizeDelta;
        layer.anchoredPosition = boardArea.anchoredPosition;
    }
}
