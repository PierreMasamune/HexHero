using System.Collections.Generic;
using UnityEngine;

public static class SummonManager
{
    /// <summary>
    /// Called once at the start of each player turn and each enemy turn.
    /// Handles both one-time battle-start spawns and cooldown-based periodic spawns.
    /// </summary>
    public static void ApplyStartOfTurnSummons(List<Unit> units, int currentTurn, HexGridManager grid)
    {
        if (units == null || grid == null) return;

        // ★ Snapshot — SummonMinions calls RegisterSpawnedUnit which adds to
        //   `units` while we're iterating it. Iterate a copy to avoid
        //   InvalidOperationException: Collection was modified.
        var snapshot = new List<Unit>(units);

        foreach (var unit in snapshot)
        {
            if (unit == null || !unit.IsAlive) continue;
            if (unit.Definition == null) continue;
            if (unit.Definition.ability != SpecialAbility.SummonMinion) continue;

            // ---- One-time battle-start spawn (fires on TurnNumber 1) ----
            if (currentTurn == 1
                && !unit.HasSummonedAtStart
                && unit.Definition.summonAtBattleStart > 0)
            {
                int count = unit.Definition.summonAtBattleStart;
                int spawned = SummonMinions(unit, count, grid, currentTurn);
                unit.MarkSummonedAtStart(currentTurn);
                Debug.Log($"[Summon] {unit.UnitName} summoned {spawned}/{count} minions at battle start.");
            }

            // ---- Periodic spawn (cooldown) ----
            int interval = unit.Definition.summonEveryNTurns;
            if (interval > 0 && currentTurn - unit.LastSummonTurn >= interval)
            {
                int spawned = SummonMinions(unit, 1, grid, currentTurn);
                unit.MarkSummoned(currentTurn);
                Debug.Log($"[Summon] {unit.UnitName} periodic summon: {spawned}/1 minion(s) " +
                          $"on turn {currentTurn} (cooldown {interval}).");
            }
        }
    }

    /// <summary>
    /// Spawns up to `count` minions on empty cells adjacent to the summoner.
    /// Falls back to Resources/Units/BasicMinion if the summoner has no template.
    /// Returns how many were actually spawned.
    /// </summary>
    public static int SummonMinions(Unit summoner, int count, HexGridManager grid, int currentTurn)
    {
        if (summoner == null || !summoner.IsAlive || grid == null) return 0;
        if (count <= 0) return 0;

        var template = summoner.Definition != null ? summoner.Definition.summonTemplate : null;
        if (template == null)
            template = Resources.Load<UnitDefinition>("Units/BasicMinion");
        if (template == null)
        {
            Debug.LogError("[Summon] No summon template found. Assign one on the definition " +
                           "or ensure Resources/Units/BasicMinion.asset exists.");
            return 0;
        }

        // Collect empty adjacent cells
        var emptyNeighbors = new List<HexCoord>();
        foreach (var n in grid.ValidNeighbors(summoner.Cell))
        {
            if (grid.GetUnit(n) == null)
                emptyNeighbors.Add(n);
        }

        if (emptyNeighbors.Count == 0)
        {
            Debug.Log($"[Summon] {summoner.UnitName} has no empty adjacent cells. Summon blocked.");
            return 0;
        }

        int spawned = 0;
        for (int i = 0; i < count && emptyNeighbors.Count > 0; i++)
        {
            int idx = Random.Range(0, emptyNeighbors.Count);
            var cell = emptyNeighbors[idx];
            emptyNeighbors.RemoveAt(idx);

            var minion = UnitFactory.Spawn(template, summoner.Faction, grid.CellToWorld(cell));
            if (minion == null) continue;

            minion.MarkAsSummoned();
            grid.RegisterUnit(minion, cell);
            minion.MarkSummoned(currentTurn);

            if (TurnManager.Instance != null)
                TurnManager.Instance.RegisterSpawnedUnit(minion);   // adds to units list

            spawned++;
        }

        return spawned;
    }
}