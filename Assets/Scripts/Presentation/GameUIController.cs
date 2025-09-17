using UnityEngine;
using UnityEngine.SceneManagement;

public class GameUIController : MonoBehaviour
{
    [SerializeField] private string menuSceneName = "Menu";

    private int _lastRestartFrame = -1;
    private int _lastMenuFrame = -1;
    private float _lastRestartTime = 0f;
    private float _lastMenuTime = 0f;
    private const float DebounceSeconds = 0.15f;

    public void RestartLevel()
    {
        if (Time.frameCount == _lastRestartFrame) return;
        _lastRestartFrame = Time.frameCount;
        if (Time.unscaledTime - _lastRestartTime < DebounceSeconds) return;
        _lastRestartTime = Time.unscaledTime;

        var current = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(current);
    }

    public void BackToMenu()
    {
        if (Time.frameCount == _lastMenuFrame) return;
        _lastMenuFrame = Time.frameCount;
        if (Time.unscaledTime - _lastMenuTime < DebounceSeconds) return;
        _lastMenuTime = Time.unscaledTime;

        if (string.IsNullOrEmpty(menuSceneName))
        {
            Debug.LogError("[GameUIController] menuSceneName chưa được set trong Inspector.");
            return;
        }
        SceneManager.LoadScene(menuSceneName);
    }
}
