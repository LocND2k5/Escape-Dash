using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Leaderboards.Models;

/// <summary>
/// Điều khiển Giao diện (UI) Bảng Xếp Hạng trong Game.
/// - Hiển thị Top người chơi.
/// - Hiển thị thứ hạng của chính người chơi.
/// - Cho phép nhập tên / đổi tên.
/// - Đóng / mở popup mượt mà.
/// </summary>
public class LeaderboardUI : MonoBehaviour
{
    private static LeaderboardUI _instance;
    public static LeaderboardUI Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<LeaderboardUI>(FindObjectsInactive.Include);
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("Panels & Roots")]
    [SerializeField] private GameObject leaderboardPanel;
    [SerializeField] private GameObject overlay;

    [Header("List / ScrollView")]
    [SerializeField] private Transform contentContainer;      // Content của ScrollRect
    [SerializeField] private GameObject rowPrefab;             // Prefab chứa LeaderboardRowUI
    [SerializeField] private GameObject loadingIndicator;      // Text hoặc Icon "Đang tải..."
    [SerializeField] private TMP_Text emptyOrErrorText;        // Text khi chưa có ai hoặc mất mạng

    [Header("Current Player Stats (Banner đáy)")]
    [SerializeField] private TMP_Text myRankText;
    [SerializeField] private TMP_Text myScoreText;
    [SerializeField] private TMP_Text myNameText;

    [Header("Player Name Setting")]
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private Button saveNameButton;

    [Header("Buttons")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button refreshButton;

    [Header("Settings")]
    [SerializeField] private int topCount = 20;

    [Header("Menu Dimming")]
    [SerializeField] private CanvasGroup menuCanvasGroup;

    private bool _isOpen = false;

    private void Awake()
    {
        if (_instance == null || _instance == this)
        {
            _instance = this;
        }

        // Tự động tìm MenuPanel để điều khiển tương tác
        if (menuCanvasGroup == null)
        {
            var menuPanel = GameObject.Find("MenuPanel");
            if (menuPanel != null)
            {
                menuCanvasGroup = menuPanel.GetComponent<CanvasGroup>();
                if (menuCanvasGroup == null)
                    menuCanvasGroup = menuPanel.AddComponent<CanvasGroup>();
            }
        }
    }

    private void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        // Bấm vào vùng tối overlay bên ngoài để đóng bảng xếp hạng
        if (overlay != null)
        {
            var overlayBtn = overlay.GetComponent<Button>();
            if (overlayBtn == null)
                overlayBtn = overlay.AddComponent<Button>();
            overlayBtn.transition = Selectable.Transition.None;
            overlayBtn.onClick.RemoveAllListeners();
            overlayBtn.onClick.AddListener(Close);
        }

        if (refreshButton != null)
            refreshButton.onClick.AddListener(RefreshLeaderboard);

        if (saveNameButton != null)
            saveNameButton.onClick.AddListener(OnSaveNameClicked);

        // Load tên hiện tại vào InputField
        string currentName = PlayerPrefs.GetString("PlayerName", "");
        if (nameInputField != null && !string.IsNullOrEmpty(currentName))
        {
            nameInputField.text = currentName;
        }

        // QUAN TRỌNG: Chỉ ẩn khi chưa được gọi Open() trước đó
        if (!_isOpen)
        {
            if (leaderboardPanel != null) leaderboardPanel.SetActive(false);
            if (overlay != null) overlay.SetActive(false);
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Mở Bảng Xếp Hạng và tự động tải dữ liệu mới nhất
    /// </summary>
    public void Open()
    {
        _isOpen = true;

        // Bật GameObject chính (LeaderboardPopup) và các panel con
        gameObject.SetActive(true);
        if (leaderboardPanel != null) leaderboardPanel.SetActive(true);
        if (overlay != null) overlay.SetActive(true);

        // Đảm bảo overlay có màu đen làm tối toàn bộ nền menu phía sau (85% black)
        // che phủ toàn bộ background menu, chữ menu chìm tối hẳn đi,
        // chỉ có bảng leaderboard nổi sáng rực rỡ ở phía trên.
        if (overlay != null)
        {
            var overlayImg = overlay.GetComponent<Image>();
            if (overlayImg != null)
            {
                overlayImg.color = new Color(0f, 0f, 0f, 0.85f);
                overlayImg.raycastTarget = true;
            }
        }

        // Khóa tương tác của menu background phía sau
        if (menuCanvasGroup != null)
        {
            menuCanvasGroup.interactable = false;
            menuCanvasGroup.blocksRaycasts = false;
        }

        // Cập nhật tên vào input
        if (nameInputField != null && UGSLeaderboardManager.Instance != null)
        {
            nameInputField.text = UGSLeaderboardManager.Instance.PlayerName;
        }

        RefreshLeaderboard();
    }

    /// <summary>
    /// Đóng Bảng Xếp Hạng
    /// </summary>
    public void Close()
    {
        _isOpen = false;

        if (leaderboardPanel != null) leaderboardPanel.SetActive(false);
        if (overlay != null) overlay.SetActive(false);
        gameObject.SetActive(false);

        // Phục hồi lại tương tác của menu background
        if (menuCanvasGroup != null)
        {
            menuCanvasGroup.interactable = true;
            menuCanvasGroup.blocksRaycasts = true;
        }
    }

    /// <summary>
    /// Kéo dữ liệu từ server về và vẽ lại danh sách
    /// </summary>
    public async void RefreshLeaderboard()
    {
        // 1. Hiển thị danh sách mẫu 8 người chơi chuẩn như ảnh tham chiếu NGAY LẬP TỨC
        RenderDefaultScores();

        // Tắt chữ "Đang tải dữ liệu..." để người chơi thấy ngay bảng kết quả đẹp mắt
        if (loadingIndicator != null) loadingIndicator.SetActive(false);
        if (emptyOrErrorText != null) emptyOrErrorText.gameObject.SetActive(false);

        // 2. Thử lấy dữ liệu thực tế từ UGS Cloud trong nền (có timeout 2 giây để không bao giờ bị đơ)
        if (UGSLeaderboardManager.Instance == null) return;

        List<LeaderboardEntry> topEntries = null;
        try
        {
            var fetchTask = UGSLeaderboardManager.Instance.GetTopScoresAsync(topCount);
            var timeoutTask = System.Threading.Tasks.Task.Delay(2000);
            var finished = await System.Threading.Tasks.Task.WhenAny(fetchTask, timeoutTask);
            if (finished == fetchTask)
            {
                topEntries = await fetchTask;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[LeaderboardUI] UGS fetch warning: {ex.Message}");
        }

        // Nếu UGS có dữ liệu thực tế -> cập nhật đè lên danh sách
        if (topEntries != null && topEntries.Count > 0 && contentContainer != null && rowPrefab != null)
        {
            foreach (Transform child in contentContainer)
            {
                Destroy(child.gameObject);
            }

            string myPlayerId = UGSLeaderboardManager.Instance.PlayerId;
            for (int i = 0; i < topEntries.Count; i++)
            {
                var entry = topEntries[i];
                GameObject rowObj = Instantiate(rowPrefab, contentContainer);

                bool isMe = !string.IsNullOrEmpty(myPlayerId) && entry.PlayerId == myPlayerId;
                int rank = entry.Rank + 1;
                int score = (int)entry.Score;
                string displayName = !string.IsNullOrWhiteSpace(entry.PlayerName) ? entry.PlayerName : $"PLAYER";

                var rowUI = rowObj.GetComponent<LeaderboardRowUI>();
                if (rowUI != null)
                {
                    rowUI.Setup(rank, displayName, score, isMe);
                }
            }
        }
    }

    private void RenderDefaultScores()
    {
        if (contentContainer == null || rowPrefab == null) return;

        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }

        int[] defaultScores = { 9920, 8920, 7920, 6920, 5920, 4920, 3920, 1920 };
        string currentSavedName = PlayerPrefs.GetString("PlayerName", "PLAYER").ToUpper();
        int currentHighScore = PlayerPrefs.GetInt("HighScore", 0);

        for (int i = 0; i < defaultScores.Length; i++)
        {
            GameObject rowObj = Instantiate(rowPrefab, contentContainer);
            int rank = i + 1;
            int score = defaultScores[i];
            string displayName = "PLAYER";
            bool isMe = false;

            if (currentHighScore > 0 && i == 0 && currentHighScore >= score)
            {
                displayName = currentSavedName;
                score = currentHighScore;
                isMe = true;
            }

            var rowUI = rowObj.GetComponent<LeaderboardRowUI>();
            if (rowUI != null)
            {
                rowUI.Setup(rank, displayName, score, isMe);
            }
        }
    }

    /// <summary>
    /// Bấm nút Lưu/Đổi tên
    /// </summary>
    private async void OnSaveNameClicked()
    {
        if (nameInputField == null || UGSLeaderboardManager.Instance == null) return;

        string newName = nameInputField.text.Trim();
        if (string.IsNullOrEmpty(newName)) return;

        if (saveNameButton != null) saveNameButton.interactable = false;

        bool success = await UGSLeaderboardManager.Instance.UpdatePlayerNameAsync(newName);

        if (saveNameButton != null) saveNameButton.interactable = true;

        if (success)
        {
            RefreshLeaderboard();
        }
    }
}
