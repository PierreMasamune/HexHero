using System;
using System.Collections.Generic;
using UnityEngine;

public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    [Header("Starting")]
    [SerializeField] private int startingMoney = 100;

    public int Money { get; private set; }

    public event Action<int> OnMoneyChanged;
    public event Action<int> OnMoneyEarned;
    public event Action<int> OnMoneySpent;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Money = startingMoney;
    }

    private void Start()
    {
        if (StatsManager.Instance != null)
            StatsManager.Instance.OnKill += HandleKill;
        else
            Debug.LogWarning("[EconomyManager] StatsManager.Instance is null — kill rewards disabled.");
    }

    private void OnDestroy()
    {
        if (StatsManager.Instance != null)
            StatsManager.Instance.OnKill -= HandleKill;
        if (Instance == this) Instance = null;
    }

    // ==================================================================
    //  PUBLIC API
    // ==================================================================

    public void Earn(int amount)
    {
        if (amount <= 0) return;
        Money += amount;
        OnMoneyChanged?.Invoke(Money);
        OnMoneyEarned?.Invoke(amount);
    }

    public bool CanAfford(int amount) => Money >= amount;

    public bool TrySpend(int amount)
    {
        if (amount <= 0) return true;
        if (Money < amount) return false;
        Money -= amount;
        OnMoneyChanged?.Invoke(Money);
        OnMoneySpent?.Invoke(amount);
        return true;
    }

    public void SetMoney(int amount)
    {
        Money = Mathf.Max(0, amount);
        OnMoneyChanged?.Invoke(Money);
    }

    public bool TryPayUpkeepForAdvance()
    {
        int upkeep = InventoryManager.Instance != null ? InventoryManager.Instance.TotalUpkeep() : 0;
        if (upkeep <= 0) return true;
        return TrySpend(upkeep);
    }

    // ==================================================================
    //  KILL REWARDS
    // ==================================================================

    private void HandleKill(Unit killer, Unit victim)
    {
        if (killer == null || victim == null) return;

        // Enemy kill reward — any player unit killing any enemy unit
        if (killer.Faction == Faction.Player
            && victim.Faction == Faction.Enemy
            && victim.Definition != null
            && victim.Definition.killReward > 0)
        {
            Earn(victim.Definition.killReward);
            Debug.Log($"[Economy] +{victim.Definition.killReward}g (killed {victim.UnitName})");
        }

        // Chieftain extra gold per kill
        if (killer.Faction == Faction.Player
            && killer.Definition != null
            && killer.Definition.ability == SpecialAbility.ExtraGoldPerKill
            && killer.Definition.abilityValue > 0)
        {
            Earn(killer.Definition.abilityValue);
            Debug.Log($"[Economy] +{killer.Definition.abilityValue}g " +
                      $"({killer.UnitName} kill bonus)");
        }
    }

    // ==================================================================
    //  BATTLE-END REWARDS
    // ==================================================================

    /// <summary>
    /// Sums ExtraGoldPerBattle / ExtraGoldPerTurn abilityValue across surviving
    /// player units and awards it. Called on victory only — defeat ends the run.
    /// </summary>
    public void AwardBattleEndGold(IEnumerable<Unit> playerUnits)
    {
        if (playerUnits == null) return;

        int total = 0;
        foreach (var u in playerUnits)
        {
            if (u == null || !u.IsAlive) continue;
            if (u.Definition == null) continue;

            if (u.Definition.ability == SpecialAbility.ExtraGoldPerBattle
                || u.Definition.ability == SpecialAbility.ExtraGoldPerTurn)
            {
                total += u.Definition.abilityValue;
                Debug.Log($"[Economy] {u.UnitName} grants +{u.Definition.abilityValue}g at battle end.");
            }
        }

        if (total > 0)
        {
            Earn(total);
            Debug.Log($"[Economy] Battle-end total: +{total}g");
        }
    }
}