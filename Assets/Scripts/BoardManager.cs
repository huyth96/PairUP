using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BoardManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public RectTransform boardArea;     // object có GridLayoutGroup
    public GameObject tilePrefab;
    public int rows = 10, cols = 6;     // số hàng và cột
    public float spacing = 10f;

    [Header("Icons")]
    public Sprite[] sprites;            // kéo icon thú vào đây

    private Tile[,] tiles;

    void Start()
    {
        BuildBoard();
    }

    void BuildBoard()
    {
        tiles = new Tile[rows, cols];

        // tạo list id theo cặp
        List<int> pool = new List<int>();
        int cellCount = rows * cols;
        for (int i = 0; i < cellCount / 2; i++)
        {
            int id = i % sprites.Length;
            pool.Add(id);
            pool.Add(id);
        }

        // shuffle
        for (int i = 0; i < pool.Count; i++)
        {
            int j = Random.Range(i, pool.Count);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        // ép GridLayoutGroup
        var grid = boardArea.GetComponent<GridLayoutGroup>();
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = cols;
        grid.spacing = new Vector2(spacing, spacing);

        // spawn
        int k = 0;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                var go = Instantiate(tilePrefab, boardArea);
                var t = go.GetComponent<Tile>();
                int id = pool[k++];
                t.Setup(r, c, id, sprites[id]);
                t.onClicked = OnTileClicked;
                tiles[r, c] = t;
            }
        }
    }

    Tile firstSelected = null;

    void OnTileClicked(Tile t)
    {
        if (firstSelected == null)
        {
            firstSelected = t;
            // TODO: highlight tile này
        }
        else
        {
            if (t == firstSelected)
            {
                firstSelected = null;
                return;
            }

            // kiểm tra có cùng id không
            if (t.id == firstSelected.id)
            {
                Debug.Log($"Matched: {t.id}");
                firstSelected.Clear();
                t.Clear();
            }

            firstSelected = null;
        }
    }
}
