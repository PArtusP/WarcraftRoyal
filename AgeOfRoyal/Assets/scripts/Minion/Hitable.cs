using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

abstract public class Hitable : NetworkBehaviour
{
    protected Base home;
    protected HealthBar healthbar;
    public Transform aimPoint;

    public TriggerSVFX healEffect;
    private float healVfxEnd = 0;
    private Coroutine healCoroutine;

    [SerializeField]
    protected NetworkVariable<float> health { get; } = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
    public abstract float MaxHealth { get; set; }
    public float Health { get => health.Value; set => health.Value = value; }
    public float HealthPercent => health.Value / MaxHealth;
    public bool Dead => Health == 0f;
    
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
        Health = Mathf.Max(0f, Health - damage);
        if (Health == 0f)
        {
            Die();
            return true;
        }
        return false;
    }
    internal void Heal(float v)
    {
        Health = Mathf.Min(Health + v, MaxHealth);
        if (healVfxEnd > Time.time)
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
        }
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
}
