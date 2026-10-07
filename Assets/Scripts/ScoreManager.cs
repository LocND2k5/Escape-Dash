using UnityEngine;
using TMPro;

// Tính quãng đường, coin, điểm; lưu High Score / Best Distance bằng PlayerPrefs.
// Gắn lên 1 GameObject trong scene, kéo Player vào ô player.
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Transform player;
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

    private float startZ;
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
        startZ = player != null ? player.position.z : 0f;
        running = true;
        RefreshUI();
    }

    // Revive: tiếp tục tính điểm mà không reset.
    public void Resume()
    {
        startZ = player.position.z - Distance;
        running = true;
    }

    private void Update()
    {
        if (!running || player == null) return;
        Distance = Mathf.Max(0f, player.position.z - startZ);
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
        if (scoreText) scoreText.text = Score.ToString();
        if (coinText) coinText.text = Coins.ToString();
        if (distanceText) distanceText.text = Mathf.FloorToInt(Distance) + " m";
    }
}
