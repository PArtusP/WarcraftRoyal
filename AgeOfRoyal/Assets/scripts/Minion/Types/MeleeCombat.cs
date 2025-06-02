using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeCombat : MinionCombat
{
    [SerializeField] private float bonusMultiplier = 2f;
    protected override bool AttackInternal()
    {
        if (minion.Target == null) return false;

        if ((hitPoint.position - minion.Target.transform.position).magnitude > Owner.Stats.hitRadius) return false;

        var finalDamage = Owner.Stats.damage;
        if (minion.Target.GetComponent<ArcherCombat>() != null)
            finalDamage *= bonusMultiplier;

        return minion.Target.GetHit(finalDamage, minion);
    }
}

