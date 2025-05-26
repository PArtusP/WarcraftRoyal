using System.Collections.Generic;
using UnityEngine;

public class ArcherCombat : MinionCombat
{
    [SerializeField] private float bonusMultiplier = 2f;

    protected override void AttackInternal()
    {
        if (minion.Target == null) return;

        if ((hitPoint.position - minion.Target.transform.position).magnitude > Owner.Stats.hitRadius) return;

        var finalDamage = Owner.Stats.damage;
        if (minion.Target.GetComponent<MageCombat>() != null)
            finalDamage *= bonusMultiplier;

        if (minion.Target.GetHit(finalDamage, minion))
            minion.Target = null;
    }
}
