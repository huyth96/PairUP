using UnityEngine;
using UnityEngine.UI;

public class AudioToggleButton : MonoBehaviour
{
    [SerializeField] private AudioBus bus = AudioBus.BGM;
    [SerializeField] private Image targetImage;
    [SerializeField] private Sprite onSprite;
    [SerializeField] private Sprite offSprite;

    void OnEnable() => Refresh();
    void Start() => Refresh();

    public void OnButtonClick()
    {
        bool want = !AudioManager.Instance.IsOn(bus);   // đọc state thật
        AudioManager.Instance.Toggle(bus, want);
        Refresh();                                      // <-- ĐỌC LẠI THẬT, KHÔNG SetIcon(want)
    }

    private void Refresh() => SetIcon(AudioManager.Instance.IsOn(bus));

    private void SetIcon(bool on)
    {
        if (targetImage) targetImage.sprite = on ? onSprite : offSprite;
    }
}
