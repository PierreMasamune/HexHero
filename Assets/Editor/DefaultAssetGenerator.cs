#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public static class DefaultAssetGenerator
{
    private const string UnitsDir = "Assets/Resources/Units";
    private const string EnemiesDir = "Assets/Resources/Enemies";

    [MenuItem("Tools/Hex Hero/Generate Default Assets")]
    public static void GenerateAll()
    {
        Directory.CreateDirectory(UnitsDir);
        Directory.CreateDirectory(EnemiesDir);

        GenerateTroops();
        GenerateEnemies();
        GenerateKing();
        GenerateBasicMinion();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Hex Hero] Assets generated in Resources/Units and Resources/Enemies.");
    }

    // ==================================================================
    //  TROOPS
    // ==================================================================
    private static void CreateTroop(
        TroopType type, string name, int tier, Rarity rarity, UnitTag[] tags,
        int hp, int defense, int atk, int move, int range, int crit, int dodge,
        int upkeep, int cost,
        SpecialAbility ability = SpecialAbility.None, int abilityValue = 0,
        SplashMode splash = SplashMode.None, int splashRadius = 0, int splashValue = 0)
    {
        string path = $"{UnitsDir}/{type}.asset";
        var def = AssetDatabase.LoadAssetAtPath<UnitDefinition>(path);
        if (def == null)
        {
            def = ScriptableObject.CreateInstance<UnitDefinition>();
            AssetDatabase.CreateAsset(def, path);
        }

        def.unitName = name;
        def.troopType = type;
        def.tier = tier;
        def.rarity = rarity;
        def.tags = tags;
        def.maxHP = hp;
        def.defense = defense;
        def.attack = atk;
        def.movementRange = move;
        def.attackRange = range;
        def.critChance = crit;
        def.dodgeChance = dodge;
        def.upkeepCost = upkeep;
        def.recruitCost = cost;
        def.ability = ability;
        def.abilityValue = abilityValue;
        def.splashMode = splash;
        def.splashRadius = splashRadius;
        def.splashValue = splashValue;
        def.isKing = false;
        def.countsTowardUnitCap = true;
        def.returnsDamaged = true;

        EditorUtility.SetDirty(def);
    }

    private static void GenerateTroops()
    {
        // ----- TIER 1 (Common) -----
        CreateTroop(TroopType.Barbarian, "Barbarian", 1, Rarity.Common,
            new[] { UnitTag.Melee, UnitTag.Human },
            10, 1, 3, 3, 1, 10, 5, 10, 43);

        CreateTroop(TroopType.Squire, "Squire", 1, Rarity.Common,
            new[] { UnitTag.Melee, UnitTag.Tank, UnitTag.Human },
            8, 2, 2, 2, 1, 5, 5, 10, 76);

        CreateTroop(TroopType.Slinger, "Slinger", 1, Rarity.Common,
            new[] { UnitTag.Ranged, UnitTag.Human },
            4, 0, 2, 2, 2, 10, 5, 10, 54);

        CreateTroop(TroopType.Apprentice, "Apprentice", 1, Rarity.Common,
            new[] { UnitTag.Mage, UnitTag.Ranged, UnitTag.Human },
            5, 0, 2, 2, 2, 5, 5, 10, 67,
            splash: SplashMode.FlatValue, splashRadius: 1, splashValue: 1);

        CreateTroop(TroopType.Cleric, "Cleric", 1, Rarity.Common,
            new[] { UnitTag.Support, UnitTag.Human },
            5, 0, 1, 2, 2, 5, 5, 10, 69,
            ability: SpecialAbility.HealAlly, abilityValue: 1);

        CreateTroop(TroopType.Thief, "Thief", 1, Rarity.Common,
            new[] { UnitTag.Melee, UnitTag.Rogue, UnitTag.GlassCannon, UnitTag.Human },
            5, 0, 4, 4, 1, 10, 10, 10, 51);

        CreateTroop(TroopType.Horseman, "Horseman", 1, Rarity.Common,
            new[] { UnitTag.Melee, UnitTag.Fast, UnitTag.Human },
            8, 1, 3, 5, 1, 5, 10, 20, 103);

        CreateTroop(TroopType.Peasant, "Peasant", 1, Rarity.Common,
            new[] { UnitTag.Melee, UnitTag.Human },
            4, 0, 1, 1, 1, 5, 5, 0, 50,
            ability: SpecialAbility.ExtraGoldPerBattle, abilityValue: 10);

        CreateTroop(TroopType.Herd, "Herd", 1, Rarity.Common,
            new[] { UnitTag.Summoner, UnitTag.Ranged, UnitTag.Human },
            4, 0, 1, 2, 2, 5, 5, 10, 60,
            ability: SpecialAbility.SummonMinion);

        // ----- TIER 2 (Rare) -----
        CreateTroop(TroopType.Berserk, "Berserk", 2, Rarity.Rare,
            new[] { UnitTag.Melee, UnitTag.Human },
            20, 2, 8, 3, 1, 15, 5, 20, 107);

        CreateTroop(TroopType.Knight, "Knight", 2, Rarity.Rare,
            new[] { UnitTag.Melee, UnitTag.Tank, UnitTag.Human },
            15, 4, 5, 2, 1, 5, 5, 20, 152);

        CreateTroop(TroopType.Longbow, "Longbow", 2, Rarity.Rare,
            new[] { UnitTag.Ranged, UnitTag.Human },
            8, 0, 8, 2, 3, 10, 5, 20, 129);

        CreateTroop(TroopType.Mage, "Mage", 2, Rarity.Rare,
            new[] { UnitTag.Mage, UnitTag.Ranged, UnitTag.Human },
            10, 0, 2, 2, 2, 5, 5, 20, 173,
            splash: SplashMode.FlatValue, splashRadius: 2, splashValue: 3);

        CreateTroop(TroopType.Priest, "Priest", 2, Rarity.Rare,
            new[] { UnitTag.Support, UnitTag.Human },
            10, 0, 3, 2, 2, 5, 5, 20, 155,
            ability: SpecialAbility.HealAlly, abilityValue: 3);

        CreateTroop(TroopType.Rogue, "Rogue", 2, Rarity.Rare,
            new[] { UnitTag.Melee, UnitTag.Rogue, UnitTag.GlassCannon, UnitTag.Human },
            10, 0, 10, 4, 1, 15, 15, 20, 132);

        CreateTroop(TroopType.Cavalry, "Cavalry", 2, Rarity.Rare,
            new[] { UnitTag.Melee, UnitTag.Fast, UnitTag.Human },
            15, 3, 10, 5, 1, 5, 10, 30, 209);

        CreateTroop(TroopType.Farmer, "Farmer", 2, Rarity.Rare,
            new[] { UnitTag.Melee, UnitTag.Human },
            10, 0, 1, 1, 1, 5, 5, 0, 100,
            ability: SpecialAbility.ExtraGoldPerBattle, abilityValue: 10);

        CreateTroop(TroopType.BeastHandler, "BeastHandler", 2, Rarity.Rare,
            new[] { UnitTag.Summoner, UnitTag.Ranged, UnitTag.Human },
            10, 1, 2, 2, 2, 5, 5, 20, 140,
            ability: SpecialAbility.SummonMinion);

        // ----- TIER 3 (Epic except Assassin) -----
        CreateTroop(TroopType.Chieftain, "Chieftain", 3, Rarity.Epic,
            new[] { UnitTag.Melee, UnitTag.Human },
            50, 3, 15, 3, 1, 15, 5, 40, 413,
            ability: SpecialAbility.ExtraGoldPerKill, abilityValue: 5);

        CreateTroop(TroopType.Paladin, "Paladin", 3, Rarity.Epic,
            new[] { UnitTag.Melee, UnitTag.Tank, UnitTag.Human },
            50, 10, 10, 2, 1, 5, 5, 40, 613,
            ability: SpecialAbility.SelfHeal);

        CreateTroop(TroopType.Crossbow, "Crossbow", 3, Rarity.Epic,
            new[] { UnitTag.Ranged, UnitTag.Human },
            20, 0, 20, 2, 4, 5, 5, 40, 824);

        CreateTroop(TroopType.Archmage, "Archmage", 3, Rarity.Epic,
            new[] { UnitTag.Mage, UnitTag.Ranged, UnitTag.Human },
            25, 0, 15, 2, 3, 5, 5, 40, 725,
            splash: SplashMode.EqualToAttack, splashRadius: 2, splashValue: 0);

        CreateTroop(TroopType.Cardinal, "Cardinal", 3, Rarity.Epic,
            new[] { UnitTag.Support, UnitTag.Human },
            30, 0, 5, 2, 2, 5, 5, 40, 689,
            ability: SpecialAbility.AoeHeal, abilityValue: 5);

        CreateTroop(TroopType.Assassin, "Assassin", 3, Rarity.Legendary,
            new[] { UnitTag.Melee, UnitTag.Rogue, UnitTag.GlassCannon, UnitTag.Human },
            20, 0, 20, 4, 1, 20, 10, 40, 521,
            ability: SpecialAbility.Instakill, abilityValue: 15);

        CreateTroop(TroopType.Cataphract, "Cataphract", 3, Rarity.Epic,
            new[] { UnitTag.Melee, UnitTag.Fast, UnitTag.Human },
            40, 5, 30, 5, 1, 5, 10, 75, 1582,
            ability: SpecialAbility.DoubleMove);

        CreateTroop(TroopType.Merchant, "Merchant", 3, Rarity.Epic,
            new[] { UnitTag.Melee, UnitTag.Merchant, UnitTag.Human },
            15, 0, 1, 1, 1, 5, 5, 0, 300,
            ability: SpecialAbility.ExtraGoldPerTurn, abilityValue: 3);

        CreateTroop(TroopType.MonsterHunter, "MonsterHunter", 3, Rarity.Epic,
            new[] { UnitTag.Summoner, UnitTag.Ranged, UnitTag.Human },
            25, 2, 8, 2, 3, 5, 5, 50, 500,
            ability: SpecialAbility.SummonMinion);
    }

    // ==================================================================
    //  ENEMIES
    // ==================================================================
    private static void CreateEnemy(
        EnemyType type, string name, int tier, Rarity rarity,
        int hp, int defense, int atk, int move, int range, int killValue,
        SplashMode splash = SplashMode.None, int splashRadius = 0, int splashValue = 0,
        SpecialAbility ability = SpecialAbility.None, int abilityValue = 0,
        bool isBoss = false)
    {
        string path = $"{EnemiesDir}/{type}.asset";
        var def = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
        if (def == null)
        {
            def = ScriptableObject.CreateInstance<EnemyDefinition>();
            AssetDatabase.CreateAsset(def, path);
        }

        def.enemyName = name;
        def.enemyType = type;
        def.tier = tier;
        def.rarity = rarity;
        def.maxHP = hp;
        def.defense = defense;
        def.attack = atk;
        def.movementRange = move;
        def.attackRange = range;
        def.critChance = 5;
        def.dodgeChance = 5;
        def.killValue = killValue;
        def.splashMode = splash;
        def.splashRadius = splashRadius;
        def.splashValue = splashValue;
        def.ability = ability;
        def.abilityValue = abilityValue;
        def.isBoss = isBoss;

        EditorUtility.SetDirty(def);
    }

    private static void GenerateEnemies()
    {
        // ----- TIER 1 -----
        CreateEnemy(EnemyType.GoblinMelee, "Goblin Melee", 1, Rarity.Common, 4, 0, 2, 3, 1, 10);
        CreateEnemy(EnemyType.GoblinArcher, "Goblin Archer", 1, Rarity.Common, 3, 0, 1, 2, 2, 10);
        CreateEnemy(EnemyType.GoblinMage, "Goblin Mage", 1, Rarity.Common, 3, 0, 2, 2, 2, 10,
                    splash: SplashMode.FlatValue, splashRadius: 2, splashValue: 1);
        CreateEnemy(EnemyType.GoblinRider, "Goblin Rider", 1, Rarity.Common, 4, 0, 2, 5, 1, 15);
        CreateEnemy(EnemyType.Wolf, "Wolf", 1, Rarity.Common, 3, 0, 3, 4, 1, 5);
        CreateEnemy(EnemyType.MeleeBandit, "Melee Bandit", 1, Rarity.Common, 5, 0, 3, 3, 1, 10);
        CreateEnemy(EnemyType.ArcherBandit, "Archer Bandit", 1, Rarity.Common, 5, 0, 2, 2, 2, 10);
        CreateEnemy(EnemyType.Bear, "Bear", 1, Rarity.Rare, 20, 0, 5, 3, 1, 50);

        // ----- TIER 2 -----
        CreateEnemy(EnemyType.ElderGoblinMelee, "Elder Goblin Melee", 2, Rarity.Rare, 10, 1, 5, 3, 1, 10);
        CreateEnemy(EnemyType.ElderGoblinArcher, "Elder Goblin Archer", 2, Rarity.Rare, 8, 1, 3, 2, 2, 10);
        CreateEnemy(EnemyType.ElderGoblinMage, "Elder Goblin Mage", 2, Rarity.Rare, 8, 1, 10, 2, 2, 20,
                    splash: SplashMode.FlatValue, splashRadius: 2, splashValue: 3);

        // ----- TIER 3 -----
        CreateEnemy(EnemyType.AncientGoblinMelee, "Ancient Goblin Melee", 3, Rarity.Epic, 30, 3, 15, 3, 1, 20);
        CreateEnemy(EnemyType.AncientGoblinArcher, "Ancient Goblin Archer", 3, Rarity.Epic, 20, 3, 10, 2, 2, 20);
        CreateEnemy(EnemyType.AncientGoblinMage, "Ancient Goblin Mage", 3, Rarity.Epic, 20, 3, 20, 2, 2, 50,
                    splash: SplashMode.FlatValue, splashRadius: 2, splashValue: 5);

        // ----- SPECIAL -----
        CreateEnemy(EnemyType.EnemyNecromancer, "Enemy Necromancer", 2, Rarity.Epic, 20, 2, 8, 2, 3, 100,
                    ability: SpecialAbility.SummonMinion);
        CreateEnemy(EnemyType.Boss, "Boss", 3, Rarity.Legendary, 400, 6, 25, 2, 4, 500,
                    ability: SpecialAbility.SummonMinion, isBoss: true);
    }

    // ==================================================================
    //  SPECIAL UNITS
    // ==================================================================
    private static void GenerateKing()
    {
        string path = $"{UnitsDir}/{TroopType.King}.asset";
        var def = AssetDatabase.LoadAssetAtPath<UnitDefinition>(path);
        if (def == null)
        {
            def = ScriptableObject.CreateInstance<UnitDefinition>();
            AssetDatabase.CreateAsset(def, path);
        }

        def.unitName = "King";
        def.troopType = TroopType.King;
        def.tier = 3;
        def.rarity = Rarity.Legendary;
        def.tags = new[] { UnitTag.Royal, UnitTag.Melee, UnitTag.Human };
        def.maxHP = 50;
        def.defense = 5;
        def.attack = 5;
        def.movementRange = 2;
        def.attackRange = 1;
        def.critChance = 15;
        def.dodgeChance = 5;
        def.upkeepCost = 0;
        def.recruitCost = 0;
        def.isKing = true;
        def.countsTowardUnitCap = false;
        def.returnsDamaged = true;

        EditorUtility.SetDirty(def);
    }

    private static void GenerateBasicMinion()
    {
        string path = $"{UnitsDir}/BasicMinion.asset";
        var def = AssetDatabase.LoadAssetAtPath<UnitDefinition>(path);
        if (def == null)
        {
            def = ScriptableObject.CreateInstance<UnitDefinition>();
            AssetDatabase.CreateAsset(def, path);
        }

        def.unitName = "Basic Minion";
        def.tier = 0;
        def.rarity = Rarity.Common;
        def.tags = new[] { UnitTag.Melee, UnitTag.Undead };
        def.maxHP = 3;
        def.defense = 0;
        def.attack = 2;
        def.movementRange = 2;
        def.attackRange = 1;
        def.critChance = 5;
        def.dodgeChance = 5;
        def.upkeepCost = 0;
        def.recruitCost = 0;
        def.isKing = false;
        def.countsTowardUnitCap = false;   // minions never count
        def.returnsDamaged = false;   // destroyed at battle end

        EditorUtility.SetDirty(def);
    }
}
#endif