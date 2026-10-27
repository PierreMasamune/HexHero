using System.Collections.Generic;
using UnityEngine;

public class HexGridManager : MonoBehaviour
{
    [Header("Grid Shape (must match FlatTopCubeGenerator)")]
    [SerializeField] private int radius = 3;
    [SerializeField] private float cellSize = 1f;

    [Header("Surface")]
    [SerializeField] private float surfaceY = 0.5f;

    private readonly Dictionary<HexCoord, Unit> occupancy = new();

    public int Radius => radius;
    public float CellSize => cellSize;
    public float SurfaceY => surfaceY;

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

    public Unit GetUnit(HexCoord c)
        => occupancy.TryGetValue(c, out var u) ? u : null;

    public bool IsOccupied(HexCoord c) => occupancy.ContainsKey(c);

    public void SetUnit(HexCoord c, Unit unit)
    {
        if (unit == null) occupancy.Remove(c);
        else occupancy[c] = unit;
    }

    public void ClearUnit(HexCoord c) => occupancy.Remove(c);

    public IEnumerable<HexCoord> ValidNeighbors(HexCoord c)
    {
        for (int i = 0; i < 6; i++)
        {
            var n = c.Neighbor(i);
            if (Contains(n)) yield return n;
        }
    }

    public void RegisterUnit(Unit unit, HexCoord cell)
    {
        if (unit == null) return;

        if (occupancy.TryGetValue(unit.Cell, out var existing) && existing == unit)
            occupancy.Remove(unit.Cell);

        SetUnit(cell, unit);
        unit.SetCell(cell);
        unit.SnapTo(CellToWorld(cell));
    }

    public List<Unit> GetAllEnemiesOf(Faction faction)
    {
        var list = new List<Unit>();
        foreach (var u in occupancy.Values)
            if (u != null && u.IsAlive && u.Faction != faction)
                list.Add(u);
        return list;
    }

    // -------- Reachable cells with per-cell cost --------
    // Budget = unit.RemainingMovement. Cost = 1 per step (extend later for terrain).
    public Dictionary<HexCoord, int> GetReachableCosts(Unit unit)
    {
        var costs = new Dictionary<HexCoord, int>();
        var visited = new HashSet<HexCoord> { unit.Cell };
        var queue = new Queue<(HexCoord cell, int cost)>();
        queue.Enqueue((unit.Cell, 0));

        int budget = unit.RemainingMovement;

        while (queue.Count > 0)
        {
            var (cell, cost) = queue.Dequeue();
            if (cost >= budget) continue;    // can't expand further

            foreach (var next in ValidNeighbors(cell))
            {
                if (visited.Contains(next)) continue;
                if (IsOccupied(next)) continue;

                visited.Add(next);
                int newCost = cost + 1;
                costs[next] = newCost;
                queue.Enqueue((next, newCost));
            }
        }
        return costs;
    }

    // Wrapper kept for existing callers that only need the set of cells.
    public HashSet<HexCoord> GetReachableCells(Unit unit)
        => new HashSet<HexCoord>(GetReachableCosts(unit).Keys);
}