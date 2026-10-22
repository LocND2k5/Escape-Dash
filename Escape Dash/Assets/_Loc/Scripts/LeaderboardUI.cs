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
    public static LeaderboardUI Instance { get; private set; }

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

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

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

        // Mặc định ẩn popup khi mới vào game
        if (leaderboardPanel != null && leaderboardPanel.activeSelf)
        {
            leaderboardPanel.SetActive(false);
        }
        if (overlay != null) overlay.SetActive(false);
    }

    /// <summary>
    /// Mở Bảng Xếp Hạng và tự động tải dữ liệu mới nhất
    /// </summary>
    public void Open()
    {
        if (leaderboardPanel != null) leaderboardPanel.SetActive(true);
        if (overlay != null) overlay.SetActive(true);

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
        if (leaderboardPanel != null) leaderboardPanel.SetActive(false);
        if (overlay != null) overlay.SetActive(false);
    }

    /// <summary>
    /// Kéo dữ liệu từ server về và vẽ lại danh sách
    /// </summary>
    public async void RefreshLeaderboard()
    {
        if (UGSLeaderboardManager.Instance == null) return;

        // Bật loading, ẩn thông báo lỗi
        if (loadingIndicator != null) loadingIndicator.SetActive(true);
        if (emptyOrErrorText != null) emptyOrErrorText.gameObject.SetActive(false);

        // Xóa danh sách cũ
        if (contentContainer != null)
        {
            foreach (Transform child in contentContainer)
            {
                Destroy(child.gameObject);
            }
        }

        // 1. Lấy danh sách Top người chơi
        List<LeaderboardEntry> topEntries = await UGSLeaderboardManager.Instance.GetTopScoresAsync(topCount);

        // 2. Lấy thông tin thứ hạng của chính mình
        LeaderboardEntry myEntry = await UGSLeaderboardManager.Instance.GetMyScoreAsync();

        // Tắt loading
        if (loadingIndicator != null) loadingIndicator.SetActive(false);

        string myPlayerId = UGSLeaderboardManager.Instance.PlayerId;

        // 3. Hiển thị thông tin bản thân ở thanh dưới cùng
        if (myRankText != null)
        {
            if (myEntry != null && myEntry.Rank >= 0)
                myRankText.text = $"Hạng của bạn: <color=#38bdf8><b>#{myEntry.Rank + 1}</b></color>";
            else
                myRankText.text = "Hạng của bạn: <color=#94a3b8>Chưa có</color>";
        }

        if (myScoreText != null)
        {
            if (myEntry != null)
                myScoreText.text = $"Kỷ lục: <color=#fbbf24><b>{myEntry.Score:N0}</b></color>";
            else
                myScoreText.text = "Kỷ lục: <color=#94a3b8>0</color>";
        }

        if (myNameText != null)
        {
            myNameText.text = UGSLeaderboardManager.Instance.PlayerName;
        }

        // 4. Vẽ danh sách Top
        if (topEntries == null || topEntries.Count == 0)
        {
            if (emptyOrErrorText != null)
            {
                emptyOrErrorText.gameObject.SetActive(true);
                emptyOrErrorText.text = topEntries == null ? "Không thể kết nối đến máy chủ." : "Chưa có kỷ lục nào. Hãy là người đầu tiên!";
            }
            return;
        }

        if (contentContainer == null || rowPrefab == null) return;

        for (int i = 0; i < topEntries.Count; i++)
        {
            var entry = topEntries[i];
            GameObject rowObj = Instantiate(rowPrefab, contentContainer);

            bool isMe = !string.IsNullOrEmpty(myPlayerId) && entry.PlayerId == myPlayerId;
            int rank = entry.Rank + 1; // 0-based to 1-based
            int score = (int)entry.Score;
            string displayName = !string.IsNullOrWhiteSpace(entry.PlayerName) ? entry.PlayerName : $"Runner_{rank}";

            // Nếu prefab có script LeaderboardRowUI
            var rowUI = rowObj.GetComponent<LeaderboardRowUI>();
            if (rowUI != null)
            {
                rowUI.Setup(rank, displayName, score, isMe);
            }
            else
            {
                // Fallback nếu không gắn script LeaderboardRowUI mà dùng các TMP_Text con
                TMP_Text[] texts = rowObj.GetComponentsInChildren<TMP_Text>();
                if (texts.Length >= 3)
                {
                    texts[0].text = $"#{rank}";
                    texts[1].text = displayName;
                    texts[2].text = score.ToString("N0");
                }
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
