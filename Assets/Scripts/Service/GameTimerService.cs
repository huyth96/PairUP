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

    public void StopTimer() => IsRunning = false;
}
