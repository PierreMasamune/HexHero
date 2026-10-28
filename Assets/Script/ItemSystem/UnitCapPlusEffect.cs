using UnityEngine;

public class UnitCapPlusEffect : ItemEffect
{
    public override bool CanUse(ItemDefinition def, Unit caster = null) => true;

    public override bool Use(ItemDefinition def, Vector3? targetWorldPos = null,
                             Unit targetUnit = null, HexCoord? targetCell = null)
    {
        var inv = InventoryManager.Instance;
        if (inv == null) return false;

        int amount = Mathf.Max(1, def.effectValue);   // default 1 from the asset
        inv.AddUnitCap(amount);

        Debug.Log($"[Unit Cap+] Permanent +{amount} to unit cap. Now {inv.UnitCap}.");
        return true;
    }
}