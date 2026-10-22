using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Quản lý tính năng Hồi sinh (Revive) bằng Coin tích lũy.
/// - Khi Game Over: Hiển thị RevivePanel hỏi người chơi.
/// - Kiểm tra TotalCoins. Đủ thì bấm Hồi sinh, thiếu thì nút mờ.
/// - Đếm ngược (countdownDuration giây). Hết giờ / Bỏ qua → GameOverPanel.
/// - Hồi sinh: Trừ Coin, sinh Safe Runway, đẩy lùi Bò, tiếp tục chạy.
///
/// *** Tất cả UI được kéo vào từ Hierarchy trong Inspector ***
/// </summary>
public class ReviveManager : MonoBehaviour
{
    public static ReviveManager Instance { get; private set; }

    // ──────────────────────────────────────────────────────────
    // CÀI ĐẶT (chỉnh được trong Inspector)
    // ──────────────────────────────────────────────────────────
    [Header("Revive Settings")]
    [Tooltip("Số Coin cần để hồi sinh mỗi lần")]
    public int reviveCost = 50;

    [Tooltip("Thời gian đếm ngược (giây)")]
    public float countdownDuration = 5f;

    // ──────────────────────────────────────────────────────────
    // THAM CHIẾU UI — kéo vào từ Hierarchy trong Inspector
    // ──────────────────────────────────────────────────────────
    [Header("Root UI Objects")]
    [SerializeField] private Canvas      reviveCanvas;   // ReviveCanvas
    [SerializeField] private GameObject  overlayGo;      // Overlay (lớp phủ đen)
    [SerializeField] private GameObject  revivePanel;    // RevivePanel
    [SerializeField] private GameObject  gameOverPanel;  // GameOverPanel

    [Header("Revive Panel - Texts")]
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI balanceText;
    [SerializeField] private TextMeshProUGUI countdownText;

    [Header("Revive Panel - Buttons")]
    [SerializeField] private Button          reviveButton;
    [SerializeField] private TextMeshProUGUI reviveButtonText;
    [SerializeField] private Button          skipButton;

    [Header("Game Over Panel - Texts")]
    [SerializeField] private TextMeshProUGUI goScoreText;
    [SerializeField] private TextMeshProUGUI goBestText;
    [SerializeField] private TextMeshProUGUI goCoinsText;

    [Header("Game Over Panel - Buttons")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button leaderboardButton;

    // ──────────────────────────────────────────────────────────
    // TRẠNG THÁI NỘI BỘ
    // ──────────────────────────────────────────────────────────
    private bool  isPromptActive = false;
    private float currentTimer;
    private bool  hasEnoughCoins = false;

    // ──────────────────────────────────────────────────────────
    // LIFECYCLE
    // ──────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }

        // Đăng ký sự kiện click cho các nút
        if (reviveButton  != null) reviveButton .onClick.AddListener(OnReviveClicked);
        if (skipButton    != null) skipButton   .onClick.AddListener(OnSkipClicked);
        if (restartButton != null) restartButton.onClick.AddListener(RestartGame);
        if (leaderboardButton != null) leaderboardButton.onClick.AddListener(OpenLeaderboard);

        // Ẩn toàn bộ UI ngay khi khởi động để không che màn hình chơi
        HideAllUI();
    }

    private void OnEnable()  => GameEvents.OnGameOver += OnGameOverTriggered;
    private void OnDisable() => GameEvents.OnGameOver -= OnGameOverTriggered;

    // ──────────────────────────────────────────────────────────
    // HIỆN REVIVE PANEL KHI GAME OVER
    // ──────────────────────────────────────────────────────────
    private void OnGameOverTriggered() => StartCoroutine(ShowPromptRoutine());

    private IEnumerator ShowPromptRoutine()
    {
        // Chờ 0.3s để animation ngã/va chạm bắt đầu mượt
        yield return new WaitForSeconds(0.3f);

        int totalCoins = GetTotalCoins();
        hasEnoughCoins = totalCoins >= reviveCost;
        currentTimer   = countdownDuration;
        isPromptActive = true;

        UpdateUIContent(totalCoins);

        // Bật Canvas → Overlay → RevivePanel
        if (reviveCanvas)  reviveCanvas.gameObject.SetActive(true);
        if (overlayGo)     overlayGo   .SetActive(true);
        if (revivePanel)   revivePanel  .SetActive(true);
        if (gameOverPanel) gameOverPanel.SetActive(false);
    }

