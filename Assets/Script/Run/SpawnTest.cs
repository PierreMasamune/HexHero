using System.Collections.Generic;
using UnityEngine;

// ======================================================================
//  Data class — one row per unit you want to spawn.
//  Editable in the Inspector.
// ======================================================================
[System.Serializable]
public class SpawnEntry
{
    [Tooltip("Optional label for the Console log. Leave blank to use the definition's name.")]
    public string label;

    [Tooltip("Player or Enemy. Determines which definition field below is used.")]
    public Faction faction = Faction.Player;

    [Tooltip("Filled when Faction = Player.")]
    public UnitDefinition troop;

    [Tooltip("Filled when Faction = Enemy.")]
    public EnemyDefinition enemy;

    [Tooltip("World position. Snaps to the nearest hex tile.")]
    public Vector3 worldPosition = Vector3.zero;

    [Tooltip("Also add this unit to TurnManager's per-faction list so it counts in the turn loop.")]
    public bool registerWithTurnManager = true;
}

// ======================================================================
//  MonoBehaviour — put this on your GameManager (or any GameObject)
// ======================================================================
public class SpawnTest : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HexGridManager grid;

    [Header("Spawn List")]
    [Tooltip("Add one entry per unit you want to spawn. Each row is a separate unit.")]
    [SerializeField] private List<SpawnEntry> spawnList = new();

    [Header("Options")]
    [Tooltip("Run automatically on Start(). Turn off to spawn manually via the component's context menu.")]
    [SerializeField] private bool spawnOnStart = true;

    [Tooltip("Log every spawned unit's stats to the Console.")]
    [SerializeField] private bool verboseLogging = true;

    [Tooltip("Warn and skip (rather than spawn) if the target cell is already occupied.")]
    [SerializeField] private bool skipIfCellOccupied = true;

    // Cache so we don't create a new temp UnitDefinition every spawn.
    private readonly Dictionary<EnemyDefinition, UnitDefinition> enemyDefCache = new();

    // ==================================================================
    //  LIFECYCLE
    // ==================================================================

    private void Start()
    {
        if (!spawnOnStart) return;
        if (grid == null) { Debug.LogError("[SpawnTest] 'grid' is not assigned."); return; }
        SpawnAll();
    }

    // ==================================================================
    //  PUBLIC — also exposed as Inspector context-menu items
    // ==================================================================

    /// <summary>Spawns every entry in the list, in order.</summary>
    [ContextMenu("Spawn All")]
    public void SpawnAll()
    {
        if (grid == null) { Debug.LogError("[SpawnTest] Grid not assigned."); return; }
        if (spawnList == null || spawnList.Count == 0)
        {
            Debug.LogWarning("[SpawnTest] Spawn list is empty.");
            return;
        }

        int successCount = 0;
        int failCount = 0;

        foreach (var entry in spawnList)
        {
            if (SpawnOne(entry)) successCount++;
            else failCount++;
        }

        Debug.Log($"[SpawnTest] Done. Spawned {successCount} unit(s); skipped {failCount}.");
    }

    /// <summary>Clears the inspector spawn list.</summary>
    [ContextMenu("Clear Spawn List")]
    public void ClearList()
    {
        spawnList.Clear();
        Debug.Log("[SpawnTest] Spawn list cleared.");
    }

    /// <summary>Logs the current spawn list configuration.</summary>
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
            Debug.Log($"  [{i}] '{e.label}' — {e.faction} — {def} @ {e.worldPosition}");
        }
    }

    // ==================================================================
    //  CORE
    // ==================================================================

    private bool SpawnOne(SpawnEntry entry)
    {
        if (entry == null) return false;

        // ---- 1. Resolve which definition to use ----
        UnitDefinition def = null;
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

        // ---- 2. Compute cell and check occupancy ----
        var cell = grid.WorldToCell(entry.worldPosition);

        if (!grid.Contains(cell))
        {
            Debug.LogWarning($"[SpawnTest] '{displayName}' — cell {cell} is outside the grid. Skipping.");
            return false;
        }

        var existing = grid.GetUnit(cell);
        if (existing != null)
        {
            if (skipIfCellOccupied)
            {
                Debug.LogWarning($"[SpawnTest] '{displayName}' — cell {cell} already occupied by " +
                                 $"'{existing.UnitName}'. Skipping (skipIfCellOccupied = true).");
                return false;
            }
            // Fall through — RegisterUnit will replace (the grid fix from before allows this safely)
        }

        // ---- 3. Spawn ----
        var unit = UnitFactory.Spawn(def, entry.faction, entry.worldPosition);
        if (unit == null)
        {
            Debug.LogError($"[SpawnTest] '{displayName}' — UnitFactory.Spawn returned null.");
            return false;
        }

        // ---- 4. Register with grid ----
        grid.RegisterUnit(unit, cell);

        // ---- 5. Register with TurnManager ----
        if (entry.registerWithTurnManager && TurnManager.Instance != null)
            TurnManager.Instance.RegisterSpawnedUnit(unit);

        // ---- 6. Verbose log ----
        if (verboseLogging)
        {
            Debug.Log($"[SpawnTest] {displayName} spawned at cell {cell} — " +
                      $"HP {unit.CurrentHP}/{unit.MaxHP}, ATK {unit.Attack}, ARM {unit.Armor}, " +
                      $"MOVE {unit.MovementRange}, RANGE {unit.AttackRange}, " +
                      $"Crit {unit.CritChance:P0}, Dodge {unit.DodgeChance:P0}, " +
                      $"IsKing={unit.IsKing}, Faction={unit.Faction}");
        }

        return true;
    }

    /// <summary>
    /// Converts an EnemyDefinition to a UnitDefinition for the factory.
    /// Cached so we only create each temp asset once per session.
    /// (Slice 4 will add a native UnitFactory.Spawn(EnemyDefinition,...) overload,
    /// which will make this method obsolete.)
    /// </summary>
  private UnitDefinition EnemyToUnitDef(EnemyDefinition e)
{
    if (enemyDefCache.TryGetValue(e, out var cached)) return cached;

    var def = ScriptableObject.CreateInstance<UnitDefinition>();
    def.unitName      = e.enemyName;
    def.maxHP         = e.maxHP;
    def.defense       = e.defense;
    def.attack        = e.attack;
    def.movementRange = e.movementRange;
    def.attackRange   = e.attackRange;
    def.critChance    = e.critChance;
    def.dodgeChance   = e.dodgeChance;
    def.recruitCost   = 0;
    def.upkeepCost    = 0;
    def.unitPrefab    = e.unitPrefab;

    // ★ NEW — copy ability fields so enemy splash/heal/summon work
    def.splashMode      = e.splashMode;
    def.splashRadius    = e.splashRadius;
    def.splashValue     = e.splashValue;
    def.ability         = e.ability;
    def.abilityValue    = e.abilityValue;
    def.summonAtBattleStart = e.summonAtBattleStart;
    def.summonEveryNTurns   = e.summonEveryNTurns;
    def.killReward = e.killValue;
        // summonTemplate not converted here — Slice 4 handles enemy summons

        enemyDefCache[e] = def;
    return def;
}
}