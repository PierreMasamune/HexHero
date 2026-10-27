using UnityEngine;

[CreateAssetMenu(fileName = "Unit_", menuName = "HexHero/Unit Definition")]
public class UnitDefinition : ScriptableObject
{
    [Header("Identity")]
    public string unitName;
    public TroopType troopType;
    public Sprite icon;
    [TextArea] public string description;
    public UnitTag[] tags;
    public Rarity rarity;
    public int tier = 1;

    [Header("Base Stats")]
    public int maxHP = 10;
    public int defense = 0;    // flat reduction, cannot go below 1 dmg
    public int attack = 2;
    public int movementRange = 3;
    public int attackRange = 1;
    [Range(0, 100)] public int critChance = 10;
    [Range(0, 100)] public int dodgeChance = 5;

    [Header("Splash / AOE")]
    public SplashMode splashMode = SplashMode.None;
    public int splashRadius = 0;
    public int splashValue = 0;   // flat dmg OR percent, based on splashMode

    [Header("Economy")]
    public int upkeepCost = 10;
    public int recruitCost = 50;

    [Header("Special Ability")]
    public SpecialAbility ability = SpecialAbility.None;
    public int abilityValue = 0;   // heal amount, gold amount, etc.

    [Header("Summoning")]
    public int summonAtBattleStart = 0;
    public int summonEveryNTurns = 0;   // 0 = only at start
    public UnitDefinition summonTemplate;            // null = spawns a BasicMinion by default

    [Header("King & Persistence")]
    public bool isKing = false;
    public bool countsTowardUnitCap = true;
    public bool returnsDamaged = true;

    public bool HasTag(UnitTag t)
    {
        foreach (var tag in tags) if (tag == t) return true;
        return false;
    }
}