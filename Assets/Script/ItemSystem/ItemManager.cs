using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime inventory of consumable items. Owned by RunManager (persisted in RunState).
/// </summary>
public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }

    [Header("Item Catalog (drag assets here for prototype)")]
    [Tooltip("Every ItemDefinition asset. In the full game, loaded via Resources.")]
    [SerializeField] private List<ItemDefinition> allItemDefinitions = new();

    [Header("Debug")]
    [SerializeField] private bool verboseLogging = true;

    // itemId ? owned count
    private readonly Dictionary<string, int> inventory = new();
    // itemId ? ItemDefinition (fast lookup)
    private readonly Dictionary<string, ItemDefinition> definitionsById = new();

    public event Action OnInventoryChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Build the id ? definition index
        foreach (var def in allItemDefinitions)
            if (def != null && !string.IsNullOrEmpty(def.itemId))
                definitionsById[def.itemId] = def;
    }

    // ==================================================================
    //  PUBLIC QUERIES
    // ==================================================================

    public int GetCount(string itemId)
        => inventory.TryGetValue(itemId, out int c) ? c : 0;

    public bool Has(string itemId) => GetCount(itemId) > 0;

    public ItemDefinition GetDefinition(string itemId)
        => definitionsById.TryGetValue(itemId, out var d) ? d : null;

    public IReadOnlyDictionary<string, int> All => inventory;

    // ==================================================================
    //  ADD / REMOVE
    // ==================================================================

    public void Add(string itemId, int count = 1)
    {
        if (string.IsNullOrEmpty(itemId) || count <= 0) return;
        inventory.TryGetValue(itemId, out int cur);
        inventory[itemId] = cur + count;

        if (verboseLogging)
            Debug.Log($"[ItemManager] Added {count}x '{itemId}'. Now own {inventory[itemId]}.");

        OnInventoryChanged?.Invoke();
    }

    public void Add(ItemDefinition def, int count = 1)
    {
        if (def != null) Add(def.itemId, count);
    }

    public bool Remove(string itemId, int count = 1)
    {
        if (string.IsNullOrEmpty(itemId) || count <= 0) return false;
        if (!inventory.TryGetValue(itemId, out int cur) || cur < count) return false;

        cur -= count;
        if (cur <= 0) inventory.Remove(itemId);
        else inventory[itemId] = cur;

        if (verboseLogging)
            Debug.Log($"[ItemManager] Removed {count}x '{itemId}'. Now own {GetCount(itemId)}.");

        OnInventoryChanged?.Invoke();
        return true;
    }

    // ==================================================================
    //  USE
    // ==================================================================

    /// <summary>
    /// Use one item of the given type. Returns true on success.
    /// The item is only consumed if the effect actually succeeds.
    /// </summary>
    public bool UseItem(string itemId,
                        Vector3? targetWorldPos = null,
                        Unit targetUnit = null,
                        HexCoord? targetCell = null)
    {
        var def = GetDefinition(itemId);
        if (def == null) { Debug.LogWarning($"[ItemManager] Unknown itemId '{itemId}'"); return false; }
        if (GetCount(itemId) <= 0) { Debug.Log($"[ItemManager] No '{itemId}' in inventory."); return false; }

        var effect = ItemEffectRegistry.Get(def.effectType);
        if (effect == null) return false;

        if (!effect.CanUse(def))
        {
            Debug.Log($"[ItemManager] Cannot use '{itemId}' right now.");
            return false;
        }

        if (!effect.Use(def, targetWorldPos, targetUnit, targetCell))
        {
            Debug.Log($"[ItemManager] Effect for '{itemId}' failed.");
            return false;
        }

        Remove(itemId, 1);
        return true;
    }

    public bool UseItem(ItemDefinition def,
                        Vector3? targetWorldPos = null,
                        Unit targetUnit = null,
                        HexCoord? targetCell = null)
    {
        return def != null && UseItem(def.itemId, targetWorldPos, targetUnit, targetCell);
    }
}