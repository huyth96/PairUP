using UnityEngine;

public class GameController : MonoBehaviour
{
    [Header("Scene Wiring")]
    [SerializeField] BoardPresenter board;
    [SerializeField] private UILinePathRendererAdapter pathRenderer; // <- đổi kiểu
    [SerializeField] ScoreView scoreView;
    [SerializeField] TimerView timerView;
    [SerializeField] GameTimerService timerService;    
    [SerializeField] ResultView resultView;   

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


        if (timerService)
        {
            timerService.StartTimer(timerService.MaxTime); 
            timerService.OnTimeUp += HandleTimeUp;         
        }
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

            StartCoroutine(ResolveMatchAndMaybeWin((Tile)_first, (Tile)t, 0.2f));


            _score.Add(10);
            // 👇 check win
            if (board.AllTilesCleared())
                OnWin();
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
    private void HandleTimeUp()
    {
        Debug.Log("Time is up → Lose");
        OnLose();
    }
    void OnWin()
    {
        Debug.Log("YOU WIN!");
        HighScoreManager.Instance.TrySetHighScore(_score.Score); // cập nhật trước
        timerService.StopTimer();
        resultView.ShowWin(_score.Score);                        // rồi mới hiển thị
    }


    void OnLose()
    {
        Debug.Log("YOU LOSE!");
        HighScoreManager.Instance.TrySetHighScore(_score.Score);
        resultView.ShowLose(_score.Score);
    }

    private System.Collections.IEnumerator ResolveMatchAndMaybeWin(Tile a, Tile b, float animDuration)
    {
        if (a) a.PlayClearAnimation(animDuration);
        if (b) b.PlayClearAnimation(animDuration);

        // chờ anim kết thúc + buffer nhỏ
        yield return new WaitForSeconds(animDuration + 0.05f);

        // (tùy chọn) xóa line sau khi đã clear xong
        _renderer?.Clear();

        if (board.AllTilesCleared())
            OnWin();
    }

}
