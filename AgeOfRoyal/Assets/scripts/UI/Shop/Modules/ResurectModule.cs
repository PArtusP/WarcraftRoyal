using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "___ - MOD Resurect", menuName = "Unit Modules/Resurect", order = 3)]
public class ResurectModule : UnitModule
{
    [SerializeField] protected float cooldown = 5f;
    [SerializeField] protected float delay = 30f;
    /// <summary>
    /// Let to 0 to disable maximum health flat bonus.
    /// </summary>
    [SerializeField] protected float maxHealthFlat = 0;
    /// <summary>
    ///  Let to 0 to disable minimum health flat bonus.
    /// </summary>
    [SerializeField] protected float minHealthFlat = 0;
    [SerializeField] protected float healthPercent = 1f;
    [SerializeField] protected TargetPicking picking;

    float lastUsed = 0f;

    public override float Cooldown => cooldown;
    public TargetPicking Picking { get => picking; set => picking = value; }

    public override float Radius => Mathf.Infinity;

    public override string Description => $"Resurect the most expensive a unit every {cooldown}s after {delay}s."; // @TODO ;

    public override float Delay => delay;

    public override int Use(MinionCombat owner, int maxTargetOverride = -1)
    {
        if (!owner.IsServer) return 0;
        var maxTarget = maxTargetOverride == -1 ? picking.MaxTarget : maxTargetOverride;
        var minions = FindTargets(owner).Take(maxTarget).ToList();
        var unitsManager = Object.FindFirstObjectByType<UnitsManager>();


        var nbTouched = 0;
        foreach (var h in minions)
        {
            unitsManager.Resurect(h, healthPercent, maxHealthFlat, minHealthFlat);
            if (OnTargetVfx != null)
            {
                h.PlayVfx(OnTargetVfx);
                h.PlayModuleOnTargetVfxClientRpc(ID, owner.NetworkObjectId, OnTargetVfx.id.ToString());
            }
            nbTouched++;
        }
        if (nbTouched > 0)
        {
            NextUse = Time.time + cooldown;
            if (OnSelfVfx != null)
            {
                owner.Owner.PlayVfx(OnSelfVfx);
                owner.Owner.PlayModuleOnSelfVfxClientRpc(ID, owner.NetworkObjectId, OnSelfVfx.id.ToString());
            };
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

    public override void Init(MinionCombat owner)
    { 
    }

    public override List<Minion> FindTargets(MinionCombat owner)
    { 
        var unitsManager = Object.FindFirstObjectByType<UnitsManager>();
        return picking.PickTargets(unitsManager.Deads.Select(m => m as Minion).Where(m => m != null).ToList(), owner.Owner).ToList();
    }
}
