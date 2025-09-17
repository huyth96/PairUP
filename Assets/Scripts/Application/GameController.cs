using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem; // New Input System (tùy chọn)
#endif

public class GameController : MonoBehaviour
{
    [Header("Scene Wiring")]
    [SerializeField] private BoardPresenter board;
    [SerializeField] private UILinePathRendererAdapter pathRenderer;
    [SerializeField] private ScoreView scoreView;
    [SerializeField] private TimerView timerView;
    [SerializeField] private GameTimerService timerService;
    [SerializeField] private ResultView resultView;

    [Header("Audio")]
    [SerializeField] private AudioClip clickSfx;
    [SerializeField] private AudioClip matchSfx;
    [SerializeField] private AudioClip matchFailSfx;

    // Services
    private IPathRenderer _renderer;
    private IPathFinder _finder;
    private IScoreService _score;
    private IPlayerProgressService _progress;

    // State chọn ô
    private ITileView _first;

    // === Chống double-tap trên tile ===
    private ITileView _lastClicked;
    private float _lastClickTime;
    private const float TapDebounce = 0.15f; // 150ms

    // === Hint (Search) ===
    private Coroutine _hintCo;
    private bool _hintActive;
    private const float HintShowSeconds = 1.5f;

    // ===== CONFIG CHO NÚT KỸ NĂNG =====
    private const int HintCost = 15;
    private const int ShuffleCost = 15;
    private const int AddTimeCost = 15;
    private const float AddTimeSeconds = 30f;

    // Debounce chống double-tap cho 3 nút
    private int _lastHintFrame = -1, _lastShuffleFrame = -1, _lastAddTimeFrame = -1;
    private float _lastHintTime = 0f, _lastShuffleTime = 0f, _lastAddTimeTime = 0f;
    private const float ButtonDebounceSeconds = 0.15f;

    void Awake()
    {
        _renderer = pathRenderer;
        _score = new ScoreService();
        _progress = new PlayerProgressService();

        if (scoreView) scoreView.Bind(_score);
        if (timerView && timerService) timerView.Bind(timerService);
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
            // nếu bạn muốn auto start, có thể StartTimer(maxTime) trong service
            // hoặc đã gọi từ service Start()
        }
    }

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        // Phím test hint trong Editor
        if (Keyboard.current != null && Keyboard.current.f5Key.wasPressedThisFrame)
        {
            OnPressSearch();
        }
#endif
    }

    // ====== NÚT: SEARCH / HINT (tốn 20 điểm) ======
    public void OnPressSearch()
    {
        // chống double cùng frame + trong 150ms
        if (Time.frameCount == _lastHintFrame) return;
        _lastHintFrame = Time.frameCount;
        if (Time.unscaledTime - _lastHintTime < ButtonDebounceSeconds) return;
        _lastHintTime = Time.unscaledTime;

        if (!TrySpend(HintCost)) return;

        TriggerHint();
    }

    // ====== NÚT: SHUFFLE / TRỘN (tốn 15 điểm) ======
    public void OnPressShuffle()
    {
        if (Time.frameCount == _lastShuffleFrame) return;
        _lastShuffleFrame = Time.frameCount;
        if (Time.unscaledTime - _lastShuffleTime < ButtonDebounceSeconds) return;
        _lastShuffleTime = Time.unscaledTime;

        if (!TrySpend(ShuffleCost)) return;

        // Clear chọn & đường vẽ
        if (_first is Tile sel) sel.SetSelected(false);
        _first = null;
        _renderer?.Clear();

        // Luôn trộn, rồi đảm bảo còn nước đi
        board.ForceShuffle(_finder);

        Debug.Log("[Skill] Force shuffle (-15 pts).");
    }

    // ====== NÚT: THÊM GIỜ +30s (tốn 30 điểm) ======
    public void OnPressAddTime()
    {
        if (Time.frameCount == _lastAddTimeFrame) return;
        _lastAddTimeFrame = Time.frameCount;
        if (Time.unscaledTime - _lastAddTimeTime < ButtonDebounceSeconds) return;
        _lastAddTimeTime = Time.unscaledTime;

        if (!TrySpend(AddTimeCost)) return;

        if (timerService != null)
        {
            // yêu cầu bạn đã thêm public void AddTime(float)
            timerService.AddTime(AddTimeSeconds);
            Debug.Log("[Skill] +30s time (-30 pts).");
        }
    }

    // ====== CORE GAME CLICK ======
    private void HandleTileClicked(ITileView t)
    {
        if (t == null || t.IsRemoved)
        {
            Debug.Log("Clicked null or removed tile");
            return;
        }

        // 🔒 Debounce TRƯỚC, đừng phát âm thanh vội
        if (_lastClicked == t && (Time.unscaledTime - _lastClickTime) < TapDebounce)
            return;

        _lastClicked = t;
        _lastClickTime = Time.unscaledTime;

        // ✅ Chắc chắn đây là click hợp lệ rồi mới phát SFX
        if (clickSfx) AudioManager.Instance.PlaySFX(clickSfx);

        Debug.Log($"GameController.HandleTileClicked: {t.Row},{t.Col}, id={t.Id}");

        if (_first == null)
        {
            _first = t;
            if (_first is Tile tileView1) tileView1.SetSelected(true);
            return;
        }

        if (_first == t)
        {
            // Bỏ chọn nếu không phải duplicate tap trong khoảng debounce
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
            if (matchFailSfx) AudioManager.Instance.PlaySFX(matchFailSfx);
            Debug.Log("No match or path not found");
            if (_first is Tile tileView3) tileView3.SetSelected(false);
            if (t is Tile tileView4) tileView4.SetSelected(false);
        }

        _first = null;
    }

    // ====== HINT FLOW ======
    private void TriggerHint()
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
            if (matchFailSfx) AudioManager.Instance.PlaySFX(matchFailSfx);
        }
    }

    private IEnumerator ShowHintRoutine(Tile a, Tile b, List<Vector2Int> path)
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

    // ====== KẾT THÚC / KẾT QUẢ ======
    private void HandleTimeUp()
    {
        Debug.Log("Time is up → Lose");
        OnLose();
    }

    private void OnWin()
    {
        Debug.Log("YOU WIN!");
        int score = _score.Score;

        _progress.RecordGame(score);
        HighScoreManager.Instance.TrySetHighScore(score);
        if (timerService) timerService.StopTimer();

        resultView.ShowWin(score, _progress.HighScore);
    }

    private void OnLose()
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
        if (matchSfx) AudioManager.Instance.PlaySFX(matchSfx);
        yield return new WaitForSeconds(animDuration + 0.05f);
        _renderer?.Clear();

        if (board.AllTilesCleared())
        {
            OnWin();
            yield break;
        }

        // nếu bế tắc thì xáo cho đến khi có nước đi
        board.ShuffleUntilMovable(_finder, maxAttempts: 20);
    }

    // ====== UTILS ======
    private bool TrySpend(int cost)
    {
        if (_score.Score < cost)
        {
            Debug.Log($"[Skill] Not enough points. Need {cost}, have {_score.Score}");
            if (matchFailSfx) AudioManager.Instance.PlaySFX(matchFailSfx);
            return false;
        }
        _score.Add(-cost);
        return true;
    }

}
