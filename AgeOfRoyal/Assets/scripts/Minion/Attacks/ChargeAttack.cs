using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "___ - Charge attack", menuName = "Unit Actions/Unit Attacks/Charge Attack", order = 5)]
public class ChargeAttack : UnitAttack
{
    [SerializeField] private TargetPicking targetPicking;
    [SerializeField] private List<UnitModule> onHitModules = new List<UnitModule>(); 
    [SerializeField] private float chargeSpeed = 12f;
    [SerializeField] private float chargeAcceleration = 2f;
    [SerializeField] private float damage = 20f;
    [SerializeField] private float stopDistance = 1.5f;
    [SerializeField] private float minRadius = 5f;

    override public string AnimationTrigger => "Charge";

    override public float MinRadius  => minRadius;

    public List<UnitModule> OnHitModules => onHitModules; 

    public override bool Use(UnitWithoutState owner)
    {
        var target = targetPicking.PickTargets(radius, owner).FirstOrDefault();
        if (target == null || target.Dead)
            return false;

        owner.SetTarget(target);

        Transform targetTransform = owner.Target.transform;

        owner.Controller.StartCharge(targetTransform, chargeSpeed, chargeAcceleration, stopDistance, () =>
        {
            // Play impact animation
            owner.PlayAnimation("ChargeImpact");

            // Deal damage
            owner.Target.GetHit(damage, owner); 

            // Execute on-hit modules (e.g., dispel)
            foreach (var module in onHitModules)
            {
                module.UseOnTarget(owner.Combat, new List<UnitWithoutState>() { target });
            }
        });
        return true;
    }
}
