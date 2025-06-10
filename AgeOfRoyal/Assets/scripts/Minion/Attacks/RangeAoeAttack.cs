using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "___ - Range AOE attack", menuName = "Unit Attack/Range AOE Attack", order = 3)]
public class RangeAoeAttack : UnitAttack
{ 
    [SerializeField] protected float radius = 3f;
    override public bool Use(UnitWithoutState owner)
    {
        if (owner.Target == null) return false;
        owner.Combat.PlayShootVfx(owner.Target);
        owner.Combat.PlayShootVfxClientRpc(owner.Target.NetworkObjectId);

        var cols = Physics.OverlapSphere(owner.Target.transform.position, radius, owner.HitableLayer);

        var totalDamage = 0f;

        cols.ToList().ForEach(c =>
        {
            var hitable = c.GetComponent<Hitable>();
            if (hitable != null && hitable != owner && hitable.Health > 0 && hitable.Home != owner.Home)
            {
                var finalDamage = owner.Stats.damage;
                if (owner.Target is Minion m && m.Type == bonusAgainst)
                    finalDamage *= bonusMultiplier;
                totalDamage += finalDamage;
                hitable.GetHit(finalDamage, owner);
            }
        });


        return totalDamage > 0;
    }
}