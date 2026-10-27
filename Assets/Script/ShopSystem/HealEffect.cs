using UnityEngine;

public class HealEffect : ItemEffect
{
    public override bool CanUse(ItemDefinition def, Unit caster = null) => true;

    public override bool Use(ItemDefinition def, Vector3? targetWorldPos = null,
                             Unit targetUnit = null, HexCoord? targetCell = null)
    {
        if (!targetCell.HasValue)
        {
            Debug.LogWarning("[Heal] No target cell provided.");
            return false;
        }

        var grid = HexGridManager.Instance;
        if (grid == null) return false;

        HexCoord center = targetCell.Value;

        // EFFECT VALUE 1: Heal Amount
        // If it's 0 or less, treat it as a "Full Heal" (9999)
        int healAmount = def.effectValue > 0 ? def.effectValue : 9999;

        // EFFECT VALUE 2: Radius
        // 0 = single target, 1 = 7-hex cluster, 99 = whole map
        int radius = def.effectValue2;

        int healedCount = 0;

        // Loop through every tile on the battlefield
        foreach (var cell in grid.AllCells())
        {
            var u = grid.GetUnit(cell);
            if (u == null || !u.IsAlive) continue;
            if (u.Faction != Faction.Player) continue; // Only heal players

            int dist = HexCoord.Distance(center, cell);

            if (dist <= radius)
            {
                u.Heal(healAmount);
                healedCount++;
            }
        }

        Debug.Log($"[Heal] Detonated at {center}. Healed {healedCount} unit(s) for {healAmount} HP each.");
        return true;
    }
}