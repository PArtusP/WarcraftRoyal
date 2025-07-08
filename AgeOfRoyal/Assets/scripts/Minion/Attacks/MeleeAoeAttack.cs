using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "___ - Melee aoe attack", menuName = "Unit Actions/Unit Attacks/Melee Aoe Attack", order = 2)]
public class MeleeAoeAttack : UnitAttack
{
    [SerializeField] protected float aoeRadius = 3f;
    override public bool Use(UnitWithoutState owner)
    {
        if (owner.Target == null) return false;

        if ((owner.Combat.HitPoint.position - owner.Target.transform.position).magnitude > owner.Stats.hitRadius) return false;

        var cols = Physics.OverlapSphere(owner.Combat.HitPoint.position, aoeRadius, owner.HitableLayer);

        var totalDamage = 0f;

        cols.ToList().ForEach(c =>
        {
            var hitable = c.GetComponent<Hitable>();
            if (hitable != null && hitable != owner && hitable.Health > 0 && hitable.Home != owner.Home)
            {
                var finalDamage = owner.Stats.damage;
                if (owner.Target is UnitWithoutState m && m.Type == bonusAgainst)
                    finalDamage *= bonusMultiplier;
                totalDamage += finalDamage;
                if (Statuses.Any()) Statuses.ForEach(s => hitable.AddEffect(s));
                hitable.GetHit(finalDamage, owner);
            }
        });


        return totalDamage > 0;
    }
}
