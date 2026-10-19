using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class HexSelector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera cam;
    [SerializeField] private HexGridManager grid;
    [SerializeField] private HexHighlightManager highlight;
    [SerializeField] private LayerMask gridLayer;

    [Header("Hover Highlight (optional)")]
    [SerializeField] private Transform hoverHighlight;
    [SerializeField] private float highlightYOffset = 0.05f;

    [Header("Selection Highlight (optional)")]
    [SerializeField] private Transform selectionHighlight;

    [Header("Debug")]
    [SerializeField] private bool debugRaycast = false;

    [Header("State")]
    public HexCoord HoveredCell;
    public HexCoord SelectedCell;
    public bool HasHover { get; private set; }
    public bool HasSelection { get; private set; }

    public event System.Action<HexCoord> OnCellClicked;
    public event System.Action<Unit> OnUnitSelected;
    public event System.Action<Unit, HexCoord> OnUnitMoved;
    public event System.Action<Unit, Unit> OnUnitAttacked;
    public event System.Action OnDeselected;

    private Unit selectedUnit;
    private Dictionary<HexCoord, int> cachedMoveCosts;   // cell → cost to reach it
    private bool inputEnabled = true;

    private void Reset()
    {
        cam = Camera.main;
        grid = FindAnyObjectByType<HexGridManager>();
        highlight = FindAnyObjectByType<HexHighlightManager>();
    }

    private void Start()
    {
        if (cam == null) cam = Camera.main;
        if (grid == null) grid = FindAnyObjectByType<HexGridManager>();
        if (highlight == null) highlight = FindAnyObjectByType<HexHighlightManager>();

        if (hoverHighlight != null) hoverHighlight.gameObject.SetActive(false);
        if (selectionHighlight != null) selectionHighlight.gameObject.SetActive(false);
    }

    public void EnableInput(bool enable)
    {
        inputEnabled = enable;
        if (!enable) Deselect();
    }

    private void Update()
    {
        if (!inputEnabled) return;
        if (Mouse.current == null) return;

        UpdateHover();

        if (Mouse.current.leftButton.wasPressedThisFrame && HasHover)
            HandleClick(HoveredCell);
    }

    private void UpdateHover()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, gridLayer))
        {
            if (debugRaycast)
                Debug.Log($"Hit: {hit.collider.gameObject.name} | " +
                          $"Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");

            HexCoord cell = grid.WorldToCell(hit.point);
            if (grid.Contains(cell))
            {
                HoveredCell = cell;
                HasHover = true;

                if (hoverHighlight != null)
                {
                    hoverHighlight.gameObject.SetActive(true);
                    Vector3 p = grid.CellToWorld(cell);
                    hoverHighlight.position = new Vector3(p.x, p.y + highlightYOffset, p.z);
                }
                return;
            }
        }

        HasHover = false;
        if (hoverHighlight != null) hoverHighlight.gameObject.SetActive(false);
    }

    private void HandleClick(HexCoord cell)
    {
        OnCellClicked?.Invoke(cell);

        Unit clicked = grid.GetUnit(cell);

        // ---- No selection → try to select a player unit ----
        if (selectedUnit == null)
        {
            if (clicked != null && clicked.Faction == Faction.Player && clicked.CanStillAct)
                Select(clicked);
            else
                SelectedCell = cell;
            return;
        }

        // ---- Clicking the same unit → toggle off ----
        if (clicked == selectedUnit)
        {
            Deselect();
            return;
        }

        // ---- Clicking another friendly player unit → switch selection ----
        if (clicked != null && clicked.Faction == Faction.Player)
        {
            if (clicked.CanStillAct) Select(clicked);
            else Deselect();
            return;
        }

        // ---- Clicking an enemy → attack if in range ----
        if (clicked != null && clicked.Faction == Faction.Enemy)
        {
            bool inRange = !selectedUnit.HasAttacked
                        && HexCoord.Distance(selectedUnit.Cell, clicked.Cell) <= selectedUnit.AttackRange;

            if (inRange)
            {
                StatsManager.Instance.ResolveAttack(selectedUnit, clicked);
                selectedUnit.MarkAttacked();        // sets HasAttacked = true AND zeroes remaining movement
                OnUnitAttacked?.Invoke(selectedUnit, clicked);
            }
            Deselect();
            return;
        }

        // ---- Clicking an empty cell → move if it's reachable & we have budget ----
        if (clicked == null
            && selectedUnit.CanMove
            && cachedMoveCosts != null
            && cachedMoveCosts.TryGetValue(cell, out int moveCost))
        {
            HexCoord from = selectedUnit.Cell;

            grid.ClearUnit(from);
            selectedUnit.SetCell(cell);
            grid.SetUnit(cell, selectedUnit);
            selectedUnit.SnapTo(grid.CellToWorld(cell));

            selectedUnit.SpendMovement(moveCost);   // ← deduct ONLY the cost spent

            OnUnitMoved?.Invoke(selectedUnit, from);

            // Recompute highlights from the new cell so a partial move leaves
            // the remaining blue cells on screen for a second move.
            if (selectedUnit.CanMove) RefreshHighlights();
            else Deselect();

            return;
        }

        // ---- Anything else cancels ----
        Deselect();
    }

    private void Select(Unit unit)
    {
        selectedUnit = unit;
        SelectedCell = unit.Cell;
        HasSelection = true;
        RefreshHighlights();
        OnUnitSelected?.Invoke(unit);
    }

    private void Deselect()
    {
        bool had = selectedUnit != null;
        selectedUnit = null;
        cachedMoveCosts = null;
        HasSelection = false;

        if (selectionHighlight != null) selectionHighlight.gameObject.SetActive(false);
        if (highlight != null) highlight.Clear();
        if (had) OnDeselected?.Invoke();
    }

    private void RefreshHighlights()
    {
        if (selectedUnit == null) { Deselect(); return; }

        SelectedCell = selectedUnit.Cell;

        cachedMoveCosts = selectedUnit.CanMove
            ? grid.GetReachableCosts(selectedUnit)
            : new Dictionary<HexCoord, int>();

        var enemies = grid.GetAllEnemiesOf(selectedUnit.Faction);
        if (highlight != null)
            highlight.Show(selectedUnit, grid, new List<HexCoord>(cachedMoveCosts.Keys), enemies);

        if (selectionHighlight != null)
        {
            selectionHighlight.gameObject.SetActive(true);
            Vector3 p = grid.CellToWorld(selectedUnit.Cell);
            selectionHighlight.position = new Vector3(p.x, p.y + highlightYOffset, p.z);
        }
    }
}