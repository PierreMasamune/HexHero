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

        // Read from Inspector
        int healAmount = def.effectValue > 0 ? def.effectValue : 9999;
        int radius = def.effectValue2;

        Debug.Log($"[Heal DEBUG] Detonated at {center}. Searching for Player units within radius {radius} to heal for {healAmount} HP.");

        int healedCount = 0;

        foreach (var cell in grid.AllCells())
        {
            var u = grid.GetUnit(cell);

            // Check what is on the tile
            if (u != null)
            {
                int dist = HexCoord.Distance(center, cell);
                Debug.Log($"[Heal DEBUG] Found '{u.UnitName}' (Faction: {u.Faction}) at {cell}. Distance from center: {dist}. HP: {u.CurrentHP}/{u.MaxHP}");

                if (!u.IsAlive) continue;
                if (u.Faction != Faction.Player) continue;

                if (dist <= radius)
                {
                    int beforeHP = u.CurrentHP;
                    u.Heal(healAmount);
                    int afterHP = u.CurrentHP;

                    Debug.Log($"[Heal DEBUG] -> Healing {u.UnitName}! HP before: {beforeHP}, HP after: {afterHP}");
                    healedCount++;
                }
            }
        }

        Debug.Log($"[Heal] Finished. Healed {healedCount} unit(s).");
        return true;
    }
}