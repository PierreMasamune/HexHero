using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

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

    [Header("Preparing UI (your or friend's)")]
    [Tooltip("The panel that shows during preparing. Hidden when the battle starts.")]
    [SerializeField] private GameObject preparingRoot;
    [Tooltip("The button that starts the battle.")]
    [SerializeField] private Button startBattleButton;
    [Tooltip("Optional TMP text on the Start Battle button — updated to 'Place units first' when empty.")]
    [SerializeField] private TMP_Text startBattleLabel;
    [Tooltip("Optional — resets placed units back to inventory.")]
    [SerializeField] private Button resetPlacementButton;

    [Header("Placement Zone")]
    [Tooltip("Cells with q <= this value are valid placement cells for the player.")]
    [SerializeField] private int playerZoneMaxQ = 0;

    [Header("King Auto-Placement")]
    [SerializeField] private bool autoPlaceKing = true;
    [SerializeField] private HexCoord preferredKingCell = new HexCoord(-2, 0);

    [Header("Enemy Spawner (temporary — replaced by FightDefinition later)")]
    [Tooltip("Enemies spawned when Start Battle is clicked.")]
    [SerializeField] private List<SpawnEntry> enemySetup = new();

    // ==================================================================
    //  EVENTS — FightNodeController listens to this
    // ==================================================================

    public event Action<MapNodeResult> OnFightComplete;

    // ==================================================================
    //  STATE
    // ==================================================================

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
    }

    private void Start()
    {
        if (turnManager != null) turnManager.OnStateChanged += HandleTurnStateChanged;

        if (startBattleButton != null) startBattleButton.onClick.AddListener(StartBattleClicked);
        if (resetPlacementButton != null) resetPlacementButton.onClick.AddListener(ResetPlacementClicked);

        // Don't auto-start — FightNodeController calls EnterPreparing() explicitly.
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
    //  PUBLIC API — enter preparing
    // ==================================================================

    /// <summary>Called by FightNodeController when a Fight/Boss node is entered.</summary>
    public void EnterPreparing()
    {
        isPreparing = true;
        battleRunning = false;

        if (preparingRoot != null) preparingRoot.SetActive(true);
        if (startBattleButton != null) startBattleButton.gameObject.SetActive(true);
        if (resetPlacementButton != null) resetPlacementButton.gameObject.SetActive(true);

        if (selector != null) selector.EnableInput(false);

        // Auto-place the King
        if (autoPlaceKing) TryAutoPlaceKing();

        UpdateStartBattleButton();
        Debug.Log("[Preparing] Entered preparing stage.");
    }

    private void TryAutoPlaceKing()
    {
        if (InventoryManager.Instance == null) return;

        var king = InventoryManager.Instance.GetKing();
        if (king == null) return;

        // Candidate cells: preferred first, then all empty player-zone cells
        var candidates = new List<HexCoord> { preferredKingCell };
        candidates.AddRange(grid.GetEmptyPlayerZoneCells(playerZoneMaxQ));

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

        Debug.LogWarning("[Preparing] No empty player-zone cell for auto King placement.");
    }

    // ==================================================================
    //  PUBLIC API — friend's inventory UI calls these
    // ==================================================================

    /// <summary>
    /// Called by the inventory UI when the player clicks a unit card.
    /// Enables placement mode: green highlights on valid cells.
    /// </summary>
    public void SetSelectedInstance(UnitInstance instance)
    {
        if (!isPreparing)
        {
            Debug.LogWarning("[Preparing] SetSelectedInstance called outside preparing stage.");
            return;
        }

        if (instance == null)
        {
            CancelSelection();
            return;
        }

        selectedInstance = instance;

        var cells = grid.GetEmptyPlayerZoneCells(playerZoneMaxQ);
        validPlacementCells = new HashSet<HexCoord>(cells);

        if (validPlacementCells.Count == 0)
        {
            Debug.LogWarning("[Preparing] No valid placement cells available (all occupied).");
            CancelSelection();
            return;
        }

        // Green highlight on valid cells
        if (highlight != null)
            highlight.ShowHealPreview(new List<HexCoord>(validPlacementCells));

        Debug.Log($"[Preparing] Selected '{instance.definition?.unitName}' for placement. " +
                  $"{validPlacementCells.Count} valid cell(s).");
    }

    /// <summary>Called by the inventory UI when the player clicks off a card or clicks the same card again.</summary>
    public void CancelSelection()
    {
        selectedInstance = null;
        validPlacementCells = null;
        if (highlight != null) highlight.ClearHealPreview();
    }

    // ==================================================================
    //  CLICK HANDLERS (during preparing)
    // ==================================================================

    private void TryPlaceSelectedAt(HexCoord cell)
    {
        if (selectedInstance == null) return;

        if (TryPlaceUnit(selectedInstance, cell, silent: false))
        {
            CancelSelection();
            UpdateStartBattleButton();
        }
    }

    private bool TryPlaceUnit(UnitInstance instance, HexCoord cell, bool silent)
    {
        if (instance == null || instance.definition == null) return false;

        if (!grid.Contains(cell)) { if (!silent) Debug.LogWarning($"Cell {cell} out of grid."); return false; }
        if (grid.GetUnit(cell) != null) { if (!silent) Debug.LogWarning($"Cell {cell} occupied."); return false; }
        if (cell.q > playerZoneMaxQ) { if (!silent) Debug.LogWarning($"Cell {cell} outside player zone."); return false; }

        // Unit cap
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

        // Spawn
        var unit = UnitFactory.Spawn(instance, Faction.Player, grid.CellToWorld(cell));
        if (unit == null) { if (!silent) Debug.LogError("UnitFactory.Spawn returned null."); return false; }

        grid.RegisterUnit(unit, cell);
        if (turnManager != null) turnManager.RegisterSpawnedUnit(unit);

        deployed[unit] = instance;

        // ★ Friend's inventory: remove from inventory. If the method is missing, comment this line
        //   and instead call whatever their API exposes (e.g. InventoryManager.Instance.RemoveByReference).
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.RemoveUnit(instance);

        if (!silent)
            Debug.Log($"[Preparing] Placed '{instance.definition.unitName}' at {cell} " +
                      $"({instance.currentHP}/{instance.maxHP} HP).");

        return true;
    }

    // ==================================================================
    //  BUTTONS
    // ==================================================================

    private void StartBattleClicked()
    {
        if (deployed.Count == 0)
        {
            Debug.LogWarning("[Preparing] Place at least one unit before starting.");
            return;
        }

        CancelSelection();
        isPreparing = false;
        battleRunning = true;

        // Hide preparing panel — HUD / battle scene takes over
        if (preparingRoot != null) preparingRoot.SetActive(false);
        if (selector != null) selector.EnableInput(true);

        // Spawn enemies
        SpawnEnemiesFromSetup();

        // Begin battle
        if (turnManager != null) turnManager.BeginNewBattle();
    }

    private void ResetPlacementClicked()
    {
        var toReturn = new List<Unit>();
        foreach (var kv in deployed)
            if (kv.Key != null && !kv.Key.IsKing) toReturn.Add(kv.Key);

        foreach (var u in toReturn)
        {
            var instance = deployed[u];

            // Preserve current HP
            u.WriteBackTo(instance);

            grid.ClearUnit(u.Cell);
            if (InventoryManager.Instance != null)
                InventoryManager.Instance.ReturnUnit(instance);
            deployed.Remove(u);
            Destroy(u.gameObject);
        }

        // Re-place the King
        if (autoPlaceKing) TryAutoPlaceKing();
        UpdateStartBattleButton();
    }

    // ==================================================================
    //  TURN STATE HANDLING — cleanup on battle end
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
        Debug.Log("[Preparing] Victory — returning survivors.");

        // Return living player units to inventory (with their final HP)
        var livingPlayers = grid.GetAllLivingUnits(Faction.Player);
        foreach (var unit in livingPlayers)
        {
            if (unit == null || unit.IsTransient) continue;
            if (!deployed.ContainsKey(unit)) continue;

            var instance = deployed[unit];
            unit.WriteBackTo(instance);

            if (instance.IsAlive && InventoryManager.Instance != null)
                InventoryManager.Instance.ReturnUnit(instance);

            grid.ClearUnit(unit.Cell);
            Destroy(unit.gameObject);
        }

        // Clean up remaining enemies
        var livingEnemies = grid.GetAllLivingUnits(Faction.Enemy);
        foreach (var e in livingEnemies)
        {
            if (e == null) continue;
            grid.ClearUnit(e.Cell);
            Destroy(e.gameObject);
        }

        deployed.Clear();

        // Notify FightNodeController → RunManager → ShowMap
        OnFightComplete?.Invoke(MapNodeResult.Success);
    }

    private void HandleDefeat()
    {
        battleRunning = false;
        Debug.Log("[Preparing] Defeat.");

        // Clean up board
        var living = new List<Unit>();
        living.AddRange(grid.GetAllLivingUnits(Faction.Player));
        living.AddRange(grid.GetAllLivingUnits(Faction.Enemy));

        foreach (var u in living)
        {
            if (u == null) continue;
            grid.ClearUnit(u.Cell);
            Destroy(u.gameObject);
        }
        deployed.Clear();

        OnFightComplete?.Invoke(MapNodeResult.Fail);
    }

    // ==================================================================
    //  ENEMY SPAWNING (temporary)
    // ==================================================================

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
        d.unitName = e.enemyName;
        d.maxHP = e.maxHP;
        d.defense = e.defense;
        d.attack = e.attack;
        d.movementRange = e.movementRange;
        d.attackRange = e.attackRange;
        d.critChance = e.critChance;
        d.dodgeChance = e.dodgeChance;
        d.unitPrefab = e.unitPrefab;
        d.splashMode = e.splashMode;
        d.splashRadius = e.splashRadius;
        d.splashValue = e.splashValue;
        d.ability = e.ability;
        d.abilityValue = e.abilityValue;
        d.summonAtBattleStart = e.summonAtBattleStart;
        d.summonEveryNTurns = e.summonEveryNTurns;
        d.killReward = e.killValue;

        enemyDefCache[e] = d;
        return d;
    }

    // ==================================================================
    //  HELPERS
    // ==================================================================

    private void UpdateStartBattleButton()
    {
        if (startBattleButton == null) return;
        bool canStart = deployed.Count > 0;
        startBattleButton.interactable = canStart;
        if (startBattleLabel != null)
            startBattleLabel.text = canStart ? "Start Battle" : "Place units first";
    }

    private bool TryGetCellUnderMouse(out HexCoord cell)
    {
        cell = default;
        if (cam == null || grid == null) return false;

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane plane = new Plane(Vector3.up,
            new Vector3(0f, grid.SurfaceY + grid.transform.position.y, 0f));

        if (!plane.Raycast(ray, out float enter)) return false;

        Vector3 world = ray.GetPoint(enter);
        cell = grid.WorldToCell(world);
        return grid.Contains(cell);
    }
}