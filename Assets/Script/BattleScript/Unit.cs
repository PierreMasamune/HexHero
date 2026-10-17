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
    [SerializeField] private int movementRange = 3;
    [SerializeField] private int attackRange = 1;

    // ----- Public read-only stats -----
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

    public bool IsAlive => currentHP > 0;

    // ----- Grid + per-turn state -----
    public Vector3Int Cell { get; set; }
    public bool HasMoved { get; private set; }
    public bool HasAttacked { get; private set; }
    public bool CanStillAct => IsAlive && !HasAttacked;

    public void MarkMoved() => HasMoved = true;
    public void MarkAttacked() => HasAttacked = true;
    public void ResetForTurn() { HasMoved = false; HasAttacked = false; }

    // ----- Mutation API (used ONLY by StatsManager) -----
    public void TakeDamage(int amount)
    {
        currentHP = Mathf.Max(0, currentHP - amount);
        if (currentHP == 0) Debug.Log($"{unitName} has been defeated.");
    }

    public void Heal(int amount) => currentHP = Mathf.Min(maxHP, currentHP + amount);
}