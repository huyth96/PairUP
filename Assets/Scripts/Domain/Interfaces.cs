using System;
using System.Collections.Generic;
using UnityEngine;

public interface ITileView
{
    int Row { get; }
    int Col { get; }
    int Id { get; }
    bool IsRemoved { get; }
    void Show(Sprite s);
    void Hide();
    event Action<ITileView> OnClicked;
}

public interface IPathFinder
{
    // Trả về path đã simplify (A, (turn?), (turn?), B)
    bool TryGetPath((int r, int c) a, (int r, int c) b, out List<Vector2Int> path);
}

public interface IPathRenderer
{
    void DrawPath(IList<Vector2Int> cells);
    void Clear();
}

public interface IScoreService
{
    int Score { get; }
    void Add(int points);
    void Reset();
    event Action<int> OnScoreChanged;
}

public interface IGameTimer
{
    float Duration { get; }
    float TimeLeft { get; }
    bool IsRunning { get; }
    void StartTimer(float duration);
    void StopTimer();
    event Action<float> OnTick;   // ratio 0..1
    event Action OnTimeUp;
}
