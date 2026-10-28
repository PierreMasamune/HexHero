using System;
using UnityEngine;

[Serializable]
public class UnitInstance
{
    public UnitDefinition definition;

    // These are public, so you can edit them directly in the Inspector!
    public int currentHP;
    public int maxHP;

    public bool IsAlive => currentHP > 0;
    public bool IsKing => definition != null && definition.isKing;

    public UnitInstance(UnitDefinition def)
    {
        definition = def;
        SyncWithDefinition();
    }

    /// <summary>
    /// Pulls the max HP from the definition and heals the unit to full.
    /// </summary>
    [ContextMenu("Sync HP with Definition")]
    public void SyncWithDefinition()
    {
        if (definition != null)
        {
            maxHP = definition.maxHP;
            currentHP = maxHP;
        }
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

    public void SyncFromBattle(int hpAfterBattle)
    {
        currentHP = Mathf.Clamp(hpAfterBattle, 0, maxHP);
    }
}