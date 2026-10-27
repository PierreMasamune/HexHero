using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Config")]
    [SerializeField] private int baseUnitCap = 6;

    // ===== Troops =====
    private readonly List<UnitInstance> units = new();
    public IReadOnlyList<UnitInstance> Units => units;

    // ===== Unit cap =====
    private int permanentCapBonus;
    public int UnitCap => baseUnitCap + permanentCapBonus;

    // ===== Events =====
    public event Action OnUnitsChanged;
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

    public void MergeSurvivors(IEnumerable<(UnitDefinition def, int hpAfterBattle)> survivors)
    {
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
    //  SAVE / LOAD (called by RunState)
    // ============================================================
    // Note: Items are now handled by ItemManager, so we only save Units here.
    public void RestoreFromRunState(List<UnitInstance> savedUnits, int savedCapBonus)
    {
        units.Clear();
        if (savedUnits != null) units.AddRange(savedUnits);

        permanentCapBonus = Mathf.Max(0, savedCapBonus);

        OnUnitsChanged?.Invoke();
        OnUnitCapChanged?.Invoke(UnitCap);
    }
}