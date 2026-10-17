using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class HexGridManager : MonoBehaviour
{
    [Header("Grid Shape (must match FlatTopCubeGenerator)")]
    [SerializeField] private int radius = 3;
    [SerializeField] private float cellSize = 1f;

    [Header("Surface")]
    [Tooltip("Y coordinate of the top surface where units stand. " +
             "Set this to your FlatTopCubeGenerator's wallHeight (e.g. 0.5).")]
    [SerializeField] private float surfaceY = 0.5f;

    private readonly Dictionary<HexCoord, HexUnit> occupancy = new();

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

    public HexUnit GetUnit(HexCoord c)
        => occupancy.TryGetValue(c, out var u) ? u : null;

    public bool IsOccupied(HexCoord c) => occupancy.ContainsKey(c);

    public void SetUnit(HexCoord c, HexUnit unit)
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

    private void Start()
    {
        // Test 1: center cell (0,0) should be at world origin-ish + surfaceY
        Debug.Log($"(0,0) -> world {CellToWorld(new HexCoord(0, 0))}");

        // Test 2: cell (1,0) should be ~1.5*cellSize to the +X
        Debug.Log($"(1,0) -> world {CellToWorld(new HexCoord(1, 0))}");

        // Test 3: round-trip — converting (1,1) to world and back should give (1,1)
        var c = new HexCoord(1, 1);
        var w = CellToWorld(c);
        var back = WorldToCell(w);
        Debug.Log($"(1,1) -> world {w} -> {back}  (should be (1,1))");

        // Test 4: enumerate all cells in a radius-3 grid
        int count = 0;
        foreach (var cell in AllCells()) count++;
        Debug.Log($"Total cells: {count}  (radius 3 should be 37)");
    }

}