using System.Collections.Generic;
using UnityEngine;

public class MageCombat : MinionCombat
{
    [SerializeField] private float bonusMultiplier = 2f;

    protected override void AttackInternal()
    {
        if (minion.Target == null) return;

        if ((hitPoint.position - minion.Target.transform.position).magnitude > hitRadius) return;

        var finalDamage = damage;
        if (minion.Target.GetComponent<MeleeCombat>() != null)
            finalDamage *= bonusMultiplier;

        if (minion.Target.GetHit(finalDamage, minion))
            minion.Target = null;
    }
}
