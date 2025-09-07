using UnityEngine;
using TMPro;

public class ResultView : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] GameObject winPanel;
    [SerializeField] GameObject losePanel;

    [Header("Win Texts")]
    [SerializeField] TMP_Text winScoreText;
    [SerializeField] TMP_Text winHighScoreText;

    [Header("Lose Texts")]
    [SerializeField] TMP_Text loseScoreText;
    [SerializeField] TMP_Text loseHighScoreText;

    void SetPanels(bool showWin)
    {
        if (winPanel) winPanel.SetActive(showWin);
        if (losePanel) losePanel.SetActive(!showWin);
    }

    public void ShowWin(int score)
    {
        int high = HighScoreManager.Instance
            ? HighScoreManager.Instance.HighScore
            : PlayerPrefs.GetInt("HighScore", 0);

        SetPanels(true);
        if (winScoreText) winScoreText.text = score.ToString();
        if (winHighScoreText) winHighScoreText.text = high.ToString();
    }

    public void ShowLose(int score)
    {
        int high = HighScoreManager.Instance
            ? HighScoreManager.Instance.HighScore
            : PlayerPrefs.GetInt("HighScore", 0);

        SetPanels(false);
        if (loseScoreText) loseScoreText.text = score.ToString();
        if (loseHighScoreText) loseHighScoreText.text = high.ToString();
    }

}
