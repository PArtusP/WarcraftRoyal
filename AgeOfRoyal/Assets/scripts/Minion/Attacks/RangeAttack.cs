using UnityEngine;

[CreateAssetMenu(fileName = "___ - Range attack", menuName = "Unit Actions/Unit Attacks/Range Attack", order = 2)]
public class RangeAttack : UnitAttack
{

    override public bool Use(UnitWithoutState owner)
    {
        Debug.Log("Use RangeAttack: " + owner.name + " Target: " + (owner.Target != null ? owner.Target.name : "null"));
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