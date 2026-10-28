#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MapBaker
{
    [MenuItem("Tools/Hex Hero/Bake Map From Scene")]
    public static void Bake()
    {
        // Find every MapNodeMarker in every loaded scene
        var markers = Object.FindObjectsByType<MapNodeMarker>(FindObjectsSortMode.None);
        if (markers.Length == 0)
        {
            Debug.LogWarning("[MapBaker] No MapNodeMarker found in any open scene.");
            return;
        }

        // Gather bounds so we can normalize positions
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        foreach (var m in markers)
        {
            var p = (Vector2)m.transform.position;
            if (p.x < min.x) min.x = p.x;
            if (p.y < min.y) min.y = p.y;
            if (p.x > max.x) max.x = p.x;
            if (p.y > max.y) max.y = p.y;
        }

        // Ask where to save
        string path = EditorUtility.SaveFilePanelInProject(
            "Save Baked Map", "Map_Chapter1", "asset",
            "Choose where to save the baked MapData asset",
            "Assets/Resources/Maps");

        if (string.IsNullOrEmpty(path)) return;

        var map = AssetDatabase.LoadAssetAtPath<MapData>(path);
        if (map == null)
        {
            map = ScriptableObject.CreateInstance<MapData>();
            AssetDatabase.CreateAsset(map, path);
        }

        map.nodes.Clear();

        // Position scale: convert world units to UI pixels.
        // 1 world unit → 100 UI px, and flip Y (world Y up, UI Y down).
        const float pixelScale = 100f;
        Vector2 range = max - min;
        // Guard against zero-size (all nodes stacked)
        if (range.x < 0.01f) range.x = 0.01f;
        if (range.y < 0.01f) range.y = 0.01f;

        foreach (var m in markers)
        {
            var p = (Vector2)m.transform.position;
            // Center the map around (0,0) and scale to px
            float px = (p.x - (min.x + max.x) * 0.5f) * pixelScale;
            float py = (p.y - (min.y + max.y) * 0.5f) * pixelScale;

            var node = new MapNodeData
            {
                id = m.id,
                type = m.type,
                chapter = m.chapter,
                tier = m.tier,
                label = m.label,
                position = new Vector2(px, py)
            };

            var children = new List<string>();
            foreach (var c in m.children)
                if (c != null) children.Add(c.id);
            node.children = children.ToArray();

            map.nodes.Add(node);
        }

        EditorUtility.SetDirty(map);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log($"[MapBaker] Baked {map.nodes.Count} nodes → {path}");
    }
}
#endif