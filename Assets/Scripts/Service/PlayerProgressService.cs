using System.Collections.Generic;

public class PlayerProgressService : IPlayerProgressService
{
    private PlayerData data;

    public PlayerProgressService()
    {
        data = SaveSystem.Load();
    }

    public int HighScore => data.highScore;
    public IReadOnlyList<int> RecentScores => data.recentScores;

    public void RecordGame(int score)
    {
        if (score > data.highScore)
            data.highScore = score;

        data.recentScores.Add(score);
        if (data.recentScores.Count > 10)
            data.recentScores.RemoveAt(0);

        SaveSystem.Save(data);
    }
}
