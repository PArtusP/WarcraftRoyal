
using System;
using System.Linq;
using UnityEngine;

public enum HitableEffectType
{
    Burning,
    Freeze,
    Poisoned
}
[Serializable]
public class HitableEffectDamage
{
    public float value;
    public HitableEffectType type;
}
[Serializable]
abstract public class HitableEffectStatus
{
    public float value;
    public float resistance = 0f;
    public float Value { get => value; set => this.value = value; }
    abstract public float Cooldown { get; set; }
    abstract public HitableEffectType Type { get; }

    virtual internal void Add(float v) => value = Mathf.Min(1f, value + v);
    abstract internal void Apply(Hitable hitable);
}
[Serializable]
abstract public class TiltDamageStatus : HitableEffectStatus
{
    protected float nextHit = 0f;
    abstract public float TiltFrequency { get; }

    abstract public float Damage { get; }

    internal override void Apply(Hitable hitable)
    {
        if (Value == 0f) return;
        var statut = hitable.Status.effectStatuses.First(s => s.Type == Type);
        if (nextHit <= Time.time)
        {
            hitable.GetHit(Damage * (1f - statut.resistance), null);
            ApplyInternal(hitable);
            nextHit = Time.time + TiltFrequency;
        }
        Value = Mathf.Max(0f, Value - Cooldown * Time.deltaTime);
    }

    abstract protected void ApplyInternal(Hitable hitable);
}
[Serializable]
public class BurningStatus : TiltDamageStatus
{
    float checkRadius = 1.5f;
    public float cooldown = .08f;
    public override HitableEffectType Type => HitableEffectType.Burning;

    public override float Cooldown { get => cooldown; set => cooldown = value; }
    public override float TiltFrequency => .3f;

    public override float Damage => 1.2f;

    override protected void ApplyInternal(Hitable hitable)
    {
        var hits = Physics.SphereCastAll(hitable.transform.position, checkRadius, hitable.transform.forward, checkRadius, hitable.gameObject.layer);

        hits.ToList().ForEach(h =>
        {
            if (h.rigidbody)
            {
                var victim = h.rigidbody.GetComponent<Hitable>();
                if (victim != null && victim != hitable)
                    victim.AddEffect(new HitableEffectDamage() { type = HitableEffectType.Burning, value = Value });
            }
        });
    }

}

[Serializable]
public class PoisonedStatus : TiltDamageStatus
{
    public int nbStack = 0;
    public float cooldown = .11f;
    public override float TiltFrequency => .5f;
    public override float Damage => .6f;
    public override HitableEffectType Type => HitableEffectType.Poisoned;

    public override float Cooldown { get => cooldown; set => cooldown = value; }

    internal override void Apply(Hitable hitable)
    {
        if (nbStack == 0 && Value == 0f) return;
        var statut = hitable.Status.effectStatuses.First(s => s.Type == Type);
        if (nextHit <= Time.time)
        {
            var ticks = nbStack;
            while (ticks > 0)
            {
                hitable.GetHit(Damage * (1f - statut.resistance), null);
                ApplyInternal(hitable);
                ticks--;
            }
            nextHit = Time.time + TiltFrequency;
        }

        Value = Mathf.Max(0f, Value - Cooldown * Time.deltaTime);
        if (Value > 0f) return;
        if (nbStack > 1) Value = 1f;
        nbStack--;

    }
    protected override void ApplyInternal(Hitable hitable) { }
    internal override void Add(float v)
    {
        value = Mathf.Min(1f, value + v);
        nbStack++;
    }
}
[Serializable]
public class FreezeStatus : HitableEffectStatus
{
    public float cooldown = .5f;

    float endFroze = 0f;
    float frozenTime = 2f;
    private Hitable owner;

    public override HitableEffectType Type => HitableEffectType.Freeze;

    public override float Cooldown { get => cooldown; set => cooldown = value; }
    internal override void Apply(Hitable hitable)
    {
        this.owner = hitable;
        if (Value == 0f) return;
        if (Value == 1f && endFroze > Time.time) return;

        Value = Mathf.Max(0f, Value - Cooldown * Time.deltaTime);
        SetSpeed(1f - Value);
    }

    private void SetSpeed(float value, float frozenTime = -1f)
    {
        if (frozenTime == -1f) frozenTime = cooldown;
        var v = Mathf.Lerp(.2f, .8f, value);
        owner.ReduceMaxSpeedTemporary(value == 0f ? 0f : v, frozenTime);  
        owner.SetAnimatorSpeedTemporary(value == 0f ? 0f : v, frozenTime);
    }

    internal override void Add(float v)
    {
        base.Add(v);
        if (Value == 1f)
        {
            endFroze = Time.time + frozenTime;
            SetSpeed(0f, frozenTime);
        }
    }
}