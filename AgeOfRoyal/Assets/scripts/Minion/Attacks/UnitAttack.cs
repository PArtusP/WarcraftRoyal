using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
abstract public class UnitAction : ScriptableObject
{
    abstract public string AnimationTrigger { get; }
    abstract public bool Use(UnitWithoutState owner);
}


[System.Serializable]
abstract public class UnitAttack : UnitAction
{
    [SerializeField] protected float bonusMultiplier = 2f;
    [SerializeField] protected Class bonusAgainst;

    override public string AnimationTrigger => "Attack";  
}

[System.Serializable]
public class ModulesAction : UnitAction
{
    [SerializeField] protected string animationTrigger = "ModuleAction";
    [SerializeField] protected List<UnitModule> modules;
    [SerializeField] protected int maxTargetTotal = 1;

    override public string AnimationTrigger => animationTrigger;

    public override bool Use(UnitWithoutState owner)
    {
        var targetCount = 0;

        foreach (var module in modules)
        {
            if (targetCount >= maxTargetTotal) break;
            targetCount += module.Use(owner.Combat, maxTargetTotal - targetCount);
        }
        return targetCount > 0; // Return true if at least one module was used
    }
}
