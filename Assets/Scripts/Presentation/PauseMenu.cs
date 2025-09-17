using UnityEngine;
using UnityEngine.UI;

public class PanelToggle : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject targetPanel;   // Panel cần bật/tắt
    [SerializeField] private Button openButton;        // Nút mở
    [SerializeField] private Button closeButton;       // Nút đóng

    void Awake()
    {
        if (openButton) openButton.onClick.AddListener(OpenPanel);
        if (closeButton) closeButton.onClick.AddListener(ClosePanel);

        // Đảm bảo panel ban đầu ẩn
        if (targetPanel) targetPanel.SetActive(false);
    }

    public void OpenPanel()
    {
        if (targetPanel) targetPanel.SetActive(true);
    }

    public void ClosePanel()
    {
        if (targetPanel) targetPanel.SetActive(false);
    }
}
