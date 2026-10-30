using System.Collections.Generic;
using UnityEngine;

// Gắn lên mỗi Chunk prefab (cùng object với script Chunk).
// Chunk chỉ còn Road + CityS. Script này tự rải obstacle, coin và power-up ngẫu nhiên.
// Chunk.ResetState() gọi Generate() mỗi lần chunk được tái sử dụng.
// Yêu cầu: root của prefab obstacle/coin/power-up đặt ở X=0, Z=0 và pivot đúng chỗ. Script chỉ đổi X (làn) và Z (hàng),
// giữ nguyên Y, rotation, scale của prefab.
[RequireComponent(typeof(Chunk))]
public class ChunkRandomizer : MonoBehaviour
{
    [System.Serializable]
    public class Option
    {
        public GameObject prefab;          // prefab có script Obstacle (Light / Heavy chọn trên prefab)
        public float weight = 1f;          // càng lớn càng hay xuất hiện
        public int poolSize = 6;           // số bản tạo sẵn trong 1 chunk
    }

    [System.Serializable]
    public class PowerUpOption
    {
        public GameObject prefab;          // prefab có script PowerUpPickup
        public float weight = 1f;
    }

    [Header("Prefabs")]
    [SerializeField] Option[] obstacles;
    [SerializeField] GameObject coinPrefab;
    [SerializeField] int coinPoolSize = 24;

    [Header("Layout")]
    [SerializeField] float laneDistance = 2f;   // phải bằng PlayerController.laneDistance
    [SerializeField] float firstRowZ = 3f;      // hàng đầu tiên cách đầu chunk bao nhiêu mét
    [SerializeField] float rowSpacing = 7f;     // khoảng cách giữa các hàng
    [SerializeField] float endMargin = 2f;      // chừa cuối chunk

    [Header("Random")]
    [Range(0f, 1f)] [SerializeField] float emptyRowChance = 0.25f;  // hàng hoàn toàn trống
    [Range(0f, 1f)] [SerializeField] float obstacleChance = 0.6f;    // mỗi làn không an toàn có obstacle
    [Range(0f, 1f)] [SerializeField] float coinRowChance = 0.6f;     // hàng có coin trên làn an toàn
    [SerializeField] int coinsPerRow = 3;
    [SerializeField] float coinSpacing = 1.5f;

    [Header("Power-ups")]
    [SerializeField] PowerUpOption[] powerUps;
    [Range(0f, 1f)] [SerializeField] float powerUpChance = 0.25f;    // xác suất chunk này có 1 power-up
    [SerializeField] int minChunksBetweenPowerUps = 4;               // tối thiểu bao nhiêu chunk giữa 2 power-up

    // Dùng chung giữa các chunk
    static int safeLane = 1;
    static int chunksSincePowerUp = 99;
    public static void ResetPattern() { safeLane = 1; chunksSincePowerUp = 99; }

    Chunk chunk;
    List<GameObject>[] obstaclePools;
    List<GameObject> coinPool;
    List<GameObject>[] powerUpPools;   // mỗi loại 1 bản (mỗi chunk tối đa 1 power-up)
    float totalWeight;
    float powerUpTotalWeight;

    void Awake()
    {
        chunk = GetComponent<Chunk>();

        obstaclePools = new List<GameObject>[obstacles != null ? obstacles.Length : 0];
        for (int i = 0; i < obstaclePools.Length; i++)
        {
            obstaclePools[i] = new List<GameObject>();
            var o = obstacles[i];
            if (o.prefab == null) continue;
            totalWeight += Mathf.Max(0f, o.weight);
            for (int k = 0; k < o.poolSize; k++)
                obstaclePools[i].Add(CreateItem(o.prefab));
        }

        coinPool = new List<GameObject>();
        if (coinPrefab != null)
            for (int k = 0; k < coinPoolSize; k++)
                coinPool.Add(CreateItem(coinPrefab));

        powerUpPools = new List<GameObject>[powerUps != null ? powerUps.Length : 0];
        for (int i = 0; i < powerUpPools.Length; i++)
        {
            powerUpPools[i] = new List<GameObject>();
            if (powerUps[i].prefab == null) continue;
            powerUpTotalWeight += Mathf.Max(0f, powerUps[i].weight);
            powerUpPools[i].Add(CreateItem(powerUps[i].prefab));
        }
    }

