using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

[System.Serializable]
abstract public class ResurectModule : UnitModule
{
    [SerializeField] protected float cooldown = 5f;
    [SerializeField] protected float maxHealthFlat = 0;
    [SerializeField] protected float healthPercent = 1f;
    [SerializeField] protected TargetPicking picking;

    float lastUsed = 0f;

    public float Cooldown => cooldown;
    public TargetPicking Picking { get => picking; set => picking = value; }
    public override int Use(MinionCombat owner, int maxTargetOverride = -1)
    {
        if (!owner.IsServer) return 0;
        var maxTarget = maxTargetOverride == -1 ? picking.MaxTarget : maxTargetOverride;
        var unitsManager = Object.FindFirstObjectByType<UnitsManager>();
        List<Minion> minions = picking.PickTargets(unitsManager.Deads.Select(m => m as Minion).Where(m => m != null).ToList(), owner.Owner).Take(maxTarget).ToList();


        var nbTouched = 0;
        foreach (var h in minions)
        {
            unitsManager.Resurect(h, healthPercent, maxHealthFlat);
            h.PlayVfx(OnTargetVfx);
            h.PlayModuleOnTargetVfxClientRpc(ID, owner.NetworkObjectId, OnTargetVfx.id.ToString());
            nbTouched++;
        }
        if (nbTouched > 0)
        {
            NextUse = Time.time + cooldown;
            owner.Owner.PlayVfx(OnSelfVfx);
            owner.Owner.PlayModuleOnSelfVfxClientRpc(ID, owner.NetworkObjectId, OnSelfVfx.id.ToString());
        }
        return nbTouched;
    }
    public override UnitModule Clone()
    {
        var clone = Instantiate(this);
        clone.ID = this.ID;

        // Optional deep clone if TargetPicking is mutable
        clone.Picking = picking != null ? picking.Clone() : null;

        return clone;
    }
}
