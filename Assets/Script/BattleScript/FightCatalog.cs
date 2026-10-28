using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FightCatalog", menuName = "HexHero/Fight Catalog")]
public class FightCatalog : ScriptableObject
{
    [Header("Tier 1 (Easy)")]
    public List<FightDefinition> tier1Fights = new();

    [Header("Tier 2 (Medium)")]
    public List<FightDefinition> tier2Fights = new();

    [Header("Tier 3 (Hard)")]
    public List<FightDefinition> tier3Fights = new();

    /// <summary>Gets a random fight based on the exact tier requested by the map node.</summary>
    public FightDefinition GetRandomFight(int tier)
    {
        List<FightDefinition> pool;

        if (tier == 2) pool = tier2Fights;
        else if (tier >= 3) pool = tier3Fights;
        else pool = tier1Fights;

        // Fallback if the selected tier is empty
        if (pool == null || pool.Count == 0) pool = tier1Fights;
        if (pool == null || pool.Count == 0) return null;

        return pool[Random.Range(0, pool.Count)];
    }
}