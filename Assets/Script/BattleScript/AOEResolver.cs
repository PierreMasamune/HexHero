using System.Collections.Generic;
using UnityEngine;

public static class AoeResolver
{
    /// <summary>
    /// Computes raw pre-dodge/pre-armor splash damage for a unit's splash mode.
    ///   FlatValue      — splashValue flat (matches the [N] notation in the troop table)
    ///   EqualToAttack  — same as attacker.Attack (Archmage)
    ///   PercentOfAttack — attack × splashValue% (use splashValue = 75 for the "75% to ring" rule)
    /// </summary>
    public static int ComputeSplashDamage(Unit attacker, SplashMode mode, int splashValue)
    {
        if (attacker == null) return 0;
        return mode switch
        {
            SplashMode.FlatValue => Mathf.Max(1, splashValue),
            SplashMode.EqualToAttack => Mathf.Max(1, attacker.Attack),
            SplashMode.PercentOfAttack => Mathf.Max(1, Mathf.RoundToInt(attacker.Attack * (splashValue / 100f))),
            _ => 0
        };
    }

    /// <summary>
    /// Returns all living units within `splashRadius` hex-distance of `targetCell`,
    /// excluding the primary target and the attacker. Includes both factions (friendly fire ON).
    /// </summary>
    public static List<Unit> FindSplashTargets(
        HexGridManager grid,
        HexCoord targetCell,
        int splashRadius,
        Unit primaryTarget,
        Unit attacker)
    {
        var result = new List<Unit>();
        if (grid == null || splashRadius <= 0) return result;

        for (int dq = -splashRadius; dq <= splashRadius; dq++)
        {
            int rMin = Mathf.Max(-splashRadius, -dq - splashRadius);
            int rMax = Mathf.Min(splashRadius, -dq + splashRadius);
            for (int dr = rMin; dr <= rMax; dr++)
            {
                if (dq == 0 && dr == 0) continue;    // skip primary target's cell
                var cell = new HexCoord(targetCell.q + dq, targetCell.r + dr);
                if (!grid.Contains(cell)) continue;

                var u = grid.GetUnit(cell);
                if (u == null || !u.IsAlive) continue;
                if (u == primaryTarget || u == attacker) continue;

                result.Add(u);
            }
        }
        return result;
    }
}