    GameObject CreateItem(GameObject prefab)
    {
        var go = Instantiate(prefab, transform);
        go.SetActive(false);
        return go;
    }

    // Gọi mỗi khi chunk được dùng lại
    public void Generate()
    {
        if (obstaclePools == null) return;   // chưa Awake

        foreach (var pool in obstaclePools) foreach (var g in pool) g.SetActive(false);
        foreach (var g in coinPool) g.SetActive(false);
        foreach (var pool in powerUpPools) foreach (var g in pool) g.SetActive(false);

        float endZ = chunk.Length - endMargin;
        int rowCount = endZ >= firstRowZ ? Mathf.FloorToInt((endZ - firstRowZ) / rowSpacing) + 1 : 0;
        if (rowCount == 0) return;

        // Quyết định chunk này có power-up không, và nằm ở hàng nào
        int powerRow = -1;
        chunksSincePowerUp++;
        if (powerUpTotalWeight > 0f && chunksSincePowerUp >= minChunksBetweenPowerUps && Random.value < powerUpChance)
        {
            powerRow = Random.Range(0, rowCount);
            chunksSincePowerUp = 0;
        }

        for (int r = 0; r < rowCount; r++)
        {
            float z = firstRowZ + r * rowSpacing;

            // Làn an toàn chỉ lệch tối đa 1 làn mỗi hàng
            safeLane = Mathf.Clamp(safeLane + Random.Range(-1, 2), 0, 2);

            if (Random.value >= emptyRowChance)
            {
                for (int lane = 0; lane < 3; lane++)
                {
                    if (lane == safeLane) continue;
                    if (Random.value > obstacleChance) continue;
                    int idx = PickObstacle();
                    if (idx >= 0) Place(obstaclePools[idx], lane, z);
                }
            }

            if (r == powerRow)
            {
                // Power-up đứng thay chỗ coin ở làn an toàn của hàng này
                int pi = PickPowerUp();
                if (pi >= 0) Place(powerUpPools[pi], safeLane, z);
            }
            else if (Random.value < coinRowChance)
            {
                for (int k = 0; k < coinsPerRow; k++)
                {
                    float cz = z + (k - (coinsPerRow - 1) * 0.5f) * coinSpacing;
                    Place(coinPool, safeLane, cz);
                }
            }
        }
    }

    int PickObstacle()
    {
        if (totalWeight <= 0f) return -1;
        float r = Random.value * totalWeight;
        for (int i = 0; i < obstacles.Length; i++)
        {
            if (obstacles[i].prefab == null) continue;
            r -= Mathf.Max(0f, obstacles[i].weight);
            if (r <= 0f) return i;
        }
        return -1;
    }

    int PickPowerUp()
    {
        if (powerUpTotalWeight <= 0f) return -1;
        float r = Random.value * powerUpTotalWeight;
        for (int i = 0; i < powerUps.Length; i++)
        {
            if (powerUps[i].prefab == null) continue;
            r -= Mathf.Max(0f, powerUps[i].weight);
            if (r <= 0f) return i;
        }
        return -1;
    }

    // Lấy 1 bản đang tắt trong pool, đặt vào đúng làn/Z, giữ nguyên Y của prefab
    void Place(List<GameObject> pool, int lane, float z)
    {
        foreach (var g in pool)
        {
            if (g.activeSelf) continue;
            float y = g.transform.localPosition.y;
            g.transform.localPosition = new Vector3((lane - 1) * laneDistance, y, z);
            g.SetActive(true);
            return;
        }
        // pool hết: bỏ qua (tăng poolSize nếu muốn dày hơn)
    }

    // Hộp cyan = vị trí pivot của obstacle ở mỗi làn/hàng. So với vạch vàng của đường để kiểm tra lệch.
    void OnDrawGizmos()
    {
        var ch = GetComponent<Chunk>();
        if (ch == null) return;
        Gizmos.color = Color.cyan;
        for (float z = firstRowZ; z <= ch.Length - endMargin; z += rowSpacing)
        {
            Gizmos.DrawLine(transform.TransformPoint(-laneDistance * 1.5f, 0.05f, z),
                            transform.TransformPoint(laneDistance * 1.5f, 0.05f, z));
            for (int lane = 0; lane < 3; lane++)
                Gizmos.DrawWireCube(transform.TransformPoint((lane - 1) * laneDistance, 0.05f, z),
                                    new Vector3(1.6f, 0.1f, 0.4f));
        }
    }
}
