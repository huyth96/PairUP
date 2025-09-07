using UnityEngine;
using UnityEngine.SceneManagement;

public class GameUIController : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] string menuSceneName = "Menu";   // điền đúng tên scene menu của bạn

    // Gọi từ nút "Chơi lại"
    public void RestartLevel()
    {
        // nếu có pause giữa chừng thì mở lại
        Time.timeScale = 1f;

        // reload chính scene hiện tại
        var current = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(current);
    }

    // Gọi từ nút "Về menu"
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

    // (tuỳ chọn) nếu bạn có panel pause
    public void Pause(bool isPaused)
    {
        Time.timeScale = isPaused ? 0f : 1f;
    }
}
