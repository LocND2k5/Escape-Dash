using UnityEngine;
using TMPro;

// Tính quãng đường, coin, điểm; lưu High Score / Best Distance bằng PlayerPrefs.
// Gắn lên 1 GameObject trong scene, kéo TrackManager vào ô trackManager.
// (Map chạy, Player đứng yên nên quãng đường lấy từ TrackManager.DistanceTravelled.
//  Nếu không gán TrackManager thì dùng vị trí Z của Player như cũ.)
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private TrackManager trackManager;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private TMP_Text distanceText;
    // MỚI: (tùy chọn) hiện High Score ngay trên HUD lúc đang chơi
    [SerializeField] private TMP_Text highScoreText;

    [Header("Settings")]
    [SerializeField] private float pointsPerMeter = 1f;
    [SerializeField] private int pointsPerCoin = 10;

    public float Distance { get; private set; }
    public int Coins { get; private set; }
    public int TotalCoins { get; private set; }
    public int Score => Mathf.FloorToInt(Distance * pointsPerMeter) + Coins * pointsPerCoin;
    public int HighScore { get; private set; }
    public float BestDistance { get; private set; }

    private float startDistance;
    private bool running;

    // MỚI: đổi từ private -> public để scene Khang (GameOverUI, MainMenu) đọc được cùng key
    public const string HighScoreKey = "HighScore";
    public const string BestDistanceKey = "BestDistance";
    private const string TotalCoinsKey = "TotalCoins";

    // MỚI: kết quả của ván vừa chơi, để scene Khang hiển thị sau khi chết
    public const string LastScoreKey = "LastScore";
    public const string LastDistanceKey = "LastDistance";
    public const string LastCoinsKey = "LastCoins";
    public const string NewHighScoreKey = "NewHighScore"; // 1 = ván vừa rồi phá kỷ lục

    private void Awake()
    {
        Instance = this;
        HighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        BestDistance = PlayerPrefs.GetFloat(BestDistanceKey, 0f);
        TotalCoins = PlayerPrefs.GetInt(TotalCoinsKey, 0);
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

    public bool TrySpendCoins(int amount)
    {
        if (TotalCoins >= amount)
        {
            TotalCoins -= amount;
            PlayerPrefs.SetInt(TotalCoinsKey, TotalCoins);
            PlayerPrefs.Save();
            RefreshUI();
            return true;
        }
        return false;
    }

    private void Start() => ResetRun();

    // Tổng quãng đường hiện tại (map cuộn hoặc Player chạy)
    private float CurrentTotalDistance()
    {
        if (trackManager != null) return trackManager.DistanceTravelled;
        return player != null ? player.position.z : 0f;
    }

    // Gọi khi bắt đầu ván mới (hoặc revive nếu muốn giữ điểm thì đừng gọi hàm này).
    public void ResetRun()
    {
        Distance = 0f;
        Coins = 0;
        startDistance = CurrentTotalDistance();
        running = true;
        RefreshUI();
    }

    // Revive: tiếp tục tính điểm mà không reset.
    public void Resume()
    {
        startDistance = CurrentTotalDistance() - Distance;
        running = true;
    }

    private void Update()
    {
        if (!running) return;
        Distance = Mathf.Max(0f, CurrentTotalDistance() - startDistance);
        RefreshUI();
    }

    private void AddCoin(int value)
    {
        if (!running) return;
        Coins += value;
        RefreshUI();
    }

    private void StopAndSave()
    {
        if (!running) return;
        running = false;

        // MỚI: nhớ xem ván này có phá kỷ lục không (phải xét TRƯỚC khi ghi đè HighScore)
        bool isNewHighScore = Score > HighScore;

        if (Score > HighScore)
        {
            HighScore = Score;
            PlayerPrefs.SetInt(HighScoreKey, HighScore);
        }
        if (Distance > BestDistance)
        {
            BestDistance = Distance;
            PlayerPrefs.SetFloat(BestDistanceKey, BestDistance);
        }

        // MỚI: lưu kết quả ván vừa chơi cho màn Game Over bên scene Khang đọc
        PlayerPrefs.SetInt(LastScoreKey, Score);
        PlayerPrefs.SetFloat(LastDistanceKey, Distance);
        PlayerPrefs.SetInt(LastCoinsKey, Coins);
        PlayerPrefs.SetInt(NewHighScoreKey, isNewHighScore ? 1 : 0);

        PlayerPrefs.Save();

        // Tự động gửi điểm lên Bảng Xếp Hạng Trực Tuyến UGS
        if (UGSLeaderboardManager.Instance != null && Score > 0)
        {
            _ = UGSLeaderboardManager.Instance.SubmitScoreAsync(Score);
        }
    }

    private void RefreshUI()
    {
        if (scoreText) scoreText.text = Score.ToString();
        if (coinText) coinText.text = Coins.ToString();
        if (distanceText) distanceText.text = Mathf.FloorToInt(Distance) + " m";

        // MỚI: High Score trên HUD (hiện kỷ lục cũ; nếu ván này vượt thì hiện điểm hiện tại)
        if (highScoreText) highScoreText.text = "BEST: " + Mathf.Max(HighScore, Score);
    }
}