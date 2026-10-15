using System;
using System.Reflection;
using UnityEngine;

public class TestUnitSpawner : MonoBehaviour
{
    public HexGridManager grid;
    public HexUnit unit;
    public HexCoord startCell = new HexCoord(0, 0);

    private void Start()
    {
        unit.Initialize(grid, startCell);
    }

    private void OnValidate()
    {
        // Run safely in editor and at runtime: try to initialize the unit first.
        if (unit == null || grid == null)
            return;

        try
        {
            // Preferred: delegate placement to the unit's Initialize method (same as Start).
            unit.Initialize(grid, startCell);
            return;
        }
        catch (Exception)
        {
            // fall through to best-effort placement below
        }

        // Best-effort fallback: try to ask the grid/manager for a world position for startCell via reflection,
        // otherwise attempt to compute using a HexGrid component and HexMatrix.Center if HexCoord exposes coordinates.
        try
        {
            // 1) Try common method names on the grid object that may return a Vector3 when given a HexCoord
            string[] candidateMethods = { "CellToWorld", "CellToWorldPosition", "GetCellWorldPosition", "GetCellCenter", "WorldPositionForCell" };
            MethodInfo found = null;
            foreach (var name in candidateMethods)
            {
                found = grid.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (found != null) break;
            }

            if (found != null)
            {
                // call method with startCell and apply result if Vector3
                var result = found.Invoke(grid, new object[] { startCell });
                if (result is Vector3 v)
                {
                    unit.transform.position = v;
                    return;
                }
            }

            // 2) Try to find a HexGrid component (layout provider) on the grid GameObject or parents
            var gridGO = (grid as MonoBehaviour)?.gameObject;
            HexGrid hexLayout = null;
            if (gridGO != null)
                hexLayout = gridGO.GetComponentInChildren<HexGrid>() ?? gridGO.GetComponentInParent<HexGrid>();

            if (hexLayout != null)
            {
                // 3) Attempt to read integer coordinates from HexCoord (common field/property names)
                Type coordType = startCell.GetType();
                int x = int.MinValue, z = int.MinValue;
                FieldInfo fx = coordType.GetField("x") ?? coordType.GetField("q") ?? coordType.GetField("col");
                FieldInfo fz = coordType.GetField("z") ?? coordType.GetField("r") ?? coordType.GetField("row");
                PropertyInfo px = coordType.GetProperty("x") ?? coordType.GetProperty("q") ?? coordType.GetProperty("col");
                PropertyInfo pz = coordType.GetProperty("z") ?? coordType.GetProperty("r") ?? coordType.GetProperty("row");

                if (fx != null && fz != null)
                {
                    x = Convert.ToInt32(fx.GetValue(startCell));
                    z = Convert.ToInt32(fz.GetValue(startCell));
                }
                else if (px != null && pz != null)
                {
                    x = Convert.ToInt32(px.GetValue(startCell));
                    z = Convert.ToInt32(pz.GetValue(startCell));
                }

                if (x != int.MinValue && z != int.MinValue)
                {
                    Vector3 world = HexMatrix.Center(hexLayout.HexSize, x, z, hexLayout.Orientation) + (hexLayout.transform?.position ?? Vector3.zero);
                    unit.transform.position = world;
                    return;
                }
            }
        }
        catch
        {
            // swallow any exceptions from reflection/fallbacks - OnValidate must be robust
        }

        // If we get here, we couldn't place via Initialize or heuristics; leave unit alone.
    }
}