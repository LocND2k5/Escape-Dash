using UnityEngine;

// Gắn lên prefab vật phẩm power-up. Collider bật Is Trigger. Chọn Type trong Inspector.
// Nhặt xong thì tắt object (tương thích pool trong ChunkRandomizer).
[RequireComponent(typeof(Collider))]
public class PowerUpPickup : MonoBehaviour
{
    [SerializeField] private PowerUpType type;
    [SerializeField] private float rotateSpeed = 120f;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Update()
    {
        transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (PowerUpManager.Instance != null) PowerUpManager.Instance.Activate(type);
        gameObject.SetActive(false);
    }
}
