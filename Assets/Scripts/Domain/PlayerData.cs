using System;
using System.Collections.Generic;

[Serializable]
public class PlayerData
{
    public int highScore;
    public List<int> recentScores = new List<int>();
}
