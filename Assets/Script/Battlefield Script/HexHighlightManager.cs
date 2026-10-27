using System.Collections.Generic;
using UnityEngine;

public enum AttackOutlineMode
{
    RegionBoundary, // one continuous outline around the whole attack range
    PerCell         // one hex ring around each attackable cell
}

public class HexHighlightManager : MonoBehaviour
{
    [SerializeField] private HexGridManager grid;
    [SerializeField] private float yOffset = 0.02f;
    [SerializeField] private float sizeShrink = 0.90f;   // fits inside gap walls

    [Header("Colors (auto-material)")]
    [SerializeField] private Color moveColor = new Color(0.25f, 0.55f, 1f, 0.45f);       // blue
    [SerializeField] private Color attackColor = new Color(1f, 0.25f, 0.25f, 0.55f);     // red
    [SerializeField] private Color cannotAttackColor = new Color(1f, 1f, 1f, 0.35f);     // white

    [Header("Optional override materials")]
    [SerializeField] private Material moveMaterial;
    [SerializeField] private Material attackMaterial;
    [SerializeField] private Material cannotAttackMaterial;

    // =============== ATTACK RANGE OUTLINE ===============

    [Header("Attack Range Outline")]
    [SerializeField] private AttackOutlineMode attackOutlineMode = AttackOutlineMode.RegionBoundary;
    [SerializeField] private Color attackOutlineColor = new Color(1f, 0.15f, 0.15f, 1f);  // strong red
    [SerializeField] private Material attackOutlineMaterialOverride;
    [Tooltip("Y offset for the outline, must be higher than yOffset so it renders on top of the fill.")]
    [SerializeField] private float attackOutlineYOffset = 0.03f;
    [Tooltip("Thickness as a fraction of hex radius (0.05 = 5%).")]
    [Range(0.01f, 0.15f)]
    [SerializeField] private float attackOutlineWidth = 0.05f;
    [Tooltip("Outline sits just outside the cell fill. 1.05 = 5% larger than the fill boundary.")]
    [Range(1.0f, 1.15f)]
    [SerializeField] private float attackOutlineScale = 1.05f;

    // ====================================================

    private Mesh flatHex;
    private Mesh attackOutlineMesh;
    private Material attackOutlineMaterial;

    private readonly List<HexCoord> moveCells = new();
    private readonly List<HexCoord> attackCells = new();
    private readonly List<HexCoord> cannotAttackCells = new();

    // Outline state
    private readonly List<HexCoord> attackOutlineCells = new();
    private HexCoord attackOutlineCenter;
    private bool attackOutlineVisible;

    private void Awake()
    {
        flatHex = BuildFlatHexMesh();

        if (moveMaterial == null) moveMaterial = MakeUnlit(moveColor);
        if (attackMaterial == null) attackMaterial = MakeUnlit(attackColor);
        if (cannotAttackMaterial == null) cannotAttackMaterial = MakeUnlit(cannotAttackColor);

        attackOutlineMaterial = attackOutlineMaterialOverride != null
            ? attackOutlineMaterialOverride
            : MakeUnlit(attackOutlineColor);
    }

    // ==================================================================
    //  SHOW / CLEAR — fills
    // ==================================================================

    /// <summary>Show a selection: blue = reachable, red = attackable enemy, white = out-of-range enemy.</summary>
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

    /// <summary>Clear everything (fills + outline).</summary>
    public void Clear()
    {
        moveCells.Clear();
        attackCells.Clear();
        cannotAttackCells.Clear();
        ClearAttackOutline();
    }

    // ==================================================================
    //  ATTACK RANGE OUTLINE
    // ==================================================================

    /// <summary>
    /// Show a red outline around the attack range.
    /// <paramref name="attackableCells"/> — every cell the unit can hit from its current position.
    /// <paramref name="unitCell"/> — the unit's own cell (used to skip interior edges so there's
    /// no outline ring around the hole the unit stands in).
    /// </summary>
    public void ShowAttackOutline(IList<HexCoord> attackableCells, HexCoord unitCell)
    {
        attackOutlineCells.Clear();
        attackOutlineCenter = unitCell;

        if (attackableCells != null)
            foreach (var c in attackableCells)
                attackOutlineCells.Add(c);

        attackOutlineVisible = attackOutlineCells.Count > 0;

        if (attackOutlineVisible) RebuildAttackOutlineMesh();
        else if (attackOutlineMesh != null) attackOutlineMesh.Clear();
    }

    /// <summary>Hide the attack range outline without touching the fills.</summary>
    public void ClearAttackOutline()
    {
        attackOutlineVisible = false;
        attackOutlineCells.Clear();
        if (attackOutlineMesh != null) attackOutlineMesh.Clear();
    }

