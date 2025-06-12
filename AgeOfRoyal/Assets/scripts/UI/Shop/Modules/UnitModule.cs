using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static Unity.VisualScripting.Member;
using static UnityEngine.GraphicsBuffer;

[System.Serializable]
abstract public class UnitModule : ScriptableObject
{
    public int ID = -1;
    [SerializeField] TriggerSVFX onTargetVfx;
    [SerializeField] TriggerSVFX onSelfVfx;
    public TriggerSVFX OnTargetVfx => onTargetVfx;
    public TriggerSVFX OnSelfVfx => onSelfVfx;
    abstract public float Radius { get; }
    abstract public string Description { get; }
    abstract public void Init(MinionCombat owner); 
    /// <summary>
    /// Return the number of targets on which the module has been applied
    /// </summary>
    /// <param name="owner"></param>
    /// <param name="maxTargetOverride"></param>
    /// <returns></returns>
    abstract public int Use(MinionCombat owner, int maxTargetOverride = -1); 
    
    public virtual UnitModule Clone()
    {
        return Instantiate(this);
    }
}

public enum Target
{
    Friends,
    Foes,
    All
}

[System.Serializable]
abstract public class AoeUnitModule : UnitModule
{
    [SerializeField] protected float cooldown = 5f;
    [SerializeField] protected float radius = 8f;
    [SerializeField] protected TargetPicking picking;

    float lastUsed = 0f;

    public float Cooldown => cooldown;
    override public float Radius => radius; 
    public TargetPicking Picking { get => picking; set => picking = value; }
    public abstract bool VfxLoop { get; }
    public override int Use(MinionCombat owner, int maxTargetOverride = -1)
    {
        if (!owner.IsServer) return 0;
        var maxTarget = maxTargetOverride == -1 ? picking.MaxTarget : maxTargetOverride;
        List<Minion> minions;
        minions = FindTargets(owner); 

        owner.Owner.PlayVfx(OnSelfVfx);
        owner.Owner.PlayModuleOnSelfVfxClientRpc(ID, owner.NetworkObjectId);

        var nbTouched = 0;
        foreach (var h in minions)
        {
            nbTouched++;
            ApplyEffect(h, owner);
            if (nbTouched >= maxTarget)
                break; // Stop if we reached the max target limit 
        }
        DrawCircle(owner.transform.position, radius, 12, nbTouched > 0 ? Color.green : Color.red);
        return nbTouched;
    }

    protected abstract List<Minion> PreApplyChecks(List<Minion> minions, MinionCombat owner);

    public List<Minion> FindTargets(MinionCombat owner)
    {
        List<Minion> minions;
        var cols = Physics.OverlapSphere(owner.HitPoint.position, radius, GameLayers.Hitable.Mask);
        minions = picking.PickTargets(cols
            .Where(col => col.GetComponent<Minion>() != null && col.GetComponent<Minion>() != owner.Owner)
            .Select(col => col.GetComponent<Minion>()).ToList(), owner.Owner); // @TODO on self to change here 
        return PreApplyChecks(minions, owner);
    }

    protected void ApplyEffect(Minion target, MinionCombat owner)
    {
        if(!VfxLoop || !OnTargetVfx.Playing)
        {
            target.PlayVfx(OnTargetVfx);
            target.PlayModuleOnTargetVfxClientRpc(ID, owner.NetworkObjectId);
        }
        ApplyEffectInternal(target, owner);
    }
    protected abstract void ApplyEffectInternal(Minion target, MinionCombat owner);

    void DrawCircle(Vector3 center, float radius, int segments, Color color)
    {
        float angleStep = 360f / segments;

        for (int i = 0; i < segments; i++)
        {
            float angleCurrent = Mathf.Deg2Rad * (i * angleStep);
            float angleNext = Mathf.Deg2Rad * ((i + 1) % segments * angleStep);

            Vector3 pointCurrent = center + new Vector3(Mathf.Cos(angleCurrent), 0f, Mathf.Sin(angleCurrent)) * radius;
            Vector3 pointNext = center + new Vector3(Mathf.Cos(angleNext), 0f, Mathf.Sin(angleNext)) * radius;

            Debug.DrawLine(pointCurrent, pointNext, color);
        }
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