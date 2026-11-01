using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // MỚI: để dùng TMP_Text

public class MainMenu : MonoBehaviour
{
    // MỚI: (tùy chọn) text hiện kỷ lục ngay trên menu chính
    [Header("High Score (tùy chọn)")]
    [SerializeField] private TMP_Text highScoreText;
    [SerializeField] private TMP_Text bestDistanceText;

    // MỚI: đọc kỷ lục từ PlayerPrefs mỗi khi menu bật lên
    private void Start()
    {
        int highScore = PlayerPrefs.GetInt(ScoreManage.HighScoreKey, 0);
        float bestDistance = PlayerPrefs.GetFloat(ScoreManage.BestDistanceKey, 0f);

        if (highScoreText) highScoreText.text = "HIGH SCORE: " + highScore;
        if (bestDistanceText) bestDistanceText.text = "BEST DISTANCE: " + Mathf.FloorToInt(bestDistance) + " m";
    }

    // Called by the Start/Play button
    public void PlayGame()
    {
        // Loads the next scene in Build Settings (index 0 = Menu, index 1 = Gameplay)
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    // Called by the Exit button
    public void QuitGame()
    {
        Debug.Log("Quit Game!");
        Application.Quit(); // Works in a built/standalone player

#if UNITY_EDITOR
        // Also stops Play mode when testing inside the Unity Editor
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}