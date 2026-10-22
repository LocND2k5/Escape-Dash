using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Quản lý UI Gameplay HUD và Pause Menu.
/// Đặt trong thư mục Assets/Khang/.
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

    [Header("Settings")]
    [Tooltip("Phím tắt để mở/đóng Pause Menu")]
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;

    private bool isPaused = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }
    }

    private void Start()
    {
        // Khi bắt đầu gameplay, đảm bảo thời gian chạy bình thường và hiện HUD
        Time.timeScale = 1f;
        isPaused = false;

        if (hudPanel != null) hudPanel.SetActive(true);
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    private void Update()
    {
        // Hỗ trợ phím tắt Esc hoặc P để bật/tắt Pause
        if (Input.GetKeyDown(pauseKey) || Input.GetKeyDown(KeyCode.P))
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
}
