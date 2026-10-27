using UnityEngine;

public enum ItemUseContext
{
    Anywhere,   // usable on the map, in shop, in battle (during your turn)
    BattleOnly, // only usable during a battle (Fireball)
    MapOnly     // only usable outside battle (Discount Voucher might be one of these)
}

public enum ItemEffectType
{
    AoeFireball,
    Heal,             
    DiscountEffect,
    UnitCapPlus,
}

[CreateAssetMenu(fileName = "Item_", menuName = "HexHero/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    [Header("Identity")]
    public string itemName;
    public string itemId;          // unique stable id, e.g. "fireball", "big_potion"
    public Sprite icon;
    public GameObject itemPrefab;      // optional visual (e.g., fireball projectile prefab)
    [TextArea] public string description;
    public Rarity rarity = Rarity.Common;

    [Header("Economy")]
    public int baseCost = 50;    // shop buy price
    public int sellValue = 25;    // shop sell price (typically half)

    [Header("Effect")]
    public ItemEffectType effectType = ItemEffectType.AoeFireball;
    public ItemUseContext useContext = ItemUseContext.Anywhere;
    


    [Header("Effect Parameters")]
    [Tooltip("Primary effect value. Fireball: Radius (e.g., 2). Heal: Heal Amount.")]
    public int effectValue = 10;
    [Tooltip("Secondary effect value. Fireball: Center Damage (e.g., 10). Heal: Radius.")]
    public int effectValue2 = 10; 
    [Tooltip("Does this item need a target? (Fireball = true, Potion = true, Unit Cap+ = false)")]
    public bool requiresTarget = true;

    [Header("Stocking")]
    [Tooltip("Relative weight when picking items from the ShopCatalog. Higher = more likely.")]
    public int shopWeight = 100;
}