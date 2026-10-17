using UnityEngine;

public class HexUnit : MonoBehaviour
{
    public HexCoord Cell { get; private set; }
    public int MoveRange = 3;

    private HexGridManager grid;

    public void Initialize(HexGridManager gridManager, HexCoord startCell)
    {
        grid = gridManager;
        // Force-clear the default (0,0) so MoveTo's ClearUnit doesn't nuke another unit.
        Cell = new HexCoord(int.MinValue, int.MinValue);
        MoveTo(startCell, snap: true);
    }

    public void MoveTo(HexCoord cell, bool snap = false)
    {
        if (grid == null)
        {
            Debug.LogWarning($"{name}: HexUnit has no grid — call Initialize first.", this);
            return;
        }

        grid.ClearUnit(Cell);
        Cell = cell;
        grid.SetUnit(cell, this);

        if (snap) transform.position = grid.CellToWorld(cell);
    }

    public void SnapToCell()
    {
        if (grid != null) transform.position = grid.CellToWorld(Cell);
    }
}