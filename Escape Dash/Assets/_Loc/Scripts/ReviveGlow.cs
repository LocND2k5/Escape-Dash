using UnityEngine;

/// <summary>
/// Vòng sáng nhấp nháy quanh nhân vật sau khi hồi sinh.
/// Không cần prefab hay asset: gọi ReviveGlow.Show(target, duration) là tự tạo vòng sáng
/// (vẽ bằng texture sinh lúc chạy), luôn quay mặt về camera, nhấp nháy rồi tự biến mất.
/// </summary>
public class ReviveGlow : MonoBehaviour
{
    private const float BlinksPerSecond = 3f;

    private static Sprite ringSprite;

    private SpriteRenderer sr;
    private Color baseColor;
    private float baseScale;
    private float duration;
    private float elapsed;

    /// <summary>Tạo vòng sáng gắn vào target, tồn tại duration giây (tạo lại thì thay vòng cũ).</summary>
    public static void Show(Transform target, Vector3 localCenter, float diameter, float duration, Color color)
    {
        if (target == null || duration <= 0f) return;

        var old = target.GetComponentInChildren<ReviveGlow>();
        if (old != null) Destroy(old.gameObject);

        var go = new GameObject("ReviveGlow");
        go.transform.SetParent(target, false);
        go.transform.localPosition = localCenter;

        var glow = go.AddComponent<ReviveGlow>();
        glow.sr = go.AddComponent<SpriteRenderer>();
        glow.sr.sprite = GetRingSprite();
        glow.sr.sortingOrder = 100;                 // vẽ lên trên các vật khác
        glow.baseColor = color;
        glow.baseScale = diameter;                  // sprite rộng 1 đơn vị nên scale = đường kính
        glow.duration = duration;
        go.transform.localScale = Vector3.one * diameter;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed >= duration) { Destroy(gameObject); return; }

        // Nhấp nháy: độ sáng dao động, vòng phồng nhẹ theo nhịp; mờ dần ở 0.5 giây cuối
        float pulse = 0.5f + 0.5f * Mathf.Sin(elapsed * BlinksPerSecond * Mathf.PI * 2f);
        float fade = Mathf.Clamp01((duration - elapsed) / 0.5f);

        var c = baseColor;
        c.a = Mathf.Lerp(0.25f, 1f, pulse) * fade;
        sr.color = c;
        transform.localScale = Vector3.one * baseScale * Mathf.Lerp(0.95f, 1.1f, pulse);
    }

    private void LateUpdate()
    {
        // Luôn quay mặt về camera để thấy rõ hình tròn
        var cam = Camera.main;
        if (cam != null) transform.rotation = cam.transform.rotation;
    }

    // Texture vòng tròn mềm viền, sinh một lần rồi dùng lại
    private static Sprite GetRingSprite()
    {
        if (ringSprite != null) return ringSprite;

        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float half = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(half, half)) / half; // 0 tâm, 1 mép
                // Vòng sáng nằm quanh bán kính 0.82, dày ~0.12, mờ mềm hai bên
                float ring = Mathf.Clamp01(1f - Mathf.Abs(d - 0.82f) / 0.12f);
                // Quầng sáng nhẹ bên trong
                float inner = d < 0.82f ? Mathf.Clamp01(d / 0.82f) * 0.18f : 0f;
                float a = Mathf.Max(ring * ring, inner);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();

        ringSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return ringSprite;
    }
}
