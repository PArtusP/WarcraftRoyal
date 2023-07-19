using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hitable : MonoBehaviour
{
    [Header("Team")]
    protected Base home;

    [Header("Stats")]
    [SerializeField] protected float health = 100f;
    protected float deathBonus = 100f;

    protected HealthBar healthbar;



    public Base Home { get => home; set => home = value; }

    private void Awake()
    {
        healthbar = GetComponentInChildren<HealthBar>();
        healthbar.SetMaxHealth(health);
        healthbar.SetHealth(health);
    }

    virtual public bool GetHit(float damage, Hitable opponent)
    {
        health = Mathf.Max(0f, health - damage);
        healthbar.SetHealth(health);
        if (health == 0f)
        {
            Die();
            opponent.RegisterKill(deathBonus);
            return true;
        }
        return false;
    }

    virtual public void RegisterKill(float deathBonus)
    {
        // do something
    }

    virtual public void Die()
    {
        Destroy(gameObject);
    }
}
