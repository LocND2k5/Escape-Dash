using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Quản lý toàn bộ UI trong Gameplay Scene (Linh / Loc):
/// - HUD hiển thị Điểm (Score), Xu (Coin), Quãng đường (Distance)
/// - Pause Menu (Tạm dừng, Tiếp tục, Chơi lại, Về Menu)
/// - Game Over: ReviveManager hỏi Hồi sinh trước; bấm Bỏ qua → TriggerGameOverScreen() về Game Over ở MenuScence.
/// Nằm trong thư mục Assets/Khang/.
/// </summary>
public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance { get; private set; }

    [Header("Panels")]
    [Tooltip("Panel chứa HUD hiển thị khi đang chạy (Điểm, Coin, Nút Pause...)")]
    [SerializeField] private GameObject hudPanel;

    [Tooltip("Panel Popup khi bấm Tạm Dừng")]
    [SerializeField] private GameObject pausePanel;

    [Header("HUD Text Elements (Tự động cập nhật nếu kéo vào)")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI distanceText;

    [Header("Game Over & Revive Bridge")]
    [Tooltip("Kéo nút màu xanh (Restart Button) trong GameOverPanel của ReviveManager vào đây")]
    [SerializeField] private Button reviveRestartButton;

    [Header("Settings")]
    [Tooltip("Phím tắt để mở/đóng Pause Menu")]
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;

    private bool isPaused = false;
    private bool isPlayerDead = false; // Đang chờ Hồi sinh / Bỏ qua → khóa phím Pause

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }
    }

    // Không chuyển scene ngay khi Game Over nữa: ReviveManager hiện RevivePanel trước,
    // chỉ khi bấm Bỏ qua (hoặc hết giờ) mới gọi TriggerGameOverScreen().
    private void OnEnable()
    {
        GameEvents.OnGameOver      += HandleGameOverEvent;
        GameEvents.OnPlayerRevived += HandlePlayerRevived;
    }

    private void OnDisable()
    {
        GameEvents.OnGameOver      -= HandleGameOverEvent;
        GameEvents.OnPlayerRevived -= HandlePlayerRevived;
    }

    private void Start()
    {
        // Khi bắt đầu gameplay, đảm bảo thời gian chạy bình thường và hiện HUD
        Time.timeScale = 1f;
        isPaused = false;

        if (hudPanel != null) hudPanel.SetActive(true);
        if (pausePanel != null) pausePanel.SetActive(false);

        // Thiết lập đè hành vi cho nút xanh của ReviveManager (nếu có gán)
        if (reviveRestartButton != null)
        {
            reviveRestartButton.onClick.RemoveAllListeners();
            reviveRestartButton.onClick.AddListener(TriggerGameOverScreen);
        }
    }

    private void Update()
    {
        // Hỗ trợ phím tắt Esc hoặc P để bật/tắt Pause
        if (!isPlayerDead && (Input.GetKeyDown(pauseKey) || Input.GetKeyDown(KeyCode.P)))
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }

        // Tự động cập nhật số liệu lên HUD nếu có ScoreManage trong scene
        UpdateHUDInfo();
    }

    private void UpdateHUDInfo()
    {
        if (ScoreManage.Instance != null)
        {
            if (scoreText != null) 
                scoreText.text = "Score: " + ScoreManage.Instance.Score;
            if (coinText != null) 
                coinText.text = "Coin: " + ScoreManage.Instance.Coins;
            if (distanceText != null) 
                distanceText.text = "Distance: " + Mathf.FloorToInt(ScoreManage.Instance.Distance) + " m";
        }
        else if (ScoreManager.Instance != null)
        {
            if (scoreText != null) 
                scoreText.text = "Score: " + ScoreManager.Instance.Score;
            if (coinText != null) 
                coinText.text = "Coin: " + ScoreManager.Instance.Coins;
            if (distanceText != null) 
                distanceText.text = "Distance: " + Mathf.FloorToInt(ScoreManager.Instance.Distance) + " m";
        }
    }

    // ==========================================
    // CÁC HÀM PAUSE MENU (Gắn vào OnClick của Button)
    // ==========================================

    /// <summary>
    /// Gọi khi bấm nút Pause trên HUD hoặc nhấn Esc
    /// </summary>
    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f; // Dừng toàn bộ chuyển động trong game

        if (pausePanel != null) pausePanel.SetActive(true);
    }

    /// <summary>
    /// Gọi khi bấm nút Tiếp tục (Resume) trong Pause Menu
    /// </summary>
    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f; // Tiếp tục chạy game bình thường

        if (pausePanel != null) pausePanel.SetActive(false);
    }

    /// <summary>
    /// Gọi khi bấm nút Chơi lại (Restart)
    /// </summary>
    public void RestartGame()
    {
        Time.timeScale = 1f; // Nhớ trả về 1 trước khi load lại scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// Gọi khi bấm nút Thoát về Menu chính (Home / Main Menu)
    /// </summary>
    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        // Load MenuScence (index 0 trong Build Settings)
        SceneManager.LoadScene(0);
    }

    // ==========================================
    // XỬ LÝ CHUYỂN TIẾP GAME OVER VỀ MENUSCENCE
    // ==========================================

    private void HandleGameOverEvent()
    {
        isPlayerDead = true;
        if (isPaused) ResumeGame();

        // Không có ReviveManager trong scene → chuyển thẳng sang Game Over như cũ
        if (ReviveManager.Instance == null)
            TriggerGameOverScreen();
    }

    private void HandlePlayerRevived()
    {
        isPlayerDead = false;
    }

    /// <summary>
    /// Lưu cờ báo Game Over và load về MenuScence để hiện Game Over Panel
    /// </summary>
    public void TriggerGameOverScreen()
    {
        PlayerPrefs.SetInt(MenuSceneManager.GameOverKey, 1);
        PlayerPrefs.Save();

        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }
}
