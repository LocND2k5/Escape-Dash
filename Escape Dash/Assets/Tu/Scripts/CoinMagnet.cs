using UnityEngine;

// Gắn lên Player (root). Khi power-up Magnet bật, hút các Coin trong bán kính về phía Player.
public class CoinMagnet : MonoBehaviour
{
    [SerializeField] private float radius = 8f;        // hút coin trong bán kính này
    [SerializeField] private float pullSpeed = 28f;    // tốc độ coin bay về Player
    [SerializeField] private float targetHeight = 0.9f; // coin bay về điểm này (ngang thân)
    [SerializeField] private float collectDistance = 0.4f;

    private void Update()
    {
        if (!PowerUpManager.IsActive(PowerUpType.Magnet)) return;

        Vector3 target = transform.position + Vector3.up * targetHeight;
        float r2 = radius * radius;
        float dt = Time.deltaTime;

        // Duyệt ngược vì Collect() sẽ xoá coin khỏi danh sách
        var coins = Coin.Active;
        for (int i = coins.Count - 1; i >= 0; i--)
        {
            Coin c = coins[i];
            Vector3 to = target - c.transform.position;
            if (to.sqrMagnitude > r2) continue;

            if (to.sqrMagnitude <= collectDistance * collectDistance)
                c.Collect();
            else
                c.transform.position = Vector3.MoveTowards(c.transform.position, target, pullSpeed * dt);
        }
    }
}
