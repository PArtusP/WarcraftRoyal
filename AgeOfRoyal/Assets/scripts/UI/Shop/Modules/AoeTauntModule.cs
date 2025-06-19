using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "___ - MOD AOE Taunt", menuName = "Unit Modules/AOE Taunt", order = 4)]
public class AoeTauntModule : AoeUnitModule
{
    [SerializeField] bool vfxLoop;

    public override string Description => $"{(picking.SameTeam == Target.Friends ? "Allies" : picking.SameTeam == Target.Foes ? "Foes" : "Units")} within {radius}m target this unit";

    public override bool VfxLoop => vfxLoop;

    public override void Init(MinionCombat owner) {    }

    override protected void ApplyEffectInternal(UnitWithoutState target, MinionCombat owner) => target.SetTarget(owner.Owner); 

    protected override List<UnitWithoutState> PreApplyChecks(List<UnitWithoutState> minions, MinionCombat owner)
    {
        minions.RemoveAll(m => m.Target == owner.Owner && m.Target is UnitWithoutState mi && !mi.Taunting);
        return minions;
    }
}
