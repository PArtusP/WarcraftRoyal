using UnityEngine;

public class MageCombat : MinionCombat
{
    [SerializeField] private float bonusMultiplier = 2f;

    protected override bool AttackInternal()
    {
        if (minion.Target == null) return false;

        PlayVfx(minion.Target);
        PlayVfxClientRpc(minion.Target.NetworkObjectId);

        if ((hitPoint.position - minion.Target.transform.position).magnitude > Owner.Stats.hitRadius) return false;

        var finalDamage = Owner.Stats.damage;
        if (minion.Target.GetComponent<MeleeCombat>() != null)
            finalDamage *= bonusMultiplier;

        return minion.Target.GetHit(finalDamage, minion);
    }

}
