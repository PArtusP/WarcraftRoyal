using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
public class SpellMage : Minion
{
    protected override void SetUpConditionInternal(List<AttackConditions<MinionState>> conditons)
    { 
        var startTime = Time.time; 
        var modAction = (Actions[1] as ModulesAction);
        var modules = modAction.Modules.Select(m => m as UnitModule);
        AddActionsInternal(new List<UnitAction>() { modAction }); 
        base.SetUpConditionInternal(conditons);
    } 
}
