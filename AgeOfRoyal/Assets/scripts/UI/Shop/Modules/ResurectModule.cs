using System.Collections;
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
    public abstract bool VfxLoop => false;
    public override int Use(MinionCombat owner, int maxTargetOverride = -1)
    {
        if (!owner.IsServer) return 0;
        var maxTarget = maxTargetOverride == -1 ? picking.MaxTarget : maxTargetOverride;
        var unitsManager = owner.FindFirstObjectByType<UnitsManager>();
        List<Minion> minions = picking.PickTargets(unitsManager.Deads).Take(maxTarget).ToList(); 


        var nbTouched = 0;
        foreach (var h in minions)
        {
            unitsManager.Resurect(h, healthPercent, maxHealthFlat);
            h.PlayVfx(OnTargetVfx);
            h.PlayModuleOnTargetVfxClientRpc(ID, owner.NetworkObjectId);
            nbTouched++;
        } 
        if(nbTouched > 0){
        owner.Owner.PlayVfx(OnSelfVfx);
        owner.Owner.PlayModuleOnSelfVfxClientRpc(ID, owner.NetworkObjectId);
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
