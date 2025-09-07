using UnityEngine;

public class HighScoreManager : MonoBehaviour
{
    public static HighScoreManager Instance { get; private set; }

    public int HighScore { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadHighScore();
    }

    public void TrySetHighScore(int score)
    {
        if (score > HighScore)
        {
            HighScore = score;
            PlayerPrefs.SetInt("HighScore", HighScore);
            PlayerPrefs.Save();
        }
    }

    private void LoadHighScore()
    {
        HighScore = PlayerPrefs.GetInt("HighScore", 0);
    }
}