    private void RebuildAttackOutlineMesh()
    {
        if (attackOutlineMesh == null) attackOutlineMesh = new Mesh { name = "AttackOutline" };
        else attackOutlineMesh.Clear();

        // Union of attackable cells + the unit's own cell — used for interior-edge detection.
        // The unit's cell is NOT drawn, but its presence means ring cells won't draw the
        // edge facing the unit (avoiding a weird hexagonal hole in the middle).
        var inRange = new HashSet<HexCoord>(attackOutlineCells);
        inRange.Add(attackOutlineCenter);

        var verts = new List<Vector3>();
        var tris = new List<int>();

        float outer = grid.CellSize * sizeShrink * attackOutlineScale;
        float inner = outer - grid.CellSize * attackOutlineWidth;
        Vector3 lift = Vector3.up * attackOutlineYOffset;

        if (attackOutlineMode == AttackOutlineMode.PerCell)
        {
            foreach (var cell in attackOutlineCells)
            {
                if (cell.Equals(attackOutlineCenter)) continue;
                AddHexRing(verts, tris, grid.CellToWorld(cell) + lift, outer, inner);
            }
        }
        else // RegionBoundary
        {
            foreach (var cell in attackOutlineCells)
            {
                if (cell.Equals(attackOutlineCenter)) continue;
                Vector3 center = grid.CellToWorld(cell) + lift;

                for (int i = 0; i < 6; i++)
                {
                    int dirIndex = (6 - i) % 6;    // edge i ↔ neighbor direction
                    var dir = HexCoord.Direction(dirIndex);
                    var neighbor = new HexCoord(cell.q + dir.q, cell.r + dir.r);

                    if (inRange.Contains(neighbor)) continue;   // interior edge, skip

                    AddEdgeQuad(verts, tris, center, i, outer, inner);
                }
            }
        }

        attackOutlineMesh.SetVertices(verts);
        attackOutlineMesh.SetTriangles(tris, 0);
        attackOutlineMesh.RecalculateBounds();
    }

    private void AddEdgeQuad(List<Vector3> verts, List<int> tris, Vector3 center,
                             int edgeIndex, float outer, float inner)
    {
        float aA = edgeIndex * 60f * Mathf.Deg2Rad;
        float aB = ((edgeIndex + 1) % 6) * 60f * Mathf.Deg2Rad;

        Vector3 vAOuter = center + new Vector3(Mathf.Cos(aA) * outer, 0, Mathf.Sin(aA) * outer);
        Vector3 vBOuter = center + new Vector3(Mathf.Cos(aB) * outer, 0, Mathf.Sin(aB) * outer);
        Vector3 vAInner = center + new Vector3(Mathf.Cos(aA) * inner, 0, Mathf.Sin(aA) * inner);
        Vector3 vBInner = center + new Vector3(Mathf.Cos(aB) * inner, 0, Mathf.Sin(aB) * inner);

        int b = verts.Count;
        verts.Add(vAOuter);
        verts.Add(vBOuter);
        verts.Add(vBInner);
        verts.Add(vAInner);

        // Winding matches BuildFlatHexMesh so the same Cull Back or Cull Off
        // shader works without flipping.
        tris.Add(b + 0); tris.Add(b + 2); tris.Add(b + 1);
        tris.Add(b + 0); tris.Add(b + 3); tris.Add(b + 2);
    }

    private void AddHexRing(List<Vector3> verts, List<int> tris, Vector3 center, float outer, float inner)
    {
        for (int i = 0; i < 6; i++)
            AddEdgeQuad(verts, tris, center, i, outer, inner);
    }

    // ==================================================================
    //  DRAW
    // ==================================================================

    private void Update()
    {
        if (grid == null) return;
        float scale = grid.CellSize * sizeShrink;
        Vector3 lift = Vector3.up * yOffset;

        // Fills
        DrawCells(moveCells, moveMaterial, scale, lift);
        DrawCells(attackCells, attackMaterial, scale, lift);
        DrawCells(cannotAttackCells, cannotAttackMaterial, scale, lift);

        // Outline — drawn once as a single mesh
        if (attackOutlineVisible
            && attackOutlineMesh != null
            && attackOutlineMesh.vertexCount > 0
            && attackOutlineMaterial != null)
        {
            Graphics.DrawMesh(attackOutlineMesh, Matrix4x4.identity, attackOutlineMaterial, 0);
        }
    }

    private void DrawCells(List<HexCoord> cells, Material mat, float scale, Vector3 lift)
    {
        if (mat == null || cells.Count == 0) return;
        foreach (var c in cells)
        {
            var trs = Matrix4x4.TRS(grid.CellToWorld(c) + lift, Quaternion.identity, Vector3.one * scale);
            Graphics.DrawMesh(flatHex, trs, mat, 0);
        }
    }

    // ==================================================================
    //  MESH + MATERIAL HELPERS
    // ==================================================================

    private static Mesh BuildFlatHexMesh()
    {
        var m = new Mesh { name = "FlatHexHighlight" };
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

    private static Material MakeUnlit(Color c)
    {
        var shader = Shader.Find("Sprites/Default")
                     ?? Shader.Find("Unlit/Transparent")
                     ?? Shader.Find("Unlit/Color");
        var mat = new Material(shader) { name = "Highlight_" + c };
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        return mat;
    }
}