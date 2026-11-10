using System.IO;
using UnityEditor;
using UnityEngine;

// Menu: Tools > Export > Obstacle Images
// Chụp ảnh 3 vật cản (Obstacle_Jump, Obstacle_Down, Obstcale_Death) thành PNG 1024x1024
// và lưu vào thư mục ObstacleImages ở gốc dự án (ngoài Assets).
public static class ObstacleImageExporter
{
    const int Size = 1024;

    static readonly string[] PrefabNames = { "Obstacle_Jump", "Obstacle_Down", "Obstcale_Death" };
    static readonly string[] FileNames = { "1_Low_Barrier.png", "2_Overhead_Barrier.png", "3_Deadly_Obstacle.png" };

    [MenuItem("Tools/Export/Obstacle Images")]
    static void Export()
    {
        string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "ObstacleImages"));
        Directory.CreateDirectory(outDir);

        int done = 0;
        var preview = new PreviewRenderUtility();
        try
        {
            for (int i = 0; i < PrefabNames.Length; i++)
            {
                var prefab = FindPrefab(PrefabNames[i]);
                if (prefab == null)
                {
                    Debug.LogWarning("ObstacleImageExporter: không tìm thấy prefab " + PrefabNames[i]);
                    continue;
                }

                var tex = Render(preview, prefab);
                if (tex == null) continue;

                File.WriteAllBytes(Path.Combine(outDir, FileNames[i]), tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                done++;
            }
        }
        finally
        {
            preview.Cleanup();
        }

        Debug.Log($"ObstacleImageExporter: đã xuất {done}/3 ảnh vào {outDir}");
        EditorUtility.DisplayDialog("Obstacle Images", $"Đã xuất {done}/3 ảnh vào:\n{outDir}", "OK");
        EditorUtility.RevealInFinder(outDir);
    }

    static GameObject FindPrefab(string prefabName)
    {
        foreach (var guid in AssetDatabase.FindAssets(prefabName + " t:Prefab"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == prefabName)
                return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
        return null;
    }

    static Texture2D Render(PreviewRenderUtility preview, GameObject prefab)
    {
        var instance = Object.Instantiate(prefab);
        instance.transform.position = Vector3.zero;
        instance.transform.rotation = Quaternion.identity;
        preview.AddSingleGO(instance);

        var renderers = instance.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Object.DestroyImmediate(instance);
            return null;
        }

        Bounds bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);

        var cam = preview.camera;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
        cam.fieldOfView = 30f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 200f;

        // Nhìn từ phía người chơi (mặt trước vật cản), hơi chếch và từ trên xuống
        var rotation = Quaternion.Euler(22f, -28f, 0f);
        float radius = bounds.extents.magnitude;
        float distance = radius / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.05f;
        cam.transform.rotation = rotation;
        cam.transform.position = bounds.center - rotation * Vector3.forward * distance;

        preview.lights[0].intensity = 1.2f;
        preview.lights[0].transform.rotation = Quaternion.Euler(40f, 40f, 0f);
        preview.lights[1].intensity = 0.6f;
        preview.ambientColor = new Color(0.45f, 0.45f, 0.45f, 1f);

        preview.BeginStaticPreview(new Rect(0, 0, Size, Size));
        cam.Render();
        var tex = preview.EndStaticPreview();

        Object.DestroyImmediate(instance);
        return tex;
    }
}
