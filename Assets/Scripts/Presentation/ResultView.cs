using UnityEngine;

public class ResultView : MonoBehaviour
{
    [SerializeField] GameObject winPanel;
    [SerializeField] GameObject losePanel;
    [SerializeField] TMPro.TMP_Text scoreText;

    public void ShowWin(int score)
    {
        winPanel.SetActive(true);
        scoreText.text = $"Score: {score}";
    }

    public void ShowLose(int score)
    {
        losePanel.SetActive(true);
        scoreText.text = $"Score: {score}";
    }
}
