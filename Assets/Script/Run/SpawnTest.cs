using UnityEngine;

public class SpawnTest : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HexGridManager grid;

    [Header("Spawn Positions (world space)")]
    [SerializeField] private Vector3 kingSpawnPos = new Vector3(0f, 0f, 0f);
    [SerializeField] private Vector3 barbSpawnPos = new Vector3(3f, 0f, 0f);
    [SerializeField] private Vector3 enemySpawnPos = new Vector3(-3f, 0f, 0f);

    [Header("Toggles")]
    [SerializeField] private bool spawnKing = true;
    [SerializeField] private bool spawnBarb = true;
    [SerializeField] private bool spawnEnemy = true;

    private void Start()
    {
        if (grid == null)
        {
            Debug.LogError("[SpawnTest] 'grid' reference not set. Drag HexGridManager into the slot.");
            return;
        }

        if (spawnKing) SpawnAndRegister("Units/King", Faction.Player, kingSpawnPos, "King");
        if (spawnBarb) SpawnAndRegister("Units/Barb", Faction.Player, barbSpawnPos, "Barb");
        if (spawnEnemy) SpawnAndRegister("Enemies/GoblinMelee", Faction.Enemy, enemySpawnPos, "GoblinMelee");
    }

    private void SpawnAndRegister(string resourcePath, Faction side, Vector3 worldPos, string label)
    {
        // 1. Load the definition asset
        UnitDefinition def = null;

        if (side == Faction.Player)
            def = Resources.Load<UnitDefinition>(resourcePath);
        else
        {
            // Enemies use EnemyDefinition assets. Convert to a UnitDefinition-equivalent
            // for testing by loading the EnemyDefinition and translating its fields.
            var enemy = Resources.Load<EnemyDefinition>(resourcePath);
            if (enemy != null)
                def = EnemyToUnitDef(enemy);
        }

        if (def == null)
        {
            Debug.LogError($"[SpawnTest] Could not load asset '{resourcePath}'. " +
                           $"Make sure it exists in Assets/Resources/{resourcePath}.asset.");
            return;
        }

        // 2. Spawn via UnitFactory
        var unit = UnitFactory.Spawn(def, side, worldPos);
        if (unit == null)
        {
            Debug.LogError($"[SpawnTest] Spawn failed for '{label}'.");
            return;
        }

        // 3. Register with the grid (snaps to nearest tile + records occupancy)
        var cell = grid.WorldToCell(unit.transform.position);
        grid.RegisterUnit(unit, cell);

        // 4. Confirmation log
        Debug.Log($"[SpawnTest] {label} spawned at cell {cell} — " +
                  $"HP {unit.CurrentHP}/{unit.MaxHP}, ATK {unit.Attack}, ARM {unit.Armor}, " +
                  $"MOVE {unit.MovementRange}, RANGE {unit.AttackRange}, " +
                  $"Crit {unit.CritChance:P0}, Dodge {unit.DodgeChance:P0}, " +
                  $"IsKing={unit.IsKing}, Faction={unit.Faction}");
    }

    /// <summary>
    /// Quick-and-dirty translation so we can test enemies before UnitFactory
    /// gains a true EnemyDefinition overload (that comes in Slice 4).
    /// </summary>
    private UnitDefinition EnemyToUnitDef(EnemyDefinition e)
    {
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
        return def;
    }
}