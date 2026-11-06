using UnityEngine;

/// <summary>
/// Quản lý toàn bộ Âm thanh: Nhạc nền (BGM) và Hiệu ứng (SFX / VFX).
/// Tự phát hiện phím điều khiển (Jump, Slide) độc lập mà không cần sửa code của Lộc.
/// Lắng nghe các event sẵn có từ GameEvents (OnCoinCollected, OnPlayerHit, OnGameOver).
/// Hỗ trợ Master Volume điều khiển tổng, BGM Volume và VFX Volume riêng biệt.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [Tooltip("AudioSource dành riêng cho BGM (bật Loop)")]
    [SerializeField] private AudioSource bgmSource;
    [Tooltip("AudioSource dành cho các hiệu ứng SFX / VFX")]
    [SerializeField] private AudioSource sfxSource;

    [Header("Background Music (BGM)")]
    public AudioClip bgmMusic;

    [Header("Sound Effects (SFX / VFX)")]
    public AudioClip jumpSound;
    public AudioClip slideSound;
    public AudioClip coinSound;
    public AudioClip hitSound;       // Khi vấp phải vật cản lần 1
    public AudioClip gameOverSound;  // Khi Game Over

    [Header("Volume Settings (0.0 to 1.0)")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float bgmVolume = 0.8f;
    [Range(0f, 1f)] public float vfxVolume = 1f;

    // Getter tương thích ngược nếu có script cũ gọi sfxVolume
    public float sfxVolume => vfxVolume;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Tải các mức âm lượng đã lưu từ PlayerPrefs
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        bgmVolume    = PlayerPrefs.GetFloat("BGMVolume", 0.8f);
        vfxVolume    = PlayerPrefs.GetFloat("VFXVolume", 1f);

        SetupSources();
    }

    private void SetupSources()
    {
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }

        ApplyVolumes();
    }

    private void Start()
    {
        PlayBGM(bgmMusic);
    }

    private void Update()
    {
        // Tự nhận diện Input phím bấm để phát SFX Jump / Slide độc lập
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space))
        {
            PlaySFX(jumpSound);
        }

        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            PlaySFX(slideSound);
        }
    }

    private void OnEnable()
    {
        GameEvents.OnCoinCollected += PlayCoinSFX;
        GameEvents.OnPlayerHit     += PlayHitSFX;
        GameEvents.OnGameOver      += PlayGameOverSFX;
    }

    private void OnDisable()
    {
        GameEvents.OnCoinCollected -= PlayCoinSFX;
        GameEvents.OnPlayerHit     -= PlayHitSFX;
        GameEvents.OnGameOver      -= PlayGameOverSFX;
    }

    // ──────────────────────────────────────────────
    // CÁC HÀM ĐIỀU CHỈNH ÂM LƯỢNG (SETTINGS)
    // ──────────────────────────────────────────────

    /// <summary>
    /// Điều chỉnh Master Volume: Thay đổi âm lượng tổng,
    /// đồng thời ảnh hưởng cả BGM và VFX ngay lập tức.
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.Save();
        ApplyVolumes();
    }

    /// <summary>
    /// Điều chỉnh BGM Volume: Chỉ ảnh hưởng nhạc nền,
    /// không làm thay đổi âm lượng VFX.
    /// </summary>
    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("BGMVolume", bgmVolume);
        PlayerPrefs.Save();
        ApplyVolumes();
    }

    /// <summary>
    /// Điều chỉnh VFX Volume: Chỉ ảnh hưởng hiệu ứng âm thanh (nhảy, trượt, ăn coin...),
    /// không làm thay đổi BGM.
    /// </summary>
    public void SetVFXVolume(float volume)
    {
        vfxVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("VFXVolume", vfxVolume);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Áp dụng âm lượng thực tế cho BGM Source dựa trên Master và BGM.
    /// </summary>
    public void ApplyVolumes()
    {
        if (bgmSource != null)
        {
            bgmSource.volume = masterVolume * bgmVolume;
        }
    }

    // ──────────────────────────────────────────────
    // CÁC HÀM PHÁT ÂM THANH
    // ──────────────────────────────────────────────

    public void PlayBGM(AudioClip clip)
    {
        if (clip == null || bgmSource == null) return;
        bgmSource.clip = clip;
        bgmSource.volume = masterVolume * bgmVolume;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        if (bgmSource != null) bgmSource.Stop();
    }

    /// <summary>
    /// Phát hiệu ứng âm thanh (VFX / SFX).
    /// Âm lượng phát ra được nhân đồng thời với Master Volume và VFX Volume.
    /// </summary>
    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || sfxSource == null) return;
        float finalVolume = masterVolume * vfxVolume * volumeScale;
        sfxSource.PlayOneShot(clip, finalVolume);
    }

    private void PlayCoinSFX(int coinValue)
    {
        PlaySFX(coinSound);
    }

    private void PlayHitSFX()
    {
        PlaySFX(hitSound);
    }

    private void PlayGameOverSFX()
    {
        StopBGM();
        PlaySFX(gameOverSound);
    }
}
