using UnityEngine;

// Chu kỳ ngày đêm. Gắn lên 1 GameObject rỗng trong scene, kéo Directional Light vào ô Sun.
// time01: 0 = bình minh, 0.25 = giữa trưa, 0.5 = hoàng hôn, 0.75 = nửa đêm, 1 = lại bình minh.
public class DayNightCycle : MonoBehaviour
{
    [SerializeField] Light sun;                          // Directional Light
    [SerializeField] float cycleDuration = 60f;          // giây cho 1 chu kỳ đầy đủ
    [Range(0f, 1f)]
    [SerializeField] float startTime = 0.15f;            // bắt đầu ban ngày
    [SerializeField] float yaw = 30f;                    // xoay ngang để bóng đổ chéo, không chạy dọc đường

    [Header("Thay đổi theo chu kỳ (trái = bình minh ... phải = bình minh kế tiếp)")]
    [SerializeField] Gradient sunColor;
    [SerializeField] AnimationCurve sunIntensity;
    [SerializeField] Gradient ambientColor;
    [SerializeField] Gradient fogColor;
    [SerializeField] bool affectFog = true;

    float time;

    void Start()
    {
        time = startTime;
        Apply();
    }

    void Update()
    {
        time = (time + Time.deltaTime / cycleDuration) % 1f;
        Apply();
    }

    void Apply()
    {
        if (sun == null) return;

        // x = 0: mặt trời ở chân trời, x = 90: ngay trên đầu, x = 180: lặn, x = 270: bên dưới đất
        sun.transform.rotation = Quaternion.Euler(time * 360f, yaw, 0f);
        sun.color = sunColor.Evaluate(time);
        sun.intensity = sunIntensity.Evaluate(time);

        RenderSettings.ambientLight = ambientColor.Evaluate(time);
        if (affectFog && RenderSettings.fog)
            RenderSettings.fogColor = fogColor.Evaluate(time);
    }

    // Giá trị mặc định hợp lý, chỉ chạy khi thêm component hoặc bấm Reset. Chỉnh lại trong Inspector tùy ý.
    void Reset()
    {
        sun = RenderSettings.sun != null ? RenderSettings.sun : FindFirstObjectByType<Light>();

        sunColor = Make(new Color(1f, 0.6f, 0.35f), Color.white, new Color(1f, 0.5f, 0.3f), new Color(0.15f, 0.2f, 0.4f));
        ambientColor = Make(new Color(0.65f, 0.5f, 0.45f), new Color(0.7f, 0.75f, 0.85f), new Color(0.6f, 0.4f, 0.4f), new Color(0.15f, 0.18f, 0.3f));
        fogColor = Make(new Color(0.9f, 0.65f, 0.5f), new Color(0.7f, 0.8f, 0.95f), new Color(0.85f, 0.5f, 0.4f), new Color(0.05f, 0.07f, 0.15f));

        sunIntensity = new AnimationCurve(
            new Keyframe(0f, 0.4f),
            new Keyframe(0.25f, 1.2f),
            new Keyframe(0.5f, 0.4f),
            new Keyframe(0.58f, 0f),
            new Keyframe(0.92f, 0f),
            new Keyframe(1f, 0.4f));
    }

    static Gradient Make(Color sunrise, Color noon, Color sunset, Color night)
    {
        var g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(sunrise, 0f),
                new GradientColorKey(noon, 0.25f),
                new GradientColorKey(sunset, 0.5f),
                new GradientColorKey(night, 0.75f),
                new GradientColorKey(sunrise, 1f)
            },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        return g;
    }
}
