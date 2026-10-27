using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds every troop the player owns (bought, damaged, healed) between fights,
/// plus the consumable items. Persists via RunState.
/// </summary>
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Config")]
    [SerializeField] private int baseUnitCap = 6;

    // ===== Troops =====
    private readonly List<UnitInstance> units = new();
    public IReadOnlyList<UnitInstance> Units => units;

    // ===== Items (counts, not instances — items are stateless) =====
    private readonly Dictionary<string, int> items = new(); // itemId -> count
    public IReadOnlyDictionary<string, int> Items => items;

    // ===== Unit cap =====
    private int permanentCapBonus;
    public int UnitCap => baseUnitCap + permanentCapBonus;

    // ===== Events =====
    public event Action OnUnitsChanged;
    public event Action OnItemsChanged;
    public event Action<int> OnUnitCapChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ============================================================
    //  TROOPS
    // ============================================================

    public bool TryAddUnit(UnitDefinition def, out UnitInstance added)
    {
        added = null;
        if (def == null) return false;
        if (units.Count >= UnitCap && !def.isKing) return false;

        added = new UnitInstance(def);
        units.Add(added);
        OnUnitsChanged?.Invoke();
        return true;
    }

    public void RemoveUnit(UnitInstance instance)
    {
        if (instance == null) return;
        if (units.Remove(instance)) OnUnitsChanged?.Invoke();
    }

    /// <summary>Called after a battle ends. Returns all survivors to inventory (with their final HP).</summary>
    public void MergeSurvivors(IEnumerable<(UnitDefinition def, int hpAfterBattle)> survivors)
    {
        // Called from FightNodeController after the fight — implementation in Slice 3.
        // Left as a stub here so the class is complete and unit-testable.
        OnUnitsChanged?.Invoke();
    }

    public int TotalUpkeep()
    {
        int sum = 0;
        foreach (var u in units)
            if (u.definition != null) sum += u.definition.upkeepCost;
        return sum;
    }

    public bool HasKing()
    {
        foreach (var u in units) if (u.IsKing) return true;
        return false;
    }

    public UnitInstance GetKing()
    {
        foreach (var u in units) if (u.IsKing) return u;
        return null;
    }

    public void AddUnitCap(int delta)
    {
        if (delta == 0) return;
        permanentCapBonus = Mathf.Max(0, permanentCapBonus + delta);
        OnUnitCapChanged?.Invoke(UnitCap);
    }

    // ============================================================
    //  ITEMS
    // ============================================================

    public void AddItem(string itemId, int count = 1)
    {
        if (string.IsNullOrEmpty(itemId) || count <= 0) return;
        items.TryGetValue(itemId, out int cur);
        items[itemId] = cur + count;
        OnItemsChanged?.Invoke();
    }

    public bool RemoveItem(string itemId, int count = 1)
    {
        if (string.IsNullOrEmpty(itemId)) return false;
        if (!items.TryGetValue(itemId, out int cur) || cur < count) return false;
        cur -= count;
        if (cur <= 0) items.Remove(itemId);
        else items[itemId] = cur;
        OnItemsChanged?.Invoke();
        return true;
    }

    public int GetItemCount(string itemId)
    {
        return items.TryGetValue(itemId, out int c) ? c : 0;
    }

    // ============================================================
    //  SAVE / LOAD (called by RunState)
    // ============================================================

    public void RestoreFromRunState(List<UnitInstance> savedUnits, Dictionary<string, int> savedItems, int savedCapBonus)
    {
        units.Clear();
        if (savedUnits != null) units.AddRange(savedUnits);

        items.Clear();
        if (savedItems != null)
            foreach (var kv in savedItems) items[kv.Key] = kv.Value;

        permanentCapBonus = Mathf.Max(0, savedCapBonus);

        OnUnitsChanged?.Invoke();
        OnItemsChanged?.Invoke();
        OnUnitCapChanged?.Invoke(UnitCap);
    }
}