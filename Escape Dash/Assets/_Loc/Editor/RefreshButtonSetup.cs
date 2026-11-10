using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Menu: Tools > Leaderboard > Apply Refresh Button Style
// Mở MenuScence rồi chạy. Đổi nút RefreshButton (ô xanh vuông, chữ emoji lỗi) thành nút tròn có icon mũi tên xoay,
// đặt đối xứng với nút đóng ở góc trên trái và nối vào LeaderboardUI.refreshButton.
public static class RefreshButtonSetup
{
    const string IconPath = "Assets/Sprites/UIRefreshButton.png";

    [MenuItem("Tools/Leaderboard/Apply Refresh Button Style")]
    static void Apply()
    {
        var importer = AssetImporter.GetAtPath(IconPath) as TextureImporter;
        if (importer == null)
        {
            EditorUtility.DisplayDialog("Refresh button", "Không tìm thấy " + IconPath, "OK");
            return;
        }

        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(IconPath);

        Button refresh = null, close = null;
        foreach (var b in Resources.FindObjectsOfTypeAll<Button>())
        {
            if (!b.gameObject.scene.IsValid()) continue;
            if (b.name == "RefreshButton") refresh = b;
            else if (b.name == "CloseButton") close = b;
        }

        if (refresh == null)
        {
            EditorUtility.DisplayDialog("Refresh button", "Mở MenuScence trước (không thấy đối tượng RefreshButton).", "OK");
            return;
        }

        var go = refresh.gameObject;
        Undo.RegisterFullObjectHierarchyUndo(go, "Apply Refresh Button Style");
        go.SetActive(true);

        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = Color.white;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        refresh.targetGraphic = image;

        // Bỏ chữ emoji cũ (font không có ký tự này nên hiện ô vuông trắng)
        foreach (var label in go.GetComponentsInChildren<TMP_Text>(true))
            label.gameObject.SetActive(false);

        // Đặt đối xứng với nút đóng
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        if (close != null)
        {
            var c = (RectTransform)close.transform;
            rt.pivot = new Vector2(1f - c.pivot.x, c.pivot.y);
            rt.sizeDelta = c.sizeDelta;
            rt.anchoredPosition = new Vector2(-c.anchoredPosition.x, c.anchoredPosition.y);
        }
        else
        {
            rt.sizeDelta = new Vector2(46f, 46f);
            rt.anchoredPosition = new Vector2(8f, -12f);
        }

        refresh.transition = Selectable.Transition.ColorTint;
        var colors = refresh.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 0.85f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
        colors.selectedColor = Color.white;
        refresh.colors = colors;

        // Nối nút vào LeaderboardUI
        var board = Object.FindFirstObjectByType<LeaderboardUI>(FindObjectsInactive.Include);
        if (board != null)
        {
            var so = new SerializedObject(board);
            var prop = so.FindProperty("refreshButton");
            if (prop != null)
            {
                prop.objectReferenceValue = refresh;
                so.ApplyModifiedProperties();
            }
        }

        EditorSceneManager.MarkSceneDirty(go.scene);
        Debug.Log("Refresh button style applied. Nhớ lưu scene (Ctrl+S).");
    }
}
