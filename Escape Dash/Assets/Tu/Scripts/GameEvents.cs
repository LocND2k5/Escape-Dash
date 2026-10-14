using System;

// Cầu nối giữa Obstacle/Coin/Score và GameManager (Lộc), UI (Bảo Khang), Audio.
public static class GameEvents
{
    public static event Action OnGameOver;
    public static event Action<int> OnCoinCollected;      // giá trị coin

    public static event Action OnPlayerHit;               // Obstacle -> PlayerController
    public static event Action<float> OnPlayerStumbled;   // PlayerController -> Bull

    public static void RaiseGameOver() => OnGameOver?.Invoke();
    public static void RaiseCoinCollected(int value) => OnCoinCollected?.Invoke(value);
    public static void RaisePlayerHit() => OnPlayerHit?.Invoke();
    public static void RaisePlayerStumbled(float duration) => OnPlayerStumbled?.Invoke(duration);
}