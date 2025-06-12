using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
public class SpellMage : Minion
{
    protected override void SetUpConditionInternal(List<AttackConditions<MinionState>> conditons)
    {
        conditons.Clear();

        var modAction = (Actions[1] as ModulesAction);
        var modules = modAction.Modules.Select(m => m as AoeUnitModule);
        conditons.Add(new AttackConditions<MinionState>
        {
            action = Actions[1],
            NextStage = InCombat,
            Condition = new AttackCondition<MinionState>
            {
                Check = (owner, target) =>
                {
                    List<Minion> minions = new List<Minion>();
                    foreach (var item in modules) 
                        minions.AddRange(item.FindTargets(Combat));
                    Debug.Log("Check: " + (minions.Count > 0));
                    var res = minions.Count > 0; // Check if there are any targets in range 

                    return res;
                },
                outRadius = modAction.MinRadius,
                inRadius = 0f,
                cooldown = modules.Min(m => m.Cooldown),
            }
        });
        conditons.Add(new AttackConditions<MinionState>
        {
            action = Actions[0],
            NextStage = InCombat,
            Condition = new AttackCondition<MinionState>
            {
                outRadius = Stats.hitRadius,
                cooldown = Stats.cooldown,
            }
        });
        base.SetUpConditionInternal(conditons);
    }
}
