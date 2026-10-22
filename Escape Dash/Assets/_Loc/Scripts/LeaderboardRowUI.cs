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
    [SerializeField] private Color normalColor = new Color(0.15f, 0.15f, 0.2f, 0.6f);
    [SerializeField] private Color highlightColor = new Color(0.2f, 0.45f, 0.85f, 0.85f);
    [SerializeField] private Color top1Color = new Color(0.95f, 0.75f, 0.15f, 1f); // Vàng cho Top 1
    [SerializeField] private Color top2Color = new Color(0.8f, 0.8f, 0.85f, 1f);   // Bạc cho Top 2
    [SerializeField] private Color top3Color = new Color(0.8f, 0.5f, 0.2f, 1f);    // Đồng cho Top 3

    public void Setup(int rank, string playerName, int score, bool isCurrentPlayer)
    {
        // 1. Hiển thị Rank
        if (rankText != null)
        {
            rankText.text = $"#{rank}";
            if (rank == 1) rankText.color = top1Color;
            else if (rank == 2) rankText.color = top2Color;
            else if (rank == 3) rankText.color = top3Color;
            else rankText.color = Color.white;
        }

        // 2. Hiển thị Tên người chơi
        if (nameText != null)
        {
            nameText.text = string.IsNullOrWhiteSpace(playerName) ? $"Runner_{rank}" : playerName;
            nameText.color = isCurrentPlayer ? Color.cyan : Color.white;
        }

        // 3. Hiển thị Điểm
        if (scoreText != null)
        {
            scoreText.text = score.ToString("N0"); // Format có dấu phẩy: 12,345
        }

        // 4. Đổi màu nền nếu là chính người chơi này
        if (backgroundImage != null)
        {
            backgroundImage.color = isCurrentPlayer ? highlightColor : normalColor;
        }
    }
}
