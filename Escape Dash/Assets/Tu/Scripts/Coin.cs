using UnityEngine;

// Gắn lên prefab Coin. Collider bật Is Trigger.
// Tự xoay, khi nhặt thì tắt object (tương thích Object Pooling).
[RequireComponent(typeof(Collider))]
public class Coin : MonoBehaviour
{
    [SerializeField] private int value = 1;
    [SerializeField] private float rotateSpeed = 180f;

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
        GameEvents.RaiseCoinCollected(value);
        gameObject.SetActive(false); // pool sẽ SetActive(true) lại khi tái sử dụng
    }
}
