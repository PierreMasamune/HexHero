using UnityEngine;

public enum Rarity { Common, Rare, Epic, Legendary }

public enum TroopType
{
    // T1
    Barbarian, Squire, Slinger, Apprentice, Cleric, Thief, Horseman, Peasant, Herd,
    // T2
    Berserk, Knight, Longbow, Mage, Priest, Rogue, Cavalry, Farmer, BeastHandler,
    // T3
    Chieftain, Paladin, Crossbow, Archmage, Cardinal, Assassin, Cataphract, Merchant, MonsterHunter,
    // Special
    King
}

public enum EnemyType
{
    // T1
    GoblinMelee, GoblinArcher, GoblinMage, GoblinRider, Wolf, MeleeBandit, ArcherBandit, Bear,
    // T2
    ElderGoblinMelee, ElderGoblinArcher, ElderGoblinMage,
    AncientGoblinMelee, AncientGoblinArcher, AncientGoblinMage,
    // Special
    EnemyNecromancer, Boss
}

public enum UnitTag
{
    Melee, Ranged, Mage, Support, Human, Beast, Summoner,
    Rogue, Merchant, Royal, Undead, Boss, GlassCannon, Tank, Fast
}

public enum SpecialAbility
{
    None,
    HealAlly,           // Cleric, Priest
    AoeHeal,            // Cardinal
    Splash,             // Apprentice, Mage, Archmage, Goblin Mage
    SummonMinion,       // Herd, BeastHandler, MonsterHunter, EnemyNecromancer, Boss
    SelfHeal,           // Paladin
    Instakill,          // Assassin
    DoubleMove,         // Cataphract
    ExtraGoldPerBattle, // Peasant, Farmer
    ExtraGoldPerKill,   // Chieftain
    ExtraGoldPerTurn,   // Merchant
}

public enum SplashMode
{
    None,
    FlatValue,      // ring takes a fixed flat damage (Apprentice, Mage, Goblin Mage)
    EqualToAttack,  // ring takes attack value too (Archmage)
    PercentOfAttack // ring takes N% of attack (your Mage AOE rule, default 75)
}