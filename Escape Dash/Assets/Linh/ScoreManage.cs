using UnityEngine;
using TMPro;

// Tính quãng đường, coin, điểm; lưu High Score / Best Distance bằng PlayerPrefs.
// Gắn lên 1 GameObject trong scene, kéo ChunkSpawner vào ô spawner.
// Player đứng yên, chỉ có chunk chạy, nên quãng đường lấy từ ChunkSpawner.Distance.
// MỚI: power-up DoubleScore nhân đôi điểm quãng đường VÀ số xu nhặt được.
public class ScoreManage : MonoBehaviour
{
    public static ScoreManage Instance { get; private set; }

    [Header("References")]
    [SerializeField] private ChunkSpawner spawner;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private TMP_Text distanceText;

    [Header("Settings")]
    [SerializeField] private float pointsPerMeter = 1f;
    [SerializeField] private int pointsPerCoin = 10;
    [SerializeField] private int doubleScoreMultiplier = 2;

    public float Distance { get; private set; }
    public int Coins { get; private set; }
    // MỚI: điểm quãng đường cộng dồn theo từng frame (để nhân đôi đúng đoạn có power-up)
    private float distancePoints;
    public int Score => Mathf.FloorToInt(distancePoints) + Coins * pointsPerCoin;
    public int HighScore { get; private set; }
    public float BestDistance { get; private set; }
    public bool IsNewHighScore { get; private set; }

    private float lastSpawnerDistance;   // spawner.Distance ở frame trước
    private bool running;

    private int shownScore = -1;
    private int shownCoins = -1;
    private int shownMeters = -1;

    public const string HighScoreKey = "HighScore";
    public const string BestDistanceKey = "BestDistance";
    public const string LastScoreKey = "LastScore";
    public const string LastDistanceKey = "LastDistance";
    public const string LastCoinsKey = "LastCoins";
    public const string NewHighScoreKey = "NewHighScore";

    private int Multiplier => PowerUpManager.IsActive(PowerUpType.DoubleScore) ? doubleScoreMultiplier : 1;

    private void Awake()
    {
        Instance = this;
        HighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        BestDistance = PlayerPrefs.GetFloat(BestDistanceKey, 0f);

        if (spawner == null)
            Debug.LogWarning("ScoreManage: chưa gán ChunkSpawner, quãng đường sẽ luôn bằng 0.", this);
    }

    private void OnEnable()
    {
        GameEvents.OnCoinCollected += AddCoin;
        GameEvents.OnGameOver += StopAndSave;
    }

    private void OnDisable()
    {
        GameEvents.OnCoinCollected -= AddCoin;
        GameEvents.OnGameOver -= StopAndSave;
    }

    private void Start() => ResetRun();

    public void ResetRun()
    {
        Distance = 0f;
        distancePoints = 0f;
        Coins = 0;
        IsNewHighScore = false;
        lastSpawnerDistance = spawner != null ? spawner.Distance : 0f;
        running = true;
        RefreshUI(true);
    }

    // Revive: tiếp tục tính điểm mà không reset.
    public void Resume()
    {
        if (spawner != null) lastSpawnerDistance = spawner.Distance;
        running = true;
    }

    private void Update()
    {
        if (!running || spawner == null) return;

        float current = spawner.Distance;
        float delta = Mathf.Max(0f, current - lastSpawnerDistance);
        lastSpawnerDistance = current;

        Distance += delta;
        distancePoints += delta * pointsPerMeter * Multiplier;
        RefreshUI();
    }

    private void AddCoin(int value)
    {
        if (!running) return;
        Coins += value * Multiplier;
        RefreshUI();
    }

    private void StopAndSave()
    {
        if (!running) return;
        running = false;

        IsNewHighScore = Score > HighScore;
        if (IsNewHighScore)
        {
            HighScore = Score;
            PlayerPrefs.SetInt(HighScoreKey, HighScore);
        }
        if (Distance > BestDistance)
        {
            BestDistance = Distance;
            PlayerPrefs.SetFloat(BestDistanceKey, BestDistance);
        }

        PlayerPrefs.SetInt(LastScoreKey, Score);
        PlayerPrefs.SetFloat(LastDistanceKey, Distance);
        PlayerPrefs.SetInt(LastCoinsKey, Coins);
        PlayerPrefs.SetInt(NewHighScoreKey, IsNewHighScore ? 1 : 0);

        PlayerPrefs.Save();
        RefreshUI(true);
    }

    private void RefreshUI(bool force = false)
    {
        int score = Score;
        int meters = Mathf.FloorToInt(Distance);

        if (scoreText && (force || score != shownScore))
            scoreText.text = "Score: " + score;
        if (coinText && (force || Coins != shownCoins))
            coinText.text = "Coin: " + Coins;
        if (distanceText && (force || meters != shownMeters))
            distanceText.text = "Distance: " + meters + " m";

        shownScore = score;
        shownCoins = Coins;
        shownMeters = meters;
    }
}
