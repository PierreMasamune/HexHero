using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private const string KEY = "hexhero_run";

    public static void Save(RunState state)
    {
        if (state == null) return;

        // Ensure all definition names are up to date before saving
        foreach (var unit in state.units)
        {
            if (unit.definition != null) unit.definitionName = unit.definition.name;
        }

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

            // ★ Re-link the ScriptableObject references!
            if (state != null && state.units != null)
            {
                foreach (var unit in state.units)
                {
                    if (string.IsNullOrEmpty(unit.definitionName)) continue;

                    // Load the asset from the Resources folder
                    unit.definition = Resources.Load<UnitDefinition>($"Units/{unit.definitionName}");
                    if (unit.definition == null)
                    {
                        Debug.LogWarning($"[SaveSystem] Could not find UnitDefinition '{unit.definitionName}' in Resources/Units!");
                    }
                }
            }
            return state != null;
        }
        catch
        {
            return false;
        }
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