using UnityEngine;

// Gắn lên prefab chướng ngại vật (low barrier, overhead barrier...).
// Yêu cầu: Collider có bật Is Trigger. Player có tag "Player" + Rigidbody (kinematic cũng được).
[RequireComponent(typeof(Collider))]
public class Obstacle : MonoBehaviour
{
    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        GameEvents.RaiseGameOver();
    }
}
