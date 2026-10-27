#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class TerrainPainterWindow : EditorWindow
{
    [SerializeField] private HexGridManager grid;
    [SerializeField] private HexTerrainMap terrainMap;
    [SerializeField] private TerrainDefinition selectedTerrain;
    [SerializeField] private bool paintMode;
    [SerializeField] private bool eraseMode;
    [SerializeField] private bool showHoverPreview = true;

    [MenuItem("Tools/Hex Hero/Terrain Painter")]
    public static void ShowWindow()
        => GetWindow<TerrainPainterWindow>("Terrain Painter");

    private void OnEnable() => SceneView.duringSceneGui += OnSceneGUI;
    private void OnDisable() => SceneView.duringSceneGui -= OnSceneGUI;

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "1. Drag grid + terrain map below.\n" +
            "2. Pick a TerrainDefinition asset.\n" +
            "3. Toggle Paint Mode and click hexes in Scene view (3D).",
            MessageType.Info);

        grid = (HexGridManager)EditorGUILayout.ObjectField("Grid", grid, typeof(HexGridManager), true);
        terrainMap = (HexTerrainMap)EditorGUILayout.ObjectField("Terrain Map", terrainMap, typeof(HexTerrainMap), true);
        selectedTerrain = (TerrainDefinition)EditorGUILayout.ObjectField("Terrain", selectedTerrain, typeof(TerrainDefinition), false);

        EditorGUILayout.Space(4);
        paintMode = EditorGUILayout.Toggle("Paint Mode", paintMode);
        eraseMode = EditorGUILayout.Toggle("Erase Mode", eraseMode);
        showHoverPreview = EditorGUILayout.Toggle("Hover Preview", showHoverPreview);

        EditorGUILayout.Space(4);
        if (grid == null) EditorGUILayout.HelpBox("Assign a Grid.", MessageType.Warning);
        if (terrainMap == null) EditorGUILayout.HelpBox("Assign a Terrain Map.", MessageType.Warning);
        if (!eraseMode && selectedTerrain == null)
            EditorGUILayout.HelpBox("Assign a Terrain asset (or toggle Erase Mode).", MessageType.Warning);

        EditorGUILayout.Space(8);
        if (GUILayout.Button("Clear All Terrain"))
        {
            if (terrainMap == null) return;
            Undo.RecordObject(terrainMap, "Clear Terrain");
            terrainMap.ClearAll();
            EditorUtility.SetDirty(terrainMap);
            SceneView.RepaintAll();
            Debug.Log("[TerrainPainter] All terrain cleared.");
        }

        if (GUILayout.Button("Log Painted Cells"))
        {
            if (terrainMap == null) return;
            var list = terrainMap.Painted;
            Debug.Log($"[TerrainPainter] {list.Count} painted cell(s).");
            foreach (var e in list)
                Debug.Log($"  {e.cell} → {e.terrain?.terrainName ?? "null"}");
        }
    }

    private void OnSceneGUI(SceneView scene)
    {
        if (!paintMode || grid == null || terrainMap == null) return;

        // ---- Hover preview ----
        if (showHoverPreview && TryGetCellUnderMouse(out HexCoord hoverCell) && grid.Contains(hoverCell))
        {
            Vector3 c = grid.CellToWorld(hoverCell);
            Handles.color = eraseMode
                ? new Color(1f, 0.3f, 0.3f, 0.6f)
                : new Color(1f, 1f, 0.2f, 0.6f);
            Handles.DrawWireDisc(c, Vector3.up, grid.CellSize * 0.9f);
            scene.Repaint();
        }

        // ---- Click handling ----
        var e = Event.current;
        if (e.type != EventType.MouseDown || e.button != 0) return;
        if (e.alt) return;

        if (!TryGetCellUnderMouse(out HexCoord cell))
        {
            Debug.Log("[TerrainPainter] Click — ray didn't hit the ground plane.");
            return;
        }
        if (!grid.Contains(cell))
        {
            Debug.Log($"[TerrainPainter] Click — cell {cell} is outside the grid radius.");
            return;
        }

        Undo.RecordObject(terrainMap, "Paint Terrain");

        if (eraseMode)
        {
            terrainMap.SetTerrain(cell, terrainMap.DefaultTerrain);
            Debug.Log($"[TerrainPainter] Erased {cell}.");
        }
        else if (selectedTerrain != null)
        {
            terrainMap.SetTerrain(cell, selectedTerrain);
            Debug.Log($"[TerrainPainter] Painted {cell} → {selectedTerrain.terrainName}.");
        }
        else
        {
            Debug.LogWarning("[TerrainPainter] No terrain selected — nothing painted.");
            return;
        }

        EditorUtility.SetDirty(terrainMap);
        e.Use();
        SceneView.RepaintAll();
    }

    private bool TryGetCellUnderMouse(out HexCoord cell)
    {
        cell = default;
        if (grid == null) return false;

        var e = Event.current;
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        Plane plane = new Plane(Vector3.up,
            new Vector3(0f, grid.SurfaceY + grid.transform.position.y, 0f));

        if (!plane.Raycast(ray, out float enter)) return false;

        Vector3 world = ray.GetPoint(enter);
        cell = grid.WorldToCell(world);
        return true;
    }
}
#endif