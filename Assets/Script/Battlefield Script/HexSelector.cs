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
    public static HexSelector Instance { get; private set; }
    private ItemDefinition targetingItem;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }
    public bool HasHover { get; private set; }
    public bool HasSelection { get; private set; }

    public event System.Action<HexCoord> OnCellClicked;
    public event System.Action<Unit> OnUnitSelected;
    public event System.Action<Unit, HexCoord> OnUnitMoved;
    public event System.Action<Unit, Unit> OnUnitAttacked;
    public event System.Action<Unit, Unit> OnUnitHealed;
    public event System.Action OnDeselected;

    private Unit selectedUnit;
    private Dictionary<HexCoord, int> cachedMoveCosts;
    private bool inputEnabled = true;

    private HexCoord lastPreviewedCell = new HexCoord(int.MinValue, int.MinValue);

    // ==================================================================
    //  LIFECYCLE
    // ==================================================================

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

        if (grid != null) grid.OnUnitDied += HandleUnitDied;

        if (hoverHighlight != null) hoverHighlight.gameObject.SetActive(false);
        if (selectionHighlight != null) selectionHighlight.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (grid != null) grid.OnUnitDied -= HandleUnitDied;
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

        if (targetingItem != null)
        {
            UpdateHover(); 

            if (Mouse.current.leftButton.wasPressedThisFrame && HasHover)
            {
                // Left click: Use the item!
                ItemManager.Instance.UseItem(targetingItem, targetCell: HoveredCell);
                CancelItemTargeting(); // <--- THIS clears the targeting mode!
            }
            else if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                // Right click: Cancel targeting
                CancelItemTargeting(); // <--- THIS clears the targeting mode!
            }

            // Return here so the normal unit-selection code below doesn't run
            return;
        }

        // 2. NORMAL SELECTION MODE
        UpdateHover();

        if (Mouse.current.leftButton.wasPressedThisFrame && HasHover)
            HandleClick(HoveredCell);
    }
    public void StartTargetingItem(ItemDefinition item)
    {
        Deselect(); // Clear any selected unit
        targetingItem = item;
        Debug.Log($"[Selector] Targeting mode ON for {item.itemName}. Left click to use, right click to cancel.");
    }

    private void CancelItemTargeting()
    {
        targetingItem = null;
        Debug.Log("[Selector] Targeting mode OFF.");
    }

    // ==================================================================
    //  HOVER
    // ==================================================================

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

                UpdatePreviews(cell);
                return;
            }
        }

        HasHover = false;
        if (hoverHighlight != null) hoverHighlight.gameObject.SetActive(false);
        if (highlight != null) { highlight.ClearAoePreview(); highlight.ClearHealPreview(); }
        lastPreviewedCell = new HexCoord(int.MinValue, int.MinValue);
    }

    private void UpdatePreviews(HexCoord hoveredCell)
    {
        if (highlight == null) return;
        if (hoveredCell.Equals(lastPreviewedCell)) return;
        lastPreviewedCell = hoveredCell;

        highlight.ClearAoePreview();
        highlight.ClearHealPreview();

        if (selectedUnit == null || selectedUnit.HasAttacked) return;
        if (selectedUnit.Definition == null) return;

        var target = grid.GetUnit(hoveredCell);
        if (target == null || !target.IsAlive) return;
        if (HexCoord.Distance(selectedUnit.Cell, hoveredCell) > selectedUnit.AttackRange) return;

        // Yellow AOE preview for splash attacks
        if (target.Faction != selectedUnit.Faction
            && selectedUnit.Definition.splashMode != SplashMode.None
            && selectedUnit.Definition.splashRadius > 0)
        {
            highlight.ShowAoePreview(ComputeAreaCells(hoveredCell, selectedUnit.Definition.splashRadius, false));
        }

        // Green heal preview for healers
        if (target.Faction == selectedUnit.Faction
            && target.CurrentHP < target.MaxHP
            && (selectedUnit.Definition.ability == SpecialAbility.HealAlly
                || selectedUnit.Definition.ability == SpecialAbility.AoeHeal))
        {
            var cells = new List<HexCoord> { hoveredCell };
            if (selectedUnit.Definition.ability == SpecialAbility.AoeHeal)
            {
                int radius = selectedUnit.Definition.splashRadius;
                if (radius <= 0) radius = 1;
                cells.AddRange(ComputeAreaCells(hoveredCell, radius, false));
            }
            highlight.ShowHealPreview(cells);
        }
    }

    private List<HexCoord> ComputeAreaCells(HexCoord center, int radius, bool includeCenter)
    {
        var cells = new List<HexCoord>();
        for (int dq = -radius; dq <= radius; dq++)
        {
            int rMin = Mathf.Max(-radius, -dq - radius);
            int rMax = Mathf.Min(radius, -dq + radius);
            for (int dr = rMin; dr <= rMax; dr++)
            {
                if (dq == 0 && dr == 0 && !includeCenter) continue;
                var cell = new HexCoord(center.q + dq, center.r + dr);
                if (!grid.Contains(cell)) continue;
                cells.Add(cell);
            }
        }
        return cells;
    }

    // ==================================================================
    //  CLICK
    // ==================================================================

    private void HandleClick(HexCoord cell)
    {
        OnCellClicked?.Invoke(cell);

        Unit clicked = grid.GetUnit(cell);

        // No selection → try to select a player unit
        if (selectedUnit == null)
        {
            if (clicked != null && clicked.Faction == Faction.Player && clicked.CanStillAct)
                Select(clicked);
            else
                SelectedCell = cell;
            return;
        }

        // Clicking the same unit → toggle off
        if (clicked == selectedUnit) { Deselect(); return; }

        // Healer clicked a wounded ally in range → heal
        if (IsHealer(selectedUnit)
            && clicked != null
            && clicked.Faction == Faction.Player
            && clicked.IsAlive
            && clicked.CurrentHP < clicked.MaxHP
            && HexCoord.Distance(selectedUnit.Cell, clicked.Cell) <= selectedUnit.AttackRange)
        {
            StatsManager.Instance.ResolveHeal(selectedUnit, clicked);
            selectedUnit.MarkAttacked();
            OnUnitHealed?.Invoke(selectedUnit, clicked);
            Deselect();
            return;
        }

        // Clicking another friendly player unit → switch selection
        if (clicked != null && clicked.Faction == Faction.Player)
        {
            if (clicked.CanStillAct) Select(clicked);
            else Deselect();
            return;
        }

        // Clicking an enemy → attack if in range
        if (clicked != null && clicked.Faction == Faction.Enemy)
        {
            bool inRange = !selectedUnit.HasAttacked
                        && HexCoord.Distance(selectedUnit.Cell, clicked.Cell) <= selectedUnit.AttackRange;

            if (inRange)
            {
                StatsManager.Instance.ResolveAttack(selectedUnit, clicked);
                selectedUnit.MarkAttacked();
                OnUnitAttacked?.Invoke(selectedUnit, clicked);

                // Cataphract keeps selection if they still have movement (Double Move)
                if (selectedUnit.CanMove)
                {
                    RefreshHighlights();
                    return;
                }
            }
            Deselect();
            return;
        }

        // Clicking an empty cell → move if reachable
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
            selectedUnit.SpendMovement(moveCost);

            // Terrain entry damage (vine)
            ApplyEntryDamage(selectedUnit, cell);

            OnUnitMoved?.Invoke(selectedUnit, from);

            if (!selectedUnit.IsAlive) { Deselect(); return; }
            if (selectedUnit.CanMove) RefreshHighlights();
            else Deselect();
            return;
        }

        Deselect();
    }

    private void ApplyEntryDamage(Unit unit, HexCoord cell)
    {
        if (unit == null || grid == null || grid.TerrainMap == null) return;

        int dmg = grid.TerrainMap.GetEntryDamage(cell);
        if (dmg <= 0) return;

        var terrain = grid.TerrainMap.GetTerrain(cell);
        string name = terrain != null ? terrain.terrainName : "terrain";

        unit.TakeDamage(dmg);
        Debug.Log($"[Terrain] {unit.UnitName} took {dmg} dmg entering {name}. " +
                  $"({unit.CurrentHP}/{unit.MaxHP} HP)");
    }

    private bool IsHealer(Unit u)
    {
        if (u == null || u.Definition == null || u.HasAttacked) return false;
        var a = u.Definition.ability;
        return a == SpecialAbility.HealAlly || a == SpecialAbility.AoeHeal;
    }

    // ==================================================================
    //  SELECTION STATE
    // ==================================================================

    private void Select(Unit unit)
    {
        selectedUnit = unit;
        SelectedCell = unit.Cell;
        HasSelection = true;
        ResetPreviewTracking();
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
        ResetPreviewTracking();
        if (had) OnDeselected?.Invoke();
    }

    private void ResetPreviewTracking()
    {
        lastPreviewedCell = new HexCoord(int.MinValue, int.MinValue);
    }

    private void RefreshHighlights()
    {
        if (selectedUnit == null) { Deselect(); return; }

        SelectedCell = selectedUnit.Cell;

        cachedMoveCosts = selectedUnit.CanMove
            ? grid.GetReachableCosts(selectedUnit)
            : new Dictionary<HexCoord, int>();

        // Hide enemy fills if the unit has already attacked (can't attack again)
        var enemies = selectedUnit.HasAttacked
            ? new List<Unit>()
            : grid.GetAllEnemiesOf(selectedUnit.Faction);

        if (highlight != null)
            highlight.Show(selectedUnit, grid, new List<HexCoord>(cachedMoveCosts.Keys), enemies);

        if (highlight != null)
        {
            if (!selectedUnit.HasAttacked && selectedUnit.AttackRange > 0)
                highlight.ShowAttackOutline(GetCellsInAttackRange(selectedUnit), selectedUnit.Cell);
            else
                highlight.ClearAttackOutline();
        }

        if (selectionHighlight != null)
        {
            selectionHighlight.gameObject.SetActive(true);
            Vector3 p = grid.CellToWorld(selectedUnit.Cell);
            selectionHighlight.position = new Vector3(p.x, p.y + highlightYOffset, p.z);
        }
    }

    private List<HexCoord> GetCellsInAttackRange(Unit unit)
    {
        if (unit == null || unit.AttackRange <= 0) return new List<HexCoord>();
        return ComputeAreaCells(unit.Cell, unit.AttackRange, includeCenter: false);
    }

    private void HandleUnitDied(Unit unit)
    {
        if (unit == selectedUnit) Deselect();
    }
}