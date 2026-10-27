using UnityEngine;

[CreateAssetMenu(fileName = "Unit_", menuName = "HexHero/Unit Definition")]
public class UnitDefinition : ScriptableObject
{
    [Header("Identity")]
    public string unitName;
    public TroopType troopType;
    public Sprite icon;
    [Tooltip("3D model prefab used when this troop is spawned on the board. " +
             "Leave empty to fall back to Resources/Prefabs/UnitPrefab.")]
    public GameObject unitPrefab;
    [TextArea] public string description;
    public UnitTag[] tags;
    public Rarity rarity;
    public int tier = 1;

    [Header("Base Stats")]
    public int maxHP = 10;
    public int defense = 0;
    public int attack = 2;
    public int movementRange = 3;
    public int attackRange = 1;
    [Range(0, 100)] public int critChance = 10;
    [Range(0, 100)] public int dodgeChance = 5;

    [Header("Splash / AOE")]
    public SplashMode splashMode = SplashMode.None;
    public int splashRadius = 0;
    public int splashValue = 0;

    [Header("Economy")]
    public int upkeepCost = 10;
    public int recruitCost = 50;

    [Header("Special Ability")]
    public SpecialAbility ability = SpecialAbility.None;
    public int abilityValue = 0;

    [Header("Summoning")]
    public int summonAtBattleStart = 0;
    public int summonEveryNTurns = 0;
    public UnitDefinition summonTemplate;

    [Header("King & Persistence")]
    public bool isKing = false;
    public bool countsTowardUnitCap = true;
    public bool returnsDamaged = true;

    public bool HasTag(UnitTag t)
    {
        if (tags == null) return false;
        foreach (var tag in tags) if (tag == t) return true;
        return false;
    }
}