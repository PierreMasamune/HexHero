using UnityEngine;

public class BigHealEffect : ItemEffect
{
    public override bool CanUse(ItemDefinition def, Unit caster = null) => true;

    public override bool Use(ItemDefinition def, Vector3? targetWorldPos = null,
                             Unit targetUnit = null, HexCoord? targetCell = null)
    {
        if (!targetCell.HasValue)
        {
            Debug.LogWarning("[Big Heal] No target cell provided.");
            return false;
        }

        var grid = HexGridManager.Instance;
        if (grid == null) return false;

        HexCoord center = targetCell.Value;

        // If effectValue is 0, default to full heal (9999). 
        // Otherwise, use the number from the asset.
        int healAmount = def.effectValue > 0 ? def.effectValue : 9999;

        // Use effectValue2 for radius. If it's 0 in the asset, default to 1 (a 7-hex cluster)
        int radius = def.effectValue2 > 0 ? def.effectValue2 : 1;

        int healedCount = 0;

        // Loop through the battlefield, not the inventory
        foreach (var cell in grid.AllCells())
        {
            var u = grid.GetUnit(cell);
            if (u == null || !u.IsAlive) continue;
            if (u.Faction != Faction.Player) continue; // Only heal player units

            int dist = HexCoord.Distance(center, cell);

            // If the unit is inside the AOE radius
            if (dist <= radius)
            {
                u.Heal(healAmount);
                healedCount++;
            }
        }

        Debug.Log($"[Big Heal] Detonated at {center}. Healed {healedCount} unit(s) for {healAmount} HP each.");
        return true;
    }
}