using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "___ - MOD AOE Buff", menuName = "Unit Modules/AOE Buff", order = 1)]
public class AoeBuffModule : AoeUnitModule
{
    [SerializeField] private UnitBuff buff = new UnitBuff();
    [SerializeField] private List<Minion> targets = new List<Minion>();

    public override string Description =>
        $"{(picking.SameTeam == Target.Friends ? "Grants" : "Inflicts")} " +
        $"{buff.PowerUp.Short}" +
        $"{(buff.Heal > 0 ? $"{(buff.PowerUp.Short != string.Empty ? " & " : string.Empty)}{buff.Heal}HP/s" : string.Empty)}" +
        $"{(buff.Dispel ? $"{(buff.PowerUp.Short != string.Empty || buff.Heal > 0 ? " & " : string.Empty)}dispel" : string.Empty)} " +
        $"to {picking.MaxTarget} {(picking.SameTeam == Target.Friends ? "allies" : picking.SameTeam == Target.Foes ? "foes" : "unit")} " +
        $"within {radius}m";

    public string Short =>
        $"{(picking.SameTeam == Target.Friends ? "Grants" : "Inflicts")} " +
        $"{buff.PowerUp.Short}" +
        $"{(buff.Heal > 0 ? $"{(buff.PowerUp.Short != string.Empty ? " & " : string.Empty)}{buff.Heal}HP/s" : string.Empty)}" +
        $"{(buff.Dispel ? $"{(buff.PowerUp.Short != string.Empty || buff.Heal > 0 ? " & " : string.Empty)}dispel" : string.Empty)}";

    public override bool VfxLoop => buff.BuffType == UnitBuffType.Aura;

    public override void Init(MinionCombat owner)
    {
        buff = buff.Clone();
        buff.Source = owner.Owner;
    }

    override protected void ApplyEffectInternal(Minion target, MinionCombat owner)
    {
        if (buff.Source == null) buff.Source = owner.Owner;
        if (!targets.Contains(target))
        {
            Debug.Log($"Applying aoe buff: '{Short}' to {target.Name} by {owner.Owner.Name}");
            targets.Add(target);
            if (buff.BuffType != UnitBuffType.Aura)
                owner.StartCoroutine(RemoveBlacklist(target));
            target.AddBuff(buff, buff.Duration, owner.Owner);
        }
    }

    public IEnumerator RemoveBlacklist(Minion victim)
    {
        yield return buff.BuffType == UnitBuffType.Temporary 
            || buff.BuffType == UnitBuffType.Stackable 
            || buff.BuffType == UnitBuffType.Refreshable ?
            new WaitForEndOfFrame()
            : new WaitForSeconds(buff.Duration); //maybe not for OneShot and permanent ?

        if (targets.Contains(victim))
            targets.Remove(victim);
    }

    protected override List<Minion> PreApplyChecks(List<Minion> minions, MinionCombat owner)
    {
        targets.RemoveAll(t => t == null);
        switch (buff.BuffType)
        {
            case UnitBuffType.Aura:
                var notInRangeAnymore = targets.Where(t => !minions.Contains(t)).ToList();
                notInRangeAnymore.ForEach(t =>
                {
                    owner.Owner.PlayVfx(OnTargetVfx, false);
                    owner.Owner.PlayModuleOnTargetVfxClientRpc(ID, owner.NetworkObjectId, false);
                    t.RemoveBuff(buff);
                }); // Remove targets not in the current minions list
                notInRangeAnymore.ForEach(b => targets.Remove(b)); 
                break; 
                case UnitBuffType.Temporary:
                case UnitBuffType.Refreshable:
                minions.RemoveAll(m => m.Buffs.Count(b => b.SourceId == buff.SourceId) >= buff.MaxStack); // Remove already buffed targets
                break;
                case UnitBuffType.Stackable:
                minions.RemoveAll(m => m.Buffs.Count(b => b.SourceId == buff.SourceId && b.Source == buff.Source) >= buff.MaxStack); // Remove already buffed targets
                break;
        }

        return minions;
    }

}
