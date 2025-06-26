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
    [SerializeField] protected float delay = 10f; 
    [SerializeField] protected List<UnitWithoutState> spawnList = new List<UnitWithoutState>(); 

    float lastUsed = 0f;

    public override float Cooldown => cooldown; 

    public override float Radius => Mathf.Infinity;

    public override string Description => $"Resurect the most expensive a unit every {cooldown}s after {delay}s."; // @TODO ;

    public override float Delay => delay;

    public override int Use(MinionCombat owner, int maxTargetOverride = -1)
    {
        if (!owner.IsServer) return 0;
        return UseOnTarget(owner, spawnList);
    }

    public override int UseOnTarget(MinionCombat owner, List<UnitWithoutState> targets)
    { 
        var nbTouched = 0;
        foreach (var h in targets)
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

    public override List<UnitWithoutState> FindTargets(MinionCombat owner)
    { 
        var unitsManager = Object.FindFirstObjectByType<UnitsManager>();
        return picking.PickTargets(unitsManager.Deads.Select(m => m as UnitWithoutState).Where(m => m != null).ToList(), owner.Owner).ToList();
    }
}
