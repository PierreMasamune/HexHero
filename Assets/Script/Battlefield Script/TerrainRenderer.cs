using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]   // ★ so it draws in Edit mode (not just Play mode)
public class TerrainRenderer : MonoBehaviour
{
    [SerializeField] private HexGridManager grid;
    [SerializeField] private HexTerrainMap terrainMap;
    [SerializeField] private float yOffset = 0.015f;
    [SerializeField] private float sizeShrink = 0.98f;

    private Mesh flatHex;
    private readonly Dictionary<TerrainDefinition, Material> matCache = new();

    private void OnEnable()
    {
        if (flatHex == null) flatHex = BuildFlatHexMesh();
    }

    private void Update()
    {
        if (grid == null || terrainMap == null) return;
        if (flatHex == null) flatHex = BuildFlatHexMesh();

        float scale = grid.CellSize * sizeShrink;

        foreach (var cell in grid.AllCells())
        {
            var t = terrainMap.GetTerrain(cell);
            if (t == null || t == terrainMap.DefaultTerrain) continue;

            var mat = GetMaterial(t);
            if (mat == null) continue;

            var trs = Matrix4x4.TRS(grid.CellToWorld(cell) + Vector3.up * yOffset,
                                    Quaternion.identity, Vector3.one * scale);
            Graphics.DrawMesh(flatHex, trs, mat, 0);
        }
    }

    private Material GetMaterial(TerrainDefinition t)
    {
        if (t.material != null) return t.material;
        if (matCache.TryGetValue(t, out var m) && m != null) return m;

        var shader = Shader.Find("Sprites/Default")
                     ?? Shader.Find("Unlit/Transparent")
                     ?? Shader.Find("Unlit/Color");
        if (shader == null) return null;

        m = new Material(shader) { name = $"Terrain_{t.terrainName}", hideFlags = HideFlags.DontSave };
        if (m.HasProperty("_Color")) m.SetColor("_Color", t.color);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", t.color);
        matCache[t] = m;
        return m;
    }

    private static Mesh BuildFlatHexMesh()
    {
        var m = new Mesh { name = "TerrainFlatHex", hideFlags = HideFlags.DontSave };
        var v = new Vector3[7];
        v[0] = Vector3.zero;
        for (int i = 0; i < 6; i++)
        {
            float a = Mathf.Deg2Rad * 60f * i;
            v[i + 1] = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
        }
        var t = new int[18];
        for (int i = 0; i < 6; i++)
        {
            t[i * 3 + 0] = 0;
            t[i * 3 + 1] = 1 + ((i + 1) % 6);
            t[i * 3 + 2] = 1 + i;
        }
        m.vertices = v;
        m.triangles = t;
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }
}