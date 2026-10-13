using UnityEngine;

// SCRIPT TẠM để test prototype. Xóa khi Player Controller của Lộc xong.
// Chạy thẳng theo trục Z, đổi 3 làn bằng A/D hoặc phím mũi tên trái/phải.
public class TempPlayerRunner : MonoBehaviour
{
    [SerializeField] private float forwardSpeed = 8f;
    [SerializeField] private float laneDistance = 2f;
    [SerializeField] private float laneChangeSpeed = 12f;

    private int lane = 1; // 0 trái, 1 giữa, 2 phải
    private bool alive = true;

    private void OnEnable() => GameEvents.OnGameOver += Stop;
    private void OnDisable() => GameEvents.OnGameOver -= Stop;

    private void Stop()
    {
        alive = false;
        Debug.Log("GAME OVER");
    }

    private void Update()
    {
        if (!alive) return;

        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) lane = Mathf.Max(0, lane - 1);
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) lane = Mathf.Min(2, lane + 1);

        float targetX = (lane - 1) * laneDistance;
        Vector3 pos = transform.position;
        pos.x = Mathf.MoveTowards(pos.x, targetX, laneChangeSpeed * Time.deltaTime);
        pos.z += forwardSpeed * Time.deltaTime;
        transform.position = pos;
    }
}
