using UnityEngine;

[CreateAssetMenu(fileName = "Enemy_", menuName = "HexHero/Enemy Definition")]
public class EnemyDefinition : ScriptableObject
{
    [Header("Identity")]
    public string enemyName;
    public EnemyType enemyType;
    public Sprite icon;
    [Tooltip("3D model prefab used when this enemy is spawned on the board. " +
             "Leave empty to fall back to Resources/Prefabs/UnitPrefab.")]
    public GameObject unitPrefab;
    [TextArea] public string description;
    public Rarity rarity;
    public int tier = 1;

    [Header("Base Stats")]
    public int maxHP = 4;
    public int defense = 0;
    public int attack = 2;
    public int movementRange = 3;
    public int attackRange = 1;
    [Range(0, 100)] public int critChance = 5;
    [Range(0, 100)] public int dodgeChance = 5;

    [Header("Splash / AOE")]
    public SplashMode splashMode = SplashMode.None;
    public int splashRadius = 0;
    public int splashValue = 0;

    [Header("Reward")]
    public int killValue = 10;

    [Header("Special Ability")]
    public SpecialAbility ability = SpecialAbility.None;
    public int abilityValue = 0;

    [Header("Summoning (Boss / Enemy Necromancer)")]
    public int summonAtBattleStart = 0;
    public int summonEveryNTurns = 0;
    public EnemyDefinition summonTemplate;

    [Header("Boss Mechanics")]
    public bool isBoss = false;
    [Tooltip("Percent damage taken while shielded (0 = not shielded).")]
    [Range(0, 100)] public int shieldPercent = 0;
}