#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

// Tạo nhân vật phong cách cartoon bo tròn (kiểu Subway Surfers): đầu to, hoodie, nón, jeans, sneaker to, balo.
// Menu: Escape Dash > Build Low-Poly Runner  -> tạo prefab + Humanoid Avatar + mesh + material.
//       Escape Dash > Swap Selected Player Model -> thay model cũ của Player đang chọn bằng prefab mới.
//
// Prefab dùng Humanoid Avatar nên chạy được với PlayerAnimator.controller (clip humanoid của UAL1_Standard).
// Pivot ở CHÂN, nhân vật nhìn về +Z, cao ~1.8m (khớp CapsuleCollider của PlayerController).
public static class LowPolyRunnerBuilder
{
    private const string OutDir = "Assets/Tu/Models/Player/LowPolyRunner";
    private const string ControllerPath = "Assets/Tu/Models/PlayerAnimator.controller";
    private const string PrefabPath = OutDir + "/LowPolyRunner.prefab";

    private const int Seg = 16;   // số cạnh quanh trục
    private const int Ring = 8;   // số vòng nửa cầu

    private static readonly Color Skin = new Color(0.98f, 0.78f, 0.62f);
    private static readonly Color Hoodie = new Color(0.96f, 0.47f, 0.12f);
    private static readonly Color HoodieLight = new Color(1.00f, 0.78f, 0.35f);
    private static readonly Color Jeans = new Color(0.20f, 0.33f, 0.62f);
    private static readonly Color Shoe = new Color(0.97f, 0.97f, 0.97f);
    private static readonly Color Sole = new Color(0.16f, 0.16f, 0.20f);
    private static readonly Color Accent = new Color(0.10f, 0.66f, 0.66f);
    private static readonly Color Cap = new Color(0.86f, 0.14f, 0.20f);
    private static readonly Color CapBrim = new Color(0.62f, 0.08f, 0.14f);
    private static readonly Color Hair = new Color(0.30f, 0.17f, 0.09f);
    private static readonly Color EyeWhite = Color.white;
    private static readonly Color Pupil = new Color(0.08f, 0.08f, 0.10f);

    private static readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();

