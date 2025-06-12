using System.Collections.Generic;
using System.Linq;
using UnityEngine;
 
[CreateAssetMenu(fileName = "___ - Module Action", menuName = "Unit Actions/Module Action", order = 2)]
public class ModulesAction : UnitAction
{
    [SerializeField] protected string animationTrigger = "Spell";
    [SerializeField] protected List<UnitModule> modules;
    [SerializeField] protected int maxTargetTotal = 1; 
    override public string AnimationTrigger => animationTrigger;

    public List<UnitModule> Modules { get => modules; set => modules = value; }

    public override float MaxRadius => modules.Max(m => m.Radius);
    public override float MinRadius => modules.Min(m => m.Radius);

    public override bool Use(UnitWithoutState owner)
    {
        var targetCount = 0;

        foreach (var module in modules)
        {
            if (targetCount >= maxTargetTotal) break;
            targetCount += module.Use(owner.Combat, maxTargetTotal - targetCount);
        }
        return targetCount > 0; // Return true if at least one module was used
    }    public override UnitAction Clone()
    {
        var clone = Instantiate(this);
        clone.Modules = new List<UnitModule>(this.modules.Select(m => m.Clone())); // Or deep clone if needed
        return clone;
    }
}