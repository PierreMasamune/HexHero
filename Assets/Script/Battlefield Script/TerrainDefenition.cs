using UnityEngine;

public enum TerrainType { Plains, Forest, Mountain, Water, Vine }

[CreateAssetMenu(fileName = "Terrain_", menuName = "HexHero/Terrain Definition")]
public class TerrainDefinition : ScriptableObject
{
    [Header("Identity")]
    public string terrainName = "Plains";
    public TerrainType terrainType = TerrainType.Plains;
    public Sprite icon;
    [TextArea] public string description;

    [Header("Movement")]
    [Tooltip("Movement points to enter. -1 = impassable (water).")]
    public int movementCost = 1;

    [Header("Combat — applies to units standing ON this terrain")]
    [Range(0, 90)]
    [Tooltip("Percent damage reduction. 20 = incoming damage reduced by 20%.")]
    public int damageReductionPercent = 0;

    [Tooltip("Damage dealt to any unit that ENDS its movement on this cell. " +
             "Fires once per move, not per turn.")]
    public int entryDamage = 0;

    [Header("Visuals")]
    public Color color = Color.white;
    [Tooltip("Optional override material. Leave blank to auto-generate from color.")]
    public Material material;

    public bool IsPassable => movementCost >= 0;
}