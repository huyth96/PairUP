using UnityEngine;
using UnityEngine.UI;

public class AudioToggleButton : MonoBehaviour
{
    [SerializeField] private AudioBus bus = AudioBus.BGM;
    [SerializeField] private Image targetImage;
    [SerializeField] private Sprite onSprite;
    [SerializeField] private Sprite offSprite;

    private int _lastFrame = -1;          // chống double cùng frame
    private float _lastClickTime = 0f;    // chống double trong khoảng thời gian
    private const float DebounceSeconds = 0.15f; // 150ms

    void OnEnable() => Refresh();
    void Start() => Refresh();

    public void OnButtonClick()
    {
        // chặn double cùng frame
        if (Time.frameCount == _lastFrame) return;
        _lastFrame = Time.frameCount;

        // chặn double trong khoảng thời gian
        if (Time.unscaledTime - _lastClickTime < DebounceSeconds) return;
        _lastClickTime = Time.unscaledTime;

        bool want = !AudioManager.Instance.IsOn(bus);
        AudioManager.Instance.Toggle(bus, want);
        Refresh();
    }

    private void Refresh() => SetIcon(AudioManager.Instance.IsOn(bus));
    private void SetIcon(bool on) =>
        targetImage.sprite = on ? onSprite : offSprite;
}
