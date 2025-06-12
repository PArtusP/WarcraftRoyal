using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "___ - Range Piercing Attack", menuName = "Unit Actions/Unit Attacks/Range Piercing Attack", order = 4)]
public class RangePiercingAttack : UnitAttack
{
    [SerializeField] protected float touchUnitMultiplier = 0.66f;
    [SerializeField] protected float detectionRadius = .5f;

    public override bool Use(UnitWithoutState owner)
    {
        if (owner.Target == null) return false;


        Vector3 origin = owner.transform.position + owner.Target.aimPoint.position.y * Vector3.up;
        Vector3 direction = (owner.Target.aimPoint.position - origin).normalized;
        float range = owner.Stats.hitRadius;

        Debug.DrawLine(origin, origin + direction * range, Color.magenta, 1f);
        RaycastHit[] hits = Physics.SphereCastAll(origin, detectionRadius, direction, range - detectionRadius, owner.HitableLayer);

        float damage = owner.Stats.damage;
        float totalDamage = 0f;
        HashSet<Hitable> damaged = new HashSet<Hitable>();

        foreach (var hit in hits)
        {
            var hitable = hit.collider.GetComponent<Hitable>();
            if (hitable != null && hitable != owner && hitable.Health > 0 && hitable.Home != owner.Home)
            {
                // Prevent double-damaging the same unit if hit multiple colliders
                if (damaged.Contains(hitable)) continue;
                damaged.Add(hitable);

                float finalDamage = damage;
                if (hitable is Minion m && m.Type == bonusAgainst)
                    finalDamage *= bonusMultiplier;

                totalDamage += finalDamage;
                hitable.GetHit(finalDamage, owner);
                damage *= touchUnitMultiplier;
            }
        }

        var vfxTarget = owner.Target;
        var last = damaged.OrderBy(m => (owner.transform.position - m.transform.position).magnitude).FirstOrDefault();
        if (last)
            vfxTarget = last; 

        // Play VFX
        owner.Combat.PlayShootVfx(vfxTarget);
        owner.Combat.PlayShootVfxClientRpc(vfxTarget.NetworkObjectId);

        return totalDamage > 0;
    }
}
