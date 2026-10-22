using UnityEngine;
using TMPro;

// Tính quãng đường, coin, điểm; lưu High Score / Best Distance bằng PlayerPrefs.
// Gắn lên 1 GameObject trong scene, kéo ChunkSpawner vào ô spawner.
// Player đứng yên, chỉ có chunk chạy, nên quãng đường lấy từ ChunkSpawner.Distance
// (không đọc player.position.z nữa).
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

    public float Distance { get; private set; }
    public int Coins { get; private set; }
    public int TotalCoins { get; private set; } // Tổng coin tích luỹ qua các ván
    public int Score => Mathf.FloorToInt(Distance * pointsPerMeter) + Coins * pointsPerCoin;
    public int HighScore { get; private set; }
    public float BestDistance { get; private set; }
    public bool IsNewHighScore { get; private set; }   // true nếu ván vừa rồi phá kỷ lục (cho UI Game Over)

    private float startDistance;   // giá trị spawner.Distance lúc bắt đầu ván
    private bool running;

    // Giá trị đang hiển thị: chỉ cập nhật chữ khi có thay đổi (tránh tạo chuỗi mới mỗi frame)
    private int shownScore = -1;
    private int shownCoins = -1;
    private int shownMeters = -1;

    private const string HighScoreKey = "HighScore";
    private const string BestDistanceKey = "BestDistance";
    private const string TotalCoinsKey = "TotalCoins";

    private void Awake()
    {
        Instance = this;
        HighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        BestDistance = PlayerPrefs.GetFloat(BestDistanceKey, 0f);
        TotalCoins = PlayerPrefs.GetInt(TotalCoinsKey, 0);

        if (spawner == null)
            Debug.LogWarning("ScoreManage: chưa gán ChunkSpawner, quãng đường sẽ luôn bằng 0.", this);
    }

    private void OnEnable()
    {
        GameEvents.OnCoinCollected += AddCoin;
        GameEvents.OnGameOver += StopAndSave;
        GameEvents.OnPlayerRevived += Resume;
    }

    private void OnDisable()
    {
        GameEvents.OnCoinCollected -= AddCoin;
        GameEvents.OnGameOver -= StopAndSave;
        GameEvents.OnPlayerRevived -= Resume;
    }

    private void Start() => ResetRun();

    // Gọi khi bắt đầu ván mới (hoặc revive nếu muốn giữ điểm thì đừng gọi hàm này).
    public void ResetRun()
    {
        Distance = 0f;
        Coins = 0;
        IsNewHighScore = false;
        startDistance = spawner != null ? spawner.Distance : 0f;
        running = true;
        RefreshUI(true);
    }

    // Revive: tiếp tục tính điểm mà không reset.
    public void Resume()
    {
        if (spawner != null) startDistance = spawner.Distance - Distance;
        running = true;
        RefreshUI(true);
    }

    // Trừ coin khi hồi sinh
    public bool TrySpendCoins(int amount)
    {
        if (TotalCoins >= amount)
        {
            TotalCoins -= amount;
            PlayerPrefs.SetInt(TotalCoinsKey, TotalCoins);
            PlayerPrefs.Save();
            RefreshUI(true);
            return true;
        }
        return false;
    }

    private void Update()
    {
        if (!running || spawner == null) return;
        Distance = Mathf.Max(0f, spawner.Distance - startDistance);
        RefreshUI();
    }

    private void AddCoin(int value)
    {
        if (!running) return;
        Coins += value;
        TotalCoins += value; // Lưu vĩnh viễn vào ví
        PlayerPrefs.SetInt(TotalCoinsKey, TotalCoins);
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
        PlayerPrefs.SetInt(TotalCoinsKey, TotalCoins);
        PlayerPrefs.Save();
        RefreshUI(true);

        // Tự động gửi điểm lên Bảng Xếp Hạng Trực Tuyến UGS
        if (UGSLeaderboardManager.Instance != null && Score > 0)
        {
            _ = UGSLeaderboardManager.Instance.SubmitScoreAsync(Score);
        }
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