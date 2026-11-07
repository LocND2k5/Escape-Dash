using UnityEngine;
using TMPro;

/// <summary>
/// Hiển thị tốc độ thực tế mà đường đang cuộn (m/s) lên HUD.
/// Gắn lên đối tượng Text (TMP) chứa con số tốc độ.
/// Tốc độ tăng dần theo thời gian chơi, tụt xuống 40% khi nhân vật vấp rồi hồi dần.
/// </summary>
public class SpeedDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private PlayerController player;   // dự phòng nếu scene không có ChunkSpawner

    private ChunkSpawner spawner;
    private int shownTenths = -1;   // chỉ cập nhật chữ khi giá trị đổi (tránh tạo chuỗi mới mỗi frame)

    private void Awake()
    {
        if (speedText == null) speedText = GetComponent<TMP_Text>();
    }

    private void Start()
    {
        spawner = FindFirstObjectByType<ChunkSpawner>();
        if (player == null) player = FindFirstObjectByType<PlayerController>();
    }

    private void Update()
    {
        if (speedText == null) return;

        float speed;
        if (spawner != null) speed = spawner.Running ? spawner.CurrentSpeed : 0f;
        else if (player != null) speed = player.CurrentSpeed;
        else return;

        int tenths = Mathf.RoundToInt(speed * 10f);
        if (tenths == shownTenths) return;

        shownTenths = tenths;
        speedText.text = (tenths / 10f).ToString("0.0");
    }
}
