using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct TerrainCellEntry
{
    public HexCoord cell;
    public TerrainDefinition terrain;
}

[ExecuteAlways]   // ★ keeps byCell in sync in Edit mode
public class HexTerrainMap : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HexGridManager grid;
    [SerializeField] private TerrainDefinition defaultTerrain;

    [Header("Painted Cells")]
    [SerializeField] private List<TerrainCellEntry> painted = new();

    private readonly Dictionary<HexCoord, TerrainDefinition> byCell = new();

    public HexGridManager Grid => grid;
    public TerrainDefinition DefaultTerrain => defaultTerrain;

    private void OnEnable() => Rebuild();

    // Called by Unity when the Inspector changes a serialized field
    private void OnValidate() => Rebuild();

    public void Rebuild()
    {
        byCell.Clear();
        foreach (var e in painted)
            if (e.terrain != null)
                byCell[e.cell] = e.terrain;
    }

    public TerrainDefinition GetTerrain(HexCoord cell)
        => byCell.TryGetValue(cell, out var t) ? t : defaultTerrain;

    public bool IsPassable(HexCoord cell)
    {
        var t = GetTerrain(cell);
        return t == null || t.IsPassable;
    }

    public int GetMoveCost(HexCoord cell)
    {
        var t = GetTerrain(cell);
        return t != null ? Mathf.Max(1, t.movementCost) : 1;
    }

    public int GetDamageReductionPercent(HexCoord cell)
        => GetTerrain(cell)?.damageReductionPercent ?? 0;

    public int GetEntryDamage(HexCoord cell)
        => GetTerrain(cell)?.entryDamage ?? 0;

    public void SetTerrain(HexCoord cell, TerrainDefinition terrain)
    {
        painted.RemoveAll(e => e.cell.Equals(cell));

        if (terrain == null || terrain == defaultTerrain)
        {
            byCell.Remove(cell);
        }
        else
        {
            painted.Add(new TerrainCellEntry { cell = cell, terrain = terrain });
            byCell[cell] = terrain;
        }
    }

    public void ClearAll()
    {
        painted.Clear();
        byCell.Clear();
    }

    public IReadOnlyList<TerrainCellEntry> Painted => painted;
}