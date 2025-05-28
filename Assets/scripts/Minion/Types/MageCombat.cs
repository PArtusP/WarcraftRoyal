using System.Collections.Generic;
using UnityEngine;

public class MageCombat : MinionCombat
{
    [SerializeField] private float bonusMultiplier = 2f;
    [SerializeField] private ProjectileMove vfx;

    protected override void AttackInternal()
    { 
        if (minion.Target == null) return;

        var fx = Instantiate(vfx, hitPoint.transform.position, hitPoint.transform.rotation, null);
        fx.Target = minion.Target.aimPoint;

        if ((hitPoint.position - minion.Target.transform.position).magnitude > Owner.Stats.hitRadius) return;

        var finalDamage = Owner.Stats.damage;
        if (minion.Target.GetComponent<MeleeCombat>() != null)
            finalDamage *= bonusMultiplier;

        if (minion.Target.GetHit(finalDamage, minion))
            minion.Target = null;
    }
}
