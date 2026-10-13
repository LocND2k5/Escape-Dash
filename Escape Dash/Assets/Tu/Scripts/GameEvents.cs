using System;

// Cầu nối giữa Obstacle/Coin/Score và GameManager (Lộc), UI (Bảo Khang), Audio.
public static class GameEvents
{
    public static event Action OnGameOver;
    public static event Action<int> OnCoinCollected; // giá trị coin

    public static void RaiseGameOver() => OnGameOver?.Invoke();
    public static void RaiseCoinCollected(int value) => OnCoinCollected?.Invoke(value);
}
