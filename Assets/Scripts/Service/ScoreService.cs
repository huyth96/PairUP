public sealed class ScoreService : IScoreService
{
    public int Score { get; private set; }
    public event System.Action<int> OnScoreChanged;

    public void Add(int p)
    {
        if (p <= 0) return;
        Score += p;
        OnScoreChanged?.Invoke(Score);
    }

    public void Reset()
    {
        Score = 0;
        OnScoreChanged?.Invoke(Score);
    }
}
