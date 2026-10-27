using UnityEngine;

public class DiscountEffect : ItemEffect
{
    public override bool CanUse(ItemDefinition def, Unit caster = null) => true;

    public override bool Use(ItemDefinition def, Vector3? targetWorldPos = null,
                             Unit targetUnit = null, HexCoord? targetCell = null)
    {
        if (ShopController.Instance == null)
        {
            Debug.LogWarning("[Discount Voucher] No shop is open — this item only works inside a shop.");
            return false;
        }

        ShopController.Instance.ActivateDiscountVoucher();
        Debug.Log("[Discount Voucher] Activated — the next purchase in this shop is FREE.");
        return true;
    }
}