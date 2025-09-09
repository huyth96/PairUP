using UnityEngine;
using UnityEngine.SceneManagement;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem; // new input system
#endif

public class GameUIController : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private string menuSceneName = "Menu";
    [SerializeField] private KeyCode toggleKey = KeyCode.Escape; // fallback khi dùng old input
    [SerializeField] private bool startPaused = false;

    [Header("Refs")]
    [SerializeField] private GameObject hudPanel;    // optional
    [SerializeField] private GameObject pausePanel;  // panel pause (gồm settings/audio)

    private bool isPaused;

    void Start()
    {
        if (pausePanel) pausePanel.SetActive(false);
        if (hudPanel) hudPanel.SetActive(true);

        if (startPaused) OpenPause();
        else { Time.timeScale = 1f; isPaused = false; }
    }

    void Update()
    {
        if (PressedToggleThisFrame())
        {
            if (isPaused) ClosePause();
            else OpenPause();
        }
    }

    // ===== Input helper: hỗ trợ cả New & Old Input System =====
    private bool PressedToggleThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        // New Input System
        var k = Keyboard.current;
        if (k != null && k.escapeKey.wasPressedThisFrame) return true;

        var g = Gamepad.current;
        if (g != null && g.startButton.wasPressedThisFrame) return true;

        return false;
#else
        // Old Input System
        return Input.GetKeyDown(toggleKey);
#endif
    }

    // ===== NÚT MỞ/ĐÓNG PAUSE =====
    public void OpenPause()
    {
        if (pausePanel) pausePanel.SetActive(true);
        if (hudPanel) hudPanel.SetActive(false);
        Time.timeScale = 0f;
        isPaused = true;
    }

    public void ClosePause()
    {
        if (pausePanel) pausePanel.SetActive(false);
        if (hudPanel) hudPanel.SetActive(true);
        Time.timeScale = 1f;
        isPaused = false;
    }

    public void TogglePause()
    {
        if (isPaused) ClosePause();
        else OpenPause();
    }

    // ===== SCENE CONTROL =====
    public void RestartLevel()
    {
        Time.timeScale = 1f;
        var current = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(current);
    }

    public void BackToMenu()
    {
        Time.timeScale = 1f;
        if (string.IsNullOrEmpty(menuSceneName))
        {
            Debug.LogError("[GameUIController] menuSceneName chưa được set trong Inspector.");
            return;
        }
        SceneManager.LoadScene(menuSceneName);
    }
}
