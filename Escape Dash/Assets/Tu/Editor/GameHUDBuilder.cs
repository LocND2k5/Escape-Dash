#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Menu: Tools > Build Game HUD
// Tự dựng HUD (Score, Distance, Coin, 3 power-up có vòng đếm ngược) dưới Canvas và gắn sẵn vào ScoreManage / PowerUpHUD.
// Đặt file này trong thư mục Assets/Editor. Chạy lại menu bất cứ lúc nào để dựng lại từ đầu.
public static class GameHUDBuilder
{
    const string RootName = "GameHUD";
    static readonly Vector2 TL = new Vector2(0f, 1f);
    static readonly Vector2 TR = new Vector2(1f, 1f);
    static readonly Vector2 Mid = new Vector2(0.5f, 0.5f);

    static Material outlineMat;

    [MenuItem("Tools/Build Game HUD")]
    public static void Build()
    {
        // ---------- Canvas ----------
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            var cgo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(cgo, "Build HUD");
            canvas = cgo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // Xoá HUD dựng lần trước, ẩn các object làm tay cũ (không xoá, bạn tự xoá sau nếu muốn)
        var old = canvas.transform.Find(RootName);
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
        foreach (string n in new[] { "Score", "Coin", "Distance", "PowerUpHUD" })
        {
            var t = canvas.transform.Find(n);
            if (t != null)
            {
                Undo.RecordObject(t.gameObject, "Hide old HUD");
                t.gameObject.SetActive(false);
            }
        }

        // ---------- Tài nguyên ----------
        Sprite bg = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        Sprite knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        Sprite iconMagnet = FindSprite("icon_magnet");
        Sprite iconStar = FindSprite("icon_star");
        Sprite iconShoes = FindSprite("icon_shoes");
        Sprite iconCoin = FindSprite("icon_coin");
        outlineMat = FindOutlineMaterial();

        // ---------- Gốc ----------
        var root = NewRect(RootName, canvas.transform);
        Stretch(root);

        // ---------- Score + Distance (trên trái) ----------
        var scorePanel = NewImage("ScorePanel", root, bg, new Color(0f, 0f, 0f, 0.35f), true).rectTransform;
        Place(scorePanel, TL, TL, new Vector2(40f, -40f), new Vector2(520f, 170f));

        var scoreLabel = NewText("ScoreLabel", scorePanel, "SCORE", 26f, new Color(1f, 1f, 1f, 0.7f), TextAlignmentOptions.TopLeft, false);
        Place(scoreLabel.rectTransform, TL, TL, new Vector2(28f, -14f), new Vector2(300f, 32f));

        var scoreText = NewText("ScoreText", scorePanel, "0", 68f, Color.white, TextAlignmentOptions.TopLeft, true);
        Place(scoreText.rectTransform, TL, TL, new Vector2(28f, -42f), new Vector2(470f, 80f));

        var distanceText = NewText("DistanceText", scorePanel, "0 m", 34f, new Color(0.8f, 0.92f, 1f), TextAlignmentOptions.TopLeft, false);
        Place(distanceText.rectTransform, TL, TL, new Vector2(28f, -126f), new Vector2(470f, 40f));

        // ---------- Coin (trên phải) ----------
        var coinPanel = NewImage("CoinPanel", root, bg, new Color(0f, 0f, 0f, 0.35f), true).rectTransform;
        Place(coinPanel, TR, TR, new Vector2(-40f, -40f), new Vector2(320f, 96f));

        var coinIcon = NewImage("CoinIcon", coinPanel, iconCoin != null ? iconCoin : knob,
                                iconCoin != null ? Color.white : new Color(1f, 0.78f, 0.15f), false);
        coinIcon.preserveAspect = true;
        Place(coinIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(64f, 64f));
        if (iconCoin == null)
        {
            var inner = NewImage("CoinInner", coinIcon.rectTransform, knob, new Color(0.85f, 0.58f, 0.08f), false);
            Place(inner.rectTransform, Mid, Mid, Vector2.zero, new Vector2(40f, 40f));
        }

        var coinText = NewText("CoinText", coinPanel, "0", 56f, new Color(1f, 0.84f, 0.29f), TextAlignmentOptions.MidlineRight, true);
        Place(coinText.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-26f, 0f), new Vector2(200f, 70f));

        // ---------- Power-up (dưới Score) ----------
        var row = NewRect("PowerUpRow", root);
        Place(row, TL, TL, new Vector2(40f, -232f), new Vector2(520f, 130f));
        var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 22f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        BuildPowerIcon(row, "Icon_Magnet", iconMagnet, knob, new Color(0.25f, 0.62f, 1f), out var magnetRoot, out var magnetFill, out var magnetTime);
        BuildPowerIcon(row, "Icon_DoubleScore", iconStar, knob, new Color(1f, 0.8f, 0.2f), out var starRoot, out var starFill, out var starTime);
        BuildPowerIcon(row, "Icon_SuperJump", iconShoes, knob, new Color(1f, 0.38f, 0.32f), out var shoesRoot, out var shoesFill, out var shoesTime);

