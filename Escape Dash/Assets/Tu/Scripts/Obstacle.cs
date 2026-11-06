using UnityEngine;

// Loại va chạm.
// Light = luôn chỉ vấp (còn cơ hội), Heavy = luôn thua ngay,
// Directional = tùy hướng va chạm (kiểu Subway Surfers):
//   - Đâm trực diện         -> thua ngay
//   - Quẹt cạnh khi đổi làn -> chỉ vấp
//   - Chạm mặt trên         -> chỉ vấp
// LƯU Ý: giữ nguyên thứ tự enum vì Inspector lưu theo số (Light=0, Heavy=1).
public enum ObstacleHitType { Light, Heavy, Directional }

// Gắn lên prefab chướng ngại vật (low barrier, overhead barrier...).
// Yêu cầu: Collider có bật Is Trigger. Player có tag "Player" + Rigidbody (kinematic cũng được).
[RequireComponent(typeof(Collider))]
public class Obstacle : MonoBehaviour
{
    [SerializeField] private ObstacleHitType hitType = ObstacleHitType.Directional;

    [Header("Directional")]
    [Tooltip("Chân Player cao hơn (mặt trên vật cản - giá trị này) thì tính là chạm mặt trên -> chỉ vấp")]
    [SerializeField] private float topGrazeTolerance = 0.35f;

    [Tooltip("Phần thân Player chồng lên vật cản theo trục X nhỏ hơn tỉ lệ này thì tính là quẹt cạnh -> chỉ vấp (0.5 = 50% bề rộng Player)")]
    [SerializeField] private float sideGrazeOverlap = 0.5f;

    private Collider col;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Awake()
    {
        col = GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        bool fatal = IsFatalHit(other);

        // Chẩn đoán: in ra vật cản nào vừa va vào Player và kết quả
        Debug.Log($"[Obstacle] '{name}' ({hitType}) va Player tại {transform.position} -> {(fatal ? "THUA" : "VẤP")}", this);

        if (fatal)
            GameEvents.RaiseGameOver();
        else
            GameEvents.RaisePlayerHit();
    }

    private bool IsFatalHit(Collider player)
    {
        if (hitType == ObstacleHitType.Heavy) return true;
        if (hitType == ObstacleHitType.Light) return false;

        Bounds p = player.bounds;
        Bounds o = col.bounds;

        // Chạm mặt trên: Player nhảy qua rồi cuộn xuống đè lên mặt trên vật cản
        if (p.min.y >= o.max.y - topGrazeTolerance) return false;

        // Quẹt cạnh: chỉ chồng lên vật cản một phần nhỏ theo trục X
        float overlapX = Mathf.Min(p.max.x, o.max.x) - Mathf.Max(p.min.x, o.min.x);
        if (overlapX < p.size.x * sideGrazeOverlap) return false;

        // Còn lại: đâm trực diện
        return true;
    }
}
