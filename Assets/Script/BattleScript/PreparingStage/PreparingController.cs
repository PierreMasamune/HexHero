using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PreparingController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HexGridManager grid;
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private HexSelector selector;
    [SerializeField] private HexHighlightManager highlight;
    [SerializeField] private Camera cam;

    [Header("Preparing UI")]
    [Tooltip("The whole preparing canvas. Shown/hidden when entering/leaving preparing.")]
    [SerializeField] private GameObject preparingRoot;

    [Header("Placement Zone (Radius)")]
    [Tooltip("The center hex of the placement circle. (0,0) is the middle of the board.")]
    [SerializeField] private HexCoord placementZoneCenter = new HexCoord(-2, 0);
    [Tooltip("How many hexes out from the center can the player place units?")]
    [SerializeField] private int placementZoneRadius = 2;

    [Header("King Auto-Placement")]
    [SerializeField] private bool autoPlaceKing = true;
    [SerializeField] private HexCoord preferredKingCell = new HexCoord(-2, 0);

    // ★ NEW: The Inspector list you asked for!
    [Header("Manual Enemy Setup (For Testing)")]
    [Tooltip("If you put enemies here, they will override the FightDefinition and spawn exactly where you say. " +
             "Leave this empty to use the handcrafted enemies from the map node's FightDefinition.")]
    [SerializeField] private List<SpawnEntry> manualEnemySetup = new();

    [Header("Debug Options")]
    [SerializeField] private bool showPlacementOutlines = true;
    [SerializeField] private bool preSpawnEnemiesDuringPreparation = true;

    public event Action<MapNodeResult> OnFightComplete;
    public event Action<bool> OnCanStartBattleChanged;
    public bool CanStartBattle => deployed.Count > 0;

    private readonly Dictionary<Unit, UnitInstance> deployed = new();
    private readonly Dictionary<EnemyDefinition, UnitDefinition> enemyDefCache = new();
    private UnitInstance selectedInstance;
    private HashSet<HexCoord> validPlacementCells;
    private bool isPreparing;
    private bool battleRunning;

    private FightDefinition activeBattle;

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
        if (!isPreparing || selectedInstance == null || Mouse.current == null) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        if (!TryGetCellUnderMouse(out HexCoord cell)) return;

        if (validPlacementCells != null && validPlacementCells.Contains(cell))
            TryPlaceSelectedAt(cell);
        else
            CancelSelection();
    }

    // ==================================================================
    //  BATTLE LOADING
    // ==================================================================

    public void LoadBattle(FightDefinition battle)
    {
        activeBattle = battle;
        EnterPreparing();
    }

    public void EnterPreparing()
    {
        if (isPreparing) return;

        isPreparing = true;
        battleRunning = false;

        if (preparingRoot != null) preparingRoot.SetActive(true);
        if (selector != null) selector.EnableInput(false);

        if (autoPlaceKing) TryAutoPlaceKing();

        if (preSpawnEnemiesDuringPreparation)
        {
            // ★ CHECK: Use manual list if it has items, otherwise use the map's FightDefinition
            if (manualEnemySetup != null && manualEnemySetup.Count > 0)
            {
                Debug.Log("[Preparing] Using MANUAL enemy setup from the Inspector.");
                SpawnEnemiesFromSetup(manualEnemySetup);
            }
            else if (activeBattle != null && activeBattle.enemies != null)
            {
                Debug.Log("[Preparing] Using handcrafted enemies from FightDefinition.");
                SpawnEnemiesFromSetup(activeBattle.enemies);
            }
            else
            {
                Debug.LogWarning("[Preparing] No enemies found in manual list OR FightDefinition.");
            }
        }

        if (showPlacementOutlines && highlight != null)
            highlight.ShowPlacementOutline(grid.GetEmptyZoneCells(placementZoneCenter, placementZoneRadius));

        FireCanStartChanged();
    }

    private void TryAutoPlaceKing()
    {
        if (InventoryManager.Instance == null) return;
        var king = InventoryManager.Instance.GetKing();
        if (king == null) return;

        var candidates = new List<HexCoord> { preferredKingCell };
        candidates.AddRange(grid.GetEmptyZoneCells(placementZoneCenter, placementZoneRadius));

        foreach (var cell in candidates)
        {
            if (!grid.Contains(cell) || grid.GetUnit(cell) != null) continue;
            if (TryPlaceUnit(king, cell, silent: true))
            {
                Debug.Log($"[Preparing] Auto-placed King at {cell}.");
                return;
            }
        }
    }

    // ==================================================================
    //  SELECTION & BUTTONS
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
        {
            if (manualEnemySetup != null && manualEnemySetup.Count > 0)
                SpawnEnemiesFromSetup(manualEnemySetup);
            else if (activeBattle != null)
                SpawnEnemiesFromSetup(activeBattle.enemies);
        }

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
        if (!grid.Contains(cell) || grid.GetUnit(cell) != null) return false;

        if (HexCoord.Distance(placementZoneCenter, cell) > placementZoneRadius)
        {
            if (!silent) Debug.LogWarning($"Cell {cell} is outside the placement radius.");
            return false;
        }

        int deployedCounting = 0;
        foreach (var kv in deployed)
            if (kv.Key != null && kv.Key.IsAlive && kv.Key.Definition.countsTowardUnitCap) deployedCounting++;

        int cap = InventoryManager.Instance != null ? InventoryManager.Instance.UnitCap : 6;
        if (instance.definition.countsTowardUnitCap && deployedCounting >= cap)
        {
            if (!silent) Debug.LogWarning($"[Preparing] Unit cap reached ({cap}).");
            return false;
        }

        var unit = UnitFactory.Spawn(instance, Faction.Player, grid.CellToWorld(cell));
        if (unit == null) return false;

        grid.RegisterUnit(unit, cell);
        if (turnManager != null) turnManager.RegisterSpawnedUnit(unit);
        deployed[unit] = instance;

        if (InventoryManager.Instance != null) InventoryManager.Instance.RemoveUnit(instance);

        if (showPlacementOutlines && highlight != null && isPreparing)
            highlight.ShowPlacementOutline(grid.GetEmptyZoneCells(placementZoneCenter, placementZoneRadius));

        return true;
    }

    // ==================================================================
    //  ENEMY SPAWNING
    // ==================================================================

    private void SpawnEnemiesFromSetup(List<SpawnEntry> enemies)
    {
        if (enemies == null || enemies.Count == 0) return;

        int spawnedCount = 0;

        foreach (var entry in enemies)
        {
            if (entry == null || entry.enemy == null) continue;

            if (!grid.Contains(entry.cell))
            {
                Debug.LogWarning($"[Preparing] Enemy '{entry.enemy.enemyName}' wanted to spawn at {entry.cell}, but that is outside the grid!");
                continue;
            }

            if (grid.GetUnit(entry.cell) != null)
            {
                Debug.LogWarning($"[Preparing] Enemy '{entry.enemy.enemyName}' wanted to spawn at {entry.cell}, but it is already occupied!");
                continue;
            }

            var def = EnemyToUnitDef(entry.enemy);
            var unit = UnitFactory.Spawn(def, Faction.Enemy, grid.CellToWorld(entry.cell));

            if (unit == null) continue;

            grid.RegisterUnit(unit, entry.cell);
            if (turnManager != null) turnManager.RegisterSpawnedUnit(unit);
            spawnedCount++;
        }

        Debug.Log($"[Preparing] Successfully spawned {spawnedCount} / {enemies.Count} enemies.");
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