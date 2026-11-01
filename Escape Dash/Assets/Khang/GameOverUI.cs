using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // MỚI: để dùng TMP_Text

/// <summary>
/// Attach to GameOverPanel inside Khang (Hub) scene.
/// On Start: checks if the player just died (flag from GameOverLoader).
///   → Yes: hides MainMenu, shows Game Over panel.
///   → No:  stays hidden, MainMenu shows normally.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject mainMenuPanel; // Assign MenuPanel from Khang scene

    // MỚI: các text hiển thị kết quả (tùy chọn, không gán thì bỏ qua)
    [Header("Result Texts")]
    [SerializeField] private TMP_Text scoreText;        // Điểm ván vừa chơi
    [SerializeField] private TMP_Text highScoreText;    // Kỷ lục điểm
    [SerializeField] private TMP_Text distanceText;     // Quãng đường ván vừa chơi
    [SerializeField] private TMP_Text bestDistanceText; // Quãng đường xa nhất
    [SerializeField] private TMP_Text coinText;         // Số coin ván vừa chơi
    [SerializeField] private GameObject newHighScoreLabel; // Chữ/hình "NEW HIGH SCORE!"

    private void Start()
    {
        // Check if we came here because of a Game Over
        if (PlayerPrefs.GetInt(GameOverLoader.GameOverKey, 0) == 1)
        {
            // Clear the flag
            PlayerPrefs.SetInt(GameOverLoader.GameOverKey, 0);
            PlayerPrefs.Save();

            // MỚI: đổ kết quả ván vừa chơi + kỷ lục lên panel
            ShowResults();

            // Show Game Over panel, hide Main Menu
            if (gameOverPanel) gameOverPanel.SetActive(true);
            if (mainMenuPanel) mainMenuPanel.SetActive(false);
        }
        else
        {
            // Normal entry → hide Game Over panel, show Main Menu
            if (gameOverPanel) gameOverPanel.SetActive(false);
            if (mainMenuPanel) mainMenuPanel.SetActive(true);
        }
    }

    // MỚI: đọc dữ liệu do ScoreManage lưu (cùng key) và hiển thị
    // MỚI: mỗi dòng có chữ nhãn rõ ràng phía trước con số (SCORE: 16, COIN: 0...)
    private void ShowResults()
    {
        int lastScore = PlayerPrefs.GetInt(ScoreManage.LastScoreKey, 0);
        float lastDistance = PlayerPrefs.GetFloat(ScoreManage.LastDistanceKey, 0f);
        int lastCoins = PlayerPrefs.GetInt(ScoreManage.LastCoinsKey, 0);
        int highScore = PlayerPrefs.GetInt(ScoreManage.HighScoreKey, 0);
        float bestDistance = PlayerPrefs.GetFloat(ScoreManage.BestDistanceKey, 0f);
        bool isNewHighScore = PlayerPrefs.GetInt(ScoreManage.NewHighScoreKey, 0) == 1;

        if (scoreText) scoreText.text = "SCORE: " + lastScore;
        if (highScoreText) highScoreText.text = "HIGH SCORE: " + highScore;
        if (coinText) coinText.text = "COIN: " + lastCoins;
        if (distanceText) distanceText.text = "DISTANCE: " + Mathf.FloorToInt(lastDistance) + " m";
        if (bestDistanceText) bestDistanceText.text = "BEST DISTANCE: " + Mathf.FloorToInt(bestDistance) + " m";
        if (newHighScoreLabel) newHighScoreLabel.SetActive(isNewHighScore);
    }

    // ── Button callbacks ──────────────────────────────────────────
    public void OnRestartButton()
    {
        Time.timeScale = 1f;
        // Load the gameplay scene (Linh = index 1 in Build Settings)
        SceneManager.LoadScene(1);
    }

    public void OnHomeButton()
    {
        // Already in Khang, just switch panels
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (mainMenuPanel) mainMenuPanel.SetActive(true);
    }
}