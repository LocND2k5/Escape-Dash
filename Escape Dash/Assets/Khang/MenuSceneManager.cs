using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Quản lý toàn bộ UI trong MenuScence:
/// - Main Menu Panel (Play, Leaderboard, Thoát)
/// - Name Input Popup (Nhập tên trước khi chơi)
/// - Game Over Panel (Chơi lại, Về Menu chính khi kết thúc ván)
/// Nằm trong thư mục Assets/Khang/.
/// </summary>
public class MenuSceneManager : MonoBehaviour
{
    public static MenuSceneManager Instance { get; private set; }

    // Key dùng chung để giao tiếp trạng thái Game Over giữa các Scene
    public const string GameOverKey = "ShowGameOver";

    [Header("Panels")]
    [Tooltip("Panel Menu chính ban đầu")]
    [SerializeField] private GameObject mainMenuPanel;

    [Tooltip("Panel Popup nhập tên người chơi (chỉ hiện khi bấm Play)")]
    [SerializeField] private GameObject namePopupPanel;

    [Tooltip("Panel kết thúc ván chơi (Game Over)")]
    [SerializeField] private GameObject gameOverPanel;

    [Header("Input Field")]
    [Tooltip("TMP_InputField nhập tên người chơi")]
    [SerializeField] private TMP_InputField nameInputField;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }
    }

    private void Start()
    {
        // Luôn trả tốc độ game về 1 khi vào Menu
        Time.timeScale = 1f;

        // Ẩn Popup nhập tên ban đầu
        if (namePopupPanel != null)
            namePopupPanel.SetActive(false);

        if (nameInputField != null)
            nameInputField.text = "";

        // Kiểm tra xem vừa chết từ Scene Gameplay trở về hay mới vào game
        if (PlayerPrefs.GetInt(GameOverKey, 0) == 1)
        {
            // Xóa cờ Game Over để lần sau vào bình thường
            PlayerPrefs.SetInt(GameOverKey, 0);
            PlayerPrefs.Save();

            // Hiển thị Game Over, ẩn Menu chính
            if (gameOverPanel != null) gameOverPanel.SetActive(true);
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        }
        else
        {
            // Vào bình thường: Hiện Menu chính, ẩn Game Over
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        }
    }

    // ==========================================
    // CÁC HÀM CỦA MAIN MENU & POPUP NHẬP TÊN
    // ==========================================

    /// <summary>
    /// Gọi khi bấm nút PLAY trên Menu chính
    /// </summary>
    public void OnPlayButtonClicked()
    {
        if (namePopupPanel != null)
        {
            namePopupPanel.SetActive(true);

            if (nameInputField != null)
                nameInputField.text = "";
        }
    }

    /// <summary>
    /// Gọi khi bấm nút Xác nhận / Bắt đầu chơi trong Popup
    /// </summary>
    public void OnConfirmNameClicked()
    {
        string enteredName = nameInputField != null ? nameInputField.text.Trim() : "";

        // Nếu người chơi để trống, tạo tên ngẫu nhiên
        if (string.IsNullOrEmpty(enteredName))
        {
            enteredName = "Runner_" + Random.Range(1000, 9999);
        }

        // Lưu tên vào máy
        PlayerPrefs.SetString("PlayerName", enteredName);
        PlayerPrefs.Save();

        // Đồng bộ tên lên bảng xếp hạng UGS
        if (UGSLeaderboardManager.Instance != null)
            _ = UGSLeaderboardManager.Instance.UpdatePlayerNameAsync(enteredName);

        // Vào gameplay scene (ưu tiên tìm scene "Loc", rồi "Linh", hoặc scene kế tiếp)
        LoadGameplayScene();
    }

    /// <summary>
    /// Gọi khi bấm nút Hủy / Đóng Popup nhập tên
    /// </summary>
    public void OnCancelNameClicked()
    {
        if (namePopupPanel != null)
            namePopupPanel.SetActive(false);
    }

    /// <summary>
    /// Mở bảng xếp hạng Leaderboard
    /// </summary>
    public void OpenLeaderboard()
    {
        if (LeaderboardUI.Instance != null)
        {
            LeaderboardUI.Instance.Open();
        }
        else
        {
            var lb = FindFirstObjectByType<LeaderboardUI>(FindObjectsInactive.Include);
            if (lb != null)
            {
                lb.gameObject.SetActive(true);
                lb.Open();
            }
        }
    }

    /// <summary>
    /// Thoát game
    /// </summary>
    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // ==========================================
    // CÁC HÀM CỦA GAME OVER PANEL
    // ==========================================

    /// <summary>
    /// Gọi khi bấm nút Chơi lại (Restart / Play Again) trên màn Game Over
    /// </summary>
    public void OnRestartButton()
    {
        Time.timeScale = 1f;
        LoadGameplayScene();
    }

    /// <summary>
    /// Gọi khi bấm nút Về Menu chính (Home) trên màn Game Over
    /// </summary>
    public void OnHomeButton()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (namePopupPanel != null) namePopupPanel.SetActive(false);
    }

    // ── Hàm phụ trợ tải Gameplay Scene ──
    private void LoadGameplayScene()
    {
        Time.timeScale = 1f;
        if (Application.CanStreamedLevelBeLoaded("Loc"))
            SceneManager.LoadScene("Loc");
        else if (Application.CanStreamedLevelBeLoaded("Linh"))
            SceneManager.LoadScene("Linh");
        else
            SceneManager.LoadScene(1);
    }
}
