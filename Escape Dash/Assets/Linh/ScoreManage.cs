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
    public int Score => Mathf.FloorToInt(Distance * pointsPerMeter) + Coins * pointsPerCoin;
    public int HighScore { get; private set; }
    public float BestDistance { get; private set; }

    private float startDistance;   // giá trị spawner.Distance lúc bắt đầu ván
    private bool running;

    private const string HighScoreKey = "HighScore";
    private const string BestDistanceKey = "BestDistance";

    private void Awake()
    {
        Instance = this;
        HighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        BestDistance = PlayerPrefs.GetFloat(BestDistanceKey, 0f);
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

    // Gọi khi bắt đầu ván mới (hoặc revive nếu muốn giữ điểm thì đừng gọi hàm này).
    public void ResetRun()
    {
        Distance = 0f;
        Coins = 0;
        startDistance = spawner != null ? spawner.Distance : 0f;
        running = true;
        RefreshUI();
    }

    // Revive: tiếp tục tính điểm mà không reset.
    public void Resume()
    {
        if (spawner != null) startDistance = spawner.Distance - Distance;
        running = true;
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
        RefreshUI();
    }

    private void StopAndSave()
    {
        if (!running) return;
        running = false;

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
        PlayerPrefs.Save();
    }

    private void RefreshUI()
    {
        if (scoreText) scoreText.text = "Score: "+ Score.ToString();
        if (coinText) coinText.text ="Coin: "+ Coins.ToString();
        if (distanceText) distanceText.text ="Distance: "+ Mathf.FloorToInt(Distance) + " m";
    }
}