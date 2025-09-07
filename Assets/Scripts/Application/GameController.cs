using UnityEngine;

public class GameController : MonoBehaviour
{
    [Header("Scene Wiring")]
    [SerializeField] BoardPresenter board;
    [SerializeField] private UILinePathRendererAdapter pathRenderer; // <- đổi kiểu
    [SerializeField] ScoreView scoreView;
    [SerializeField] TimerView timerView;
    [SerializeField] GameTimerService timerService;    // component cùng GameObject hoặc ở đâu cũng được

    // Services
    IPathRenderer _renderer;
    IPathFinder _finder;
    IScoreService _score;

    // State
    ITileView _first;

    void Awake()
    {
        _renderer = pathRenderer;          // không cần cast
        _score = new ScoreService();
        if (scoreView) scoreView.Bind(_score);
        if (timerView && timerService) timerView.Bind(timerService);
    }
    void Start()
    {
        var (r, c) = board.Size;
        _finder = new GridPathFinderBFS(r, c, (row, col) => board.IsWalkable(row, col));
        board.OnTileClicked += HandleTileClicked;

        if (timerService) timerService.StartTimer(60f);
    }

    void HandleTileClicked(ITileView t)
    {
        if (t == null || t.IsRemoved)
        {
            Debug.Log("Clicked null or removed tile");
            return;
        }

        Debug.Log($"GameController.HandleTileClicked: {t.Row},{t.Col}, id={t.Id}");

        if (_first == null)
        {
            _first = t;
            Debug.Log($"First selected: {t.Row},{t.Col}");
            if (_first is Tile tileView1) tileView1.SetSelected(true);
            return;
        }

        if (_first == t)
        {
            Debug.Log("Clicked same tile again → reset");
            if (_first is Tile tileView2) tileView2.SetSelected(false);
            _first = null;
            return;
        }

        Debug.Log($"Second selected: {_first.Row},{_first.Col} and {t.Row},{t.Col}");

        if (_first.Id == t.Id &&
     _finder.TryGetPath((_first.Row, _first.Col), (t.Row, t.Col), out var path))
        {
            Debug.Log("Match found, path length=" + path.Count);
            _renderer?.DrawPath(path);

            // Gọi animation clear thay vì ClearTile ngay lập tức
            if (_first is Tile tileA) tileA.PlayClearAnimation();
            if (t is Tile tileB) tileB.PlayClearAnimation();

            _score.Add(10);
        }
        else
        {
            Debug.Log("No match or path not found");

            // reset hiệu ứng khi không match
            if (_first is Tile tileView3) tileView3.SetSelected(false);
            if (t is Tile tileView4) tileView4.SetSelected(false);
        }

        _first = null;
    }
}
