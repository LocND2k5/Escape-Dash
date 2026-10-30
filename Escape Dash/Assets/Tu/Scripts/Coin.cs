using System.Collections.Generic;
using UnityEngine;

// Gắn lên prefab Coin. Collider bật Is Trigger.
// Tự xoay, khi nhặt thì tắt object (tương thích Object Pooling).
// MỚI: giữ danh sách coin đang bật (Active) cho CoinMagnet, và hàm Collect() dùng chung.
[RequireComponent(typeof(Collider))]
public class Coin : MonoBehaviour
{
    public static readonly List<Coin> Active = new List<Coin>();

    [SerializeField] private int value = 1;
    [SerializeField] private float rotateSpeed = 180f;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnEnable() => Active.Add(this);
    private void OnDisable() => Active.Remove(this);

    private void Update()
    {
        transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        Collect();
    }

    public void Collect()
    {
        if (!gameObject.activeSelf) return;
        GameEvents.RaiseCoinCollected(value);
        gameObject.SetActive(false); // pool sẽ SetActive(true) lại khi tái sử dụng
    }
}
