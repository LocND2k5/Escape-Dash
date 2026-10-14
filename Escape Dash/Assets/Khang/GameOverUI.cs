using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Attach to the GameOverPanel.
/// Listens to GameEvents.OnGameOver → shows this panel automatically.
/// Reads final stats from ScoreManage.Instance.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("Panel")]
    [Tooltip("The root Game Over panel GameObject (this panel itself)")]
    [SerializeField] private GameObject gameOverPanel;

    [Header("Stat Labels")]
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private TMP_Text highScoreText;
    [SerializeField] private TMP_Text finalDistanceText;
    [SerializeField] private TMP_Text bestDistanceText;
    [SerializeField] private TMP_Text finalCoinsText;

    // ──────────────────────────────────────────────
    //  Unity lifecycle
    // ──────────────────────────────────────────────
    private void Awake()
    {
        // Hide the panel at the start of the scene
        if (gameOverPanel) gameOverPanel.SetActive(false);
    }

    private void OnEnable()  => GameEvents.OnGameOver += ShowGameOver;
    private void OnDisable() => GameEvents.OnGameOver -= ShowGameOver;

    // ──────────────────────────────────────────────
    //  Called automatically when the player dies
    // ──────────────────────────────────────────────
    private void ShowGameOver()
    {
        // Freeze the game
        Time.timeScale = 0f;

        // Populate labels from ScoreManage
        if (ScoreManage.Instance != null)
        {
            var sm = ScoreManage.Instance;

            if (finalScoreText)    finalScoreText.text    = "Score: "         + sm.Score;
            if (highScoreText)     highScoreText.text     = "Best Score: "    + sm.HighScore;
            if (finalDistanceText) finalDistanceText.text = "Distance: "      + Mathf.FloorToInt(sm.Distance) + " m";
            if (bestDistanceText)  bestDistanceText.text  = "Best Distance: " + Mathf.FloorToInt(sm.BestDistance) + " m";
            if (finalCoinsText)    finalCoinsText.text    = "Coins: "         + sm.Coins;
        }

        // Show the panel
        if (gameOverPanel) gameOverPanel.SetActive(true);
    }

    // ──────────────────────────────────────────────
    //  Button callbacks (wire these up in Inspector)
    // ──────────────────────────────────────────────

    /// <summary>Restart button → reload this gameplay scene.</summary>
    public void OnRestartButton()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>Home button → go back to the Main Menu (Build index 0).</summary>
    public void OnHomeButton()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }
}
