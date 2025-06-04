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

    [SerializeField] protected NetworkVariable<float> health { get; } = new NetworkVariable<float>(
            0f, 
            NetworkVariableReadPermission.Everyone, 
            NetworkVariableWritePermission.Server);
    protected abstract float MaxHealth { get; set; }
    public float Health { get => health.Value; set => health.Value = value; }
    public float HealthPercent => health.Value / MaxHealth;
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

    virtual public bool GetHit(float damage, Hitable opponent)
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
            if (healVfxEnd < Time.time + .2f) healVfxEnd = Time.time + .2f;
                return;
        }
        healVfxEnd = Time.time + .2f;
        StartCoroutine(WaitToEndHealEffect());
        healEffect.PlayBase(true, this, false, null, null, transform.position, Quaternion.identity);
    }

    private IEnumerator WaitToEndHealEffect()
    {
        while (healVfxEnd > Time.time)
            yield return new WaitForEndOfFrame();
        healEffect.PlayBase(false, this, false, null, null, transform.position, Quaternion.identity);
    }

    virtual public void Die()
    {
        OnDieEvent.Invoke();
        Destroy(gameObject);
    }

}
