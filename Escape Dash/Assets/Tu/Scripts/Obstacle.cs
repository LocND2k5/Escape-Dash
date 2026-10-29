using UnityEngine;

// MỚI: loại va chạm. Light = vấp (còn cơ hội), Heavy = thua ngay.
public enum ObstacleHitType { Light, Heavy }

// Gắn lên prefab chướng ngại vật (low barrier, overhead barrier...).
// Yêu cầu: Collider có bật Is Trigger. Player có tag "Player" + Rigidbody (kinematic cũng được).
[RequireComponent(typeof(Collider))]
public class Obstacle : MonoBehaviour
{
    // MỚI: chọn loại va chạm cho từng prefab trong Inspector
    [SerializeField] private ObstacleHitType hitType = ObstacleHitType.Light;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Chẩn đoán: in ra vật cản nào vừa va vào Player
        Debug.Log($"[Obstacle] '{name}' ({hitType}) va Player tại {transform.position}", this);

        // MỚI: Heavy thua ngay, Light chỉ báo "player bị vấp" cho PlayerController xử lý
        if (hitType == ObstacleHitType.Heavy)
            GameEvents.RaiseGameOver();
        else
            GameEvents.RaisePlayerHit();
    }
}