using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ShopCatalog", menuName = "HexHero/Shop Catalog")]
public class ShopCatalog : ScriptableObject
{
    [Header("Available Troops")]
    public List<UnitDefinition> troopPool = new();

    [Header("Available Items")]
    public List<ItemDefinition> itemPool = new();

    [Header("Stock Sizes")]
    public int troopSlots = 2;
    public int itemSlots = 2;
}