    // ──────────────────────────────────────────────────────────
    // UPDATE — đếm ngược & phím tắt
    // ──────────────────────────────────────────────────────────
    private void Update()
    {
        // Phím tắt khi ở màn hình Game Over
        if (gameOverPanel != null && gameOverPanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Space) ||
                Input.GetKeyDown(KeyCode.R)     ||
                Input.GetKeyDown(KeyCode.Return))
                RestartGame();
            return;
        }

        if (!isPromptActive) return;

        // Đếm ngược
        currentTimer -= Time.unscaledDeltaTime;
        if (countdownText != null)
            countdownText.text =
                $"Tự động bỏ qua sau: <color=#f59e0b><b>{Mathf.CeilToInt(currentTimer)}s</b></color>";

        if (currentTimer <= 0f) { OnSkipClicked(); return; }

        // Phím tắt: Space/R → Hồi sinh  |  Esc → Bỏ qua
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.R))
        {
            if (hasEnoughCoins) OnReviveClicked();
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnSkipClicked();
        }
    }

    // ──────────────────────────────────────────────────────────
    // CẬP NHẬT NỘI DUNG UI
    // ──────────────────────────────────────────────────────────
    private void UpdateUIContent(int totalCoins)
    {
        if (costText)
            costText.text = $"Chi phí: <color=#fbbf24><b>{reviveCost} Coin</b></color>";

        if (balanceText)
        {
            balanceText.text = hasEnoughCoins
                ? $"Bạn đang có: <color=#4ade80><b>{totalCoins} Coin</b></color> (Đủ điều kiện)"
                : $"Bạn đang có: <color=#ef4444><b>{totalCoins} Coin</b></color> (Cần thêm {reviveCost - totalCoins} Coin)";
        }

        if (reviveButton != null)
        {
            reviveButton.interactable = hasEnoughCoins;
            if (reviveButtonText != null)
                reviveButtonText.text = hasEnoughCoins
                    ? $"HỒI SINH (-{reviveCost} Coin) [Space]"
                    : $"KHÔNG ĐỦ COIN (Cần {reviveCost})";
        }
    }

    // ──────────────────────────────────────────────────────────
    // XỬ LÝ NÚT HỒI SINH
    // ──────────────────────────────────────────────────────────
    public void OnReviveClicked()
    {
        if (!isPromptActive || !hasEnoughCoins) return;

        bool spendSuccess = false;
        if (ScoreManage.Instance != null)
            spendSuccess = ScoreManage.Instance.TrySpendCoins(reviveCost);
        else if (ScoreManager.Instance != null)
            spendSuccess = ScoreManager.Instance.TrySpendCoins(reviveCost);
        else
        {
            int total = PlayerPrefs.GetInt("TotalCoins", 0);
            if (total >= reviveCost)
            {
                PlayerPrefs.SetInt("TotalCoins", total - reviveCost);
                PlayerPrefs.Save();
                spendSuccess = true;
            }
        }

        if (!spendSuccess) return;

        isPromptActive = false;
        HideAllUI();

        // Kích hoạt chuỗi hồi sinh toàn hệ thống
        GameEvents.RaisePlayerRevived();
    }

    // ──────────────────────────────────────────────────────────
    // XỬ LÝ NÚT BỎ QUA
    // ──────────────────────────────────────────────────────────
    public void OnSkipClicked()
    {
        if (!isPromptActive) return;

        isPromptActive = false;
        if (revivePanel) revivePanel.SetActive(false);

        ShowGameOverScreen();
    }

    // ──────────────────────────────────────────────────────────
    // MÀN HÌNH GAME OVER
    // ──────────────────────────────────────────────────────────
    private void ShowGameOverScreen()
    {
        if (gameOverPanel == null) return;

        if (reviveCanvas)  reviveCanvas.gameObject.SetActive(true);
        if (overlayGo)     overlayGo.SetActive(true);
        gameOverPanel.SetActive(true);

        int score     = ScoreManage.Instance != null ? ScoreManage.Instance.Score
                      : (ScoreManager.Instance != null ? ScoreManager.Instance.Score : 0);
        int highScore = ScoreManage.Instance != null ? ScoreManage.Instance.HighScore
                      : (ScoreManager.Instance != null ? ScoreManager.Instance.HighScore : 0);
        int totalCoins = GetTotalCoins();

        if (goScoreText) goScoreText.text = $"Điểm số: <color=#fbbf24><b>{score}</b></color>";
        if (goBestText)  goBestText .text = $"Kỷ lục: <color=#38bdf8><b>{highScore}</b></color>";
        if (goCoinsText) goCoinsText.text = $"Tổng Coin trong ví: <color=#4ade80><b>{totalCoins}</b></color>";

        // Gửi điểm lên Leaderboard trực tuyến nếu có kết nối
        int finalScore = Mathf.Max(score, highScore);
        if (UGSLeaderboardManager.Instance != null && finalScore > 0)
        {
            _ = UGSLeaderboardManager.Instance.SubmitScoreAsync(finalScore);
        }
    }

    // ──────────────────────────────────────────────────────────
    // CHƠI LẠI / TIỆN ÍCH
    // ──────────────────────────────────────────────────────────
    public void OpenLeaderboard()
    {
        if (LeaderboardUI.Instance != null)
        {
            LeaderboardUI.Instance.Open();
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void HideAllUI()
    {
        if (revivePanel)   revivePanel  .SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (overlayGo)     overlayGo    .SetActive(false);
        if (reviveCanvas)  reviveCanvas .gameObject.SetActive(false);
    }

    private int GetTotalCoins()
    {
        if (ScoreManage.Instance  != null) return ScoreManage.Instance.TotalCoins;
        if (ScoreManager.Instance != null) return ScoreManager.Instance.TotalCoins;
        return PlayerPrefs.GetInt("TotalCoins", 0);
    }
}
