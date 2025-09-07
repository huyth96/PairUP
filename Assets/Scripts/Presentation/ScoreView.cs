using TMPro;
using UnityEngine;

public class ScoreView : MonoBehaviour
{
    [SerializeField] TMP_Text scoreText;

    public void Bind(IScoreService service)
    {
        if (service == null) return;
        service.OnScoreChanged += v => { if (scoreText) scoreText.text = $"Score: {v}"; };
        if (scoreText) scoreText.text = $"Score: {service.Score}";
    }
}
