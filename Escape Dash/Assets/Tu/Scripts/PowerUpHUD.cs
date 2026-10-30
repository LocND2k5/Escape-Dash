using TMPro;
using UnityEngine;

// Tuỳ chọn: hiện icon + số giây còn lại của từng power-up đang bật.
// Mỗi Entry: chọn loại, kéo vào "root" (object icon, sẽ bật/tắt) và "timeText" (TMP, có thể để trống).
public class PowerUpHUD : MonoBehaviour
{
    [System.Serializable]
    public class Entry
    {
        public PowerUpType type;
        public GameObject root;
        public TMP_Text timeText;
        [HideInInspector] public int shown = -1;
    }

    [SerializeField] private Entry[] entries;

    private void Update()
    {
        foreach (var e in entries)
        {
            if (e.root == null) continue;
            bool active = PowerUpManager.IsActive(e.type);
            if (e.root.activeSelf != active) e.root.SetActive(active);
            if (!active || e.timeText == null) continue;

            int secs = Mathf.CeilToInt(PowerUpManager.Remaining(e.type));
            if (secs != e.shown)
            {
                e.shown = secs;
                e.timeText.text = secs + "s";
            }
        }
    }
}
