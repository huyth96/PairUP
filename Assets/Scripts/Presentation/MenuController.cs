using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System;

/// <summary>
/// Khai báo 1 entry gồm: panel + nút Open + nút Close (+ tuỳ chọn closeOthers, startOpen)
/// </summary>
[Serializable]
public struct PanelEntry
{
    [Tooltip("Chỉ để dễ nhìn trong Inspector")]
    public string name;

    [Header("Targets")]
    public GameObject panel;

    [Header("Buttons")]
    public Button openButton;
    public Button closeButton;

    [Header("Options")]
    [Tooltip("Khi mở panel này sẽ tự đóng các panel khác trong danh sách")]
    public bool closeOthers;

    [Tooltip("Trạng thái panel khi vào scene")]
    public bool startOpen;
}

public class MenuController : MonoBehaviour
{
    [Header("=== Progress UI (tuỳ chọn) ===")]
    [SerializeField] TMP_Text highScoreText;
    [SerializeField] TMP_Text[] recentScoreLines;

    [Header("=== Main Buttons (tuỳ chọn) ===")]
    [SerializeField] Button playButton;
    [SerializeField] Button clearSaveButton;

    [Header("=== Panels Managed Here ===")]
    [Tooltip("Kéo từng panel + nút Open/Close vào đây")]
    [SerializeField] PanelEntry[] panels;

    [Header("=== Config ===")]
    [SerializeField] string gameplaySceneName = "Game";

    // Progress service (không dùng singleton)
    private IPlayerProgressService _progress;

    void Awake()
    {
        // Tạo service để Load save.json (nếu có)
        _progress = new PlayerProgressService();
    }

    void Start()
    {
        // Gắn listener cho Play/Clear Save (nếu có)
        if (playButton) playButton.onClick.AddListener(StartGame);
        if (clearSaveButton) clearSaveButton.onClick.AddListener(ClearSaveAndRefresh);

        // Khởi tạo trạng thái panel & gắn listener Open/Close theo list
        InitPanels();

        // Cập nhật UI điểm
        RefreshProgressUI();
    }

    void OnEnable()
    {
        // Khi quay lại menu từ gameplay, refresh lại UI
        RefreshProgressUI();
    }

    // ================== Panels ==================
    private void InitPanels()
    {
        // Set trạng thái ban đầu
        foreach (var e in panels)
        {
            if (e.panel)
                e.panel.SetActive(e.startOpen);
        }

        // Gán sự kiện cho nút Open/Close
        foreach (var e in panels)
        {
            if (e.openButton && e.panel)
            {
                // Capture local variables để tránh issue với closure
                var panelRef = e.panel;
                var closeOthersRef = e.closeOthers;
                e.openButton.onClick.AddListener(() => OpenPanel(panelRef, closeOthersRef));
            }

            if (e.closeButton && e.panel)
            {
                var panelRef = e.panel;
                e.closeButton.onClick.AddListener(() => ClosePanel(panelRef));
            }
        }
    }

    public void OpenPanel(GameObject panel, bool closeOthers = false)
    {
        if (!panel) return;
        if (closeOthers) CloseAllExcept(panel);
        panel.SetActive(true);
    }

    public void ClosePanel(GameObject panel)
    {
        if (!panel) return;
        panel.SetActive(false);
    }

    private void CloseAllExcept(GameObject keep)
    {
        if (panels == null) return;
        for (int i = 0; i < panels.Length; i++)
        {
            var p = panels[i].panel;
            if (p && p != keep) p.SetActive(false);
        }
    }

    // ================== Progress / Save ==================
    public void ClearSaveAndRefresh()
    {
        SaveSystem.Delete();
        _progress = new PlayerProgressService(); // Load lại dữ liệu rỗng
        RefreshProgressUI();
    }

    private void RefreshProgressUI()
    {
        if (highScoreText) highScoreText.text = _progress.HighScore.ToString();

        if (recentScoreLines != null && recentScoreLines.Length > 0)
        {
            var recent = _progress.RecentScores;
            for (int i = 0; i < recentScoreLines.Length; i++)
            {
                if (!recentScoreLines[i]) continue;
                recentScoreLines[i].text = (i < recent.Count) ? recent[i].ToString() : "-";
            }
        }
    }

    // ================== Navigation ==================
    public void StartGame()
    {
        if (string.IsNullOrEmpty(gameplaySceneName))
        {
            Debug.LogError("[MenuController] gameplaySceneName is empty.");
            return;
        }
        SceneManager.LoadScene(gameplaySceneName);
    }
}
