using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Config")]
    [SerializeField] private int baseUnitCap = 6;

    // ★ Visible in the Inspector so you can edit HP manually
    [Header("Troops (Editable for Testing)")]
    [SerializeField] private List<UnitInstance> units = new();
    public IReadOnlyList<UnitInstance> Units => units;

    [System.Serializable]
    public class ItemEntry
    {
        public string itemId;
        public int count;
    }

    [Header("Items (For Testing)")]
    [SerializeField] private List<ItemEntry> startWithItems = new();

    private readonly Dictionary<string, int> items = new();
    public IReadOnlyDictionary<string, int> Items => items;

    private int permanentCapBonus;
    public int UnitCap => baseUnitCap + permanentCapBonus;

    public event Action OnUnitsChanged;
    public event Action OnItemsChanged;
    public event Action<int> OnUnitCapChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        foreach (var entry in startWithItems)
        {
            if (!string.IsNullOrEmpty(entry.itemId) && entry.count > 0)
                AddItem(entry.itemId, entry.count);
        }
    }

    // ============================================================
    //  TROOPS
    // ============================================================

    public bool TryAddUnit(UnitDefinition def, out UnitInstance added)
    {
        added = null;
        if (def == null) return false;
        if (units.Count >= UnitCap && !def.isKing) return false;

        added = new UnitInstance(def); // Constructor automatically sets maxHP & currentHP
        units.Add(added);
        OnUnitsChanged?.Invoke();
        return true;
    }

    public void RemoveUnit(UnitInstance instance)
    {
        if (instance == null) return;
        if (units.Remove(instance)) OnUnitsChanged?.Invoke();
    }

    public bool ReturnUnit(UnitInstance instance)
    {
        if (instance == null || instance.definition == null) return false;
        if (units.Contains(instance)) return true;

        if (!instance.definition.isKing && units.Count >= UnitCap)
        {
            Debug.LogWarning($"[Inventory] Cannot return {instance.definition.unitName}: cap ({UnitCap}) reached.");
            return false;
        }

        units.Add(instance);
        OnUnitsChanged?.Invoke();
        return true;
    }

    // ★ NEW: Right-click the InventoryManager component header and select this to fix all HP values
    [ContextMenu("Sync All HP to Definitions")]
    public void SyncAllHP()
    {
        foreach (var unit in units)
        {
            if (unit != null) unit.SyncWithDefinition();
        }
        OnUnitsChanged?.Invoke();
        Debug.Log("[Inventory] Synced all unit HP to their definitions.");
    }

    public int TotalUpkeep()
    {
        int sum = 0;
        foreach (var u in units)
            if (u.definition != null) sum += u.definition.upkeepCost;
        return sum;
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