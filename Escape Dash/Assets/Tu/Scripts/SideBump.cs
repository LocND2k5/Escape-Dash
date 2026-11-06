using UnityEngine;

// Gắn lên các collider hông (trái/phải) của vật cản như container. Collider bật Is Trigger.
// Player đổi làn đâm vào hông: bị đẩy về làn cũ (không xuyên qua) và bị tính 1 lần vấp (như Obstacle Light).
// Vấp lần nữa trong dangerDuration giây -> PlayerController tự xử lý Game Over.
[RequireComponent(typeof(Collider))]
public class SideBump : MonoBehaviour
{
    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var player = other.GetComponentInParent<PlayerController>();
        if (player != null) player.BumpBack();

        GameEvents.RaisePlayerHit();
    }
}
