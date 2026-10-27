using System;
using UnityEngine;
using Random = UnityEngine.Random;   // disambiguate: Random means UnityEngine.Random

public class StatsManager : MonoBehaviour
{
    public static StatsManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private HexGridManager grid;

    /// <summary>Fires when a unit is killed by another unit's attack.</summary>
    public event Action<Unit, Unit> OnKill;   // (killer, victim)

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (grid == null) grid = FindAnyObjectByType<HexGridManager>();
    }

    // ==================================================================
    //  ATTACK
    // ==================================================================

    public AttackResult ResolveAttack(Unit attacker, Unit defender)
    {
        var result = new AttackResult
        {
            Attacker = attacker,
            PrimaryTarget = defender,
            TargetCell = defender != null ? defender.Cell : default,
        };

        if (attacker == null || defender == null) return result;
        if (!attacker.IsAlive || !defender.IsAlive) return result;
        if (attacker.Faction == defender.Faction)
        {
            Debug.LogWarning($"{attacker.UnitName} cannot attack ally {defender.UnitName}.");
            return result;
        }

        ResolvePrimary(attacker, defender, result);

        if (attacker.Definition != null && attacker.Definition.splashMode != SplashMode.None)
            ResolveSplash(attacker, defender, result);

        return result;
    }

    private void ResolvePrimary(Unit attacker, Unit defender, AttackResult result)
    {
        // 1) Dodge
        if (Random.value < defender.DodgeChance)
        {
            result.PrimaryDodged = true;
            Debug.Log($"{defender.UnitName} dodged {attacker.UnitName}'s attack!");
            return;
        }

        // 2) Instakill
        if (attacker.Definition != null
            && attacker.Definition.ability == SpecialAbility.Instakill)
        {
            float chance = attacker.Definition.abilityValue / 100f;
            if (chance > 0f && Random.value < chance)
            {
                int lethal = defender.CurrentHP;
                defender.TakeDamage(lethal);
                result.PrimaryDamage = lethal;
                result.PrimaryInstakilled = true;
                result.PrimaryKilled = !defender.IsAlive;

                Debug.Log($"[Instakill] {attacker.UnitName} EXECUTED {defender.UnitName}! " +
                          $"({(chance * 100f):0.#}% chance)");

                if (result.PrimaryKilled) NotifyKill(attacker, defender);
                return;
            }
        }

        // 3) Base damage (attack − armor, min 1)
        int damage = Mathf.Max(1, attacker.Attack - defender.Armor);

        // 4) Terrain damage reduction (applies to the DEFENDER'S cell)
        if (grid != null && grid.TerrainMap != null)
        {
            int pct = grid.TerrainMap.GetDamageReductionPercent(defender.Cell);
            if (pct > 0)
            {
                int before = damage;
                damage = Mathf.Max(1, Mathf.RoundToInt(damage * (1f - pct / 100f)));
                var terr = grid.TerrainMap.GetTerrain(defender.Cell);
                string tname = terr != null ? terr.terrainName : "terrain";
                Debug.Log($"[Terrain] {defender.UnitName} on {tname}: " +
                          $"{before} → {damage} dmg ({pct}% reduction)");
            }
        }

        // 5) Critical hit (multiplier applied AFTER terrain reduction)
        bool crit = Random.value < attacker.CritChance;
        if (crit)
        {
            damage = Mathf.RoundToInt(damage * attacker.CritDamage);
            result.PrimaryCritical = true;
            Debug.Log($"{attacker.UnitName} landed a CRITICAL hit!");
        }

        // 6) Apply
        defender.TakeDamage(damage);
        result.PrimaryDamage = damage;
        result.PrimaryKilled = !defender.IsAlive;

        Debug.Log($"{attacker.UnitName} → {defender.UnitName}: {damage} dmg " +
                  $"({defender.CurrentHP}/{defender.MaxHP} HP)");

        if (result.PrimaryKilled) NotifyKill(attacker, defender);
    }

    private void ResolveSplash(Unit attacker, Unit primaryTarget, AttackResult result)
    {
        if (grid == null) { Debug.LogWarning("[StatsManager] Splash attack but 'grid' not assigned."); return; }

        var def = attacker.Definition;
        int rawDamage = AoeResolver.ComputeSplashDamage(attacker, def.splashMode, def.splashValue);
        if (rawDamage <= 0) return;

        var targets = AoeResolver.FindSplashTargets(
            grid, result.TargetCell, def.splashRadius, primaryTarget, attacker);

        foreach (var target in targets)
        {
            var hit = new SplashHit { Target = target };

            if (Random.value < target.DodgeChance)
            {
                hit.Dodged = true;
                result.SplashHits.Add(hit);
                Debug.Log($"[Splash] {target.UnitName} dodged.");
                continue;
            }

            // Splash damage is NOT reduced by terrain (only primary target's tile applies)
            int damage = Mathf.Max(1, rawDamage - target.Armor);
            target.TakeDamage(damage);
            hit.Damage = damage;
            hit.Killed = !target.IsAlive;
            result.SplashHits.Add(hit);

            Debug.Log($"[Splash] {attacker.UnitName} → {target.UnitName}: {damage} dmg " +
                      $"({target.CurrentHP}/{target.MaxHP} HP)");

            if (hit.Killed) NotifyKill(attacker, target);
        }
    }

    private void NotifyKill(Unit killer, Unit victim)
    {
        if (killer == null || victim == null) return;
        OnKill?.Invoke(killer, victim);
    }

    // ==================================================================
    //  HEAL
    // ==================================================================

    public HealResult ResolveHeal(Unit healer, Unit target)
    {
        var result = new HealResult { Healer = healer, PrimaryTarget = target };

        if (healer == null || target == null) return result;
        if (!healer.IsAlive || !target.IsAlive) return result;
        if (healer.Faction != target.Faction) return result;
        if (healer.Definition == null) return result;

        var ability = healer.Definition.ability;
        if (ability != SpecialAbility.HealAlly && ability != SpecialAbility.AoeHeal) return result;

        if (HexCoord.Distance(healer.Cell, target.Cell) > healer.AttackRange)
        {
            Debug.LogWarning($"[Heal] {healer.UnitName} → {target.UnitName}: out of range.");
            return result;
        }

        int healAmount = Mathf.Max(1, healer.Definition.abilityValue);

        int before = target.CurrentHP;
        target.Heal(healAmount);
        int healed = target.CurrentHP - before;
        result.PrimaryHealAmount = healed;

        if (healed > 0)
            Debug.Log($"[Heal] {healer.UnitName} → {target.UnitName}: +{healed} HP " +
                      $"({target.CurrentHP}/{target.MaxHP})");

        if (ability == SpecialAbility.AoeHeal)
        {
            int radius = healer.Definition.splashRadius;
            if (radius <= 0) radius = 1;

            for (int dq = -radius; dq <= radius; dq++)
            {
                int rMin = Mathf.Max(-radius, -dq - radius);
                int rMax = Mathf.Min(radius, -dq + radius);
                for (int dr = rMin; dr <= rMax; dr++)
                {
                    if (dq == 0 && dr == 0) continue;
                    var cell = new HexCoord(target.Cell.q + dq, target.Cell.r + dr);
                    if (grid == null || !grid.Contains(cell)) continue;
                    var u = grid.GetUnit(cell);
                    if (u == null || !u.IsAlive) continue;
                    if (u.Faction != healer.Faction) continue;

                    int hpBefore = u.CurrentHP;
                    u.Heal(healAmount);
                    int hpAfter = u.CurrentHP;
                    if (hpAfter > hpBefore)
                    {
                        result.AoeHeals.Add(new AoeHealHit { Target = u, Healed = hpAfter - hpBefore });
                        Debug.Log($"[AOE Heal] {healer.UnitName} → {u.UnitName}: +{hpAfter - hpBefore} HP " +
                                  $"({u.CurrentHP}/{u.MaxHP})");
                    }
                }
            }
        }

        return result;
    }
}