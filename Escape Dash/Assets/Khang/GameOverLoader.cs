using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach to any GameObject in the Linh (Gameplay) scene.
/// When the player hits an obstacle → GameEvents.OnGameOver fires
/// → saves a flag → loads back to Khang (Hub) scene
/// → Khang's GameOverUI reads the flag and shows the Game Over panel.
/// </summary>
public class GameOverLoader : MonoBehaviour
{
    // Key used to signal Khang scene that Game Over should be shown
    public const string GameOverKey = "ShowGameOver";

    private void OnEnable()  => GameEvents.OnGameOver += HandleGameOver;
    private void OnDisable() => GameEvents.OnGameOver -= HandleGameOver;

    private void HandleGameOver()
    {
        // Save flag so Khang scene knows to show Game Over panel
        PlayerPrefs.SetInt(GameOverKey, 1);
        PlayerPrefs.Save();

        // Reset time before switching scenes
        Time.timeScale = 1f;

        // Load Khang hub scene (index 0 in Build Settings)
        SceneManager.LoadScene(0);
    }
}
