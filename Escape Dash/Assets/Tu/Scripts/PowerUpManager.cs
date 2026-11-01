using System;
using UnityEngine;

public enum PowerUpType { Magnet, DoubleScore, SuperJump }

// Gắn lên 1 GameObject trong GameScene (ví dụ cùng object với ScoreManage).
// Giữ thời gian còn lại của từng power-up. Script khác hỏi bằng PowerUpManager.IsActive(...).
public class PowerUpManager : MonoBehaviour
{
    public static PowerUpManager Instance { get; private set; }

    // (loại, đang bật hay tắt): UI / hiệu ứng có thể lắng nghe
    public static event Action<PowerUpType, bool> OnChanged;

    [Header("Thời gian (giây)")]
    [SerializeField] private float magnetDuration = 10f;
    [SerializeField] private float doubleScoreDuration = 10f;
    [SerializeField] private float superJumpDuration = 8f;

    private readonly float[] remaining = new float[3];

    private void Awake() => Instance = this;
    private void OnDestroy() { if (Instance == this) Instance = null; }
    private void OnEnable() => GameEvents.OnGameOver += ClearAll;
    private void OnDisable() => GameEvents.OnGameOver -= ClearAll;

    public static bool IsActive(PowerUpType t) => Instance != null && Instance.remaining[(int)t] > 0f;
    public static float Remaining(PowerUpType t) => Instance != null ? Instance.remaining[(int)t] : 0f;
    public static float Remaining01(PowerUpType t) => Instance != null ? Instance.remaining[(int)t] / Instance.Duration(t) : 0f;

    public float Duration(PowerUpType t)
    {
        switch (t)
        {
            case PowerUpType.Magnet: return magnetDuration;
            case PowerUpType.DoubleScore: return doubleScoreDuration;
            default: return superJumpDuration;
        }
    }

    // Nhặt lại khi đang bật thì tính lại từ đầu (giống Subway Surfers)
    public void Activate(PowerUpType t)
    {
        int i = (int)t;
        bool wasActive = remaining[i] > 0f;
        remaining[i] = Duration(t);
        if (!wasActive) OnChanged?.Invoke(t, true);
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        for (int i = 0; i < remaining.Length; i++)
        {
            if (remaining[i] <= 0f) continue;
            remaining[i] -= dt;
            if (remaining[i] <= 0f)
            {
                remaining[i] = 0f;
                OnChanged?.Invoke((PowerUpType)i, false);
            }
        }
    }

    public void ClearAll()
    {
        for (int i = 0; i < remaining.Length; i++)
        {
            if (remaining[i] > 0f)
            {
                remaining[i] = 0f;
                OnChanged?.Invoke((PowerUpType)i, false);
            }
        }
    }
}
