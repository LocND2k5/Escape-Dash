using UnityEditor;
using UnityEngine;

// Menu: Tools/Obstacles/Report Visual vs Collider
// In ra Console kích thước hình thật (Renderer) và Collider của từng prefab vật cản,
// tính theo toạ độ local của object gốc, để chỉnh collider cho khớp với hình.
public static class ObstacleBoundsReport
{
    [MenuItem("Tools/Obstacles/Report Visual vs Collider")]
    private static void Report()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null || prefab.GetComponent<Obstacle>() == null) continue;

            var root = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            bool has = false;
            Bounds visual = new Bounds();
            foreach (var r in root.GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled || r.gameObject == root) continue; // bỏ mesh ProBuilder placeholder ở gốc
                if (!has) { visual = r.bounds; has = true; } else visual.Encapsulate(r.bounds);
            }

            var col = root.GetComponent<BoxCollider>();
            string colText = col != null
                ? $"collider center={col.center} size={Vector3.Scale(col.size, col.transform.lossyScale)}"
                : "không có BoxCollider";
            Debug.Log($"[Obstacle] {path}\n  hình: center={visual.center} size={visual.size}\n  {colText}");

            Object.DestroyImmediate(root);
        }
    }
}
