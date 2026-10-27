using System.Collections.Generic;
using UnityEngine;

// ======================================================================
//  One row per unit you want to spawn.
//  Positions are in HEX coordinates (q, r) — the same space the grid
//  uses — NOT world coordinates.
// ======================================================================
[System.Serializable]
public class SpawnEntry
{
    [Tooltip("Optional label for the Console log. Blank = uses the definition's name.")]
    public string label;

    [Tooltip("Player or Enemy. Determines which definition field below is used.")]
    public Faction faction = Faction.Player;

    [Tooltip("Filled when Faction = Player.")]
    public UnitDefinition troop;

    [Tooltip("Filled when Faction = Enemy.")]
    public EnemyDefinition enemy;

    [Tooltip("Hex coordinate (q, r). Use the same (q, r) you painted the grid with. " +
             "Example: (0, 0) is the grid center.")]
    public HexCoord cell = HexCoord.Zero;

    [Tooltip("Also add to TurnManager's per-faction list so it counts in the turn loop.")]
    public bool registerWithTurnManager = true;
}

// ======================================================================
//  MonoBehaviour — put this on your GameManager (or any GameObject)
// ======================================================================
public class SpawnTest : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HexGridManager grid;

    [Header("Spawn List (hex coords)")]
    [Tooltip("Add one entry per unit. Position is (q, r) — the hex coordinate space.")]
    [SerializeField] private List<SpawnEntry> spawnList = new();

    [Header("Options")]
    [Tooltip("Run automatically on Start(). Turn off to spawn manually via the context menu.")]
    [SerializeField] private bool spawnOnStart = true;

    [Tooltip("Log every spawned unit's stats to the Console.")]
    [SerializeField] private bool verboseLogging = true;

    [Tooltip("Warn and skip (rather than overwrite) if the target cell already has a unit.")]
    [SerializeField] private bool skipIfCellOccupied = true;

    // Cache enemy → UnitDefinition conversions so we only create one temp per session.
    private readonly Dictionary<EnemyDefinition, UnitDefinition> enemyDefCache = new();

    // ==================================================================
    //  LIFECYCLE
    // ==================================================================

    private void Start()
    {
        if (!spawnOnStart) return;
        if (!ValidateGrid()) return;
        SpawnAll();
    }

    // ==================================================================
    //  PUBLIC / CONTEXT MENU
    // ==================================================================

    [ContextMenu("Spawn All")]
    public void SpawnAll()
    {
        if (!ValidateGrid()) return;

        if (spawnList == null || spawnList.Count == 0)
        {
            Debug.LogWarning("[SpawnTest] Spawn list is empty.");
            return;
        }

        int success = 0, fail = 0;
        foreach (var entry in spawnList)
        {
            if (SpawnOne(entry)) success++;
            else fail++;
        }

        Debug.Log($"[SpawnTest] Done. Spawned {success} unit(s); skipped {fail}.");
    }

    [ContextMenu("Clear Spawn List")]
    public void ClearList()
    {
        spawnList.Clear();
        Debug.Log("[SpawnTest] Spawn list cleared.");
    }

    [ContextMenu("Log Current List")]
    public void LogList()
    {
        if (spawnList == null || spawnList.Count == 0)
        {
            Debug.Log("[SpawnTest] Spawn list is empty.");
            return;
        }

        Debug.Log($"[SpawnTest] Spawn list has {spawnList.Count} entries:");
        for (int i = 0; i < spawnList.Count; i++)
        {
            var e = spawnList[i];
            string def = e.faction == Faction.Player
                ? (e.troop != null ? e.troop.unitName : "<no troop>")
                : (e.enemy != null ? e.enemy.enemyName : "<no enemy>");
            Debug.Log($"  [{i}] '{e.label}' — {e.faction} — {def} @ cell ({e.cell.q}, {e.cell.r})");
        }
    }

    // ==================================================================
    //  CORE
    // ==================================================================

    private bool SpawnOne(SpawnEntry entry)
    {
        if (entry == null) return false;

        // ---- 1. Resolve definition ----
        UnitDefinition def;
        string displayName = string.IsNullOrEmpty(entry.label) ? "?" : entry.label;

        if (entry.faction == Faction.Player)
        {
            if (entry.troop == null)
            {
                Debug.LogWarning($"[SpawnTest] '{displayName}' — Faction=Player but no 'troop' assigned.");
                return false;
            }
            def = entry.troop;
            displayName = string.IsNullOrEmpty(entry.label) ? def.unitName : entry.label;
        }
        else
        {
            if (entry.enemy == null)
            {
                Debug.LogWarning($"[SpawnTest] '{displayName}' — Faction=Enemy but no 'enemy' assigned.");
                return false;
            }
            def = EnemyToUnitDef(entry.enemy);
            displayName = string.IsNullOrEmpty(entry.label) ? entry.enemy.enemyName : entry.label;
        }

        // ---- 2. Validate the target hex ----
        HexCoord cell = entry.cell;

        if (!grid.Contains(cell))
        {
            Debug.LogWarning($"[SpawnTest] '{displayName}' — cell ({cell.q}, {cell.r}) is outside the grid radius. Skipping.");
            return false;
        }

        var existing = grid.GetUnit(cell);
        if (existing != null)
        {
            if (skipIfCellOccupied)
            {
                Debug.LogWarning($"[SpawnTest] '{displayName}' — cell ({cell.q}, {cell.r}) already occupied by " +
                                 $"'{existing.UnitName}'. Skipping.");
                return false;
            }
            // Fall through — RegisterUnit will replace it (grid fix handles this safely)
        }

        // ---- 3. Spawn — convert hex to world via the grid ----
        Vector3 worldPos = grid.CellToWorld(cell);
        var unit = UnitFactory.Spawn(def, entry.faction, worldPos);
        if (unit == null)
        {
            Debug.LogError($"[SpawnTest] '{displayName}' — UnitFactory.Spawn returned null.");
            return false;
        }

        // ---- 4. Register at the exact hex cell ----
        grid.RegisterUnit(unit, cell);

        // ---- 5. Register with TurnManager ----
        if (entry.registerWithTurnManager && TurnManager.Instance != null)
            TurnManager.Instance.RegisterSpawnedUnit(unit);

        // ---- 6. Log ----
        if (verboseLogging)
        {
            Debug.Log($"[SpawnTest] {displayName} spawned at cell ({cell.q}, {cell.r}) — " +
                      $"HP {unit.CurrentHP}/{unit.MaxHP}, ATK {unit.Attack}, ARM {unit.Armor}, " +
                      $"MOVE {unit.MovementRange}, RANGE {unit.AttackRange}, " +
                      $"Crit {unit.CritChance:P0}, Dodge {unit.DodgeChance:P0}, " +
                      $"IsKing={unit.IsKing}, Faction={unit.Faction}");
        }

        return true;
    }

    // ==================================================================
    //  HELPERS
    // ==================================================================

    private bool ValidateGrid()
    {
        if (grid == null)
        {
            Debug.LogError("[SpawnTest] 'Grid' is not assigned. Drag the scene's HexGridManager " +
                           "into the field. Spawning is disabled.");
            return false;
        }
        return true;
    }

    /// <summary>
    /// Converts an EnemyDefinition to a UnitDefinition for the factory.
    /// Cached per-enemy so we only create each temp asset once per session.
    /// </summary>
    private UnitDefinition EnemyToUnitDef(EnemyDefinition e)
    {
        if (enemyDefCache.TryGetValue(e, out var cached)) return cached;

        var def = ScriptableObject.CreateInstance<UnitDefinition>();
        def.unitName = e.enemyName;
        def.maxHP = e.maxHP;
        def.defense = e.defense;
        def.attack = e.attack;
        def.movementRange = e.movementRange;
        def.attackRange = e.attackRange;
        def.critChance = e.critChance;
        def.dodgeChance = e.dodgeChance;
        def.recruitCost = 0;
        def.upkeepCost = 0;
        def.unitPrefab = e.unitPrefab;

        def.splashMode = e.splashMode;
        def.splashRadius = e.splashRadius;
        def.splashValue = e.splashValue;
        def.ability = e.ability;
        def.abilityValue = e.abilityValue;
        def.summonAtBattleStart = e.summonAtBattleStart;
        def.summonEveryNTurns = e.summonEveryNTurns;

        def.killReward = e.killValue;

        enemyDefCache[e] = def;
        return def;
    }
}