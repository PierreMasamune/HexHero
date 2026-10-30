using System;
using UnityEngine;

public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    [Header("Starting")]
    [SerializeField] private int startingMoney = 100;

    public int Money { get; private set; }

    // ★ NEW: Snapshot of money before the battle starts
    public int StartOfBattleMoney { get; private set; }

    public int LastBattleKillGold { get; private set; }
    public int LastBattleAbilityGold { get; private set; }

    public event Action<int> OnMoneyChanged;
    public event Action<int> OnMoneyEarned;
    public event Action<int> OnMoneySpent;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (RunManager.Instance != null && RunManager.Instance.State != null)
        {
            // 1. Try to load the money from the save file
            Money = RunManager.Instance.State.money;

            // 2. If it's a brand new game (money is 0 and no nodes visited yet), give the starting money!
            if (Money == 0 && RunManager.Instance.State.visitedMapNodeIds.Count == 0)
            {
                Money = startingMoney;
                RunManager.Instance.State.money = Money; // Sync it to the RunState so it saves correctly
            }
        }
        else
        {
            Money = startingMoney;
        }

        OnMoneyChanged?.Invoke(Money);

        if (StatsManager.Instance != null)
            StatsManager.Instance.OnKill += HandleKill;
    }

    private void OnDestroy()
    {
        if (StatsManager.Instance != null)
            StatsManager.Instance.OnKill -= HandleKill;
        if (Instance == this) Instance = null;
    }

    public void Earn(int amount, bool isKillGold = false, bool isAbilityGold = false)
    {
        if (amount <= 0) return;
        Money += amount;

        if (isKillGold) LastBattleKillGold += amount;
        if (isAbilityGold) LastBattleAbilityGold += amount;

        UpdateRunState();
        OnMoneyChanged?.Invoke(Money);
        OnMoneyEarned?.Invoke(amount);
    }

    public bool TrySpend(int amount)
    {
        if (amount <= 0) return true;
        if (Money < amount) return false;

        Money -= amount;
        UpdateRunState();
        OnMoneyChanged?.Invoke(Money);
        OnMoneySpent?.Invoke(amount);
        return true;
    }

    public void SetMoney(int amount)
    {
        Money = Mathf.Max(0, amount);
        UpdateRunState();
        OnMoneyChanged?.Invoke(Money);
    }

    private void UpdateRunState()
    {
        if (RunManager.Instance != null && RunManager.Instance.State != null)
            RunManager.Instance.State.money = Money;
    }

    // ==================================================================
    //  BATTLE TRACKERS
    // ==================================================================

    public void ResetBattleTrackers()
    {
        // ★ Take a snapshot of money when the battle starts
        StartOfBattleMoney = Money;
        LastBattleKillGold = 0;
        LastBattleAbilityGold = 0;
    }

    private void HandleKill(Unit killer, Unit victim)
    {
        if (killer == null || victim == null) return;

        if (killer.Faction == Faction.Player && victim.Faction == Faction.Enemy && victim.Definition != null)
        {
            if (victim.Definition.killReward > 0)
                Earn(victim.Definition.killReward, isKillGold: true);

            if (killer.Definition != null && killer.Definition.ability == SpecialAbility.ExtraGoldPerKill)
                Earn(killer.Definition.abilityValue, isAbilityGold: true);
        }
    }

    public void AwardBattleEndGold(System.Collections.Generic.IEnumerable<Unit> playerUnits)
    {
        if (playerUnits == null) return;

        int total = 0;
        foreach (var u in playerUnits)
        {
            if (u == null || !u.IsAlive || u.Definition == null) continue;
            if (u.Definition.ability == SpecialAbility.ExtraGoldPerBattle || u.Definition.ability == SpecialAbility.ExtraGoldPerTurn)
                total += u.Definition.abilityValue;
        }

        if (total > 0)
            Earn(total, isAbilityGold: true);
    }
}