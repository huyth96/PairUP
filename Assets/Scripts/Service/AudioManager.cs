using UnityEngine;
using UnityEngine.Audio;

public enum AudioBus { Master, BGM, SFX }

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Mixer + Params (đặt đúng tên đã Expose)")]
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private string masterParam = "MasterVol";
    [SerializeField] private string bgmParam = "BGMVol";
    [SerializeField] private string sfxParam = "SFXVol";

    [Header("Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    float ToDb(float x) => Mathf.Approximately(x, 0f) ? -80f : Mathf.Log10(Mathf.Clamp01(x)) * 20f;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        // mặc định full volume
        mixer.SetFloat(masterParam, 0f);
        mixer.SetFloat(bgmParam, 0f);
        mixer.SetFloat(sfxParam, 0f);
    }

    public void SetVolume(AudioBus bus, float v01)
    {
        string p = bus == AudioBus.BGM ? bgmParam : bus == AudioBus.SFX ? sfxParam : masterParam;
        mixer.SetFloat(p, ToDb(v01));
        if (bus == AudioBus.BGM && bgmSource) bgmSource.volume = v01;
        if (bus == AudioBus.SFX && sfxSource) sfxSource.volume = v01;
    }

    public void Toggle(AudioBus bus, bool on)
    {
        string p = bus == AudioBus.BGM ? bgmParam : bus == AudioBus.SFX ? sfxParam : masterParam;
        mixer.SetFloat(p, on ? 0f : -80f);
        if (bus == AudioBus.BGM && !on) bgmSource.Stop();
    }

    public void PlayBGM(AudioClip clip, bool loop = true)
    {
        if (!clip) return;
        bgmSource.loop = loop; bgmSource.clip = clip; bgmSource.Play();
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip) sfxSource.PlayOneShot(clip);
    }
}
