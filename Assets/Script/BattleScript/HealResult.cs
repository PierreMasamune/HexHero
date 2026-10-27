using System.Collections.Generic;

public class HealResult
{
    public Unit Healer;
    public Unit PrimaryTarget;
    public int PrimaryHealAmount;
    public List<AoeHealHit> AoeHeals = new();

    public bool AnyHeal => PrimaryHealAmount > 0 || AoeHeals.Count > 0;
}

public class AoeHealHit
{
    public Unit Target;
    public int Healed;
}