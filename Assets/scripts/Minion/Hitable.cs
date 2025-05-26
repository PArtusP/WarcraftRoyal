using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

abstract public class Hitable : MonoBehaviour
{
    [Header("Team")]
    protected Base home;

    [Header("Stats")]
    [SerializeField] internal int cost;
    [SerializeField] protected float health = 100f; 

    protected HealthBar healthbar;

    public Base Home { get => home; set => home = value; }
    public UnityEvent OnDieEvent { get; internal set; } = new UnityEvent();

    private void Awake()
    {
        healthbar = GetComponentInChildren<HealthBar>();
        healthbar.SetMaxHealth(health);
        healthbar.SetHealth(health);
        AwakeInternal();
    }

    abstract protected void AwakeInternal();

    virtual public bool GetHit(float damage, Hitable opponent)
    {
        health = Mathf.Max(0f, health - damage);
        healthbar.SetHealth(health);
        if (health == 0f)
        {
            Die(); 
            return true;
        }
        return false;
    } 

    virtual public void Die()
    {
        Destroy(gameObject);
        OnDieEvent.Invoke();
    }
}
