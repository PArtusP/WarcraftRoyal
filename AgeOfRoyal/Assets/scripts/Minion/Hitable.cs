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

    [SerializeField] protected NetworkVariable<float> health { get; } = new NetworkVariable<float>(
            0f, 
            NetworkVariableReadPermission.Everyone, 
            NetworkVariableWritePermission.Server);
    public float Health { get => health.Value; set => health.Value = value; } 
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

    virtual public void Die()
    {
        OnDieEvent.Invoke();
        Destroy(gameObject);
    }
}
