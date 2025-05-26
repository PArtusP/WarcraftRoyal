using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeCombat : MinionCombat
{
    [SerializeField] private float bonusMultiplier = 2f;
    protected override void AttackInternal()
    {
        if (minion.Target == null) return;

        if ((hitPoint.position - minion.Target.transform.position).magnitude > Owner.Stats.hitRadius) return;

        var finalDamage = Owner.Stats.damage;
        if (minion.Target.GetComponent<ArcherCombat>() != null)
            finalDamage *= bonusMultiplier;

        if (minion.Target.GetHit(finalDamage, minion))
            minion.Target = null;
    }
}

