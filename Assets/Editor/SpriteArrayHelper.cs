#if UNITY_EDITOR
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class FillOnetSprites
{
    // THƯ MỤC CHỨA ICON (đổi cho phù hợp dự án của bạn)
    const string iconsFolder = "Assets/Art/Png/Parts";

    [MenuItem("Tools/Fill Onet01–Onet30 on BoardManager")]
    static void Fill()
    {
        var bm = Object.FindObjectOfType<BoardManager>();
        if (bm == null) { Debug.LogError("Không tìm thấy BoardManager trong scene."); return; }

        // Tìm tất cả Sprite trong folder chỉ định
        var guids = AssetDatabase.FindAssets("t:Sprite", new[] { iconsFolder });
        var allSprites = guids
            .Select(g => AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(s => s != null)
            .ToArray();

        // Lập map theo tên để tra cứu nhanh
        var byName = new Dictionary<string, Sprite>();
        foreach (var s in allSprites)
        {
            if (!byName.ContainsKey(s.name))
                byName.Add(s.name, s);
        }

        // Lấy đúng các sprite Onet01..Onet30 theo thứ tự
        var wanted = new List<Sprite>();
        var missing = new List<string>();

        for (int i = 1; i <= 30; i++)
        {
            string name = $"Onet{i:00}";
            if (byName.TryGetValue(name, out var sp))
                wanted.Add(sp);
            else
                missing.Add(name);
        }

        bm.sprites = wanted.ToArray();
        EditorUtility.SetDirty(bm);
        AssetDatabase.SaveAssets();

        if (missing.Count > 0)
            Debug.LogWarning($"Thiếu sprite: {string.Join(", ", missing)}");
        else
            Debug.Log($"Đã điền {wanted.Count} sprite: Onet01 → Onet30 vào BoardManager.");
    }
}
#endif

