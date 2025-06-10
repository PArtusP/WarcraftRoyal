using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "___ - MOD AOE Taunt", menuName = "Unit Modules/AOE Taunt", order = 4)]
public class AoeTauntModule : AoeUnitModule
{
    public override string Description => $"{(picking.SameTeam == Target.Friends ? "Allies" : picking.SameTeam == Target.Foes ? "Foes" : "Units")} within {radius}m target this unit";

    override protected void ApplyEffect(Hitable target, MinionCombat owner)
    {
        if (target is Minion m && m.Target != owner.Owner)
            m.SetTarget(owner.Owner);
    }
}
