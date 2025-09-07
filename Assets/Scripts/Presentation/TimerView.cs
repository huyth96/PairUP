using UnityEngine;
using UnityEngine.UI;

public class TimerView : MonoBehaviour
{
    [SerializeField] Image fillImage;

    public void Bind(IGameTimer timer)
    {
        if (timer == null) return;
        timer.OnTick += ratio => { if (fillImage) fillImage.fillAmount = ratio; };
        timer.OnTimeUp += () => Debug.Log("Time Up!");
    }
}
