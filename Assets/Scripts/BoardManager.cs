using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;  // GridLayoutGroup, LayoutRebuilder

public class BoardManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public RectTransform boardArea;         // GameObject có GridLayoutGroup
    public GameObject tilePrefab;           // Prefab 1 ô, có script Tile và Image icon
    public int rows = 10, cols = 6;
    public float spacing = 10f;

    [Header("Icons (ô)")]
    public Sprite[] sprites;                // Chỉ cần sprites[0] cho bản test

    [Header("Draw (Path)")]
    public PathDrawerIconClamp pathDrawer;  // Kéo PathLayer (có UILine + PathDrawerIconClamp) vào

    private Tile[,] tiles;  // ma trận [R, C] có border
    private int R, C;
    private Tile firstSelected;

    void Start()
    {
        BuildBoardOneIcon(); // 🔧 bản test: toàn bộ ô đều cùng 1 icon
    }

    /// <summary>
    /// Build board đủ rows x cols, TẤT CẢ ô dùng sprites[0] để dễ test path & clamp.
    /// </summary>
    void BuildBoardOneIcon()
    {
        R = rows + 2;   // có border ngoài
        C = cols + 2;
        tiles = new Tile[R, C];

        // Thiết lập GridLayoutGroup
        var grid = boardArea.GetComponent<GridLayoutGroup>();
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = cols;
        grid.spacing = new Vector2(spacing, spacing);

        // Dùng MỘT icon cho toàn bộ board (id = 0)
        int id = 0;
        for (int r = 1; r <= rows; r++)
        {
            for (int c = 1; c <= cols; c++)
            {
                var go = Instantiate(tilePrefab, boardArea);
                var t = go.GetComponent<Tile>();
                t.Setup(r, c, id, sprites != null && sprites.Length > 0 ? sprites[0] : null);
                t.onClicked = OnTileClicked;
                tiles[r, c] = t;
            }
        }

        // Ép layout cập nhật trước khi init PathDrawer
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(boardArea);

        if (pathDrawer != null)
        {
            pathDrawer.Init(tiles, rows, cols);
            pathDrawer.transform.SetAsLastSibling(); // PathLayer vẽ trên cùng
        }
    }

    // ================= CLICK HANDLER =================
    void OnTileClicked(Tile t)
    {
        if (t == null || t.removed) return;

        if (firstSelected == null) { firstSelected = t; return; }
        if (t == firstSelected) { firstSelected = null; return; }

        // Vì toàn board 1 icon, điều kiện id == nhau luôn đúng -> chỉ để test path
        if (t.id == firstSelected.id && TryGetPath(firstSelected, t, out var path))
        {
            if (pathDrawer != null) pathDrawer.DrawPath(path);
            firstSelected.Clear(); t.Clear(); // có thể tắt nếu chỉ muốn vẽ mà không xóa icon
        }

        firstSelected = null;
    }

    // ================= PATHFINDING (≤2 rẽ, có border) =================
    static readonly int[] dr = { -1, 1, 0, 0 };
    static readonly int[] dc = { 0, 0, -1, 1 };

    struct Node { public int r, c, dir, turns; public Node(int r, int c, int dir, int turns) { this.r = r; this.c = c; this.dir = dir; this.turns = turns; } }
    struct Prev { public int pr, pc, pdir; public bool has; }

    bool InBounds(int r, int c) => r >= 0 && r < R && c >= 0 && c < C;

    bool IsWalkable(int r, int c, Tile target)
    {
        if (r == target.row && c == target.col) return true;
        var t = tiles[r, c];
        return t == null || t.removed;
    }

    bool TryGetPath(Tile a, Tile b, out List<Vector2Int> cells)
    {
        cells = null;
        var q = new Queue<Node>();
        var visited = new bool[R, C, 4];
        var prev = new Prev[R, C, 4];

        // xuất phát từ 4 hướng kề a
        for (int d = 0; d < 4; d++)
        {
            int nr = a.row + dr[d], nc = a.col + dc[d];
            if (InBounds(nr, nc) && IsWalkable(nr, nc, b))
            {
                q.Enqueue(new Node(nr, nc, d, 0));
                visited[nr, nc, d] = true;
                prev[nr, nc, d] = new Prev { pr = a.row, pc = a.col, pdir = d, has = true };
            }
        }

        Node? end = null;
        while (q.Count > 0 && end == null)
        {
            var cur = q.Dequeue();
            if (cur.r == b.row && cur.c == b.col) { end = cur; break; }

            for (int nd = 0; nd < 4; nd++)
            {
                int turns = cur.turns + (nd == cur.dir ? 0 : 1);
                if (turns > 2) continue;

                int nr = cur.r + dr[nd], nc = cur.c + dc[nd];
                if (!InBounds(nr, nc) || visited[nr, nc, nd]) continue;

                if (IsWalkable(nr, nc, b))
                {
                    visited[nr, nc, nd] = true;
                    prev[nr, nc, nd] = new Prev { pr = cur.r, pc = cur.c, pdir = cur.dir, has = true };
                    q.Enqueue(new Node(nr, nc, nd, turns));
                }
            }
        }
        if (end == null) return false;

        // reconstruct
        var raw = new List<Vector2Int>();
        var e = end.Value;
        raw.Add(new Vector2Int(b.row, b.col));

        int r0 = e.r, c0 = e.c, dir = e.dir;
        var p = prev[r0, c0, dir];
        while (p.has)
        {
            raw.Add(new Vector2Int(r0, c0));
            r0 = p.pr; c0 = p.pc; dir = p.pdir;
            p = prev[r0, c0, dir];
            if (r0 == a.row && c0 == a.col) break;
        }
        raw.Add(new Vector2Int(a.row, a.col));
        raw.Reverse();

        // simplify: giữ 2 đầu + các góc rẽ
        var simplified = new List<Vector2Int>();
        simplified.Add(raw[0]);
        for (int i = 1; i < raw.Count - 1; i++)
        {
            var p0 = raw[i - 1]; var p1 = raw[i]; var p2 = raw[i + 1];
            bool straightRow = (p0.x == p1.x && p1.x == p2.x);
            bool straightCol = (p0.y == p1.y && p1.y == p2.y);
            if (!(straightRow || straightCol)) simplified.Add(p1);
        }
        simplified.Add(raw[^1]);

        cells = simplified;
        return true;
    }
}
