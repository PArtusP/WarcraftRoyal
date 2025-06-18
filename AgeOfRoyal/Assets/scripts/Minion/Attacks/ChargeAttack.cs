using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "___ - Charge attack", menuName = "Unit Actions/Unit Attacks/Charge Attack", order = 5)]
public class ChargeAttack : UnitAttack
{
    [SerializeField] private TargetPicking targetPicking;
    [SerializeField] private List<UnitModule> onHitModules = new List<UnitModule>(); 
    [SerializeField] private float chargeSpeed = 12f;
    [SerializeField] private float damage = 20f;
    [SerializeField] private float stopDistance = 1.5f; 


    public override bool Use(UnitWithoutState owner)
    {
        //var target = targetPicking.PickTargets(radius, owner).FirstOrDefault();
        if (owner.Target == null || owner.Target.Dead)
            return false;

        Transform targetTransform = owner.Target.transform;

        owner.Controller.StartCharge(targetTransform, chargeSpeed, stopDistance, () =>
        {
            // Play impact animation
            owner.PlayAnimation("ChargeImpact");

            // Deal damage
            owner.Target.GetHit(damage, owner); 

            // Execute on-hit modules (e.g., dispel)
            foreach (var module in onHitModules)
            {
                module.UseOnTarget(owner.Combat, new List<Minion>() { owner.Target as Minion});
            }
        });
        return true;
    }
}
