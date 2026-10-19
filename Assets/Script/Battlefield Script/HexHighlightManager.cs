using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class HexHighlightManager : MonoBehaviour
{
    [SerializeField] private HexGridManager grid;
    [SerializeField] private float yOffset = 0.02f;
    [SerializeField] private float sizeShrink = 0.90f;   // fits inside gap walls

    [Header("Colors (auto-material)")]
    [SerializeField] private Color moveColor = new Color(0.25f, 0.55f, 1f, 0.45f); // blue
    [SerializeField] private Color attackColor = new Color(1f, 0.25f, 0.25f, 0.55f); // red
    [SerializeField] private Color cannotAttackColor = new Color(1f, 1f, 1f, 0.35f); // white

    [Header("Optional override materials")]
    [SerializeField] private Material moveMaterial;
    [SerializeField] private Material attackMaterial;
    [SerializeField] private Material cannotAttackMaterial;

    private Mesh flatHex;

    private readonly List<HexCoord> moveCells = new();
    private readonly List<HexCoord> attackCells = new();
    private readonly List<HexCoord> cannotAttackCells = new();

    private void Awake()
    {
        flatHex = BuildFlatHexMesh();
        if (moveMaterial == null) moveMaterial = MakeUnlit(moveColor);
        if (attackMaterial == null) attackMaterial = MakeUnlit(attackColor);
        if (cannotAttackMaterial == null) cannotAttackMaterial = MakeUnlit(cannotAttackColor);
    }

    // Show a selection: blue = reachable, red = attackable enemy, white = out-of-range enemy
    public void Show(Unit selected, HexGridManager g, List<HexCoord> moves, List<Unit> enemies)
    {
        Clear();
        if (moves != null) moveCells.AddRange(moves);

        if (selected != null && enemies != null)
        {
            foreach (var e in enemies)
            {
                if (e == null || !e.IsAlive) continue;
                if (HexCoord.Distance(selected.Cell, e.Cell) <= selected.AttackRange)
                    attackCells.Add(e.Cell);
                else
                    cannotAttackCells.Add(e.Cell);
            }
        }
    }

    public void Clear()
    {
        moveCells.Clear();
        attackCells.Clear();
        cannotAttackCells.Clear();
    }

    private void Update()
    {
        if (grid == null) return;
        float scale = grid.CellSize * sizeShrink;
        Vector3 lift = Vector3.up * yOffset;

        DrawCells(moveCells, moveMaterial, scale, lift);
        DrawCells(attackCells, attackMaterial, scale, lift);
        DrawCells(cannotAttackCells, cannotAttackMaterial, scale, lift);
    }

    private void DrawCells(List<HexCoord> cells, Material mat, float scale, Vector3 lift)
    {
        if (mat == null || cells.Count == 0) return;
        foreach (var c in cells)
        {
            var trs = Matrix4x4.TRS(
                grid.CellToWorld(c) + lift,
                Quaternion.identity,
                Vector3.one * scale);
            Graphics.DrawMesh(flatHex, trs, mat, 0);
        }
    }

    // Flat-top hex, radius 1, in the XZ plane at Y=0.
    private static Mesh BuildFlatHexMesh()
    {
        var m = new Mesh { name = "FlatHexHighlight" };
        var v = new Vector3[7];
        v[0] = Vector3.zero;
        for (int i = 0; i < 6; i++)
        {
            float a = Mathf.Deg2Rad * 60f * i;    // 0°, 60°, 120°… = flat-top
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

    private static Material MakeUnlit(Color c)
    {
        // Sprites/Default works in both Built-in and URP and supports alpha.
        var shader = Shader.Find("Sprites/Default")
                     ?? Shader.Find("Unlit/Transparent")
                     ?? Shader.Find("Unlit/Color");
        var mat = new Material(shader) { name = "Highlight_" + c };
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        return mat;
    }
}