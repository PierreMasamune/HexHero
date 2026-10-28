#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class RunDebugger
{
    [MenuItem("Tools/Hex Hero/Reset Run Save")]
    public static void ResetRunSave()
    {
        // 1. Delete the save file
        SaveSystem.DeleteSave();

        // 2. Find the RunManager in the scene and reset it
        var runManager = Object.FindAnyObjectByType<RunManager>();
        if (runManager != null)
        {
            runManager.ResetRunFromEditor();
        }
        else
        {
            Debug.LogWarning("[RunDebugger] No RunManager found in the scene. Save file deleted, but state wasn't refreshed.");
        }
    }
}
#endif