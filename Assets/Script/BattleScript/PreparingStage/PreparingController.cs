using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PreparingController : MonoBehaviour
{
    // ==================================================================
    //  INSPECTOR
    // ==================================================================

    [Header("References")]
    [SerializeField] private HexGridManager grid;
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private HexSelector selector;
    [SerializeField] private HexHighlightManager highlight;
    [SerializeField] private Camera cam;

    [Header("Preparing UI")]
    [SerializeField] private GameObject preparingRoot;

    // ★ NEW: Radius-based Placement Zone
    [Header("Placement Zone (Radius)")]
    [Tooltip("The center hex of the placement circle. (0,0) is the middle of the board.")]
    [SerializeField] private HexCoord placementZoneCenter = new HexCoord(-2, 0);
    [Tooltip("How many hexes out from the center can the player place units?")]
    [SerializeField] private int placementZoneRadius = 2;

    [Header("King Auto-Placement")]
    [SerializeField] private bool autoPlaceKing = true;
    [SerializeField] private HexCoord preferredKingCell = new HexCoord(-2, 0);

    [Header("Enemy Spawner")]
    [SerializeField] private List<SpawnEntry> enemySetup = new();

    [Header("Debug Options")]
    [SerializeField] private bool showPlacementOutlines = true;
    [SerializeField] private bool preSpawnEnemiesDuringPreparation = true;

    // ==================================================================
    //  EVENTS & STATE
    // ==================================================================

    public event Action<MapNodeResult> OnFightComplete;
    public event Action<bool> OnCanStartBattleChanged;
    public bool CanStartBattle => deployed.Count > 0;

    private readonly Dictionary<Unit, UnitInstance> deployed = new();
    private readonly Dictionary<EnemyDefinition, UnitDefinition> enemyDefCache = new();
    private UnitInstance selectedInstance;
    private HashSet<HexCoord> validPlacementCells;
    private bool isPreparing;
    private bool battleRunning;

    // ==================================================================
    //  LIFECYCLE
    // ==================================================================

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (turnManager == null) turnManager = FindAnyObjectByType<TurnManager>();
        if (grid == null) grid = FindAnyObjectByType<HexGridManager>();
        if (selector == null) selector = FindAnyObjectByType<HexSelector>();
        if (highlight == null) highlight = FindAnyObjectByType<HexHighlightManager>();

        if (turnManager != null) turnManager.OnStateChanged += HandleTurnStateChanged;
        if (preparingRoot != null) preparingRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (turnManager != null) turnManager.OnStateChanged -= HandleTurnStateChanged;
    }

    private void Update()
    {
        if (!isPreparing) return;
        if (selectedInstance == null) return;
        if (Mouse.current == null) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        if (!TryGetCellUnderMouse(out HexCoord cell)) return;

        if (validPlacementCells != null && validPlacementCells.Contains(cell))
            TryPlaceSelectedAt(cell);
        else
            CancelSelection();
    }

    // ==================================================================
    //  PREPARING STATE
    // ==================================================================

    public void EnterPreparing()
    {
        if (isPreparing) return;

        isPreparing = true;
        battleRunning = false;

        if (preparingRoot != null) preparingRoot.SetActive(true);
        if (selector != null) selector.EnableInput(false);

        if (autoPlaceKing) TryAutoPlaceKing();

        if (preSpawnEnemiesDuringPreparation)
            SpawnEnemiesFromSetup();

        if (showPlacementOutlines && highlight != null)
            highlight.ShowPlacementOutline(grid.GetEmptyZoneCells(placementZoneCenter, placementZoneRadius));

        FireCanStartChanged();
        Debug.Log("[Preparing] Entered preparing stage.");
    }

    private void TryAutoPlaceKing()
    {
        if (InventoryManager.Instance == null) return;
        var king = InventoryManager.Instance.GetKing();
        if (king == null) return;

        // Try the preferred cell first, then any valid cell in the radius
        var candidates = new List<HexCoord> { preferredKingCell };
        candidates.AddRange(grid.GetEmptyZoneCells(placementZoneCenter, placementZoneRadius));

        foreach (var cell in candidates)
        {
            if (!grid.Contains(cell)) continue;
            if (grid.GetUnit(cell) != null) continue;
            if (TryPlaceUnit(king, cell, silent: true))
            {
                Debug.Log($"[Preparing] Auto-placed King at {cell}.");
                return;
            }
        }
    }

    // ==================================================================
    //  SELECTION
    // ==================================================================

    public void SetSelectedInstance(UnitInstance instance)
    {
        if (!isPreparing || instance == null) { CancelSelection(); return; }

        selectedInstance = instance;
        var cells = grid.GetEmptyZoneCells(placementZoneCenter, placementZoneRadius);
        validPlacementCells = new HashSet<HexCoord>(cells);

        if (validPlacementCells.Count == 0) { CancelSelection(); return; }

        if (highlight != null)
            highlight.ShowHealPreview(new List<HexCoord>(validPlacementCells));
    }

    public void CancelSelection()
    {
        selectedInstance = null;
        validPlacementCells = null;
        if (highlight != null) highlight.ClearHealPreview();
    }

    // ==================================================================
    //  BUTTONS
    // ==================================================================

    public void StartBattleClicked()
    {
        if (deployed.Count == 0)
        {
            Debug.LogWarning("[Preparing] Place at least one unit before starting.");
            return;
        }

        CancelSelection();
        isPreparing = false;
        battleRunning = true;

        if (preparingRoot != null) preparingRoot.SetActive(false);
        if (selector != null) selector.EnableInput(true);

        if (showPlacementOutlines && highlight != null)
        {
            highlight.ClearPlacementOutline();
            highlight.ClearHealPreview();
        }

        if (!preSpawnEnemiesDuringPreparation)
            SpawnEnemiesFromSetup();

        if (turnManager != null) turnManager.BeginNewBattle();
    }

    public void ResetPlacementClicked()
    {
        var toReturn = new List<Unit>();
        foreach (var kv in deployed)
            if (kv.Key != null && !kv.Key.IsKing) toReturn.Add(kv.Key);

        foreach (var u in toReturn)
        {
            var instance = deployed[u];
            u.WriteBackTo(instance);
            grid.ClearUnit(u.Cell);
            if (InventoryManager.Instance != null) InventoryManager.Instance.ReturnUnit(instance);
            deployed.Remove(u);
            Destroy(u.gameObject);
        }

        if (autoPlaceKing) TryAutoPlaceKing();

        if (showPlacementOutlines && highlight != null)
            highlight.ShowPlacementOutline(grid.GetEmptyZoneCells(placementZoneCenter, placementZoneRadius));

        FireCanStartChanged();
    }

    // ==================================================================
    //  PLACEMENT VALIDATION
    // ==================================================================

    private void TryPlaceSelectedAt(HexCoord cell)
    {
        if (selectedInstance == null) return;
        if (TryPlaceUnit(selectedInstance, cell, silent: false))
        {
            CancelSelection();
            FireCanStartChanged();
        }
    }

    private bool TryPlaceUnit(UnitInstance instance, HexCoord cell, bool silent)
    {
        if (instance == null || instance.definition == null) return false;
        if (!grid.Contains(cell)) { if (!silent) Debug.LogWarning($"Cell {cell} out of grid."); return false; }
        if (grid.GetUnit(cell) != null) { if (!silent) Debug.LogWarning($"Cell {cell} occupied."); return false; }

        // ★ NEW: Check if the cell is inside the circular placement zone
        if (HexCoord.Distance(placementZoneCenter, cell) > placementZoneRadius)
        {
            if (!silent) Debug.LogWarning($"Cell {cell} is outside the placement radius.");
            return false;
        }

        int deployedCounting = 0;
        foreach (var kv in deployed)
        {
            var u = kv.Key;
            if (u == null || !u.IsAlive) continue;
            if (u.Definition == null || !u.Definition.countsTowardUnitCap) continue;
            deployedCounting++;
        }
        int cap = InventoryManager.Instance != null ? InventoryManager.Instance.UnitCap : 6;
        if (instance.definition.countsTowardUnitCap && deployedCounting >= cap)
        {
            if (!silent) Debug.LogWarning($"[Preparing] Unit cap reached ({cap}).");
            return false;
        }

        var unit = UnitFactory.Spawn(instance, Faction.Player, grid.CellToWorld(cell));
        if (unit == null) { if (!silent) Debug.LogError("UnitFactory.Spawn returned null."); return false; }

        grid.RegisterUnit(unit, cell);
        if (turnManager != null) turnManager.RegisterSpawnedUnit(unit);
        deployed[unit] = instance;

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.RemoveUnit(instance);

        if (showPlacementOutlines && highlight != null && isPreparing)
            highlight.ShowPlacementOutline(grid.GetEmptyZoneCells(placementZoneCenter, placementZoneRadius));

        if (!silent)
            Debug.Log($"[Preparing] Placed '{instance.definition.unitName}' at {cell} " +
                      $"({instance.currentHP}/{instance.maxHP} HP).");
        return true;
    }

    // ==================================================================
    //  BATTLE END & HELPERS
    // ==================================================================

    private void HandleTurnStateChanged(TurnState newState)
    {
        if (!battleRunning) return;
        if (newState == TurnState.Victory) HandleVictory();
        else if (newState == TurnState.Defeat) HandleDefeat();
    }

    private void HandleVictory()
    {
        battleRunning = false;
        foreach (var unit in grid.GetAllLivingUnits(Faction.Player))
        {
            if (unit == null || unit.IsTransient || !deployed.ContainsKey(unit)) continue;
            var instance = deployed[unit];
            unit.WriteBackTo(instance);
            if (instance.IsAlive && InventoryManager.Instance != null) InventoryManager.Instance.ReturnUnit(instance);
            grid.ClearUnit(unit.Cell);
            Destroy(unit.gameObject);
        }

        foreach (var e in grid.GetAllLivingUnits(Faction.Enemy))
        {
            if (e == null) continue;
            grid.ClearUnit(e.Cell);
            Destroy(e.gameObject);
        }

        deployed.Clear();
        OnFightComplete?.Invoke(MapNodeResult.Success);
    }

    private void HandleDefeat()
    {
        battleRunning = false;
        var all = new List<Unit>();
        all.AddRange(grid.GetAllLivingUnits(Faction.Player));
        all.AddRange(grid.GetAllLivingUnits(Faction.Enemy));
        foreach (var u in all)
        {
            if (u == null) continue;
            grid.ClearUnit(u.Cell);
            Destroy(u.gameObject);
        }
        deployed.Clear();
        OnFightComplete?.Invoke(MapNodeResult.Fail);
    }

    private void SpawnEnemiesFromSetup()
    {
        if (enemySetup == null) return;
        foreach (var entry in enemySetup)
        {
            if (entry == null || entry.enemy == null) continue;
            if (!grid.Contains(entry.cell)) continue;
            if (grid.GetUnit(entry.cell) != null) continue;

            var def = EnemyToUnitDef(entry.enemy);
            var unit = UnitFactory.Spawn(def, Faction.Enemy, grid.CellToWorld(entry.cell));
            if (unit == null) continue;
            grid.RegisterUnit(unit, entry.cell);
            if (turnManager != null) turnManager.RegisterSpawnedUnit(unit);
        }
    }

    private UnitDefinition EnemyToUnitDef(EnemyDefinition e)
    {
        if (enemyDefCache.TryGetValue(e, out var c)) return c;
        var d = ScriptableObject.CreateInstance<UnitDefinition>();
        d.unitName = e.enemyName; d.maxHP = e.maxHP; d.defense = e.defense; d.attack = e.attack;
        d.movementRange = e.movementRange; d.attackRange = e.attackRange;
        d.critChance = e.critChance; d.dodgeChance = e.dodgeChance;
        d.unitPrefab = e.unitPrefab; d.splashMode = e.splashMode; d.splashRadius = e.splashRadius;
        d.splashValue = e.splashValue; d.ability = e.ability; d.abilityValue = e.abilityValue;
        d.summonAtBattleStart = e.summonAtBattleStart; d.summonEveryNTurns = e.summonEveryNTurns;
        d.killReward = e.killValue; enemyDefCache[e] = d;
        return d;
    }

    private void FireCanStartChanged() => OnCanStartBattleChanged?.Invoke(CanStartBattle);

    private bool TryGetCellUnderMouse(out HexCoord cell)
    {
        cell = default;
        if (cam == null || grid == null) return false;
        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane plane = new Plane(Vector3.up, new Vector3(0f, grid.SurfaceY + grid.transform.position.y, 0f));
        if (!plane.Raycast(ray, out float enter)) return false;
        cell = grid.WorldToCell(ray.GetPoint(enter));
        return grid.Contains(cell);
    }
}