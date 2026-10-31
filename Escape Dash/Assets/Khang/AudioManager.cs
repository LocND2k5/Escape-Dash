using UnityEngine;

/// <summary>
/// Quản lý toàn bộ Âm thanh: Nhạc nền (BGM) và Hiệu ứng (SFX).
/// Tự phát hiện phím điều khiển (Jump, Slide) độc lập mà không cần sửa code của Lộc.
/// Lắng nghe các event sẵn có từ GameEvents (OnCoinCollected, OnPlayerHit, OnGameOver).
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [Tooltip("AudioSource dành riêng cho BGM (bật Loop)")]
    [SerializeField] private AudioSource bgmSource;
    [Tooltip("AudioSource dành cho các hiệu ứng SFX")]
    [SerializeField] private AudioSource sfxSource;

    [Header("Background Music (BGM)")]
    public AudioClip bgmMusic;

    [Header("Sound Effects (SFX)")]
    public AudioClip jumpSound;
    public AudioClip slideSound;
    public AudioClip coinSound;
    public AudioClip hitSound;       // Khi vấp phải vật cản lần 1
    public AudioClip gameOverSound;  // Khi Game Over

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float bgmVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

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

        bgmSource.volume = bgmVolume;
        sfxSource.volume = sfxVolume;
    }

    private void Start()
    {
        PlayBGM(bgmMusic);
    }

    private void Update()
    {
        // Tự nhận diện Input phím bấm để phát SFX Jump / Slide độc lập
        // mà KHÔNG cần can thiệp hay sửa bất kỳ dòng code nào trong folder của Lộc
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
    // CÁC HÀM PHÁT ÂM THANH
    // ──────────────────────────────────────────────
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null || bgmSource == null) return;
        bgmSource.clip = clip;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        if (bgmSource != null) bgmSource.Stop();
    }

    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume * volumeScale);
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