    [MenuItem("Escape Dash/Build Low-Poly Runner")]
    public static void Build()
    {
        EnsureFolder("Assets/Tu/Models/Player");
        EnsureFolder(OutDir);
        // Xoá mesh/material bản cũ (prefab & avatar được ghi đè nên vẫn giữ GUID)
        AssetDatabase.DeleteAsset(OutDir + "/Meshes");
        AssetDatabase.DeleteAsset(OutDir + "/Materials");
        AssetDatabase.DeleteAsset(OutDir + "/Animations");
        AssetDatabase.DeleteAsset(OutDir + "/LowPolyRunnerAvatar.asset");
        EnsureFolder(OutDir + "/Animations");
        EnsureFolder(OutDir + "/Meshes");
        EnsureFolder(OutDir + "/Materials");
        materials.Clear();

        var root = new GameObject("LowPolyRunner");
        var bones = new Dictionary<string, Transform>();

        // ----- Skeleton (T-pose, local offsets so với bone cha) -----
        Transform hips = Bone(bones, "Hips", root.transform, new Vector3(0f, 0.95f, 0f));
        Transform spine = Bone(bones, "Spine", hips, new Vector3(0f, 0.10f, 0f));
        Transform chest = Bone(bones, "Chest", spine, new Vector3(0f, 0.20f, 0f));
        Transform neck = Bone(bones, "Neck", chest, new Vector3(0f, 0.22f, 0f));
        Transform head = Bone(bones, "Head", neck, new Vector3(0f, 0.08f, 0f));

        foreach (int side in new[] { 1, -1 })
        {
            string s = side > 0 ? "Right" : "Left";   // nhân vật nhìn +Z => bên phải ở +X
            Transform upArm = Bone(bones, s + "UpperArm", chest, new Vector3(0.22f * side, 0.18f, 0f));
            Transform loArm = Bone(bones, s + "LowerArm", upArm, new Vector3(0.28f * side, 0f, 0f));
            Transform hand = Bone(bones, s + "Hand", loArm, new Vector3(0.26f * side, 0f, 0f));

            Transform upLeg = Bone(bones, s + "UpperLeg", hips, new Vector3(0.10f * side, -0.05f, 0f));
            Transform loLeg = Bone(bones, s + "LowerLeg", upLeg, new Vector3(0f, -0.42f, 0f));
            Transform foot = Bone(bones, s + "Foot", loLeg, new Vector3(0f, -0.38f, 0f));

            // Tay áo hoodie + bo tay + bàn tay
            Limb(upArm, "Sleeve", Vector3.zero, new Vector3(0.28f * side, 0f, 0f), 0.062f, 0.054f, Hoodie);
            Limb(loArm, "Forearm", Vector3.zero, new Vector3(0.22f * side, 0f, 0f), 0.054f, 0.050f, Hoodie);
            Blob(loArm, "Cuff", new Vector3(0.235f * side, 0f, 0f), new Vector3(0.03f, 0.055f, 0.055f), Vector3.zero, HoodieLight);
            Blob(hand, "Palm", new Vector3(0.045f * side, 0f, 0f), new Vector3(0.07f, 0.045f, 0.052f), Vector3.zero, Skin);
            Blob(hand, "Thumb", new Vector3(0.03f * side, 0f, 0.04f), new Vector3(0.03f, 0.018f, 0.03f), Vector3.zero, Skin);

            // Chân: quần jeans + sneaker to bản
            Limb(upLeg, "Thigh", Vector3.zero, new Vector3(0f, -0.42f, 0f), 0.090f, 0.072f, Jeans);
            Limb(loLeg, "Shin", Vector3.zero, new Vector3(0f, -0.34f, 0f), 0.072f, 0.062f, Jeans);
            Blob(loLeg, "JeansCuff", new Vector3(0f, -0.345f, 0f), new Vector3(0.068f, 0.04f, 0.068f), Vector3.zero, Jeans);
            Blob(foot, "Shoe", new Vector3(0f, -0.035f, 0.07f), new Vector3(0.078f, 0.062f, 0.165f), Vector3.zero, Shoe);
            Blob(foot, "ShoeToe", new Vector3(0f, -0.045f, 0.18f), new Vector3(0.066f, 0.045f, 0.065f), Vector3.zero, Accent);
            Blob(foot, "Sole", new Vector3(0f, -0.078f, 0.075f), new Vector3(0.083f, 0.024f, 0.175f), Vector3.zero, Sole);
        }

        // Thân
        Blob(hips, "Pelvis", new Vector3(0f, 0.01f, 0f), new Vector3(0.18f, 0.12f, 0.12f), Vector3.zero, Jeans);
        Blob(spine, "Waist", new Vector3(0f, 0.09f, 0f), new Vector3(0.185f, 0.14f, 0.125f), Vector3.zero, Hoodie);
        Blob(chest, "Chest", new Vector3(0f, 0.10f, 0.005f), new Vector3(0.225f, 0.17f, 0.145f), Vector3.zero, Hoodie);
        Blob(chest, "HoodieBand", new Vector3(0f, -0.02f, 0.012f), new Vector3(0.195f, 0.03f, 0.135f), Vector3.zero, HoodieLight);
        Blob(chest, "Hood", new Vector3(0f, 0.22f, -0.075f), new Vector3(0.12f, 0.07f, 0.07f), Vector3.zero, Hoodie);
        // Balo
        Blob(chest, "Backpack", new Vector3(0f, 0.08f, -0.19f), new Vector3(0.15f, 0.185f, 0.095f), Vector3.zero, Accent);
        Blob(chest, "BackpackPocket", new Vector3(0f, 0.02f, -0.27f), new Vector3(0.10f, 0.08f, 0.04f), Vector3.zero, HoodieLight);
        foreach (int side in new[] { 1, -1 })
            Limb(chest, "Strap", new Vector3(0.09f * side, 0.22f, -0.04f), new Vector3(0.10f * side, 0.00f, -0.04f), 0.018f, 0.018f, Sole);

        // Cổ + đầu
        Limb(neck, "Neck", new Vector3(0f, -0.02f, 0f), new Vector3(0f, 0.07f, 0f), 0.05f, 0.05f, Skin);
        Blob(head, "Head", new Vector3(0f, 0.14f, 0.005f), new Vector3(0.135f, 0.15f, 0.14f), Vector3.zero, Skin);
        Blob(head, "Nose", new Vector3(0f, 0.125f, 0.145f), new Vector3(0.022f, 0.025f, 0.025f), Vector3.zero, Skin);
        Blob(head, "EarR", new Vector3(0.135f, 0.13f, 0f), new Vector3(0.02f, 0.04f, 0.03f), Vector3.zero, Skin);
        Blob(head, "EarL", new Vector3(-0.135f, 0.13f, 0f), new Vector3(0.02f, 0.04f, 0.03f), Vector3.zero, Skin);
        foreach (int side in new[] { 1, -1 })
        {
            Blob(head, "EyeWhite", new Vector3(0.052f * side, 0.15f, 0.125f), new Vector3(0.032f, 0.040f, 0.018f), Vector3.zero, EyeWhite);
            Blob(head, "Pupil", new Vector3(0.052f * side, 0.15f, 0.140f), new Vector3(0.018f, 0.026f, 0.010f), Vector3.zero, Pupil);
            Blob(head, "Brow", new Vector3(0.055f * side, 0.195f, 0.12f), new Vector3(0.036f, 0.010f, 0.016f), new Vector3(0f, 0f, -12f * side), Hair);
        }
        Blob(head, "Smile", new Vector3(0f, 0.075f, 0.13f), new Vector3(0.035f, 0.011f, 0.012f), Vector3.zero, Pupil);
        Blob(head, "HairBack", new Vector3(0f, 0.15f, -0.095f), new Vector3(0.125f, 0.095f, 0.065f), Vector3.zero, Hair);
        // Nón lưỡi trai
        Blob(head, "CapCrown", new Vector3(0f, 0.215f, -0.005f), new Vector3(0.148f, 0.105f, 0.155f), Vector3.zero, Cap);
        Blob(head, "CapBrim", new Vector3(0f, 0.205f, 0.155f), new Vector3(0.105f, 0.014f, 0.095f), new Vector3(8f, 0f, 0f), CapBrim);
        Blob(head, "CapButton", new Vector3(0f, 0.318f, -0.005f), new Vector3(0.02f, 0.02f, 0.02f), Vector3.zero, CapBrim);

        // ----- Animation riêng cho nhân vật (Generic rig, clip theo đường dẫn bone) -----
        var controller = BuildAnimations(root, bones);

        var animator = root.AddComponent<Animator>();
        animator.applyRootMotion = false;
        animator.runtimeAnimatorController = controller;

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        Debug.Log("[LowPolyRunner] Đã tạo " + PrefabPath + " cùng bộ animation Run/JumpStart/JumpLoop/Roll/Hit/Death.");
    }

