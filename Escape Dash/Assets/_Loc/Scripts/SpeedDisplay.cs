using UnityEngine;
using TMPro;

/// <summary>
/// Hiển thị tốc độ hiện tại của nhân vật (m/s) lên HUD.
/// Gắn lên đối tượng Text (TMP) chứa con số tốc độ.
/// Tốc độ giảm khi nhân vật vấp và tăng dần theo thời gian chơi.
/// </summary>
public class SpeedDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private PlayerController player;

    private int shownTenths = -1;   // chỉ cập nhật chữ khi giá trị đổi (tránh tạo chuỗi mới mỗi frame)

    private void Awake()
    {
        if (speedText == null) speedText = GetComponent<TMP_Text>();
    }

    private void Start()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>();
    }

    private void Update()
    {
        if (speedText == null || player == null) return;

        int tenths = Mathf.RoundToInt(player.CurrentSpeed * 10f);
        if (tenths == shownTenths) return;

        shownTenths = tenths;
        speedText.text = (tenths / 10f).ToString("0.0");
    }
}
