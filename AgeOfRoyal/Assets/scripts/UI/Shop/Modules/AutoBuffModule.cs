using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[System.Serializable]
[CreateAssetMenu(fileName = "___ - MOD auto buff", menuName = "Unit Modules/Auto Buff", order = 1)]
public class AutoBuffModule : UnitModule
{
    [SerializeField] private float delay = 0;
    [SerializeField] private float cooldown = 2f;
    [SerializeField] private UnitBuff buff = new UnitBuff();

    public override string Description =>
        $"Grants" +
        $"{buff.PowerUp.Short}" +
        $"{(buff.Heal > 0 ? $"{(buff.PowerUp.Short != string.Empty ? " & " : string.Empty)}{buff.Heal}HP/s" : string.Empty)}" +
        $"{(buff.Dispel ? $"{(buff.PowerUp.Short != string.Empty || buff.Heal > 0 ? " & " : string.Empty)}dispel" : string.Empty)} " +
        $"{(buff.HealthLink != 0f ? $"{(buff.PowerUp.Short != string.Empty || buff.Dispel || buff.Heal > 0 ? " & " : string.Empty)}add a {Mathf.RoundToInt(buff.HealthLink * 100f)}% health link" : string.Empty)}" +
        $"to self";

    public string Short =>
        $"Grants" +
        $"{buff.PowerUp.Short}" +
        $"{(buff.Heal > 0 ? $"{(buff.PowerUp.Short != string.Empty ? " & " : string.Empty)}{buff.Heal}HP{(buff.BuffType != UnitBuffType.OneShot ? "/s" : string.Empty)}" : string.Empty)}" +
        $"{(buff.Dispel ? $"{(buff.PowerUp.Short != string.Empty || buff.Heal > 0 ? " & " : string.Empty)}dispel" : string.Empty)}" +
        $"{(buff.HealthLink != 0f ? $"{(buff.PowerUp.Short != string.Empty || buff.Dispel || buff.Heal > 0 ? " & " : string.Empty)}add a {Mathf.RoundToInt(buff.HealthLink * 100f)}% health link" : string.Empty)}";

    public bool VfxLoop => buff.BuffType == UnitBuffType.Aura;

    public override float Radius => Mathf.Infinity;

    public override float Cooldown => cooldown;

    public override float Delay => delay;

    public override List<UnitWithoutState> FindTargets(MinionCombat owner) => new List<UnitWithoutState>() { owner.Owner };

    public override void Init(MinionCombat owner)
    {
        buff = buff.Clone();
        buff.Source = owner.Owner;
    }

    public override int Use(MinionCombat owner, int maxTargetOverride = -1)
    {
        if (!owner.IsServer || NextUse > Time.time) return 0;
        return UseOnTarget(owner, new List<UnitWithoutState>() { owner.Owner });
    }

    public override int UseOnTarget(MinionCombat owner, List<UnitWithoutState> targets)
    {

        var nbTouched = 0;
        foreach (var h in targets)
        {
            nbTouched++;
            ApplyEffect(h, owner);
        }
        if (nbTouched > 0)
        {
            NextUse = Time.time + cooldown;
            owner.Owner.PlayVfx(OnSelfVfx);
            owner.Owner.PlayModuleOnSelfVfxClientRpc(ID, owner.NetworkObjectId, OnSelfVfx.id.ToString());
        }
        return nbTouched;
    }

    protected void ApplyEffect(UnitWithoutState target, MinionCombat owner)
    {
        if (!VfxLoop || !OnTargetVfx.Playing)
        {
            target.PlayVfx(OnTargetVfx);
            target.PlayModuleOnTargetVfxClientRpc(ID, owner.NetworkObjectId, OnTargetVfx.id.ToString());
        }
        if (buff.Source == null) buff.Source = owner.Owner;

        Debug.Log($"Applying aoe buff: '{Short}' to {target.Name}");  
        target.AddBuff(buff.Clone(), buff.Duration, owner.Owner);
    } 
}
