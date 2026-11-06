using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
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

    [Header("Settings Audio Sliders")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider bgmVolumeSlider;
    [SerializeField] private Slider vfxVolumeSlider;

    [Header("Input Field")]
    [Tooltip("TMP_InputField nhập tên người chơi")]
    [SerializeField] private TMP_InputField nameInputField;

    [Header("Game Over Score")]
    [Tooltip("Số giây tối đa để chạy hiệu ứng đếm điểm.")]
    [SerializeField] private float scoreCountDuration = 2f;
    [Tooltip("TMP Text do bạn tạo trong GameOverPanel để hiển thị điểm.")]
    [SerializeField] private TMP_Text gameOverScoreText;

    [Header("Coin Display")]
    [Tooltip("Gán TextMeshPro dùng để hiển thị tổng coin trong menu.")]
    [SerializeField] private TMP_Text coinBalanceText;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }
    }

    private void Start()
    {
        // Luôn trả tốc độ game về 1 khi vào Menu
        Time.timeScale = 1f;

        InitAudioSliders();
        if (coinBalanceText == null)
            Debug.LogWarning("MenuSceneManager: Assign the menu coin balance TMP text in the Inspector.", this);
        RefreshCoinBalanceDisplay();
        if (gameOverScoreText != null)
            gameOverScoreText.text = "";

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
            ShowLastRunScore();
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
        RefreshCoinBalanceDisplay();
    }

    private void RefreshCoinBalanceDisplay()
    {
        if (coinBalanceText == null)
            return;

        int totalCoins = PlayerPrefs.GetInt("TotalCoins", 0);
        coinBalanceText.text = totalCoins.ToString("N0");
    }

    private void ShowLastRunScore()
    {
        if (gameOverScoreText == null)
        {
            Debug.LogError("MenuSceneManager: Assign the Game Over score TMP text in the Inspector.", this);
            return;
        }

        int lastScore = PlayerPrefs.GetInt(ScoreManage.LastScoreKey, 0);
        bool isPersonalBest = PlayerPrefs.GetInt(ScoreManage.NewHighScoreKey, 0) == 1;
        gameOverScoreText.textWrappingMode = TextWrappingModes.NoWrap;
        gameOverScoreText.text = isPersonalBest ? "BEST SCORE: 0" : "SCORE: 0";
        gameOverScoreText.color = isPersonalBest ? new Color(0.35f, 1f, 0.55f) : Color.white;
        StartCoroutine(AnimateGameOverScore(lastScore, isPersonalBest));
    }

    private IEnumerator AnimateGameOverScore(int targetScore, bool isPersonalBest)
    {
        float duration = Mathf.Max(0.01f, scoreCountDuration);
        float elapsed = 0f;
        string title = isPersonalBest ? "BEST SCORE" : "SCORE";

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            int displayedScore = Mathf.FloorToInt(targetScore * progress);
            gameOverScoreText.text = $"{title}: {displayedScore:N0}";

            if (isPersonalBest)
            {
                float blink = (Mathf.Sin(Time.unscaledTime * 8f) + 1f) * 0.5f;
                gameOverScoreText.color = Color.Lerp(
                    new Color(0.15f, 0.65f, 0.28f),
                    new Color(0.5f, 1f, 0.65f),
                    blink);
            }

            yield return null;
        }

        gameOverScoreText.text = $"{title}: {targetScore:N0}";
        if (!isPersonalBest)
            gameOverScoreText.color = Color.white;
    }

    private void InitAudioSliders()
    {
        SetupSlider(
            masterVolumeSlider,
            AudioManager.Instance != null ? AudioManager.Instance.masterVolume : PlayerPrefs.GetFloat("MasterVolume", 1f),
            SetMasterVolume);
        SetupSlider(
            bgmVolumeSlider,
            AudioManager.Instance != null ? AudioManager.Instance.bgmVolume : PlayerPrefs.GetFloat("BGMVolume", 0.8f),
            SetBGMVolume);
        SetupSlider(
            vfxVolumeSlider,
            AudioManager.Instance != null ? AudioManager.Instance.vfxVolume : PlayerPrefs.GetFloat("VFXVolume", 1f),
            SetVFXVolume);
    }

    private void SetupSlider(Slider slider, float value, UnityEngine.Events.UnityAction<float> callback)
    {
        if (slider == null)
            return;

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = Mathf.Clamp01(value);
        slider.onValueChanged.RemoveAllListeners();
        slider.onValueChanged.AddListener(callback);
    }

    public void SetMasterVolume(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMasterVolume(value);
        else
        {
            PlayerPrefs.SetFloat("MasterVolume", Mathf.Clamp01(value));
            PlayerPrefs.Save();
        }
    }

    public void SetBGMVolume(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetBGMVolume(value);
        else
        {
            PlayerPrefs.SetFloat("BGMVolume", Mathf.Clamp01(value));
            PlayerPrefs.Save();
        }
    }

    public void SetVFXVolume(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetVFXVolume(value);
        else
        {
            PlayerPrefs.SetFloat("VFXVolume", Mathf.Clamp01(value));
            PlayerPrefs.Save();
        }
    }

    // ── Hàm phụ trợ tải Gameplay Scene ──
    private void LoadGameplayScene()
    {
        Time.timeScale = 1f;

        if (Application.CanStreamedLevelBeLoaded("GamePlay"))
            SceneManager.LoadScene("GamePlay");
        {
            Debug.LogError(
                "MenuSceneManager: Gameplay scene is missing from Build Settings. " +
                "Add GamePlay.unity before building.");
        }
    }
}
