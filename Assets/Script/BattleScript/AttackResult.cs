using System.Collections.Generic;

public class AttackResult
{
    public Unit Attacker;
    public Unit PrimaryTarget;
    public HexCoord TargetCell;

    public int PrimaryDamage;
    public bool PrimaryDodged;
    public bool PrimaryCritical;
    public bool PrimaryKilled;
    public bool PrimaryInstakilled;   // ★ NEW — true if the target died from the Instakill roll, not raw damage

    public List<SplashHit> SplashHits = new();

    public bool SplashHappened => SplashHits.Count > 0;

    public IEnumerable<Unit> AllTargetsHit
    {
        get
        {
            if (!PrimaryDodged && PrimaryTarget != null) yield return PrimaryTarget;
            foreach (var s in SplashHits) if (!s.Dodged) yield return s.Target;
        }
    }
}

public class SplashHit
{
    public Unit Target;
    public int Damage;
    public bool Dodged;
    public bool Killed;
}