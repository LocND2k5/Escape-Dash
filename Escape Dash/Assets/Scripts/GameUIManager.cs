using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // Nếu bạn dùng TextMeshPro cho điểm số

public class GameUIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject hudPanel;       // Màn hình trong lúc chơi (Chứa nút Pause, Điểm số...)
    public GameObject pausePanel;     // Màn hình Tạm dừng
    public GameObject gameOverPanel;  // Màn hình Thua cuộc

    [Header("HUD Elements")]
    public TextMeshProUGUI scoreText; // Chữ hiển thị điểm trên HUD (tùy chọn)

    private bool isGamePaused = false;

    void Start()
    {
        // Khi mới bắt đầu scene Game, chỉ hiện HUD, tắt Pause và Game Over
        hudPanel.SetActive(true);
        pausePanel.SetActive(false);
        gameOverPanel.SetActive(false);
        
        // Đảm bảo thời gian chạy bình thường
        Time.timeScale = 1f;
        isGamePaused = false;
    }

    // ==========================================
    // CHỨC NĂNG PAUSE MENU
    // ==========================================
    public void PauseGame()
    {
        isGamePaused = true;
        pausePanel.SetActive(true);
        hudPanel.SetActive(false); // Ẩn HUD đi cho gọn (tuỳ bạn)
        Time.timeScale = 0f; // Dừng thời gian trong game
    }

    public void ResumeGame()
    {
        isGamePaused = false;
        pausePanel.SetActive(false);
        hudPanel.SetActive(true);
        Time.timeScale = 1f; // Chạy lại thời gian bình thường
    }

    // ==========================================
    // CHỨC NĂNG GAME OVER
    // ==========================================
    public void ShowGameOver()
    {
        gameOverPanel.SetActive(true);
        hudPanel.SetActive(false);
        Time.timeScale = 0f; // Dừng game
    }

    // ==========================================
    // CÁC NÚT ĐIỀU HƯỚNG CHUNG
    // ==========================================
    public void RestartGame()
    {
        // Trả lại thời gian bình thường trước khi load lại
        Time.timeScale = 1f;
        // Load lại Scene hiện tại (GameScene)
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        // Giả sử MenuScene của bạn ở vị trí số 0 trong Build Settings
        SceneManager.LoadScene(0);
    }
}
