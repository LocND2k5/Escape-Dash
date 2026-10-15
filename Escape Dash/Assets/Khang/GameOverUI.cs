using UnityEngine;
using UnityEngine.SceneManagement;

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

    private void Start()
    {
        // Check if we came here because of a Game Over
        if (PlayerPrefs.GetInt(GameOverLoader.GameOverKey, 0) == 1)
        {
            // Clear the flag
            PlayerPrefs.SetInt(GameOverLoader.GameOverKey, 0);
            PlayerPrefs.Save();

            // Show Game Over panel, hide Main Menu
            if (gameOverPanel)  gameOverPanel.SetActive(true);
            if (mainMenuPanel)  mainMenuPanel.SetActive(false);
        }
        else
        {
            // Normal entry → hide Game Over panel, show Main Menu
            if (gameOverPanel)  gameOverPanel.SetActive(false);
            if (mainMenuPanel)  mainMenuPanel.SetActive(true);
        }
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
