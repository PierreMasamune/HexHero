using System;
using System.Collections.Generic;
using UnityEngine;

public class HexGridManager : MonoBehaviour
{
    [Header("Grid Shape")]
    [SerializeField] private int radius = 3;
    [SerializeField] private float cellSize = 1f;

    [Header("Surface")]
    [SerializeField] private float surfaceY = 0.5f;

    [Header("Terrain")]
    [SerializeField] private HexTerrainMap terrainMap;

    [Header("Death Handling")]
    [SerializeField] private float deathDestroyDelay = 0.15f;

    public event Action<Unit> OnUnitDied;

    private readonly Dictionary<HexCoord, Unit> occupancy = new();

    public int Radius => radius;
    public float CellSize => cellSize;
    public float SurfaceY => surfaceY;
    public HexTerrainMap TerrainMap => terrainMap;

    private void OnDestroy()
    {
        foreach (var u in occupancy.Values)
            if (u != null) u.OnDied -= HandleUnitDied;
        occupancy.Clear();
    }

    // ==================================================================
    //  COORDINATES
    // ==================================================================

    public IEnumerable<HexCoord> AllCells()
    {
        for (int q = -radius; q <= radius; q++)
        {
            int rMin = Mathf.Max(-radius, -q - radius);
            int rMax = Mathf.Min(radius, -q + radius);
            for (int r = rMin; r <= rMax; r++)
                yield return new HexCoord(q, r);
        }
    }

    public bool Contains(HexCoord c) => HexCoord.Distance(HexCoord.Zero, c) <= radius;

    public Vector3 CellToWorld(HexCoord c)
        => c.ToWorld(cellSize, surfaceY) + transform.position;

    public HexCoord WorldToCell(Vector3 world)
        => HexCoord.FromWorld(world, cellSize, transform.position);

    // ==================================================================
    //  OCCUPANCY
    // ==================================================================

    public Unit GetUnit(HexCoord c)
        => occupancy.TryGetValue(c, out var u) && u != null && u.IsAlive ? u : null;

    public bool IsOccupied(HexCoord c) => occupancy.ContainsKey(c);

    public void SetUnit(HexCoord c, Unit unit)
    {
        if (unit == null) occupancy.Remove(c);
        else occupancy[c] = unit;
    }

    public void ClearUnit(HexCoord c) => occupancy.Remove(c);

    public void RegisterUnit(Unit unit, HexCoord cell)
    {
        if (unit == null) return;

        unit.OnDied -= HandleUnitDied;
        unit.OnDied += HandleUnitDied;

        if (occupancy.TryGetValue(unit.Cell, out var existing) && existing == unit)
            occupancy.Remove(unit.Cell);

        SetUnit(cell, unit);
        unit.SetCell(cell);
        unit.SnapTo(CellToWorld(cell));
    }

    private void HandleUnitDied(Unit unit)
    {
        if (unit == null) return;

        if (occupancy.TryGetValue(unit.Cell, out var existing) && existing == unit)
            occupancy.Remove(unit.Cell);

        OnUnitDied?.Invoke(unit);

        if (unit.gameObject != null)
        {
            if (deathDestroyDelay > 0f) Destroy(unit.gameObject, deathDestroyDelay);
            else Destroy(unit.gameObject);
        }
    }

    // ==================================================================
    //  NEIGHBORS + PATHFINDING
    // ==================================================================

    public IEnumerable<HexCoord> ValidNeighbors(HexCoord c)
    {
        for (int i = 0; i < 6; i++)
        {
            var n = c.Neighbor(i);
            if (Contains(n)) yield return n;
        }
    }

    public List<Unit> GetAllEnemiesOf(Faction faction)
    {
        var list = new List<Unit>();
        foreach (var u in occupancy.Values)
            if (u != null && u.IsAlive && u.Faction != faction)
                list.Add(u);
        return list;
    }

    /// <summary>
    /// Dijkstra-ish BFS with variable per-cell movement costs (from terrain).
    /// Impassable terrain is skipped. Living units block movement.
    /// Returns cell → total cost to reach it (cost = movement points spent).
    /// </summary>
    public Dictionary<HexCoord, int> GetReachableCosts(Unit unit)
    {
        var costs = new Dictionary<HexCoord, int>();
        if (unit == null) return costs;

        var queue = new Queue<(HexCoord cell, int totalCost)>();
        queue.Enqueue((unit.Cell, 0));
        int budget = unit.RemainingMovement;

        while (queue.Count > 0)
        {
            var (cell, totalCost) = queue.Dequeue();

            foreach (var next in ValidNeighbors(cell))
            {
                if (GetUnit(next) != null) continue;
                if (terrainMap != null && !terrainMap.IsPassable(next)) continue;

                int stepCost = terrainMap != null ? terrainMap.GetMoveCost(next) : 1;
                int newCost = totalCost + stepCost;
                if (newCost > budget) continue;

                if (costs.TryGetValue(next, out int existing) && existing <= newCost)
                    continue;

                costs[next] = newCost;
                queue.Enqueue((next, newCost));
            }
        }

        return costs;
    }

    public HashSet<HexCoord> GetReachableCells(Unit unit)
        => new HashSet<HexCoord>(GetReachableCosts(unit).Keys);
}