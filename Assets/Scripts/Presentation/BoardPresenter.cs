using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events; // optional
using UnityEngine.UIElements; // (không bắt buộc)
using System;

public class BoardPresenter : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] RectTransform boardArea;
    [SerializeField] GameObject tilePrefab;
    [SerializeField] int rows = 10, cols = 6;
    [SerializeField] float spacing = 10f;

    [Header("Icons")]
    [SerializeField] Sprite[] sprites;

    [Header("Path Layer")]
    [SerializeField] PathDrawerIconClamp pathDrawer;

    public event Action<ITileView> OnTileClicked;

    private Tile[,] tiles; // [1..rows, 1..cols]

    void Start()
    {
        BuildBoard_OneIcon(); // demo
    }

    public (int rows, int cols) Size => (rows, cols);

    void BuildBoard_OneIcon()
    {
        tiles = new Tile[rows + 1, cols + 1];

        var grid = boardArea.GetComponent<GridLayoutGroup>();
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = cols;
        grid.spacing = new Vector2(spacing, spacing);

        int id = 0;
        for (int r = 1; r <= rows; r++)
            for (int c = 1; c <= cols; c++)
            {
                var go = Instantiate(tilePrefab, boardArea);
                var t = go.GetComponent<Tile>();
                t.Setup(r, c, id, (sprites != null && sprites.Length > 0) ? sprites[0] : null);
                t.OnClicked += tv => OnTileClicked?.Invoke(tv);
                tiles[r, c] = t;
            }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(boardArea);

        if (pathDrawer != null)
        {
            pathDrawer.Init(tiles, rows, cols);
            pathDrawer.transform.SetAsLastSibling();
        }
    }

    // Cho pathfinder: (r,c) ngoài 1..rows/1..cols coi như trống để đi ra biên
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
}
