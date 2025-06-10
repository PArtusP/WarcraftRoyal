using UnityEngine;

[CreateAssetMenu(fileName = "___ - Range attack", menuName = "Unit Attack/Range Attack", order = 2)]
public class RangeAttack : UnitAttack
{

    override public bool Use(UnitWithoutState owner)
    {
        if (owner.Target == null) return false;
        owner.Combat.PlayShootVfx(owner.Target);
        owner.Combat.PlayShootVfxClientRpc(owner.Target.NetworkObjectId);

        if ((owner.Combat.HitPoint.position - owner.Target.transform.position).magnitude > owner.Stats.hitRadius) return false;

        var finalDamage = owner.Stats.damage;
        if (owner.Target is Minion m && m.Type == bonusAgainst)
            finalDamage *= bonusMultiplier;

        return owner.Target.GetHit(finalDamage, owner);
    }
}