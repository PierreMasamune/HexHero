using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for concrete item behavior. Each ItemEffectType has exactly one
/// ItemEffect subclass. ItemManager resolves the subclass by effectType.
/// </summary>
public abstract class ItemEffect
{
    public abstract bool CanUse(ItemDefinition def, Unit caster = null);
    public abstract bool Use(ItemDefinition def, Vector3? targetWorldPos = null,
                             Unit targetUnit = null, HexCoord? targetCell = null);
}

/// <summary>
/// Static registry that maps ItemEffectType ? concrete ItemEffect instance.
/// Every effect is stateless, so one instance per type is fine.
/// </summary>
public static class ItemEffectRegistry
{
    private static readonly Dictionary<ItemEffectType, ItemEffect> registry = new()
    {
        { ItemEffectType.AoeFireball,    new AoeFireballEffect() },
        { ItemEffectType.Heal,           new HealEffect() },
        { ItemEffectType.DiscountEffect, new DiscountEffect() },
        { ItemEffectType.UnitCapPlus,    new UnitCapPlusEffect() },
    };

    public static ItemEffect Get(ItemEffectType type)
    {
        if (registry.TryGetValue(type, out var effect)) return effect;
        Debug.LogError($"[ItemEffectRegistry] No effect registered for {type}");
        return null;
    }
}