using UnityEngine;

public sealed class GameTimerService : MonoBehaviour, IGameTimer
{
    [Header("Config")]
    [SerializeField] private float maxTime = 60f;
    public float MaxTime => maxTime;
    public float Duration { get; private set; }
    public float TimeLeft { get; private set; }
    public bool IsRunning { get; private set; }

    public event System.Action<float> OnTick;
    public event System.Action OnTimeUp;

    void Start()
    {
        // auto start khi scene load (hoặc bạn gọi thủ công trong GameController)
        StartTimer(maxTime);
    }

    void Update()
    {
        if (!IsRunning) return;
        TimeLeft -= Time.deltaTime;
        float ratio = Mathf.Clamp01(TimeLeft / Mathf.Max(0.0001f, Duration));
        OnTick?.Invoke(ratio);
        if (TimeLeft <= 0f) { IsRunning = false; OnTimeUp?.Invoke(); }
    }

    public void StartTimer(float duration)
    {
        Duration = duration;
        TimeLeft = duration;
        IsRunning = true;
    }
    public void AddTime(float seconds)
    {
        // Nếu chưa chạy thì không làm gì
        if (!IsRunning) return;

        // Cộng thêm thời gian, nhưng không vượt quá Duration gốc (nếu bạn muốn giới hạn)
        TimeLeft = Mathf.Min(TimeLeft + seconds, Duration);
    }

    public void StopTimer() => IsRunning = false;
}
