using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Hiện icon power-up đang bật: vòng đếm ngược (Filled Radial), số giây còn lại, nhấp nháy khi sắp hết.
// Gắn lên object CHA chứa 3 icon (không gắn lên icon, vì icon bị tắt thì script ngừng chạy).
public class PowerUpHUD : MonoBehaviour
{
    [System.Serializable]
    public class Entry
    {
        public PowerUpType type;
        public GameObject root;       // object icon (sẽ được bật/tắt). Nên có thêm CanvasGroup để nhấp nháy
        public Image fillImage;       // tuỳ chọn: Image Type = Filled, Radial 360, dùng làm vòng đếm ngược
        public TMP_Text timeText;     // tuỳ chọn: số giây còn lại
        [HideInInspector] public int shown = -1;
        [HideInInspector] public CanvasGroup group;
    }

    [SerializeField] private Entry[] entries;
    [SerializeField] private float warnTime = 3f;      // còn bao nhiêu giây thì bắt đầu nhấp nháy
    [SerializeField] private float blinkSpeed = 8f;

    private void Awake()
    {
        foreach (var e in entries)
            if (e.root != null) e.group = e.root.GetComponent<CanvasGroup>();
    }

    private void Update()
    {
        foreach (var e in entries)
        {
            if (e.root == null) continue;

            bool active = PowerUpManager.IsActive(e.type);
            if (e.root.activeSelf != active)
            {
                e.root.SetActive(active);
                e.shown = -1;
            }
            if (!active) continue;

            float remaining = PowerUpManager.Remaining(e.type);

            if (e.fillImage != null)
                e.fillImage.fillAmount = PowerUpManager.Remaining01(e.type);

            if (e.timeText != null)
            {
                int secs = Mathf.CeilToInt(remaining);
                if (secs != e.shown)
                {
                    e.shown = secs;
                    e.timeText.text = secs.ToString();
                }
            }

            if (e.group != null)
                e.group.alpha = remaining <= warnTime
                    ? 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(Time.time * blinkSpeed))
                    : 1f;
        }
    }
}
