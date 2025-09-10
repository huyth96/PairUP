#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text.RegularExpressions;

[CustomEditor(typeof(BoardPresenter))]
public class BoardPresenterEditor : Editor
{
    private const string IconsFolder = "Assets/Art/Png/Parts";
    private const int MaxCount = 30;
    private static readonly Regex NamePattern = new Regex(@"^Onet0*(\d+)$", RegexOptions.IgnoreCase);

    public override void OnInspectorGUI()
    {
        // Vẽ Inspector mặc định
        DrawDefaultInspector();

        GUILayout.Space(10);
        if (GUILayout.Button("Fill Onet1 → Onet30 từ Assets/Art/Png/Parts"))
        {
            FillSprites();
        }
    }

    private void FillSprites()
    {
        var bp = (BoardPresenter)target;

        // Tìm sprite trong thư mục
        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { IconsFolder });
        var map = new Dictionary<int, Sprite>();

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sp == null) continue;

            var m = NamePattern.Match(sp.name);
            if (!m.Success) continue;

            if (int.TryParse(m.Groups[1].Value, out int n) && n >= 1 && n <= MaxCount)
            {
                map[n] = sp; // sprite đúng số
            }
        }

        // Tạo list theo thứ tự 1..30
        var wanted = new List<Sprite>();
        var missing = new List<int>();
        for (int i = 1; i <= MaxCount; i++)
        {
            if (map.TryGetValue(i, out var sp)) wanted.Add(sp);
            else { wanted.Add(null); missing.Add(i); }
        }

        // Gán vào SerializedProperty (sprites là private)
        var so = new SerializedObject(bp);
        var spritesProp = so.FindProperty("sprites");

        spritesProp.arraySize = MaxCount;
        for (int i = 0; i < MaxCount; i++)
        {
            spritesProp.GetArrayElementAtIndex(i).objectReferenceValue = wanted[i];
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(bp);
        AssetDatabase.SaveAssets();

        if (missing.Count > 0)
            Debug.LogWarning($"Thiếu sprite: {string.Join(", ", missing.ConvertAll(i => $"Onet{i} / Onet{i:00}"))}");
        else
            Debug.Log($"Đã điền đủ {MaxCount} sprite: Onet1 → Onet30 vào BoardPresenter.sprites");
    }
}
#endif
