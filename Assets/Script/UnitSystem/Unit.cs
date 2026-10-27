using UnityEngine;

public enum Faction { Player, Enemy }

public class Unit : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private Faction faction = Faction.Player;
    [SerializeField] private string unitName = "Unit";
    [Tooltip("Set automatically by UnitFactory. Leave empty for manually placed units.")]
    [SerializeField] private UnitDefinition definition;

    [Header("Combat Stats")]
    [SerializeField] private int maxHP = 100;
    [SerializeField] private int currentHP = 100;
    [SerializeField] private int attack = 10;
    [SerializeField] private int armor = 2;

    [Header("Critical Hit")]
    [Range(0f, 1f)][SerializeField] private float critChance = 0.10f;
    [SerializeField] private float critDamage = 1.50f;

    [Header("Evasion")]
    [Range(0f, 1f)][SerializeField] private float dodgeChance = 0.05f;

    [Header("Range (in hex tiles)")]
    [SerializeField] private int movementRange = 3;
    [SerializeField] private int attackRange = 1;

    // ===== Read-only accessors =====
    public Faction Faction => faction;
    public string UnitName => unitName;
    public int CurrentHP => currentHP;
    public int MaxHP => maxHP;
    public int Attack => attack;
    public int Armor => armor;
    public float CritChance => critChance;
    public float CritDamage => critDamage;
    public float DodgeChance => dodgeChance;
    public int MovementRange => movementRange;
    public int AttackRange => attackRange;
    public UnitDefinition Definition => definition;

    // ===== State =====
    public HexCoord Cell { get; private set; }
    public int RemainingMovement { get; private set; }
    public bool HasAttacked { get; private set; }

    public bool IsAlive => currentHP > 0;
    public bool CanMove => IsAlive && RemainingMovement > 0;
    public bool CanStillAct => IsAlive && !HasAttacked;
    public bool IsKing => definition != null && definition.isKing;

    private void Awake()
    {
        // Set default for manually placed units (Path A).
        // InitializeFrom will overwrite this if called.
        RemainingMovement = movementRange;
    }

    // ==================================================================
    //  INITIALIZATION — called by UnitFactory (Path B)
    // ==================================================================

    /// <summary>Spawn from a UnitDefinition. Overwrites all stats.</summary>
    public void InitializeFrom(UnitDefinition def, Faction side, int startingHP = -1)
    {
        if (def == null) return;

        definition = def;
        faction = side;
        unitName = def.unitName;

        maxHP = def.maxHP;
        currentHP = startingHP >= 0
                        ? Mathf.Clamp(startingHP, 0, maxHP)
                        : maxHP;

        attack = def.attack;
        armor = def.defense;              // naming bridge: armor ↔ defense
        critChance = def.critChance / 100f;    // 15 → 0.15
        critDamage = 1.5f;                     // not on UnitDefinition (yet)
        dodgeChance = def.dodgeChance / 100f;   // 5 → 0.05
        movementRange = def.movementRange;
        attackRange = def.attackRange;

        // Reset per-turn state (Awake may already have run)
        RemainingMovement = movementRange;
        HasAttacked = false;
    }

    /// <summary>Spawn from a persistent inventory instance (keeps its current HP).</summary>
    public void InitializeFrom(UnitInstance instance, Faction side)
    {
        if (instance == null || instance.definition == null) return;
        InitializeFrom(instance.definition, side, instance.currentHP);
    }

    /// <summary>Write this unit's final HP back into a UnitInstance (called after battle).</summary>
    public void WriteBackTo(UnitInstance instance)
    {
        if (instance == null) return;
        instance.SyncFromBattle(currentHP);
    }

    // ==================================================================
    //  GRID + TURN LIFECYCLE
    // ==================================================================

    public void SetCell(HexCoord c) => Cell = c;
    public void SnapTo(Vector3 world) => transform.position = world;

    public void ResetForTurn()
    {
        RemainingMovement = movementRange;
        HasAttacked = false;
    }

    public void SpendMovement(int cost)
        => RemainingMovement = Mathf.Max(0, RemainingMovement - cost);

    public void MarkAttacked()
    {
        HasAttacked = true;
        RemainingMovement = 0;
    }

    // ==================================================================
    //  STATS MUTATION (called by StatsManager)
    // ==================================================================

    public void TakeDamage(int amount) => currentHP = Mathf.Max(0, currentHP - amount);
    public void Heal(int amount) => currentHP = Mathf.Min(maxHP, currentHP + amount);
}