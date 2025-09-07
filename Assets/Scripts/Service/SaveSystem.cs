using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private static string saveFile = Path.Combine(Application.persistentDataPath, "save.json");

    public static void Save(PlayerData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(saveFile, json);

        Debug.Log($"[SaveSystem] Saved to: {saveFile}");
        Debug.Log($"[SaveSystem] Content: {json}");
    }

    public static PlayerData Load()
    {
        if (!File.Exists(saveFile))
        {
            Debug.Log($"[SaveSystem] No save file, creating new data at {saveFile}");
            return new PlayerData();
        }

        string json = File.ReadAllText(saveFile);
        Debug.Log($"[SaveSystem] Loaded from: {saveFile}");
        Debug.Log($"[SaveSystem] Content: {json}");
        return JsonUtility.FromJson<PlayerData>(json);
    }
    public static void Delete()
    {
        if (System.IO.File.Exists(saveFile))
            System.IO.File.Delete(saveFile);
    }
}
