using UnityEngine;

public enum Faction { Player, Enemy }

public class Unit : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private Faction faction = Faction.Player;
    [SerializeField] private string unitName = "Unit";

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
    [SerializeField] private int movementRange = 3;   // this is the MAX per turn
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

    // ===== State =====
    public HexCoord Cell { get; private set; }
    public int RemainingMovement { get; private set; }
    public bool HasAttacked { get; private set; }

    public bool IsAlive => currentHP > 0;
    public bool CanMove => IsAlive && RemainingMovement > 0;
    public bool CanStillAct => IsAlive && !HasAttacked;

    private void Awake()
    {
        // Start the first turn with full movement already primed.
        RemainingMovement = movementRange;
    }

    // ===== Called by HexGridManager =====
    public void SetCell(HexCoord c) => Cell = c;
    public void SnapTo(Vector3 world) => transform.position = world;

    // ===== Turn lifecycle =====
    public void ResetForTurn()
    {
        RemainingMovement = movementRange;
        HasAttacked = false;
    }

    // ===== Movement spending =====
    /// <summary>Deduct movement points after a move.</summary>
    public void SpendMovement(int cost)
    {
        RemainingMovement = Mathf.Max(0, RemainingMovement - cost);
    }

    // ===== Action flags =====
    /// <summary>Attack ends this unit's turn AND drains any leftover movement.</summary>
    public void MarkAttacked()
    {
        HasAttacked = true;
        RemainingMovement = 0;
    }

    // ===== Stats mutation (called by StatsManager) =====
    public void TakeDamage(int amount) => currentHP = Mathf.Max(0, currentHP - amount);
    public void Heal(int amount) => currentHP = Mathf.Min(maxHP, currentHP + amount);
}