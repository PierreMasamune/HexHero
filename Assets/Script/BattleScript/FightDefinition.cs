using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Fight_", menuName = "HexHero/Fight Definition")]
public class FightDefinition : ScriptableObject
{
    [Header("Enemies")]
    [Tooltip("Add enemies and their spawn coordinates here.")]
    public List<SpawnEntry> enemies = new();

    [Header("Environment (Terrain)")]
    [Tooltip("Painting terrain here will override the scene's default terrain when this battle loads.")]
    public List<TerrainCellEntry> terrain = new();
}