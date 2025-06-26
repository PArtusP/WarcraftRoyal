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
    private Sprite icon;

    [SerializeField] TriggerSVFX onTargetVfx;
    [SerializeField] TriggerSVFX onSelfVfx;
    public TriggerSVFX OnTargetVfx => onTargetVfx;
    public TriggerSVFX OnSelfVfx => onSelfVfx;
    abstract public float Radius { get; }
    abstract public float Cooldown { get; }
    abstract public float Delay { get; }
    abstract public string Description { get; }
    public float NextUse { get; protected set; }
    public Sprite Icon => icon;

    abstract public void Init(MinionCombat owner);
    /// <summary>
    /// Return the number of targets on which the module has been applied
    /// </summary>
    /// <param name="owner"></param>
    /// <param name="maxTargetOverride"></param>
    /// <returns></returns>
    abstract public int Use(MinionCombat owner, int maxTargetOverride = -1);
    abstract public int UseOnTarget(MinionCombat owner, List<UnitWithoutState> targets);

    public virtual UnitModule Clone()
    {
        var clone = Instantiate(this);
        clone.ID = this.ID;
        return clone;
    }
    abstract public List<UnitWithoutState> FindTargets(MinionCombat owner);

}


[System.Serializable]
abstract public class AoeUnitModule : UnitModule
{
    [SerializeField] protected float cooldown = 5f;
    [SerializeField] private float delay = 0f;
    [SerializeField] protected float radius = 8f;
    [SerializeField] protected TargetPicking picking;

    float lastUsed = 0f;

    public override float Cooldown => cooldown;
    public override float Delay => delay;

    override public float Radius => radius;
    public TargetPicking Picking { get => picking; set => picking = value; }
    public abstract bool VfxLoop { get; }
    public override int Use(MinionCombat owner, int maxTargetOverride = -1)
    {
        if (!owner.IsServer || NextUse > Time.time) return 0;
        var maxTarget = maxTargetOverride == -1 ? picking.MaxTarget : maxTargetOverride;
        List<UnitWithoutState> minions;
        minions = FindTargets(owner).Take(maxTarget).ToList();
        return UseOnTarget(owner, minions);
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
        DrawCircle(owner.transform.position, radius, 12, nbTouched > 0 ? Color.green : Color.red);
        return nbTouched;
    }

    protected abstract List<UnitWithoutState> PreApplyChecks(List<UnitWithoutState> minions, MinionCombat owner);

    override public List<UnitWithoutState> FindTargets(MinionCombat owner)
    {
        List<UnitWithoutState> minions;
        var cols = Physics.OverlapSphere(owner.HitPoint.position, radius, GameLayers.Hitable.Mask);
        minions = picking.PickTargets(cols
            .Where(col => col.GetComponent<UnitWithoutState>() != null && col.GetComponent<UnitWithoutState>() != owner.Owner)
            .Select(col => col.GetComponent<UnitWithoutState>()).ToList(), owner.Owner); // @TODO on self to change here 
        return PreApplyChecks(minions, owner);
    }

    protected void ApplyEffect(UnitWithoutState target, MinionCombat owner)
    {
        if (!VfxLoop || !OnTargetVfx.Playing)
        {
            target.PlayVfx(OnTargetVfx);
            target.PlayModuleOnTargetVfxClientRpc(ID, owner.NetworkObjectId, OnTargetVfx.id.ToString());
        }
        ApplyEffectInternal(target, owner);
    }
    protected abstract void ApplyEffectInternal(UnitWithoutState target, MinionCombat owner);

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