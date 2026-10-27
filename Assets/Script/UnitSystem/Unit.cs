using System;
using UnityEngine;

public enum Faction { Player, Enemy }

public class Unit : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private Faction faction = Faction.Player;
    [SerializeField] private string unitName = "Unit";
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

    // Summon tracking
    public int LastSummonTurn { get; private set; } = -999;
    public bool HasSummonedAtStart { get; private set; }
    public bool IsSummoned { get; private set; }

    // ===== Derived state =====
    public bool IsAlive => currentHP > 0;
    public bool CanMove => IsAlive && RemainingMovement > 0;

    /// <summary>
    /// The unit has an unused action. Cataphracts (DoubleMove) can still act
    /// after attacking as long as they have movement left.
    /// </summary>
    public bool CanStillAct => IsAlive && (
        !HasAttacked ||
        (AllowsDoubleMove && RemainingMovement > 0)
    );

    public bool IsKing => definition != null && definition.isKing;
    public bool AllowsDoubleMove => definition != null && definition.ability == SpecialAbility.DoubleMove;

    /// <summary>
    /// True if this unit should be cleaned up when the battle ends.
    /// A unit is transient if it was summoned mid-battle OR its definition
    /// does not persist between fights (returnsDamaged = false).
    /// </summary>
    public bool IsTransient => IsSummoned || (definition != null && !definition.returnsDamaged);

    // ===== Events =====
    public event Action<Unit> OnDied;

    // ==================================================================
    //  LIFECYCLE
    // ==================================================================

    private void Awake()
    {
        RemainingMovement = movementRange;
    }

    // ==================================================================
    //  INITIALIZATION
    // ==================================================================

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
        armor = def.defense;
        critChance = def.critChance / 100f;
        critDamage = 1.5f;
        dodgeChance = def.dodgeChance / 100f;
        movementRange = def.movementRange;
        attackRange = def.attackRange;

        RemainingMovement = movementRange;
        HasAttacked = false;

        LastSummonTurn = -999;
        HasSummonedAtStart = false;
        IsSummoned = false;
    }

    public void InitializeFrom(UnitInstance instance, Faction side)
    {
        if (instance == null || instance.definition == null) return;
        InitializeFrom(instance.definition, side, instance.currentHP);
    }

    public void WriteBackTo(UnitInstance instance)
    {
        if (instance == null) return;
        instance.SyncFromBattle(currentHP);
    }

    // ==================================================================
    //  GRID + TURN
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

    /// <summary>
    /// Marks the unit's action as used.
    /// Normal units forfeit remaining movement; Cataphracts keep it,
    /// so they can reposition after the attack (Double Move ability).
    /// </summary>
    public void MarkAttacked()
    {
        HasAttacked = true;
        if (!AllowsDoubleMove)
            RemainingMovement = 0;
        // Cataphract: RemainingMovement is untouched — preserved for the post-attack move.
    }

    // ==================================================================
    //  SUMMON TRACKING
    // ==================================================================

    public void MarkSummoned(int turn) => LastSummonTurn = turn;

    public void MarkSummonedAtStart(int turn)
    {
        HasSummonedAtStart = true;
        LastSummonTurn = turn;
    }

    public void MarkAsSummoned() => IsSummoned = true;

    // ==================================================================
    //  STATS MUTATION
    // ==================================================================

    public void TakeDamage(int amount)
    {
        if (!IsAlive) return;
        currentHP = Mathf.Max(0, currentHP - amount);
        if (currentHP == 0) OnDied?.Invoke(this);
    }

    public void Heal(int amount)
    {
        if (!IsAlive) return;
        currentHP = Mathf.Min(maxHP, currentHP + amount);
    }
}