using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "___ - Charge attack", menuName = "Unit Actions/Unit Attacks/Charge Attack", order = 5)]
public class ChargeAttack : UnitAttack
{
    [SerializeField] private TargetPicking targetPicking;
    [SerializeField] private List<UnitModule> onHitModules = new List<UnitModule>();
    [SerializeField] private float chargeSpeed = 12f;
    [SerializeField] private float damage = 20f;
    [SerializeField] private float stopDistance = 1.5f;

    private MinionController controller;
    private MinionCombat combat;

    public override void Initialize(MinionController controller, MinionCombat combat)
    {
        this.controller = controller;
        this.combat = combat;
    }

    public override void Execute()
    {
        Unit target = targetPicking.PickTarget();
        if (target == null || !target.IsAlive)
            return;

        Transform targetTransform = target.transform;

        controller.StartCharge(targetTransform, chargeSpeed, stopDistance, () =>
        {
            // Play impact animation
            controller.Animator.Action("ChargeImpact");
            
            // Deal damage
            combat.DealDamage(target, damage);

            // Execute on-hit modules (e.g., dispel)
            foreach (var module in onHitModules)
            {
                module.Use(target);
            }
        });
    }
}
