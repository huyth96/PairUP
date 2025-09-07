using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BFS ≤ 2 rẽ, toạ độ dùng hệ 1..rows / 1..cols cho ô thật,
/// và cho phép đi "ra biên" 0 và rows+1 / cols+1.
/// walkable(r,c): true nếu (r,c) trống hoặc là đích.
/// </summary>
public sealed class GridPathFinderBFS : IPathFinder
{
    readonly int rows, cols;                 // số ô thật (không tính border)
    readonly System.Func<int, int, bool> walk; // IsWalkable(r,c)

    static readonly int[] dr = { -1, 1, 0, 0 };
    static readonly int[] dc = { 0, 0, -1, 1 };

    struct Node { public int r, c, dir, turns; public Node(int r, int c, int dir, int turns) { this.r = r; this.c = c; this.dir = dir; this.turns = turns; } }
    struct Prev { public int pr, pc, pdir; public bool has; }

    public GridPathFinderBFS(int rows, int cols, System.Func<int, int, bool> isWalkable)
    {
        this.rows = rows; this.cols = cols; this.walk = isWalkable;
    }

    bool InBounds(int r, int c) => r >= 0 && r <= rows + 1 && c >= 0 && c <= cols + 1;

    public bool TryGetPath((int r, int c) a, (int r, int c) b, out List<Vector2Int> path)
    {
        Debug.Log($"PathFinder.TryGetPath from {a} to {b}");
        path = null;

        // Cho phép bước vào ô đích
        bool WalkableWithTarget(int r, int c)
            => (r == b.r && c == b.c) || walk(r, c);

        var q = new Queue<Node>();
        var visited = new bool[rows + 3, cols + 3, 4];
        var prev = new Prev[rows + 3, cols + 3, 4];

        for (int d = 0; d < 4; d++)
        {
            int nr = a.r + dr[d], nc = a.c + dc[d];
            if (InBounds(nr, nc) && WalkableWithTarget(nr, nc))
            {
                q.Enqueue(new Node(nr, nc, d, 0));
                visited[nr, nc, d] = true;
                prev[nr, nc, d] = new Prev { pr = a.r, pc = a.c, pdir = d, has = true };
            }
        }

        Node? end = null;
        while (q.Count > 0 && end == null)
        {
            var cur = q.Dequeue();
            if (cur.r == b.r && cur.c == b.c) { end = cur; break; }

            for (int nd = 0; nd < 4; nd++)
            {
                int turns = cur.turns + (nd == cur.dir ? 0 : 1);
                if (turns > 2) continue;

                int nr = cur.r + dr[nd], nc = cur.c + dc[nd];
                if (!InBounds(nr, nc) || visited[nr, nc, nd]) continue;

                if (WalkableWithTarget(nr, nc))
                {
                    visited[nr, nc, nd] = true;
                    prev[nr, nc, nd] = new Prev { pr = cur.r, pc = cur.c, pdir = cur.dir, has = true };
                    q.Enqueue(new Node(nr, nc, nd, turns));
                }
            }
        }
        if (end == null) return false;

        // reconstruct + simplify (như cũ)
        var raw = new List<Vector2Int>();
        var e = end.Value;
        raw.Add(new Vector2Int(b.r, b.c));

        int r0 = e.r, c0 = e.c, dir = e.dir;
        var p = prev[r0, c0, dir];
        while (p.has)
        {
            raw.Add(new Vector2Int(r0, c0));
            r0 = p.pr; c0 = p.pc; dir = p.pdir;
            p = prev[r0, c0, dir];
            if (r0 == a.r && c0 == a.c) break;
        }
        raw.Add(new Vector2Int(a.r, a.c));
        raw.Reverse();

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

        path = simplified;
        return true;
    }
}
