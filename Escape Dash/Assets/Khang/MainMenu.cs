using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenu : MonoBehaviour
{
    [Header("Name Input Popup")]
    [Tooltip("Kéo Panel chứa ô nhập tên vào đây (mặc định ẩn, chỉ hiện khi bấm Play)")]
    [SerializeField] private GameObject namePopupPanel;

    [Tooltip("Kéo TMP_InputField nhập tên vào đây")]
    [SerializeField] private TMP_InputField nameInputField;

    private void Start()
    {
        // Ẩn Popup ngay từ đầu khi mở game lên
        if (namePopupPanel != null)
            namePopupPanel.SetActive(false);

        // Luôn để ô nhập tên trống
        if (nameInputField != null)
            nameInputField.text = "";
    }

    // ── Gọi khi bấm nút PLAY trên Menu chính ──
    // Thay vì vào game ngay, chỉ MỞ POPUP nhập tên
    public void OnPlayButtonClicked()
    {
        if (namePopupPanel != null)
        {
            namePopupPanel.SetActive(true);

            // Luôn để ô nhập tên trống để người chơi tự do gõ tên mới
            if (nameInputField != null)
                nameInputField.text = "";
        }
    }

    // ── Gọi khi bấm nút "Xác nhận / Bắt đầu chơi" bên trong Popup ──
    public void OnConfirmNameClicked()
    {
        string enteredName = nameInputField != null ? nameInputField.text.Trim() : "";

        // Nếu người chơi để trống, tự tạo một tên ngẫu nhiên
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

        // Vào game!
        if (Application.CanStreamedLevelBeLoaded("Loc"))
            SceneManager.LoadScene("Loc");
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    // ── Gọi khi bấm nút "Hủy" hoặc "X" trong Popup ──
    public void OnCancelNameClicked()
    {
        if (namePopupPanel != null)
            namePopupPanel.SetActive(false);
    }

    // ── Mở Bảng Xếp Hạng ──
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

    // ── Thoát Game ──
    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
