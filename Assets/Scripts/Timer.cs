using UnityEngine;
using UnityEngine.UI;

public class TimerBar : MonoBehaviour
{
    public Image fillImage;     // drag Fill vào đây
    public float duration = 60f;

    private float timeLeft;

    void Start()
    {
        timeLeft = duration;
        if (fillImage != null) fillImage.fillAmount = 1f;
    }

    void Update()
    {
        if (timeLeft > 0)
        {
            timeLeft -= Time.deltaTime;
            float ratio = Mathf.Clamp01(timeLeft / duration);
            if (fillImage != null)
                fillImage.fillAmount = ratio;
        }
        else
        {
            // TODO: hết giờ
            Debug.Log("Time Up!");
        }
    }
}
