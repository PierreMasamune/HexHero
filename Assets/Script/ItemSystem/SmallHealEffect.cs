using System.Collections.Generic;
using UnityEngine;

public class SmallHealEffect : ItemEffect
{
    public override bool CanUse(ItemDefinition def, Unit caster = null) => true;

    public override bool Use(ItemDefinition def, Vector3? targetWorldPos = null,
                             Unit targetUnit = null, HexCoord? targetCell = null)
    {
        if (!targetCell.HasValue)
        {
            Debug.LogWarning("[Heal Effect] No target cell provided.");
            return false;
        }

        var grid = HexGridManager.Instance;
        if (grid == null) return false;

        HexCoord center = targetCell.Value;
        int healAmount = Mathf.Max(1, def.effectValue); // e.g. 5
        int radius = def.effectValue2;                  // 0 = single target, 1 = AOE, 99 = whole map

        int healedCount = 0;

        // Find every friendly unit within the radius
        foreach (var cell in grid.AllCells())
        {
            var u = grid.GetUnit(cell);
            if (u == null || !u.IsAlive) continue;
            if (u.Faction != Faction.Player) continue; // only heal players

            int dist = HexCoord.Distance(center, cell);
            if (dist <= radius)
            {
                u.Heal(healAmount);
                healedCount++;
            }
        }

        Debug.Log($"[Heal Effect] Healed {healedCount} unit(s) for {healAmount} HP each.");
        return true;
    }
}