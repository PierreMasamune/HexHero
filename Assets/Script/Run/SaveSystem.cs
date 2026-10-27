using System.IO;
using UnityEngine;

/// <summary>
/// JSON save/load of a RunState. Simple and synchronous — call from
/// RunManager between chapters or on application quit.
/// </summary>
public static class SaveSystem
{
    private const string KEY = "hexhero_run";

    public static void Save(RunState state)
    {
        if (state == null) return;
        string json = JsonUtility.ToJson(state, prettyPrint: true);
#if UNITY_EDITOR || UNITY_STANDALONE
        File.WriteAllText(PathForEditor(), json);
#endif
        PlayerPrefs.SetString(KEY, json);
        PlayerPrefs.Save();
    }

    public static bool TryLoad(out RunState state)
    {
        state = null;
        string json = null;
#if UNITY_EDITOR || UNITY_STANDALONE
        var p = PathForEditor();
        if (File.Exists(p)) json = File.ReadAllText(p);
#endif
        if (string.IsNullOrEmpty(json))
            json = PlayerPrefs.GetString(KEY, string.Empty);
        if (string.IsNullOrEmpty(json)) return false;

        try
        {
            state = JsonUtility.FromJson<RunState>(json);
            return state != null;
        }
        catch { return false; }
    }

    public static void DeleteSave()
    {
        PlayerPrefs.DeleteKey(KEY);
        PlayerPrefs.Save();
#if UNITY_EDITOR || UNITY_STANDALONE
        var p = PathForEditor();
        if (File.Exists(p)) File.Delete(p);
#endif
    }

    private static string PathForEditor()
        => Path.Combine(Application.persistentDataPath, "run_save.json");
}