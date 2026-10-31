using System;
using UnityEngine;

[Serializable]
public class UnitInstance
{
    public UnitDefinition definition;

    // ★ NEW: Stores the asset name so we can save/load it as a string
    public string definitionName;

    public int currentHP;
    public int maxHP;

    public bool IsAlive => currentHP > 0;
    public bool IsKing => definition != null && definition.isKing;

    public UnitInstance(UnitDefinition def)
    {
        definition = def;
        if (def != null) definitionName = def.name; // Save the name!
        SyncWithDefinition();
    }

    [ContextMenu("Sync HP with Definition")]
    public void SyncWithDefinition()
    {
        if (definition != null)
        {
            definitionName = definition.name;
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