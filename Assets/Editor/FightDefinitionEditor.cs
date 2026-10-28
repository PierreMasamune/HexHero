using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(FightDefinition))]
public class FightDefinitionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default Inspector (Enemies, Terrain lists, etc.)
        DrawDefaultInspector();

        FightDefinition fightDef = (FightDefinition)target;

        GUILayout.Space(15);

        // Draw a nice bold button
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f); // Greenish color
        if (GUILayout.Button("📥 Copy Terrain From Current Scene", GUILayout.Height(40)))
        {
            CaptureTerrainFromScene(fightDef);
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(5);
        EditorGUILayout.HelpBox(
            "1. Open your Battle Scene and use the Terrain Painter to draw your map.\n" +
            "2. Select this FightDefinition asset.\n" +
            "3. Click the button above to copy the painted terrain into this asset.",
            MessageType.Info);
    }

    private void CaptureTerrainFromScene(FightDefinition fightDef)
    {
        // Find the terrain map in the active scene
        HexTerrainMap terrainMap = Object.FindAnyObjectByType<HexTerrainMap>();

        if (terrainMap == null)
        {
            EditorUtility.DisplayDialog("Error", "Could not find a HexTerrainMap in the current scene. Are you in the Battle Scene?", "OK");
            return;
        }

        // Copy the painted terrain list
        fightDef.terrain = new System.Collections.Generic.List<TerrainCellEntry>(terrainMap.Painted);

        // Save the asset
        EditorUtility.SetDirty(fightDef);
        AssetDatabase.SaveAssets();

        Debug.Log($"[FightDefinitionEditor] Successfully copied {fightDef.terrain.Count} terrain cells from the scene into {fightDef.name}!");
        EditorUtility.DisplayDialog("Success", $"Copied {fightDef.terrain.Count} terrain cells into the Fight Definition!", "Awesome");
    }
}