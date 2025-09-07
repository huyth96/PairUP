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

    public void ShowWin(int score, int highScore)
    {
        SetPanels(true);
        if (winScoreText) winScoreText.text = score.ToString();
        if (winHighScoreText) winHighScoreText.text = highScore.ToString();
    }

    public void ShowLose(int score, int highScore)
    {
        SetPanels(false);
        if (loseScoreText) loseScoreText.text = score.ToString();
        if (loseHighScoreText) loseHighScoreText.text = highScore.ToString();
    }
}
