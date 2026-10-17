using UnityEngine;

public class StatsManager : MonoBehaviour
{
    public static StatsManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>Resolve an attack. Returns final damage dealt (0 if dodged / illegal).</summary>
    public int ResolveAttack(Unit attacker, Unit defender)
    {
        if (attacker == null || defender == null) return 0;
        if (!attacker.IsAlive || !defender.IsAlive) return 0;

        // Faction lock: cannot hit an ally
        if (attacker.Faction == defender.Faction)
        {
            Debug.LogWarning($"{attacker.UnitName} cannot attack ally {defender.UnitName}.");
            return 0;
        }

        // 1) Dodge — the attack is ignored entirely
        if (Random.value < defender.DodgeChance)
        {
            Debug.Log($"{defender.UnitName} dodged {attacker.UnitName}'s attack!");
            return 0;
        }

        // 2) Base damage (armor is a flat reduction, min 1)
        int damage = Mathf.Max(1, attacker.Attack - defender.Armor);

        // 3) Critical hit
        bool crit = Random.value < attacker.CritChance;
        if (crit)
        {
            damage = Mathf.RoundToInt(damage * attacker.CritDamage);
            Debug.Log($"{attacker.UnitName} landed a CRITICAL hit!");
        }

        // 4) Apply
        defender.TakeDamage(damage);
        Debug.Log($"{attacker.UnitName} → {defender.UnitName}: {damage} dmg " +
                  $"({defender.CurrentHP}/{defender.MaxHP} HP)");
        return damage;
    }
}