        // ---------- Gắn script ----------
        var hud = row.gameObject.AddComponent<PowerUpHUD>();
        var hso = new SerializedObject(hud);
        var entries = hso.FindProperty("entries");
        entries.arraySize = 3;
        SetEntry(entries.GetArrayElementAtIndex(0), 0, magnetRoot, magnetFill, magnetTime);
        SetEntry(entries.GetArrayElementAtIndex(1), 1, starRoot, starFill, starTime);
        SetEntry(entries.GetArrayElementAtIndex(2), 2, shoesRoot, shoesFill, shoesTime);
        hso.ApplyModifiedProperties();

        var sm = Object.FindFirstObjectByType<ScoreManage>();
        if (sm != null)
        {
            var so = new SerializedObject(sm);
            so.FindProperty("scoreText").objectReferenceValue = scoreText;
            so.FindProperty("coinText").objectReferenceValue = coinText;
            so.FindProperty("distanceText").objectReferenceValue = distanceText;
            so.ApplyModifiedProperties();
        }
        else Debug.LogWarning("GameHUDBuilder: không thấy ScoreManage trong scene, hãy tự kéo Score/Coin/Distance Text vào.");

        // Icon power-up ẩn lúc đầu, PowerUpHUD sẽ bật khi nhặt
        magnetRoot.SetActive(false);
        starRoot.SetActive(false);
        shoesRoot.SetActive(false);

        if (iconMagnet == null || iconStar == null || iconShoes == null)
            Debug.LogWarning("GameHUDBuilder: không tìm thấy sprite icon_magnet / icon_star / icon_shoes (ảnh phải ở Sprite Mode = Single). Kéo sprite vào ô Icon bằng tay.");

        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Selection.activeGameObject = root.gameObject;
        Debug.Log("GameHUDBuilder: đã dựng xong HUD. Nhớ thay ScoreManage.cs bản mới để chữ chỉ hiện số.");
    }

    // ---------------- Power-up icon: nền tròn + vòng đếm ngược + icon + số giây ----------------
    static void BuildPowerIcon(Transform parent, string name, Sprite icon, Sprite knob, Color accent,
                               out GameObject rootGo, out Image fill, out TextMeshProUGUI time)
    {
        var bg = NewImage(name, parent, knob, new Color(0f, 0f, 0f, 0.45f), false);
        bg.rectTransform.sizeDelta = new Vector2(120f, 120f);
        bg.gameObject.AddComponent<CanvasGroup>();   // để PowerUpHUD nhấp nháy khi sắp hết giờ
        rootGo = bg.gameObject;

        fill = NewImage("Fill", bg.rectTransform, knob, new Color(accent.r, accent.g, accent.b, 0.95f), false);
        Stretch(fill.rectTransform);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Radial360;
        fill.fillOrigin = (int)Image.Origin360.Top;
        fill.fillClockwise = true;
        fill.fillAmount = 1f;

        // Đĩa tối phủ giữa => phần còn lại của Fill thành vòng viền
        var inner = NewImage("Inner", bg.rectTransform, knob, new Color(0.08f, 0.1f, 0.14f, 0.92f), false);
        Stretch(inner.rectTransform, 9f);

        var ic = NewImage("Icon", bg.rectTransform, icon != null ? icon : knob, Color.white, false);
        ic.preserveAspect = true;
        Place(ic.rectTransform, Mid, Mid, Vector2.zero, new Vector2(66f, 66f));

        time = NewText("TimeText", bg.rectTransform, "0", 28f, Color.white, TextAlignmentOptions.Top, true);
        Place(time.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(80f, 32f));
    }

    static void SetEntry(SerializedProperty e, int typeIndex, GameObject root, Image fill, TMP_Text time)
    {
        e.FindPropertyRelative("type").enumValueIndex = typeIndex;
        e.FindPropertyRelative("root").objectReferenceValue = root;
        e.FindPropertyRelative("fillImage").objectReferenceValue = fill;
        e.FindPropertyRelative("timeText").objectReferenceValue = time;
    }

    // ---------------- Helpers ----------------
    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Build HUD");
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    static Image NewImage(string name, Transform parent, Sprite sprite, Color color, bool sliced)
    {
        var rt = NewRect(name, parent);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        if (sliced) img.type = Image.Type.Sliced;
        return img;
    }

    static TextMeshProUGUI NewText(string name, Transform parent, string text, float size, Color color,
                                   TextAlignmentOptions align, bool bold)
    {
        var rt = NewRect(name, parent);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        tmp.raycastTarget = false;
        if (outlineMat != null) tmp.fontSharedMaterial = outlineMat;
        return tmp;
    }

    static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static void Stretch(RectTransform rt, float inset = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
    }

    static Sprite FindSprite(string fileName)
    {
        foreach (string guid in AssetDatabase.FindAssets(fileName + " t:Sprite"))
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guid));
            if (s != null) return s;
        }
        return null;
    }

    // Dùng material viền có sẵn của TextMeshPro nếu font mặc định là LiberationSans
    static Material FindOutlineMaterial()
    {
        var font = TMP_Settings.defaultFontAsset;
        if (font == null || !font.name.Contains("LiberationSans")) return null;
        foreach (string guid in AssetDatabase.FindAssets("LiberationSans SDF - Outline t:Material"))
            return AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
        return null;
    }
}
#endif
