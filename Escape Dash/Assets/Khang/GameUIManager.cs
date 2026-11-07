using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Quản lý toàn bộ UI trong Gameplay Scene (Linh / Loc):
/// - Pause Menu bật/tắt bằng phím ESC (không bắt buộc click chuột)
///   Bao gồm 3 thành phần chính: Tiếp tục (Resume), Chơi lại (Restart), Về Menu (Home)
///   Và một khu vực Cài đặt (Settings)
/// - Settings Panel điều chỉnh: Master Volume, BGM Volume, VFX Volume
/// Nằm trong thư mục Assets/Khang/.
/// </summary>
public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance { get; private set; }

    [Header("Panels")]
    [Tooltip("Panel chứa HUD hiển thị khi đang chạy (Điểm, Coin, Quãng đường)")]
    [SerializeField] private GameObject hudPanel;

    [Tooltip("Panel Tạm Dừng (Pause Menu) khi nhấn phím ESC")]
    [SerializeField] private GameObject pausePanel;

    [Tooltip("Panel Cài Đặt (Settings) chứa điều chỉnh âm thanh")]
    [SerializeField] private GameObject settingsPanel;

    [Header("Audio Settings Sliders (Trong Settings Panel)")]
    [Tooltip("Thanh trượt Master Volume (0.0 đến 1.0)")]
    [SerializeField] private Slider masterVolumeSlider;

    [Tooltip("Thanh trượt BGM Volume (0.0 đến 1.0)")]
    [SerializeField] private Slider bgmVolumeSlider;

    [Tooltip("Thanh trượt VFX Volume (0.0 đến 1.0)")]
    [SerializeField] private Slider vfxVolumeSlider;

    [Header("Controls")]
    [Tooltip("Phím tắt tạm dừng / tiếp tục game (Mặc định: ESC)")]
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
        if (settingsPanel != null) settingsPanel.SetActive(false);

        // Khởi tạo các thanh trượt âm lượng
        InitAudioSliders();
    }

    private void Update()
    {
        // Nhấn phím ESC để bật / tắt PauseMenu mà không cần click chuột
        if (Input.GetKeyDown(pauseKey))
        {
            TogglePauseWithEsc();
        }
    }

    // ==========================================
    // CÁC HÀM PAUSE MENU (KÍCH HOẠT QUA PHÍM ESC HOẶC NÚT)
    // ==========================================

    /// <summary>
    /// Xử lý nhấn phím ESC:
    /// - Nếu đang mở Settings: ESC đóng Settings, trở về Pause Menu
    /// - Nếu đang ở Pause Menu: ESC tiếp tục chơi (Resume)
    /// - Nếu đang chơi: ESC tạm dừng game (Pause)
    /// </summary>
    private void TogglePauseWithEsc()
    {
        if (settingsPanel != null && settingsPanel.activeSelf)
        {
            CloseSettings();
            return;
        }

        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    /// <summary>
    /// Tạm dừng game
    /// </summary>
    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f; // Dừng toàn bộ chuyển động trong game

        if (pausePanel != null) pausePanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    /// <summary>
    /// Thành phần 1: Tiếp tục chơi (Resume)
    /// </summary>
    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f; // Tiếp tục chạy game bình thường

        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    /// <summary>
    /// Thành phần 2: Chơi lại (Restart)
    /// </summary>
    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// Thành phần 3: Về Menu chính (Home / Main Menu)
    /// </summary>
    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0); // Load MenuScence (index 0 trong Build Settings)
    }

    // ==========================================
    // CÁC HÀM CÀI ĐẶT ÂM THANH (SETTINGS PANEL)
    // ==========================================

    /// <summary>
    /// Mở Settings phủ lên Pause Menu, giữ nguyên màn hình Tạm Dừng bên dưới.
    /// </summary>
    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }

    /// <summary>
    /// Đóng Settings; Pause Menu vẫn đang hiển thị phía dưới.
    /// </summary>
    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    private void InitAudioSliders()
    {
        float master = AudioManager.Instance != null ? AudioManager.Instance.masterVolume : PlayerPrefs.GetFloat("MasterVolume", 1f);
        float bgm    = AudioManager.Instance != null ? AudioManager.Instance.bgmVolume : PlayerPrefs.GetFloat("BGMVolume", 0.8f);
        float vfx    = AudioManager.Instance != null ? AudioManager.Instance.vfxVolume : PlayerPrefs.GetFloat("VFXVolume", 1f);

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.minValue = 0f;
            masterVolumeSlider.maxValue = 1f;
            masterVolumeSlider.value = master;
            masterVolumeSlider.onValueChanged.RemoveAllListeners();
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        }

        if (bgmVolumeSlider != null)
        {
            bgmVolumeSlider.minValue = 0f;
            bgmVolumeSlider.maxValue = 1f;
            bgmVolumeSlider.value = bgm;
            bgmVolumeSlider.onValueChanged.RemoveAllListeners();
            bgmVolumeSlider.onValueChanged.AddListener(SetBGMVolume);
        }

        if (vfxVolumeSlider != null)
        {
            vfxVolumeSlider.minValue = 0f;
            vfxVolumeSlider.maxValue = 1f;
            vfxVolumeSlider.value = vfx;
            vfxVolumeSlider.onValueChanged.RemoveAllListeners();
            vfxVolumeSlider.onValueChanged.AddListener(SetVFXVolume);
        }
    }

    /// <summary>
    /// Điều chỉnh Master Volume: Tác động đồng thời lên cả BGM và VFX
    /// </summary>
    public void SetMasterVolume(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(value);
        }
        else
        {
            PlayerPrefs.SetFloat("MasterVolume", value);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Điều chỉnh BGM Volume: Chỉ thay đổi âm lượng nhạc nền
    /// </summary>
    public void SetBGMVolume(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetBGMVolume(value);
        }
        else
        {
            PlayerPrefs.SetFloat("BGMVolume", value);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Điều chỉnh VFX Volume: Chỉ thay đổi âm lượng hiệu ứng âm thanh (nhảy, trượt, ăn coin...)
    /// </summary>
    public void SetVFXVolume(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetVFXVolume(value);
        }
        else
        {
            PlayerPrefs.SetFloat("VFXVolume", value);
            PlayerPrefs.Save();
        }
    }
}
