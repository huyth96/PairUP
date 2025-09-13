using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using UnityEngine.InputSystem; 

public class GameController : MonoBehaviour
{
    [Header("Scene Wiring")]
    [SerializeField] BoardPresenter board;
    [SerializeField] private UILinePathRendererAdapter pathRenderer;
    [SerializeField] ScoreView scoreView;
    [SerializeField] TimerView timerView;
    [SerializeField] GameTimerService timerService;
    [SerializeField] ResultView resultView;
    [SerializeField] private AudioClip clickSfx;
    [SerializeField] private AudioClip matchSfx;
    [SerializeField] private AudioClip matchFailSfx;

    // Services
    IPathRenderer _renderer;
    IPathFinder _finder;
    IScoreService _score;
    IPlayerProgressService _progress;

    // State
    ITileView _first;

    // === NEW: chống double-tap ===
    ITileView _lastClicked;
    float _lastClickTime;
    const float TapDebounce = 0.15f; // 150ms

    void Awake()
    {
        _renderer = pathRenderer;
        _score = new ScoreService();
        _progress = new PlayerProgressService();

        if (scoreView) scoreView.Bind(_score);
        if (timerView && timerService) timerView.Bind(timerService);
    }
    // GameController.cs — thêm vào class
    Coroutine _hintCo;
    bool _hintActive;
    const float HintShowSeconds = 1.5f;

    void Update()
    {
        // Input System only
        if (Keyboard.current != null && Keyboard.current.f5Key.wasPressedThisFrame)
        {
            TriggerHint();
        }
    }

    void TriggerHint()
    {
        if (_hintActive || board == null || _finder == null) return;

        if (board.TryFindFirstHint(_finder, out var a, out var b, out var path))
        {
            if (_hintCo != null) StopCoroutine(_hintCo);
            _hintCo = StartCoroutine(ShowHintRoutine(a, b, path));
        }
        else
        {
            Debug.Log("No available moves to hint.");
        }
    }

    IEnumerator ShowHintRoutine(Tile a, Tile b, List<Vector2Int> path)
    {
        _hintActive = true;

        // không động chạm tới _first selection hiện có; chỉ hiển thị
        if (a) a.SetHint(true);
        if (b) b.SetHint(true);

        // vẽ đường gợi ý (dùng cùng renderer; sẽ tự Clear sau)
        _renderer?.DrawPath(path);

        // blink nhẹ (không bắt buộc)
        Coroutine blinkA = null, blinkB = null;
        if (a) blinkA = StartCoroutine(a.BlinkHint(HintShowSeconds));
        if (b) blinkB = StartCoroutine(b.BlinkHint(HintShowSeconds));

        yield return new WaitForSeconds(HintShowSeconds);

        if (blinkA != null) StopCoroutine(blinkA);
        if (blinkB != null) StopCoroutine(blinkB);

        if (a) a.SetHint(false);
        if (b) b.SetHint(false);
        _renderer?.Clear();

        _hintActive = false;
    }

    void OnEnable()
    {
        if (board) board.OnTileClicked += HandleTileClicked;
        if (timerService) timerService.OnTimeUp += HandleTimeUp;
    }

    void OnDisable()
    {
        if (board) board.OnTileClicked -= HandleTileClicked;
        if (timerService) timerService.OnTimeUp -= HandleTimeUp;
    }

    void Start()
    {
        var (r, c) = board.Size;
        _finder = new GridPathFinderBFS(r, c, (row, col) => board.IsWalkable(row, col));

        if (timerService)
        {
            timerService.StartTimer(timerService.MaxTime);
        }
    }

    void HandleTileClicked(ITileView t)
    {
        if (t == null || t.IsRemoved)
        {
            Debug.Log("Clicked null or removed tile");
            return;
        }
        // 🔊 Phát SFX đã gán sẵn cho Sfx Source
        if (clickSfx)
            AudioManager.Instance.PlaySFX(clickSfx);


        // === NEW: Debounce để tránh double-fire trên mobile ===
        if (_lastClicked == t && (Time.unscaledTime - _lastClickTime) < TapDebounce)
        {
            // Debug.Log("Debounced duplicate tap");
            return;
        }
        _lastClicked = t;
        _lastClickTime = Time.unscaledTime;

        Debug.Log($"GameController.HandleTileClicked: {t.Row},{t.Col}, id={t.Id}");

        if (_first == null)
        {
            _first = t;
            if (_first is Tile tileView1) tileView1.SetSelected(true);
            return;
        }

        if (_first == t)
        {
            // Chỉ bỏ chọn nếu KHÔNG phải do duplicate tap trong khoảng debounce
            if ((Time.unscaledTime - _lastClickTime) >= TapDebounce)
            {
                if (_first is Tile tileView2) tileView2.SetSelected(false);
                _first = null;
            }
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
            if (matchFailSfx)
                AudioManager.Instance.PlaySFX(matchFailSfx);
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

        _progress.RecordGame(score);
        HighScoreManager.Instance.TrySetHighScore(score);
        if (timerService) timerService.StopTimer();

        resultView.ShowWin(score, _progress.HighScore);
    }

    void OnLose()
    {
        Debug.Log("YOU LOSE!");
        int score = _score.Score;

        _progress.RecordGame(score);
        HighScoreManager.Instance.TrySetHighScore(score);

        resultView.ShowLose(score, _progress.HighScore);
    }

    // GameController.cs — thay thế ResolveMatchAndMaybeWin
    private IEnumerator ResolveMatchAndMaybeWin(Tile a, Tile b, float animDuration)
    {
        if (a) a.PlayClearAnimation(animDuration);
        if (b) b.PlayClearAnimation(animDuration);
        if (matchSfx)
            AudioManager.Instance.PlaySFX(matchSfx);
        yield return new WaitForSeconds(animDuration + 0.05f);
        _renderer?.Clear();

        if (board.AllTilesCleared())
        {
            OnWin();
            yield break;
        }

        // NEW: nếu bế tắc thì xáo cho đến khi có nước đi
        board.ShuffleUntilMovable(_finder, maxAttempts: 20);
    }

}
