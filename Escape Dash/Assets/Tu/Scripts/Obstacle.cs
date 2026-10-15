using UnityEngine;

// Gắn lên prefab chướng ngại vật (low barrier, overhead barrier...).
// Yêu cầu: Collider có bật Is Trigger. Player có tag "Player" + Rigidbody (kinematic cũng được).
//
// instantDeath = false (mặc định): Va chạm lần 1 chỉ vấp ngã (giảm tốc, quái vật áp sát).
//                                   Va chạm lần 2 trong lúc còn nguy hiểm mới Game Over.
// instantDeath = true             : Va chạm là chết ngay lập tức (dành cho bẫy chết người,
//                                   vực thẳm, tường đặc biệt...).
[RequireComponent(typeof(Collider))]
public class Obstacle : MonoBehaviour
{
    [Tooltip("Tích chọn nếu muốn va vào là chết ngay. Bỏ trống để áp dụng luật vấp ngã 2 lần.")]
    [SerializeField] private bool instantDeath = false;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (instantDeath)
        {
            GameEvents.RaiseGameOver();   // Chết ngay lập tức
        }
        else
        {
            GameEvents.RaisePlayerHit();  // Vấp ngã lần 1; PlayerController xử lý luật 2 lần
        }
    }
}
