using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;
public enum KnightState
{
    Walk,
    Follow,
    Combat,
    Charge,
    Stop
}
public class Knight : UnitBase<KnightState>
{
    public override bool IsStopped => fsm.CurrentState == KnightState.Stop;

    public override KnightState Stop => KnightState.Stop;

    public override KnightState Walk => KnightState.Walk;

    public override KnightState Follow => KnightState.Follow;

    public override KnightState InCombat => KnightState.Combat; 
    protected override void SetUpConditionInternal(List<AttackConditions<KnightState>> conditons)
    {
        conditons.Clear();
        var startTime = Time.time;

        var modAction = (Actions[1] as ChargeAttack);
        conditons.Add(new AttackConditions<KnightState>
        {
            action = Actions[1],
            NextStage = KnightState.Charge,
            Condition = new AttackCondition<KnightState>
            {
                Check = (owner, target) =>
                {
                    return startTime + modAction.Cooldown < Time.time;
                },
                outRadius = Actions[1].MaxRadius,
                inRadius = Actions[1].MinRadius,
                cooldown = modAction.Cooldown
            }
        });
        conditons.Add(new AttackConditions<KnightState>
        {
            action = Actions[0],
            NextStage = InCombat,
            Condition = new AttackCondition<KnightState>
            {
                outRadius = Stats.hitRadius,
                cooldown = Stats.cooldown,
            }
        }); 
    }

    protected override void SetUpFSM()
    {
        base.SetUpFSM();
        fsm.states.Add(new State<KnightState>(
            KnightState.Charge,
            null,
            null,
            () => validContition.action.Use(this)
        ));
    } 
}
