using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class HitableStatus
{ 
    public List<HitableEffectStatus> effectStatuses = new List<HitableEffectStatus>()
    {
        new BurningStatus(),
        new FreezeStatus(),
        new PoisonedStatus(),
    };
}

abstract public class Hitable : NetworkBehaviour
{
    [SerializeField]
    protected Base home;
    protected HealthBar healthbar;
    [SerializeField] protected List<Renderer> meshRenderers;
    public Transform aimPoint;

    public TriggerSVFX healEffect;
    private float healVfxEnd = 0;
    private Coroutine healCoroutine;
    private HitableStatus status = new HitableStatus();

    [SerializeField]
    protected NetworkVariable<float> health { get; } = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
    public abstract float MaxHealth { get; set; }
    public float Health { get => health.Value; set => health.Value = value; }
    public float HealthPercent => health.Value / MaxHealth;
    public bool Dead => Health == 0f;
    public HitableStatus Status { get => status; }

    public Base Home { get => home; set => home = value; }
    public UnityEvent OnDieEvent { get; internal set; } = new UnityEvent();

    private void Awake()
    {
        healthbar = GetComponentInChildren<HealthBar>();
        health.OnValueChanged += UpdateHealthBar;
        AwakeInternal();
    }

    private void UpdateHealthBar(float previousValue, float newValue)
        => healthbar.SetHealth(newValue);

    abstract protected void AwakeInternal();

    virtual public bool GetHit(float damage, Hitable opponent) // @TODO called from client ?
    {
        if (Health == 0) return false;
        Health = Mathf.Max(0f, Health - damage);
        if (Health == 0f)
        {
            Die();
            return true;
        }
        return false;
    }
    internal virtual void Heal(float v)
    {
        Health = Mathf.Min(Health + v, MaxHealth);
        /*if (healVfxEnd > Time.time)
        {
            if (healVfxEnd < Time.time + .1f) healVfxEnd = Time.time + .1f;
            return;
        }
        healVfxEnd = Time.time + .1f;
        if (healCoroutine == null)
        {
            PlayHealLoopVfx(true);
            healCoroutine = StartCoroutine(WaitToEndHealEffect());

            PlayHealLoopVfxClientRpc(true);
        }*/
    }
    private void PlayHealLoopVfx(bool play) => healEffect.PlayBase(play, this, healEffect.id);

    [ClientRpc]
    private void PlayHealLoopVfxClientRpc(bool play) => PlayHealLoopVfx(play);

    private IEnumerator WaitToEndHealEffect()
    {
        while (healVfxEnd > Time.time)
            yield return new WaitForEndOfFrame();

        PlayHealLoopVfx(false);
        healCoroutine = null;
        PlayHealLoopVfxClientRpc(false);
    }

    virtual public void Die()
    {
        OnDieEvent.Invoke();
    }
    protected void ComputeEffectDamages() => Status.effectStatuses.ForEach(status => status.Apply(this));

    internal void AddEffect(HitableEffectDamage s)
    {
        var statut = Status.effectStatuses.First(h => h.Type == s.type);
        statut.Add(s.value);
        UpdateEffectVisuals();
    }
    private void UpdateEffectVisuals()
    {
        Status.effectStatuses.ForEach(s =>
        {
            switch (s.Type)
            {
                case HitableEffectType.Burning:
                    meshRenderers.ForEach(r => r.material.SetFloat("_Burning", s.Value));
                    break;
                case HitableEffectType.Freeze:
                    meshRenderers.ForEach(r => r.material.SetFloat("_Freeze", s.Value));
                    break;
                case HitableEffectType.Poisoned:
                    meshRenderers.ForEach(r => r.material.SetFloat("_Poisoned", (s as PoisonedStatus).nbStack > 0 ? 1f : s.Value));
                    break;
                default:
                    break;
            }
        });
    }

    internal abstract void ReduceMaxSpeedTemporary(float v, float frozenTime);
    internal abstract void SetAnimatorSpeedTemporary(float v, float frozenTime);
}
