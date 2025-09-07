using UnityEngine;
using System.Collections;

public class GameController : MonoBehaviour
{
    [Header("Scene Wiring")]
    [SerializeField] BoardPresenter board;
    [SerializeField] private UILinePathRendererAdapter pathRenderer;
    [SerializeField] ScoreView scoreView;
    [SerializeField] TimerView timerView;
    [SerializeField] GameTimerService timerService;
    [SerializeField] ResultView resultView;

    // Services
    IPathRenderer _renderer;
    IPathFinder _finder;
    IScoreService _score;
    IPlayerProgressService _progress;   // 👈 dùng interface thay vì class

    // State
    ITileView _first;

    void Awake()
    {
        _renderer = pathRenderer;
        _score = new ScoreService();
        _progress = new PlayerProgressService();  // implement nằm ở Services

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

        if (_first.Id == t.Id &&
            _finder.TryGetPath((_first.Row, _first.Col), (t.Row, t.Col), out var path))
        {
            Debug.Log("Match found, path length=" + path.Count);
            _renderer?.DrawPath(path);

            StartCoroutine(ResolveMatchAndMaybeWin((Tile)_first, (Tile)t, 0.2f));

            _score.Add(10);
        }
        else
        {
            Debug.Log("No match or path not found");
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
        int score = _score.Score;

        _progress.RecordGame(score);                      // 👈 lưu history
        HighScoreManager.Instance.TrySetHighScore(score); // update high score
        timerService.StopTimer();

        resultView.ShowWin(score, _progress.HighScore);   // truyền cả high score
    }

    void OnLose()
    {
        Debug.Log("YOU LOSE!");
        int score = _score.Score;

        _progress.RecordGame(score);
        HighScoreManager.Instance.TrySetHighScore(score);

        resultView.ShowLose(score, _progress.HighScore);
    }

    private IEnumerator ResolveMatchAndMaybeWin(Tile a, Tile b, float animDuration)
    {
        if (a) a.PlayClearAnimation(animDuration);
        if (b) b.PlayClearAnimation(animDuration);

        yield return new WaitForSeconds(animDuration + 0.05f);
        _renderer?.Clear();

        if (board.AllTilesCleared())
            OnWin();
    }
}
