using System;
using UnityEngine;

/// <summary>
/// A persistent inventory entry. Unlike Unit (the runtime battle object),
/// UnitInstance lives between fights and carries current HP across nodes.
/// </summary>
[Serializable]
public class UnitInstance
{
    public UnitDefinition definition;
    public int currentHP;
    public int maxHP;            // denormalized from definition for safety

    public bool IsAlive => currentHP > 0;
    public bool IsKing => definition != null && definition.isKing;

    public UnitInstance(UnitDefinition def)
    {
        definition = def;
        maxHP = def != null ? def.maxHP : 1;
        currentHP = maxHP;
    }

    public void TakeDamage(int amount)
    {
        currentHP = Mathf.Max(0, currentHP - amount);
    }

    public void Heal(int amount)
    {
        currentHP = Mathf.Min(maxHP, currentHP + amount);
    }

    public void FullHeal() => currentHP = maxHP;

    /// <summary>Called after a battle ends — clamps HP into valid range.</summary>
    public void SyncFromBattle(int hpAfterBattle)
    {
        currentHP = Mathf.Clamp(hpAfterBattle, 0, maxHP);
    }
}