using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gắn lên Prefab của 1 dòng kết quả trong ScrollView Bảng Xếp Hạng.
/// </summary>
public class LeaderboardRowUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private Image backgroundImage;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0f); // Trong suốt mặc định
    [SerializeField] private Color highlightColor = new Color(1f, 0.92f, 0.65f, 0.35f); // Viền vàng nhẹ cho chính mình
    [SerializeField] private Color nameColor = new Color(0.48f, 0.20f, 0.02f, 1f); // Màu nâu đậm đặc trưng như trong ảnh
    [SerializeField] private Color myNameColor = new Color(0.75f, 0.25f, 0.0f, 1f);

    public void Setup(int rank, string playerName, int score, bool isCurrentPlayer)
    {
        // 1. Hiển thị Rank (chỉ số 1, 2, 3... chữ trắng trên nền huy hiệu đỏ)
        if (rankText != null)
        {
            rankText.text = rank.ToString();
            rankText.color = Color.white;
        }

        // 2. Hiển thị Tên người chơi (chữ nâu đậm, in hoa đẹp mắt)
        if (nameText != null)
        {
            string displayName = string.IsNullOrWhiteSpace(playerName) ? $"PLAYER" : playerName.ToUpper();
            nameText.text = displayName;
            nameText.color = isCurrentPlayer ? myNameColor : nameColor;
        }

        // 3. Hiển thị Điểm (chữ trắng trên nền huy hiệu nâu)
        if (scoreText != null)
        {
            scoreText.text = score.ToString("N0");
            scoreText.color = Color.white;
        }

        // 4. Đổi màu nền nếu là chính người chơi này
        if (backgroundImage != null)
        {
            backgroundImage.color = isCurrentPlayer ? highlightColor : normalColor;
        }
    }
}
