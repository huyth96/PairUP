using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Vẽ polyline (UI) bằng mesh, không dùng sprite.
/// Giả định các đoạn là ngang/dọc (kiểu Onet).
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class UILine : MaskableGraphic
{
    [Header("Style")]
    public float thickness = 16f;
    [Range(0f, 1f)] public float endpointCapRatio = 0.5f; // cap cho 2 đầu mút
    public bool cornerPatch = true;                       // vá góc bằng ô vuông size=thickness
    public bool pixelSnap = true;

    private readonly List<Vector2> points = new List<Vector2>();  // local trong RectTransform của chính UILine

    /// <summary>Thay toàn bộ danh sách điểm (đã snap local trước nếu muốn).</summary>
    public void SetPoints(IList<Vector2> pts)
    {
        points.Clear();
        if (pts != null && pts.Count > 0) points.AddRange(pts);
        SetVerticesDirty();
        SetLayoutDirty();
    }

    // Không cần texture
    public override Texture mainTexture => s_WhiteTexture;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (points.Count < 2 || thickness <= 0f) return;

        // 1) copy & snap endpoints
        var P = new List<Vector2>(points.Count);
        for (int i = 0; i < points.Count; i++)
        {
            var p = points[i];
            P.Add(pixelSnap ? new Vector2(Mathf.Round(p.x), Mathf.Round(p.y)) : p);
        }

        float cap = thickness * Mathf.Clamp01(endpointCapRatio);

        // 2) vẽ từng đoạn (cap chỉ ở 2 đầu mút)
        for (int i = 0; i < P.Count - 1; i++)
        {
            Vector2 a = P[i], b = P[i + 1];
            bool vertical = Mathf.Abs(a.x - b.x) < 0.01f;
            bool horizontal = Mathf.Abs(a.y - b.y) < 0.01f;

            float capStart = (i == 0) ? cap : 0f;
            float capEnd = (i == P.Count - 2) ? cap : 0f;

            if (vertical)
            {
                float y0 = (a.y <= b.y) ? a.y - capStart : b.y - capStart;
                float y1 = (a.y <= b.y) ? b.y + capEnd : a.y + capEnd;
                AddRect(vh, new Rect(a.x - thickness * 0.5f, y0, thickness, Mathf.Max(0f, y1 - y0)));
            }
            else if (horizontal)
            {
                float x0 = (a.x <= b.x) ? a.x - capStart : b.x - capStart;
                float x1 = (a.x <= b.x) ? b.x + capEnd : a.x + capEnd;
                AddRect(vh, new Rect(x0, a.y - thickness * 0.5f, Mathf.Max(0f, x1 - x0), thickness));
            }
            else
            {
                // bảo hiểm: tách qua "via" vuông góc
                Vector2 via = new Vector2(b.x, a.y);
                if (pixelSnap) via = new Vector2(Mathf.Round(via.x), Mathf.Round(via.y));

                // đoạn 1 (không cap ở via)
                if (Mathf.Abs(a.y - via.y) < 0.01f) // ngang
                {
                    float x0 = Mathf.Min(a.x, via.x) - ((i == 0) ? cap : 0f);
                    float x1 = Mathf.Max(a.x, via.x);
                    AddRect(vh, new Rect(x0, a.y - thickness * 0.5f, Mathf.Max(0f, x1 - x0), thickness));
                }
                else                                // dọc
                {
                    float y0 = Mathf.Min(a.y, via.y) - ((i == 0) ? cap : 0f);
                    float y1 = Mathf.Max(a.y, via.y);
                    AddRect(vh, new Rect(a.x - thickness * 0.5f, y0, thickness, Mathf.Max(0f, y1 - y0)));
                }

                // đoạn 2 (cap ở cuối nếu là cuối polyline)
                if (Mathf.Abs(via.y - b.y) < 0.01f) // ngang
                {
                    float x0 = Mathf.Min(via.x, b.x);
                    float x1 = Mathf.Max(via.x, b.x) + ((i == P.Count - 2) ? cap : 0f);
                    AddRect(vh, new Rect(x0, b.y - thickness * 0.5f, Mathf.Max(0f, x1 - x0), thickness));
                }
                else                                // dọc
                {
                    float y0 = Mathf.Min(via.y, b.y);
                    float y1 = Mathf.Max(via.y, b.y) + ((i == P.Count - 2) ? cap : 0f);
                    AddRect(vh, new Rect(b.x - thickness * 0.5f, y0, thickness, Mathf.Max(0f, y1 - y0)));
                }
            }
        }

        // 3) vá góc: ô vuông ở mỗi khớp bên trong
        if (cornerPatch && P.Count >= 3)
        {
            for (int i = 1; i <= P.Count - 2; i++)
            {
                Vector2 j = P[i];
                AddRect(vh, new Rect(j.x - thickness * 0.5f, j.y - thickness * 0.5f, thickness, thickness));
            }
        }
    }

    // Thêm một rect axis-aligned vào VertexHelper (màu = this.color)
    static readonly Vector2 uv0 = Vector2.zero;
    static readonly Vector2 uv1 = Vector2.right;
    static readonly Vector2 uv2 = Vector2.one;
    static readonly Vector2 uv3 = Vector2.up;

    void AddRect(VertexHelper vh, Rect r)
    {
        int idx = vh.currentVertCount;

        UIVertex v0 = UIVertex.simpleVert; v0.color = color; v0.position = new Vector2(r.xMin, r.yMin); v0.uv0 = uv0;
        UIVertex v1 = UIVertex.simpleVert; v1.color = color; v1.position = new Vector2(r.xMax, r.yMin); v1.uv0 = uv1;
        UIVertex v2 = UIVertex.simpleVert; v2.color = color; v2.position = new Vector2(r.xMax, r.yMax); v2.uv0 = uv2;
        UIVertex v3 = UIVertex.simpleVert; v3.color = color; v3.position = new Vector2(r.xMin, r.yMax); v3.uv0 = uv3;

        vh.AddVert(v0); vh.AddVert(v1); vh.AddVert(v2); vh.AddVert(v3);
        vh.AddTriangle(idx + 0, idx + 1, idx + 2);
        vh.AddTriangle(idx + 2, idx + 3, idx + 0);
    }
}
