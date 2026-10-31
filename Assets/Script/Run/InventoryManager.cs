using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Config")]
    [Tooltip("Maximum units allowed on the battlefield at once.")]
    [SerializeField] private int baseUnitCap = 6;

    [Header("Debug Army (For Testing)")]
    [SerializeField] private List<UnitDefinition> debugStartingArmy = new();

    public IReadOnlyList<UnitInstance> Units
    {
        get
        {
            if (RunManager.Instance != null && RunManager.Instance.State != null)
                return RunManager.Instance.State.units;

            return new List<UnitInstance>();
        }
    }

    [System.Serializable]
    public class ItemEntry { public string itemId; public int count; }

    [Header("Items (For Testing)")]
    [SerializeField] private List<ItemEntry> startWithItems = new();

    private readonly Dictionary<string, int> items = new();
    public IReadOnlyDictionary<string, int> Items => items;

    public int UnitCap => baseUnitCap + (RunManager.Instance != null ? RunManager.Instance.State.unitCapBonus : 0);

    public event Action OnUnitsChanged;
    public event Action OnItemsChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        foreach (var entry in startWithItems)
        {
            if (!string.IsNullOrEmpty(entry.itemId) && entry.count > 0)
                AddItem(entry.itemId, entry.count);
        }

        // Populate debug army if it's a brand new run
        if (RunManager.Instance != null && RunManager.Instance.State.visitedMapNodeIds.Count == 0)
        {
            foreach (var unitDef in debugStartingArmy)
            {
                if (unitDef != null) TryAddUnit(unitDef, out _);
            }
        }
    }

    // ============================================================
    //  TROOPS
    // ============================================================

    public bool TryAddUnit(UnitDefinition def, out UnitInstance added)
    {
        added = null;
        if (def == null) return false;

        var list = RunManager.Instance.State.units;

        // ★ REMOVED CAP CHECK: You can now hold infinite units in inventory!

        added = new UnitInstance(def);
        list.Add(added);

        OnUnitsChanged?.Invoke();
        return true;
    }

    public void RemoveUnit(UnitInstance instance)
    {
        if (instance == null) return;
        if (RunManager.Instance.State.units.Remove(instance))
            OnUnitsChanged?.Invoke();
    }

    public bool ReturnUnit(UnitInstance instance)
    {
        if (instance == null || instance.definition == null) return false;

        var list = RunManager.Instance.State.units;
        if (list.Contains(instance)) return true;

        // ★ REMOVED CAP CHECK: Survivors always return safely.

        list.Add(instance);
        OnUnitsChanged?.Invoke();
        return true;
    }

    public int TotalUpkeep()
    {
        int sum = 0;
        foreach (var u in Units)
            if (u.definition != null) sum += u.definition.upkeepCost;
        return sum;
    }

    public UnitInstance GetKing()
    {
        foreach (var u in Units) if (u.IsKing) return u;
        return null;
    }

    public void AddUnitCap(int delta)
    {
        if (delta == 0 || RunManager.Instance == null) return;
        RunManager.Instance.State.unitCapBonus = Mathf.Max(0, RunManager.Instance.State.unitCapBonus + delta);
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

    [ContextMenu("Sync All HP to Definitions")]
    public void SyncAllHP()
    {
        foreach (var unit in Units)
        {
            if (unit != null) unit.SyncWithDefinition();
        }
        OnUnitsChanged?.Invoke();
    }
}