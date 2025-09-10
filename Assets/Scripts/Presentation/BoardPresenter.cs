using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class BoardPresenter : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] RectTransform boardArea;
    [SerializeField] GameObject tilePrefab;
    [SerializeField] int rows = 10, cols = 6;
    [SerializeField] float spacing = 10f;

    [Header("Icons")]
    [SerializeField] Sprite[] sprites;   // cần >= 30 sprite nếu rows=10, cols=6

    [Header("Path Layer")]
    [SerializeField] PathDrawerIconClamp pathDrawer;

    public event Action<ITileView> OnTileClicked;

    private Tile[,] tiles; // [0..rows+1, 0..cols+1] có border rỗng

    void Start()
    {
        BuildBoard_UniquePairs(); // gọi hàm mới
    }

    public (int rows, int cols) Size => (rows, cols);

    /// <summary>
    /// Sinh board Onet chuẩn: có border rỗng, mỗi icon xuất hiện đúng 2 lần
    /// </summary>
    void BuildBoard_UniquePairs()
    {
        int totalTiles = rows * cols;
        int pairCount = totalTiles / 2; // 10x6 = 60 => 30 cặp

        if (sprites == null || sprites.Length < pairCount)
        {
            Debug.LogError($"Cần ít nhất {pairCount} sprite để build unique pairs!");
            return;
        }

        // Tạo mảng với border rỗng
        tiles = new Tile[rows + 2, cols + 2];

        // Setup GridLayoutGroup
        var grid = boardArea.GetComponent<GridLayoutGroup>();
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = cols;
        grid.spacing = new Vector2(spacing, spacing);

        // Pool: mỗi sprite xuất hiện 2 lần
        var pool = new List<(int id, Sprite icon)>();
        for (int i = 0; i < pairCount; i++)
        {
            pool.Add((i, sprites[i]));
            pool.Add((i, sprites[i]));
        }

        // Shuffle pool
        for (int i = 0; i < pool.Count; i++)
        {
            int j = UnityEngine.Random.Range(i, pool.Count);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        // Sinh tile trong khoảng [1..rows], [1..cols]
        int index = 0;
        for (int r = 1; r <= rows; r++)
        {
            for (int c = 1; c <= cols; c++)
            {
                var (id, icon) = pool[index++];
                var go = Instantiate(tilePrefab, boardArea);
                var t = go.GetComponent<Tile>();
                t.Setup(r, c, id, icon);
                t.OnClicked += tv => OnTileClicked?.Invoke(tv);
                tiles[r, c] = t;
            }
        }

        // Force rebuild layout để grid sắp xếp chuẩn
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(boardArea);

        // Khởi tạo PathDrawer
        if (pathDrawer != null)
        {
            pathDrawer.Init(tiles, rows, cols);
            pathDrawer.transform.SetAsLastSibling();
        }

        Debug.Log($"Board {rows}x{cols} đã build {pairCount} cặp (unique, mỗi icon 2 lần).");
    }

    // Cho pathfinder: (r,c) ngoài 1..rows/1..cols coi như trống (đi ra biên)
    public bool IsWalkable(int r, int c)
    {
        if (r < 1 || r > rows || c < 1 || c > cols) return true;
        var t = tiles[r, c];
        return t == null || t.IsRemoved;
    }

    public void ClearTile(ITileView tv)
    {
        if (tv is Tile t) t.Hide();
    }

    public bool AllTilesCleared()
    {
        foreach (var tile in tiles)
        {
            if (tile != null && !tile.IsRemoved)
                return false;
        }
        return true;

    }
    // BoardPresenter.cs (thêm vào class)
    public IEnumerable<Tile> ActiveTiles()
    {
        if (tiles == null) yield break;
        for (int r = 1; r <= rows; r++)
            for (int c = 1; c <= cols; c++)
            {
                var t = tiles[r, c];
                if (t != null && !t.IsRemoved) yield return t;
            }
    }

    /// <summary>
    /// Dựa vào pathfinder: nếu tồn tại 2 ô có cùng Id mà nối được (≤2 rẽ) → true
    /// </summary>
    public bool HasAnyMove(IPathFinder finder)
    {
        // Gom theo Id
        var byId = new Dictionary<int, List<Tile>>();
        foreach (var t in ActiveTiles())
        {
            if (!byId.TryGetValue(t.Id, out var list))
                list = byId[t.Id] = new List<Tile>();
            list.Add(t);
        }

        // Duyệt từng Id (chỉ có 0 hoặc 2 phần tử nếu bạn luôn xóa theo cặp)
        foreach (var kv in byId)
        {
            var list = kv.Value;
            if (list.Count < 2) continue;

            // Nếu có nhiều hơn 2 do thiết kế khác, duyệt mọi cặp nhanh gọn
            for (int i = 0; i < list.Count; i++)
                for (int j = i + 1; j < list.Count; j++)
                {
                    var a = list[i]; var b = list[j];
                    if (finder.TryGetPath((a.Row, a.Col), (b.Row, b.Col), out _))
                        return true; // tìm thấy ít nhất 1 nước đi
                }
        }
        return false;
    }
    // BoardPresenter.cs (thêm vào class)
    public void ShuffleUntilMovable(IPathFinder finder, int maxAttempts = 20)
    {
        if (finder == null) return;

        // Nếu đã có nước đi thì khỏi xáo
        if (HasAnyMove(finder)) return;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            ShuffleOnce();

            // Nếu có Path → ổn
            if (HasAnyMove(finder))
            {
                // nếu pathDrawer dùng tâm icon, có thể Reinit nhẹ
                pathDrawer?.Reinit();
                Debug.Log($"Shuffle thành công ở lần {attempt}.");
                return;
            }
        }

        Debug.LogWarning($"Shuffle {maxAttempts} lần vẫn bế tắc. Cân nhắc rebuild board.");
    }

    /// <summary>
    /// Xáo 1 lượt: tráo Id + Icon giữa các ô còn sống. 
    /// Bảo toàn quy tắc “mỗi icon xuất hiện 2 lần” vì ta chỉ tráo chéo.
    /// </summary>
    public void ShuffleOnce()
    {
        // Thu thập Id + Sprite của tất cả ô còn lại
        var alive = new List<Tile>();
        var bag = new List<(int id, Sprite icon)>();

        foreach (var t in ActiveTiles())
        {
            alive.Add(t);
            bag.Add((t.Id, t.Icon ? t.Icon.sprite : null));
        }

        if (alive.Count <= 2) return; // 0 hoặc 1 cặp thì khỏi xáo

        // Fisher–Yates
        for (int i = bag.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (bag[i], bag[j]) = (bag[j], bag[i]);
        }

        // Gán lại Id + Sprite (không đổi vị trí/transform)
        for (int i = 0; i < alive.Count; i++)
        {
            var t = alive[i];
            var (id, icon) = bag[i];
            t.SetIdAndIcon(id, icon);   // <- thêm API nhỏ trong Tile (bên dưới)
        }

        // Nếu line layer lấy tâm icon để vẽ, call Reinit (hoặc để lần kiểm move gọi)
        // pathDrawer?.Reinit();
    }
    // BoardPresenter.cs — thêm vào class
    public bool TryFindFirstHint(IPathFinder finder, out Tile a, out Tile b, out List<Vector2Int> path)
    {
        a = b = null; path = null;
        // nhóm theo Id
        var byId = new Dictionary<int, List<Tile>>();
        for (int r = 1; r <= rows; r++)
            for (int c = 1; c <= cols; c++)
            {
                var t = tiles[r, c];
                if (t != null && !t.IsRemoved)
                {
                    if (!byId.TryGetValue(t.Id, out var list)) list = byId[t.Id] = new List<Tile>();
                    list.Add(t);
                }
            }

        // duyệt từng nhóm, thử mọi cặp trong nhóm
        foreach (var kv in byId)
        {
            var list = kv.Value;
            for (int i = 0; i < list.Count; i++)
                for (int j = i + 1; j < list.Count; j++)
                {
                    var t1 = list[i]; var t2 = list[j];
                    if (finder.TryGetPath((t1.Row, t1.Col), (t2.Row, t2.Col), out var p))
                    {
                        a = t1; b = t2; path = p;
                        return true;
                    }
                }
        }
        return false;
    }

}

