using System;
using UnityEngine;

/// <summary>
/// Money in the run. Kill rewards, shop spends, and per-node upkeep are routed through here.
/// </summary>
public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    [Header("Starting")]
    [SerializeField] private int startingMoney = 100;

    public int Money { get; private set; }

    public event Action<int> OnMoneyChanged;  // new balance
    public event Action<int> OnMoneyEarned;   // amount
    public event Action<int> OnMoneySpent;    // amount

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Money = startingMoney;
    }

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

    /// <summary>Called by MapController when the player tries to advance to a new node.</summary>
    public bool TryPayUpkeepForAdvance()
    {
        int upkeep = InventoryManager.Instance != null ? InventoryManager.Instance.TotalUpkeep() : 0;
        if (upkeep <= 0) return true;
        return TrySpend(upkeep);
    }
}