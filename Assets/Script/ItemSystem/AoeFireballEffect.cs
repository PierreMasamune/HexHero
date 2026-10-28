using System.Collections.Generic;
using UnityEngine;

public class AoeFireballEffect : ItemEffect
{
    public override bool CanUse(ItemDefinition def, Unit caster = null)
    {
        // Fireball is battle-only, and only on the player's turn
        if (TurnManager.Instance == null || TurnManager.Instance.CurrentState != TurnState.PlayerTurn)
        {
            Debug.LogWarning("[Fireball] Can only be used during the player's turn.");
            return false;
        }
        return true;
    }

    public override bool Use(ItemDefinition def, Vector3? targetWorldPos = null,
                             Unit targetUnit = null, HexCoord? targetCell = null)
    {
        if (!targetCell.HasValue)
        {
            Debug.LogWarning("[Fireball] No target cell provided.");
            return false;
        }

        var grid = HexGridManager.Instance;
        if (grid == null) return false;

        HexCoord center = targetCell.Value;

        // Based on our updated ItemDefinition:
        int radius = def.effectValue;         // e.g. 2 (Radius of the explosion)
        int centerDmg = def.effectValue2;     // e.g. 10 (Damage in the center)

        // The outer ring takes 75% of the center damage
        int ringDmg = Mathf.Max(1, Mathf.RoundToInt(centerDmg * 0.75f));

        // Find every enemy within `radius` of the center
        var affected = new List<Unit>();
        foreach (var cell in grid.AllCells())
        {
            var u = grid.GetUnit(cell);
            if (u == null || !u.IsAlive) continue;
            if (u.Faction != Faction.Enemy) continue;              // only damage enemies
            int dist = HexCoord.Distance(center, cell);
            if (dist <= radius) affected.Add(u);
        }

        int hits = 0;
        foreach (var enemy in affected)
        {
            int dist = HexCoord.Distance(center, enemy.Cell);
            int dmg = (dist == 0) ? centerDmg : ringDmg;
            enemy.TakeDamage(dmg);
            hits++;
        }

        Debug.Log($"[Fireball] Detonated at {center}. Hit {hits} enemy/enemies. " +
                  $"Center {centerDmg}, ring {ringDmg}.");
        return true;
    }
}