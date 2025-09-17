using UnityEngine;

public sealed class ScoreService : IScoreService
{
    public int Score { get; private set; }
    public event System.Action<int> OnScoreChanged;

    public void Add(int amount)
    {
        if (amount == 0) return;
        Score = Mathf.Max(0, Score + amount); // cho phép âm, không âm dưới 0
        OnScoreChanged?.Invoke(Score);
    }

    public void Reset()
    {
        Score = 0;
        OnScoreChanged?.Invoke(Score);
    }
}
