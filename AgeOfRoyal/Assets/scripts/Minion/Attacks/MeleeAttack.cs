using UnityEngine;

[CreateAssetMenu(fileName = "___ - Melee attack", menuName = "Unit Attack/Melee Attack", order = 1)]
public class MeleeAttack : UnitAttack
{

    override public bool Use(UnitWithoutState owner)
    {
        if (owner.Target == null) return false;

        if ((owner.Combat.HitPoint.position - owner.Target.transform.position).magnitude > owner.Stats.hitRadius) return false;

        var finalDamage = owner.Stats.damage;
        if (owner.Target is Minion m && m.Type == bonusAgainst)
            finalDamage *= bonusMultiplier;

        return owner.Target.GetHit(finalDamage, owner);
    }
}