    [MenuItem("Escape Dash/Swap Selected Player Model")]
    public static void SwapSelected()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) { Debug.LogError("[LowPolyRunner] Chưa có prefab. Chạy 'Build Low-Poly Runner' trước."); return; }

        var player = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponentInParent<PlayerController>() : null;
        if (player == null) { Debug.LogError("[LowPolyRunner] Hãy chọn GameObject Player (có PlayerController) trong Hierarchy."); return; }

        Undo.IncrementCurrentGroup();

        // Bỏ bản LowPolyRunner đã gắn trước đó (nếu có) để không bị trùng khi build lại
        foreach (var a in player.GetComponentsInChildren<Animator>(true))
        {
            if (a.gameObject != player.gameObject && PrefabUtility.GetCorrespondingObjectFromSource(a.gameObject) == prefab)
                Undo.DestroyObjectImmediate(a.gameObject);
        }

        var oldAnimator = player.GetComponentInChildren<Animator>(true);
        if (oldAnimator != null && oldAnimator.gameObject != player.gameObject)
        {
            Undo.RecordObject(oldAnimator.gameObject, "Hide old model");
            oldAnimator.gameObject.SetActive(false);
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, player.transform);
        Undo.RegisterCreatedObjectUndo(instance, "Add LowPolyRunner");
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;

        var so = new SerializedObject(player);
        var prop = so.FindProperty("animator");
        if (prop != null)
        {
            prop.objectReferenceValue = instance.GetComponent<Animator>();
            so.ApplyModifiedProperties();
        }
        Selection.activeGameObject = instance;
        Debug.Log("[LowPolyRunner] Đã thay model cho " + player.name + " (model cũ chỉ bị ẩn, có thể xoá sau khi kiểm tra).");
    }

    // ---------- Helpers ----------

    private static Transform Bone(Dictionary<string, Transform> map, string name, Transform parent, Vector3 localPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        map[name] = go.transform;
        return go.transform;
    }

    // Ellipsoid mịn (smooth shading) gắn vào bone.
    private static void Blob(Transform bone, string name, Vector3 center, Vector3 radii, Vector3 euler, Color color)
    {
        var mesh = MakeLathe(EllipsoidProfile(), radii, Quaternion.Euler(euler), center);
        Attach(bone, name, mesh, color);
    }

    // Capsule thon (bán kính rA -> rB) đi từ điểm a đến b, hai đầu bo tròn.
    private static void Limb(Transform bone, string name, Vector3 a, Vector3 b, float rA, float rB, Color color)
    {
        Vector3 d = b - a;
        float len = d.magnitude;
        var prof = new List<Vector2>();
        for (int i = 0; i <= Ring; i++)            // nửa cầu dưới (tâm tại a)
        {
            float ang = Mathf.Lerp(-90f, 0f, (float)i / Ring) * Mathf.Deg2Rad;
            prof.Add(new Vector2(Mathf.Cos(ang) * rA, Mathf.Sin(ang) * rA));
        }
        for (int i = 0; i <= Ring; i++)            // nửa cầu trên (tâm tại b)
        {
            float ang = Mathf.Lerp(0f, 90f, (float)i / Ring) * Mathf.Deg2Rad;
            prof.Add(new Vector2(Mathf.Cos(ang) * rB, len + Mathf.Sin(ang) * rB));
        }
        var rot = Quaternion.FromToRotation(Vector3.up, d.normalized);
        var mesh = MakeLathe(prof, Vector3.one, rot, a);
        Attach(bone, name, mesh, color);
    }

    private static List<Vector2> EllipsoidProfile()
    {
        var prof = new List<Vector2>();
        int rings = Ring * 2;
        for (int i = 0; i <= rings; i++)
        {
            float ang = Mathf.Lerp(-90f, 90f, (float)i / rings) * Mathf.Deg2Rad;
            prof.Add(new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)));   // (bán kính, y)
        }
        return prof;
    }

    // Quay profile (r, y) quanh trục Y; scale rồi xoay + dịch. Đỉnh dùng chung nên shading mượt.
    private static Mesh MakeLathe(List<Vector2> profile, Vector3 scale, Quaternion rot, Vector3 pos)
    {
        int rings = profile.Count;
        var verts = new Vector3[rings * Seg];
        for (int i = 0; i < rings; i++)
        {
            for (int j = 0; j < Seg; j++)
            {
                float t = (float)j / Seg * Mathf.PI * 2f;
                var v = new Vector3(Mathf.Cos(t) * profile[i].x * scale.x, profile[i].y * scale.y, Mathf.Sin(t) * profile[i].x * scale.z);
                verts[i * Seg + j] = rot * v + pos;
            }
        }

        var tris = new List<int>();
        for (int i = 0; i < rings - 1; i++)
        {
            for (int j = 0; j < Seg; j++)
            {
                int j1 = (j + 1) % Seg;
                int v00 = i * Seg + j, v01 = i * Seg + j1, v10 = (i + 1) * Seg + j, v11 = (i + 1) * Seg + j1;
                tris.AddRange(new[] { v00, v10, v11, v00, v11, v01 });
            }
        }

        var mesh = new Mesh { name = "Lathe" };
        mesh.vertices = verts;
        mesh.triangles = tris.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void Attach(Transform bone, string name, Mesh mesh, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(bone, false);
        string meshPath = AssetDatabase.GenerateUniqueAssetPath(OutDir + "/Meshes/" + bone.name + "_" + name + ".asset");
        AssetDatabase.CreateAsset(mesh, meshPath);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = GetMaterial(color);
    }

    private static Material GetMaterial(Color c)
    {
        if (materials.TryGetValue(c, out var m)) return m;
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        m = new Material(sh) { name = "Runner_" + ColorUtility.ToHtmlStringRGB(c) };
        m.color = c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.25f);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.25f);
        AssetDatabase.CreateAsset(m, OutDir + "/Materials/" + m.name + ".mat");
        materials[c] = m;
        return m;
    }

    // =====================================================================
    //  ANIMATION (tạo bằng code, Generic rig - key thẳng lên localEulerAngles của từng bone)
    //  Quy ước: nhân vật nhìn +Z, bên phải = +X.
    //   - Euler X dương = nghiêng/ngả ra TRƯỚC (đầu chúi tới); chân/tay buông thõng đưa ra trước = X ÂM.
    //   - Tay buông xuống = Z = -70 * side (side: phải = +1, trái = -1).
    //   - Gập khuỷu về phía trước = Y = -side * góc.
    // =====================================================================

    private sealed class Pose
    {
        public readonly Dictionary<string, Vector3> Rot = new Dictionary<string, Vector3>();
        public Vector3 HipsPos = new Vector3(0f, 0.95f, 0f);

        public static Pose Standing()
        {
            var p = new Pose();
            foreach (int side in new[] { 1, -1 })
            {
                string s = side > 0 ? "Right" : "Left";
                p.Set(s + "UpperArm", 0f, 0f, -70f * side);
                p.Set(s + "LowerArm", 0f, -side * 20f, 0f);
            }
            return p;
        }

        public Pose Set(string bone, float x, float y, float z) { Rot[bone] = new Vector3(x, y, z); return this; }
    }

    private static readonly string[] AllBones =
    {
        "Hips", "Spine", "Chest", "Neck", "Head",
        "RightUpperArm", "RightLowerArm", "RightHand", "LeftUpperArm", "LeftLowerArm", "LeftHand",
        "RightUpperLeg", "RightLowerLeg", "RightFoot", "LeftUpperLeg", "LeftLowerLeg", "LeftFoot",
    };

    private static AnimatorController BuildAnimations(GameObject root, Dictionary<string, Transform> bones)
    {
        var paths = new Dictionary<string, string>();
        foreach (var kv in bones)
            paths[kv.Key] = AnimationUtility.CalculateTransformPath(kv.Value, root.transform);

        var clips = new List<KeyValuePair<string, AnimationClip>>
        {
            new KeyValuePair<string, AnimationClip>("Run",       MakeClip(paths, "Run",       0.52f, true,  32, PoseRun)),
            new KeyValuePair<string, AnimationClip>("JumpStart", MakeClip(paths, "JumpStart", 0.25f, false, 8,  PoseJumpStart)),
            new KeyValuePair<string, AnimationClip>("JumpLoop",  MakeClip(paths, "JumpLoop",  0.80f, true,  16, PoseJumpLoop)),
            new KeyValuePair<string, AnimationClip>("Roll",      MakeClip(paths, "Roll",      0.80f, false, 24, PoseRoll)),
            new KeyValuePair<string, AnimationClip>("Hit",       MakeClip(paths, "Hit",       0.60f, false, 12, PoseHit)),
            new KeyValuePair<string, AnimationClip>("Death",     MakeClip(paths, "Death",     1.20f, false, 24, PoseDeath)),
        };

        var controller = AnimatorController.CreateAnimatorControllerAtPath(OutDir + "/LowPolyRunnerAnimator.controller");
        var sm = controller.layers[0].stateMachine;
        foreach (var c in clips)
        {
            var state = sm.AddState(c.Key);   // state đầu tiên (Run) tự thành default
            state.motion = c.Value;
        }
        // Không cần transition: PlayerController dùng animator.CrossFade(tên state).
        return controller;
    }

    private static AnimationClip MakeClip(Dictionary<string, string> paths, string name, float length, bool loop, int keyCount, Func<float, Pose> sampler)
    {
        var clip = new AnimationClip { name = name, frameRate = 30f };

        var curves = new Dictionary<string, AnimationCurve[]>();   // bone -> x,y,z
        foreach (string b in AllBones) curves[b] = new[] { new AnimationCurve(), new AnimationCurve(), new AnimationCurve() };
        var hipsPos = new[] { new AnimationCurve(), new AnimationCurve(), new AnimationCurve() };

        for (int i = 0; i <= keyCount; i++)
        {
            float u = (float)i / keyCount;
            float time = u * length;
            Pose p = sampler(loop && i == keyCount ? 0f : u);   // key cuối của clip loop = key đầu để nối mượt
            foreach (string b in AllBones)
            {
                Vector3 r = p.Rot.TryGetValue(b, out var v) ? v : Vector3.zero;
                curves[b][0].AddKey(time, r.x);
                curves[b][1].AddKey(time, r.y);
                curves[b][2].AddKey(time, r.z);
            }
            hipsPos[0].AddKey(time, p.HipsPos.x);
            hipsPos[1].AddKey(time, p.HipsPos.y);
            hipsPos[2].AddKey(time, p.HipsPos.z);
        }

        string[] axis = { "x", "y", "z" };
        foreach (string b in AllBones)
        {
            for (int a = 0; a < 3; a++)
            {
                Smooth(curves[b][a], loop);
                clip.SetCurve(paths[b], typeof(Transform), "localEulerAnglesRaw." + axis[a], curves[b][a]);
            }
        }
        for (int a = 0; a < 3; a++)
        {
            Smooth(hipsPos[a], loop);
            clip.SetCurve(paths["Hips"], typeof(Transform), "localPosition." + axis[a], hipsPos[a]);
        }

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        AssetDatabase.CreateAsset(clip, OutDir + "/Animations/" + name + ".anim");
        return clip;
    }

    private static void Smooth(AnimationCurve c, bool loop)
    {
        for (int i = 0; i < c.length; i++) c.SmoothTangents(i, 0f);

        // Clip lặp: cho key đầu và key cuối chung một độ dốc để vòng lặp không bị khựng ở chỗ nối
        if (loop && c.length >= 3)
        {
            int n = c.length - 1;
            Keyframe first = c[0], last = c[n], second = c[1], beforeLast = c[n - 1];
            float span = second.time + (last.time - beforeLast.time);
            float slope = span > 0f ? (second.value - beforeLast.value) / span : 0f;
            first.outTangent = slope;
            last.inTangent = slope;
            c.MoveKey(0, first);
            c.MoveKey(n, last);
        }
    }

    // ----- Các pose theo thời gian chuẩn hoá u (0..1) -----

    private static Pose PoseRun(float u)
    {
        // Chu kỳ chạy: thigh X âm = đưa chân ra trước, knee X dương = gập gối (cẳng chân về sau).
        // Chân chạm đất được tính ngược: độ cao Hips luôn đặt sao cho bàn chân thấp nhất vừa chạm sàn
        // => không còn chân lơ lửng / lún xuống đất, thân nhấp nhô tự nhiên theo bước chạy.
        const float hipsLean = 12f;
        float f = u * Mathf.PI * 2f;
        var p = Pose.Standing();

        float maxDrop = 0f;
        foreach (int side in new[] { 1, -1 })
        {
            string s = side > 0 ? "Right" : "Left";
            float ph = f + (side > 0 ? 0f : Mathf.PI);

            float thigh = -12f - 42f * Mathf.Sin(ph);                       // -54 (trước) .. +30 (sau)
            float k = ph + 0.4f;
            float knee = 56f + 38f * Mathf.Cos(k) - 6f * Mathf.Cos(2f * k); // ~12 lúc chống, ~92 lúc vung
            float shinPitch = hipsLean + thigh + knee;                      // góc cẳng chân so với phương thẳng đứng

            p.Set(s + "UpperLeg", thigh, 0f, 0f);
            p.Set(s + "LowerLeg", knee, 0f, 0f);
            p.Set(s + "Foot", -0.8f * shinPitch, 0f, 0f);                   // giữ bàn chân gần song song mặt đất

            float drop = 0.42f * Mathf.Cos((hipsLean + thigh) * Mathf.Deg2Rad) + 0.38f * Mathf.Cos(shinPitch * Mathf.Deg2Rad);
            maxDrop = Mathf.Max(maxDrop, drop);

            // Tay đánh ngược chân, khuỷu gập ~90 độ
            float armPh = ph + Mathf.PI;
            float swing = Mathf.Sin(armPh);
            p.Set(s + "UpperArm", -8f - 38f * swing, 0f, -68f * side);
            p.Set(s + "LowerArm", 0f, -side * (85f - 15f * swing), 0f);
        }
        // 0.15 = cao độ mắt cá (0.10) + khoảng từ Hips tới khớp háng (0.05); cộng chút "bay" giữa hai bước
        float flight = 0.02f * Mathf.Max(0f, Mathf.Cos(2f * f));
        p.HipsPos = new Vector3(0f, 0.15f + maxDrop + flight, 0f);

        p.Set("Hips", hipsLean, -8f * Mathf.Sin(f), 2.5f * Mathf.Sin(f));   // xoay hông: hông bên chân trước đưa ra trước
        p.Set("Spine", 5f, 5f * Mathf.Sin(f), -1.5f * Mathf.Sin(f));
        p.Set("Chest", 5f, 8f * Mathf.Sin(f), -1.5f * Mathf.Sin(f));        // vai xoay ngược hông
        p.Set("Neck", -12f, -3f * Mathf.Sin(f), 0f);
        p.Set("Head", -10f, -4f * Mathf.Sin(f), 0f);                        // đầu giữ nhìn thẳng
        return p;
    }

    private static Pose PoseJumpStart(float u)
    {
        float e = Mathf.SmoothStep(0f, 1f, u);   // 0 = ngồi nén, 1 = bật lên
        var p = Pose.Standing();
        p.HipsPos = new Vector3(0f, Mathf.Lerp(0.80f, 0.97f, e), 0f);
        p.Set("Hips", Mathf.Lerp(25f, -3f, e), 0f, 0f);
        p.Set("Spine", Mathf.Lerp(15f, 0f, e), 0f, 0f);
        p.Set("Head", Mathf.Lerp(-20f, -6f, e), 0f, 0f);
        foreach (int side in new[] { 1, -1 })
        {
            string s = side > 0 ? "Right" : "Left";
            p.Set(s + "UpperLeg", Mathf.Lerp(-55f, 5f, e), 0f, 0f);
            p.Set(s + "LowerLeg", Mathf.Lerp(100f, 8f, e), 0f, 0f);
            p.Set(s + "Foot", Mathf.Lerp(-30f, 15f, e), 0f, 0f);
            p.Set(s + "UpperArm", Mathf.Lerp(40f, -150f, e), 0f, Mathf.Lerp(-70f, -25f, e) * side);
            p.Set(s + "LowerArm", 0f, -side * Mathf.Lerp(30f, 25f, e), 0f);
        }
        return p;
    }

    private static Pose PoseJumpLoop(float u)
    {
        float f = u * Mathf.PI * 2f;
        float sw = Mathf.Sin(f);
        var p = Pose.Standing();
        p.HipsPos = new Vector3(0f, 0.97f + 0.01f * sw, 0f);
        p.Set("Hips", -3f, 0f, 0f);
        p.Set("Head", -4f, 0f, 0f);
        // Chân phải co gối lên, chân trái đạp ra sau
        p.Set("RightUpperLeg", -70f + 5f * sw, 0f, 0f);
        p.Set("RightLowerLeg", 95f, 0f, 0f);
        p.Set("RightFoot", -10f, 0f, 0f);
        p.Set("LeftUpperLeg", 20f - 5f * sw, 0f, 0f);
        p.Set("LeftLowerLeg", 50f, 0f, 0f);
        p.Set("LeftFoot", 20f, 0f, 0f);
        // Tay dang rộng giữ thăng bằng
        p.Set("RightUpperArm", -30f + 10f * sw, 0f, -25f);
        p.Set("RightLowerArm", 0f, -30f, 0f);
        p.Set("LeftUpperArm", 20f - 10f * sw, 0f, 25f);
        p.Set("LeftLowerArm", 0f, 30f, 0f);
        return p;
    }

    private static Pose PoseRoll(float u)
    {
        // Lộn nhào về trước: co người (env) rồi xoay Hips 360 độ quanh trục X
        float env = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(Mathf.Min(u / 0.15f, (1f - u) / 0.15f)));
        float spin = 360f * Mathf.Clamp01((u - 0.1f) / 0.8f);
        var p = Pose.Standing();
        p.HipsPos = new Vector3(0f, Mathf.Lerp(0.95f, 0.58f, env), 0f);
        p.Set("Hips", spin, 0f, 0f);
        p.Set("Spine", 40f * env, 0f, 0f);
        p.Set("Chest", 30f * env, 0f, 0f);
        p.Set("Head", 30f * env, 0f, 0f);
        foreach (int side in new[] { 1, -1 })
        {
            string s = side > 0 ? "Right" : "Left";
            p.Set(s + "UpperLeg", -115f * env, 0f, 0f);
            p.Set(s + "LowerLeg", 125f * env, 0f, 0f);
            p.Set(s + "UpperArm", -70f * env, 0f, Mathf.Lerp(-70f, -25f, env) * side);
            p.Set(s + "LowerArm", 0f, -side * Mathf.Lerp(20f, 110f, env), 0f);
        }
        return p;
    }

    private static Pose PoseHit(float u)
    {
        // Vấp: ngả người ra sau, tay vung lên rồi lấy lại thăng bằng
        float s = Mathf.Sin(u * Mathf.PI);
        var p = Pose.Standing();
        p.HipsPos = new Vector3(0f, 0.95f - 0.07f * s, 0f);
        p.Set("Hips", 10f - 30f * s, 0f, 0f);
        p.Set("Spine", -10f * s, 0f, 0f);
        p.Set("Head", -20f * s, 0f, 0f);
        p.Set("RightUpperLeg", -35f * s, 0f, 0f);
        p.Set("RightLowerLeg", 40f * s, 0f, 0f);
        p.Set("LeftUpperLeg", 20f * s, 0f, 0f);
        p.Set("LeftLowerLeg", 30f * s, 0f, 0f);
        foreach (int side in new[] { 1, -1 })
        {
            string n = side > 0 ? "Right" : "Left";
            p.Set(n + "UpperArm", -50f * s, 0f, Mathf.Lerp(-70f, -30f, s) * side);
            p.Set(n + "LowerArm", 0f, -side * (20f + 30f * s), 0f);
        }
        return p;
    }

    private static Pose PoseDeath(float u)
    {
        // Ngã ngửa ra sau rồi nằm yên (giữ pose cuối)
        float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(u / 0.7f));
        var p = Pose.Standing();
        p.HipsPos = new Vector3(0f, Mathf.Lerp(0.95f, 0.30f, e), Mathf.Lerp(0f, -0.15f, e));
        p.Set("Hips", Mathf.Lerp(10f, -90f, e), 0f, 0f);
        p.Set("Head", -10f * e, 0f, 0f);
        foreach (int side in new[] { 1, -1 })
        {
            string s = side > 0 ? "Right" : "Left";
            p.Set(s + "UpperLeg", 5f * e, 0f, 6f * side * e);
            p.Set(s + "LowerLeg", 20f * e, 0f, 0f);
            p.Set(s + "UpperArm", -20f * e, 0f, Mathf.Lerp(-70f, -25f, e) * side);
            p.Set(s + "LowerArm", 0f, -side * (20f + 20f * e), 0f);
        }
        return p;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string leaf = Path.GetFileName(path);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
#endif
