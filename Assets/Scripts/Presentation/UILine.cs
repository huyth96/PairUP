using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class UILine : MaskableGraphic
{
    [Header("Style")]
    public float thickness = 16f;

    private readonly List<Vector2> points = new List<Vector2>();

    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false;
    }

    public void SetPoints(IList<Vector2> pts)
    {
        points.Clear();
        if (pts != null && pts.Count > 0) points.AddRange(pts);
        SetVerticesDirty();
        SetLayoutDirty();
    }

    public override Texture mainTexture => s_WhiteTexture;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (points.Count < 2 || thickness <= 0f) return;

        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector2 a = points[i];
            Vector2 b = points[i + 1];

            bool vertical = Mathf.Abs(a.x - b.x) < 0.001f;
            bool horizontal = Mathf.Abs(a.y - b.y) < 0.001f;

            if (vertical)
            {
                float y0 = Mathf.Min(a.y, b.y);
                float y1 = Mathf.Max(a.y, b.y);
                AddRect(vh, new Rect(a.x - thickness * 0.5f, y0, thickness, Mathf.Max(0f, y1 - y0)));
            }
            else if (horizontal)
            {
                float x0 = Mathf.Min(a.x, b.x);
                float x1 = Mathf.Max(a.x, b.x);
                AddRect(vh, new Rect(x0, a.y - thickness * 0.5f, Mathf.Max(0f, x1 - x0), thickness));
            }
            else
            {
                // fallback: tạo via = vuông góc
                Vector2 via = new Vector2(b.x, a.y);
                // đoạn 1
                float x0 = Mathf.Min(a.x, via.x);
                float x1 = Mathf.Max(a.x, via.x);
                AddRect(vh, new Rect(x0, a.y - thickness * 0.5f, Mathf.Max(0f, x1 - x0), thickness));
                // đoạn 2
                float y0 = Mathf.Min(via.y, b.y);
                float y1 = Mathf.Max(via.y, b.y);
                AddRect(vh, new Rect(b.x - thickness * 0.5f, y0, thickness, Mathf.Max(0f, y1 - y0)));
            }
        }
    }

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
